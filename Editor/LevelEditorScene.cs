using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using static Raylib_cs.Raylib;
using Color = Raylib_cs.Color;

namespace Game.Editor;

public class EditorLayer
{
    public string Name { get; set; } = "";
    public uint[] Tiles { get; set; } = Array.Empty<uint>();
    public bool IsVisible { get; set; } = true;
}

public class LevelEditorScene : IScene
{
    private readonly MapData _mapData;
    private readonly List<EditorLayer> _layers;

    // 2D camera state
    private Vector2 _cameraOffset;
    private float _zoom = 4.5f;
    private const float BaseTileSize = 16f;
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 10.0f;
    private const float ZoomSpeed = 0.1f;

    // Panning state
    private bool _isDragging;
    private Vector2 _lastMousePos;

    // Cursor info panel
    private bool _cursorInfoFollowsMouse = true;

    // Tile painting
    private int _activeLayerIndex = 0;
    private uint _selectedTileId = 1; // 0 = eraser, 1+ = tile IDs

    // Pre-rendered rotated door texture for palette display
    private RenderTexture2D _rotatedDoorTexture;

    // File dialog state
    private bool _showSaveDialog;
    private bool _showLoadJsonDialog;
    private bool _showLoadTmxDialog;
    private string _savePath = "resources/level.json";
    private string _loadJsonPath = "resources/level.json";
    private string _loadTmxPath = "resources/map1.tmx";
    private string _statusMessage = "";
    private float _statusTimer;

    public LevelEditorScene(MapData mapData)
    {
        _mapData = mapData;

        // Center the map on screen initially (account for zoom)
        float mapPixelWidth = mapData.Width * BaseTileSize * _zoom;
        float mapPixelHeight = mapData.Height * BaseTileSize * _zoom;
        _cameraOffset = new Vector2(
            (GetScreenWidth() - mapPixelWidth) / 2f,
            (GetScreenHeight() - mapPixelHeight) / 2f
        );

        // Create layers in default render order (bottom to top)
        _layers = new List<EditorLayer>
        {
            new() { Name = "Floor", Tiles = mapData.Floor },
            new() { Name = "Walls", Tiles = mapData.Walls },
            new() { Name = "Ceiling", Tiles = mapData.Ceiling },
            new() { Name = "Doors", Tiles = mapData.Doors },
        };

        // Pre-render the door texture rotated 90 degrees for the tile palette (vertical door, ID 8)
        if (mapData.Textures.Count > 6)
        {
            var doorTex = mapData.Textures[6]; // door.png
            _rotatedDoorTexture = LoadRenderTexture(doorTex.Width, doorTex.Height);
            BeginTextureMode(_rotatedDoorTexture);
            ClearBackground(Color.Blank);
            DrawTexturePro(
                doorTex,
                new Rectangle(0, 0, doorTex.Width, doorTex.Height),
                new Rectangle(doorTex.Width / 2f, doorTex.Height / 2f, doorTex.Width, doorTex.Height),
                new Vector2(doorTex.Width / 2f, doorTex.Height / 2f),
                90f,
                Color.White
            );
            EndTextureMode();
        }
    }

    public void OnEnter()
    {
        ShowCursor();
    }

    public void OnExit()
    {
    }

    public void Update(float deltaTime)
    {
        // Toggle cursor info follow mode
        if (IsKeyPressed(KeyboardKey.C))
        {
            _cursorInfoFollowsMouse = !_cursorInfoFollowsMouse;
        }

        // Status message timer
        if (_statusTimer > 0)
        {
            _statusTimer -= deltaTime;
        }

        // Don't handle map input when ImGui wants the mouse
        bool imGuiWantsMouse = ImGui.GetIO().WantCaptureMouse;

        // Pan with right mouse button drag
        if (!imGuiWantsMouse && IsMouseButtonDown(MouseButton.Right))
        {
            var mousePos = GetMousePosition();
            if (_isDragging)
            {
                var delta = mousePos - _lastMousePos;
                _cameraOffset += delta;
            }
            _isDragging = true;
            _lastMousePos = mousePos;
        }
        else
        {
            _isDragging = false;
        }

        // Pan with WASD keys
        if (!ImGui.GetIO().WantCaptureKeyboard)
        {
            float panSpeed = 500f * deltaTime;
            if (IsKeyDown(KeyboardKey.W)) _cameraOffset.Y += panSpeed;
            if (IsKeyDown(KeyboardKey.S)) _cameraOffset.Y -= panSpeed;
            if (IsKeyDown(KeyboardKey.A)) _cameraOffset.X += panSpeed;
            if (IsKeyDown(KeyboardKey.D)) _cameraOffset.X -= panSpeed;
        }

        // Paint tiles with left mouse button (click or drag)
        if (!imGuiWantsMouse && IsMouseButtonDown(MouseButton.Left))
        {
            var paintPos = ScreenToWorld(GetMousePosition());
            int px = (int)MathF.Floor(paintPos.X);
            int py = (int)MathF.Floor(paintPos.Y);
            if (px >= 0 && px < _mapData.Width && py >= 0 && py < _mapData.Height)
            {
                var activeLayer = _layers[_activeLayerIndex];
                int index = _mapData.Width * py + px;
                activeLayer.Tiles[index] = _selectedTileId;
            }
        }

        // Zoom with scroll wheel (toward cursor) or +/- keys (toward center)
        float zoomDelta = 0f;
        Vector2 zoomAnchor = new Vector2(GetScreenWidth() / 2f, GetScreenHeight() / 2f);

        if (!imGuiWantsMouse)
        {
            float wheel = GetMouseWheelMove();
            if (Math.Abs(wheel) > 0.001f)
            {
                zoomDelta = wheel;
                zoomAnchor = GetMousePosition();
            }
        }

        if (IsKeyDown(KeyboardKey.Equal) || IsKeyDown(KeyboardKey.KpAdd))
        {
            zoomDelta = 1f * deltaTime * 5f;
        }
        else if (IsKeyDown(KeyboardKey.Minus) || IsKeyDown(KeyboardKey.KpSubtract))
        {
            zoomDelta = -1f * deltaTime * 5f;
        }

        if (Math.Abs(zoomDelta) > 0.0001f)
        {
            var worldBeforeZoom = ScreenToWorld(zoomAnchor);
            _zoom = Math.Clamp(_zoom + zoomDelta * ZoomSpeed * _zoom, MinZoom, MaxZoom);
            var worldAfterZoom = ScreenToWorld(zoomAnchor);
            var tileSize = BaseTileSize * _zoom;
            _cameraOffset += (worldAfterZoom - worldBeforeZoom) * tileSize;
        }
    }

    public void Render()
    {
        BeginDrawing();
        ClearBackground(new Color(40, 40, 40, 255));

        float tileSize = BaseTileSize * _zoom;

        // Draw grid background
        DrawMapGrid(tileSize);

        // Render tile layers in order (index 0 drawn first, higher indices on top)
        for (int i = 0; i < _layers.Count; i++)
        {
            var layer = _layers[i];
            if (!layer.IsVisible) continue;

            RenderLayer(layer, tileSize);
        }

        // Highlight hovered tile
        var mouseScreen = GetMousePosition();
        var worldPos = ScreenToWorld(mouseScreen);
        int tileX = (int)MathF.Floor(worldPos.X);
        int tileY = (int)MathF.Floor(worldPos.Y);
        bool tileInBounds = tileX >= 0 && tileX < _mapData.Width && tileY >= 0 && tileY < _mapData.Height;

        if (tileInBounds)
        {
            float highlightX = tileX * tileSize + _cameraOffset.X;
            float highlightY = tileY * tileSize + _cameraOffset.Y;
            DrawRectangleLinesEx(
                new Rectangle(highlightX, highlightY, tileSize, tileSize),
                2f, Color.Yellow);
        }

        // ImGui panels
        rlImGui.Begin();
        RenderMenuBar();
        RenderFileDialogs();
        RenderLayerPanel();
        RenderTilePalette();
        RenderInfoPanel(tileX, tileY, worldPos, tileInBounds);
        rlImGui.End();

        DrawText("Level Editor - F1 to return to game", 10, GetScreenHeight() - 70, 20, Color.White);
        DrawText($"Zoom: {_zoom:F2}x", 10, GetScreenHeight() - 45, 20, Color.LightGray);

        // Status message
        if (_statusTimer > 0 && !string.IsNullOrEmpty(_statusMessage))
        {
            const int fontSize = 30;
            int textWidth = MeasureText(_statusMessage, fontSize);
            int x = (GetScreenWidth() - textWidth) / 2;
            var statusColor = _statusMessage.StartsWith("Error") ? Color.Red : Color.Green;
            DrawText(_statusMessage, x, 55, fontSize, statusColor);
        }

        //DrawFPS(10, GetScreenHeight() - 30);

        EndDrawing();
    }

    private void RenderMenuBar()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 10));
        ImGui.SetWindowFontScale(1.5f);

        if (ImGui.BeginMainMenuBar())
        {
            ImGui.SetWindowFontScale(1.5f);

            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New"))
                {
                    ClearLevel();
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Save JSON..."))
                {
                    _showSaveDialog = true;
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Load JSON..."))
                {
                    _showLoadJsonDialog = true;
                }

                if (ImGui.MenuItem("Load TMX..."))
                {
                    _showLoadTmxDialog = true;
                }

                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }

        ImGui.PopStyleVar();
    }

    private void RenderFileDialogs()
    {
        // Open popups when requested
        if (_showSaveDialog)
        {
            ImGui.OpenPopup("Save Level JSON");
            _showSaveDialog = false;
        }
        if (_showLoadJsonDialog)
        {
            ImGui.OpenPopup("Load Level JSON");
            _showLoadJsonDialog = false;
        }
        if (_showLoadTmxDialog)
        {
            ImGui.OpenPopup("Load Level TMX");
            _showLoadTmxDialog = false;
        }

        // Save dialog
        ImGui.SetNextWindowSize(new Vector2(500, 0));
        if (ImGui.BeginPopupModal("Save Level JSON", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.SetWindowFontScale(1.5f);
            ImGui.Text("Save level data to JSON file:");
            ImGui.InputText("Path", ref _savePath, 512);

            ImGui.Spacing();
            if (ImGui.Button("Save", new Vector2(120, 0)))
            {
                try
                {
                    LevelSerializer.SaveToJson(_mapData, _savePath);
                    _statusMessage = $"Saved to {_savePath}";
                }
                catch (Exception ex)
                {
                    _statusMessage = $"Error saving: {ex.Message}";
                }
                _statusTimer = 4f;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        // Load JSON dialog
        ImGui.SetNextWindowSize(new Vector2(500, 0));
        if (ImGui.BeginPopupModal("Load Level JSON", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.SetWindowFontScale(1.5f);
            ImGui.Text("Load level data from JSON file:");
            ImGui.InputText("Path", ref _loadJsonPath, 512);

            ImGui.Spacing();
            if (ImGui.Button("Load", new Vector2(120, 0)))
            {
                try
                {
                    LevelSerializer.LoadFromJson(_mapData, _loadJsonPath);
                    RefreshLayerReferences();
                    _statusMessage = $"Loaded from {_loadJsonPath}";
                }
                catch (Exception ex)
                {
                    _statusMessage = $"Error loading: {ex.Message}";
                }
                _statusTimer = 4f;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        // Load TMX dialog
        ImGui.SetNextWindowSize(new Vector2(500, 0));
        if (ImGui.BeginPopupModal("Load Level TMX", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.SetWindowFontScale(1.5f);
            ImGui.Text("Load level data from TMX file:");
            ImGui.InputText("Path", ref _loadTmxPath, 512);

            ImGui.Spacing();
            if (ImGui.Button("Load", new Vector2(120, 0)))
            {
                try
                {
                    LevelSerializer.LoadFromTmx(_mapData, _loadTmxPath);
                    RefreshLayerReferences();
                    _statusMessage = $"Loaded TMX from {_loadTmxPath}";
                }
                catch (Exception ex)
                {
                    _statusMessage = $"Error loading TMX: {ex.Message}";
                }
                _statusTimer = 4f;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// Clear all tile data to create an empty level.
    /// </summary>
    private void ClearLevel()
    {
        int tileCount = _mapData.Width * _mapData.Height;
        _mapData.Floor = new uint[tileCount];
        _mapData.Walls = new uint[tileCount];
        _mapData.Ceiling = new uint[tileCount];
        _mapData.Doors = new uint[tileCount];
        RefreshLayerReferences();
        _statusMessage = "New empty level created";
        _statusTimer = 4f;
    }

    /// <summary>
    /// After loading new data into MapData, update the editor layer references
    /// to point to the new arrays.
    /// </summary>
    private void RefreshLayerReferences()
    {
        foreach (var layer in _layers)
        {
            layer.Tiles = layer.Name switch
            {
                "Floor" => _mapData.Floor,
                "Walls" => _mapData.Walls,
                "Ceiling" => _mapData.Ceiling,
                "Doors" => _mapData.Doors,
                _ => layer.Tiles
            };
        }
    }

    private void DrawMapGrid(float tileSize)
    {
        int screenW = GetScreenWidth();
        int screenH = GetScreenHeight();

        // Calculate visible tile range
        int startX = Math.Max(0, (int)((-_cameraOffset.X) / tileSize));
        int startY = Math.Max(0, (int)((-_cameraOffset.Y) / tileSize));
        int endX = Math.Min(_mapData.Width, (int)((screenW - _cameraOffset.X) / tileSize) + 1);
        int endY = Math.Min(_mapData.Height, (int)((screenH - _cameraOffset.Y) / tileSize) + 1);

        var gridColor = new Color(60, 60, 60, 255);

        // Vertical lines
        for (int x = startX; x <= endX; x++)
        {
            int screenX = (int)(x * tileSize + _cameraOffset.X);
            DrawLine(screenX, Math.Max(0, (int)(startY * tileSize + _cameraOffset.Y)),
                     screenX, Math.Min(screenH, (int)(endY * tileSize + _cameraOffset.Y)), gridColor);
        }

        // Horizontal lines
        for (int y = startY; y <= endY; y++)
        {
            int screenY = (int)(y * tileSize + _cameraOffset.Y);
            DrawLine(Math.Max(0, (int)(startX * tileSize + _cameraOffset.X)), screenY,
                     Math.Min(screenW, (int)(endX * tileSize + _cameraOffset.X)), screenY, gridColor);
        }
    }

    private void RenderLayer(EditorLayer layer, float tileSize)
    {
        int screenW = GetScreenWidth();
        int screenH = GetScreenHeight();

        // Calculate visible tile range for culling
        int startX = Math.Max(0, (int)((-_cameraOffset.X) / tileSize));
        int startY = Math.Max(0, (int)((-_cameraOffset.Y) / tileSize));
        int endX = Math.Min(_mapData.Width - 1, (int)((screenW - _cameraOffset.X) / tileSize) + 1);
        int endY = Math.Min(_mapData.Height - 1, (int)((screenH - _cameraOffset.Y) / tileSize) + 1);

        var ids = layer.Tiles;
        if (ids == null || ids.Length == 0) return;

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                int index = _mapData.Width * y + x;
                uint tileId = ids[index];
                if (tileId == 0) continue;

                float drawX = x * tileSize + _cameraOffset.X;
                float drawY = y * tileSize + _cameraOffset.Y;

                // Both door types (HORIZONTAL=7, VERTICAL=8) use the door texture (index 6)
                bool isVerticalDoor = tileId == (uint)Entities.DoorRotation.VERTICAL;
                bool isHorizontalDoor = tileId == (uint)Entities.DoorRotation.HORIZONTAL;
                int textureIndex = (isVerticalDoor || isHorizontalDoor)
                    ? (int)Entities.DoorRotation.HORIZONTAL - 1
                    : (int)tileId - 1;

                if (textureIndex >= 0 && textureIndex < _mapData.Textures.Count)
                {
                    var texture = _mapData.Textures[textureIndex];
                    float rotation = isVerticalDoor ? 90f : 0f;
                    var origin = isVerticalDoor
                        ? new Vector2(tileSize / 2f, tileSize / 2f)
                        : Vector2.Zero;
                    var destX = isVerticalDoor ? drawX + tileSize / 2f : drawX;
                    var destY = isVerticalDoor ? drawY + tileSize / 2f : drawY;

                    DrawTexturePro(
                        texture,
                        new Rectangle(0, 0, texture.Width, texture.Height),
                        new Rectangle(destX, destY, tileSize, tileSize),
                        origin,
                        rotation,
                        Color.White
                    );
                }
                else
                {
                    // Unknown tile ID - draw magenta placeholder
                    DrawRectangle((int)drawX, (int)drawY, (int)tileSize, (int)tileSize, Color.Magenta);
                }
            }
        }
    }

    private void RenderLayerPanel()
    {
        ImGui.SetNextWindowPos(new Vector2(GetScreenWidth() - 300, 45), ImGuiCond.FirstUseEver);
        ImGui.Begin("Layers", ImGuiWindowFlags.AlwaysAutoResize);
        ImGui.SetWindowFontScale(1.5f);

        ImGui.Text("Render Order (top = drawn first)");
        ImGui.Text("Click layer name to select for painting");
        ImGui.Separator();

        int? swapFrom = null;
        int? swapTo = null;

        for (int i = 0; i < _layers.Count; i++)
        {
            var layer = _layers[i];
            ImGui.PushID(i);

            // Visibility toggle
            bool visible = layer.IsVisible;
            if (ImGui.Checkbox("##visible", ref visible))
            {
                layer.IsVisible = visible;
            }

            // Active layer selection - clickable, highlighted when active
            ImGui.SameLine();
            bool isActive = i == _activeLayerIndex;
            if (isActive)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.5f, 0.8f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.6f, 0.9f, 1f));
            }
            if (ImGui.Button(layer.Name, new Vector2(120, 0)))
            {
                _activeLayerIndex = i;
            }
            if (isActive)
            {
                ImGui.PopStyleColor(2);
            }

            // Reorder buttons
            ImGui.SameLine(200);
            if (i > 0 && ImGui.SmallButton("Up"))
            {
                swapFrom = i;
                swapTo = i - 1;
            }
            ImGui.SameLine();
            if (i < _layers.Count - 1 && ImGui.SmallButton("Down"))
            {
                swapFrom = i;
                swapTo = i + 1;
            }

            ImGui.PopID();
        }

        // Apply swap after iteration to avoid modifying collection during loop
        if (swapFrom.HasValue && swapTo.HasValue)
        {
            // Keep active layer index pointing to the same layer after swap
            if (_activeLayerIndex == swapFrom.Value)
                _activeLayerIndex = swapTo.Value;
            else if (_activeLayerIndex == swapTo.Value)
                _activeLayerIndex = swapFrom.Value;

            (_layers[swapFrom.Value], _layers[swapTo.Value]) = (_layers[swapTo.Value], _layers[swapFrom.Value]);
        }

        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "LMB drag: Pan");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "RMB: Paint tile");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "Scroll / +/-: Zoom");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "C: Toggle cursor follow");

        ImGui.End();
    }

    private void RenderTilePalette()
    {
        ImGui.SetNextWindowPos(new Vector2(10, 45), ImGuiCond.FirstUseEver);
        ImGui.Begin("Tile Palette", ImGuiWindowFlags.AlwaysAutoResize);
        ImGui.SetWindowFontScale(1.5f);

        ImGui.Text($"Active Layer: {_layers[_activeLayerIndex].Name}");
        ImGui.Separator();

        float buttonSize = 64f;

        // Eraser (tile ID 0)
        bool isEraserSelected = _selectedTileId == 0;
        if (isEraserSelected)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1f));
        }
        if (ImGui.Button("Eraser\n(Empty)", new Vector2(buttonSize + 20, buttonSize)))
        {
            _selectedTileId = 0;
        }
        if (isEraserSelected)
        {
            ImGui.PopStyleColor(2);
        }

        ImGui.Separator();
        ImGui.Text("Tiles:");

        // Texture tile buttons in a grid
        int columns = 3;
        for (int i = 0; i < _mapData.Textures.Count; i++)
        {
            uint tileId = (uint)(i + 1);
            var texture = _mapData.Textures[i];

            if (i % columns != 0)
                ImGui.SameLine();

            ImGui.PushID(i + 100);

            bool isSelected = _selectedTileId == tileId;
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.5f, 0.8f, 1f));
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1f, 1f, 0f, 1f));
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 3f);
            }

            // For vertical door (tile ID 8), use the rotated door texture
            IntPtr texId;
            if (tileId == 8 && _rotatedDoorTexture.Texture.Id != 0)
            {
                texId = new IntPtr(_rotatedDoorTexture.Texture.Id);
            }
            else
            {
                texId = new IntPtr(texture.Id);
            }

            // RenderTexture is flipped vertically in OpenGL, so flip UVs for the rotated door
            var uv0 = (tileId == 8) ? new Vector2(0, 1) : new Vector2(0, 0);
            var uv1 = (tileId == 8) ? new Vector2(1, 0) : new Vector2(1, 1);

            if (ImGui.ImageButton($"tile_{i}", texId, new Vector2(buttonSize, buttonSize), uv0, uv1))
            {
                _selectedTileId = tileId;
            }

            if (isSelected)
            {
                ImGui.PopStyleVar();
                ImGui.PopStyleColor(2);
            }

            // Tooltip with tile ID
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"Tile ID: {tileId}");
            }

            ImGui.PopID();
        }

        ImGui.Separator();
        ImGui.Text($"Selected: {(_selectedTileId == 0 ? "Eraser" : $"ID {_selectedTileId}")}");

        ImGui.End();
    }

    private void RenderInfoPanel(int tileX, int tileY, Vector2 worldPos, bool tileInBounds)
    {
        if (_cursorInfoFollowsMouse)
        {
            var mouse = GetMousePosition();
            // Offset to the right of the cursor, clamp to screen
            float panelX = mouse.X + 25f;
            float panelY = mouse.Y - 10f;
            ImGui.SetNextWindowPos(new Vector2(panelX, panelY));
        }
        else
        {
            ImGui.SetNextWindowPos(new Vector2(GetScreenWidth() - 300, 350), ImGuiCond.FirstUseEver);
        }

        var flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing;
        if (_cursorInfoFollowsMouse)
        {
            flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar;
        }

        ImGui.Begin("Cursor Info", flags);
        ImGui.SetWindowFontScale(1.5f);

        if (tileInBounds)
        {
            ImGui.Text("Tile Coordinate");
            ImGui.Separator();
            ImGui.Text($"X: {tileX}  Y: {tileY}");

            ImGui.Spacing();
            ImGui.Text("World Coordinate");
            ImGui.Separator();
            float worldX = worldPos.X * Utilities.LevelData.QuadSize;
            float worldY = worldPos.Y * Utilities.LevelData.QuadSize;
            ImGui.Text($"X: {worldX:F1}  Z: {worldY:F1}");

            ImGui.Spacing();
            ImGui.Text("Tile Contents");
            ImGui.Separator();
            foreach (var layer in _layers)
            {
                uint tileId = _mapData.GetTile(layer.Tiles, tileX, tileY);
                string status = tileId > 0 ? $"ID {tileId}" : "empty";
                var color = tileId > 0
                    ? new Vector4(0.3f, 1f, 0.3f, 1f)
                    : new Vector4(0.5f, 0.5f, 0.5f, 1f);
                ImGui.TextColored(color, $"  {layer.Name}: {status}");
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "Outside map bounds");
        }

        ImGui.End();
    }

    /// <summary>
    /// Convert a screen position to world tile coordinates.
    /// </summary>
    private Vector2 ScreenToWorld(Vector2 screenPos)
    {
        float tileSize = BaseTileSize * _zoom;
        return new Vector2(
            (screenPos.X - _cameraOffset.X) / tileSize,
            (screenPos.Y - _cameraOffset.Y) / tileSize
        );
    }
}
