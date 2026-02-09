using System.Numerics;
using Game.Systems;
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
    private bool _cursorInfoFollowsMouse = false;

    // Tile painting
    private int _activeLayerIndex = 0;
    private uint _selectedTileId = 1; // 0 = eraser, 1+ = tile IDs

    // Enemy editing
    private const string EnemiesLayerName = "Enemies";
    private int _hoveredEnemyIndex = -1;
    private int _selectedEnemyIndex = -1;
    private bool _isDraggingEnemy;

    // Patrol path editing
    private bool _isEditingPatrolPath;
    private int _patrolEditEnemyIndex = -1;
    private List<PatrolWaypoint> _patrolPathInProgress = new();

    // Pre-rendered rotated door texture for palette display
    private RenderTexture2D _rotatedDoorTexture;

    // GUI scaling
    private float _guiScale = 1.5f;
    private const float MinGuiScale = 0.5f;
    private const float MaxGuiScale = 4.0f;
    private const float GuiScaleStep = 0.25f;

    // Simulation
    private readonly EnemySystem _enemySystem;
    private bool _isSimulating;

    // File dialog state
    private bool _showSaveDialog;
    private bool _showLoadJsonDialog;
    private bool _showLoadTmxDialog;
    private string _savePath = "resources/level.json";
    private string _loadJsonPath = "resources/level.json";
    private string _loadTmxPath = "resources/map1.tmx";
    private string _statusMessage = "";
    private float _statusTimer;

    public LevelEditorScene(MapData mapData, EnemySystem enemySystem)
    {
        _mapData = mapData;
        _enemySystem = enemySystem;

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
            new() { Name = EnemiesLayerName, Tiles = Array.Empty<uint>() },
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
        _isSimulating = false;
    }

    public void Update(float deltaTime)
    {
        // Toggle cursor info follow mode
        if (IsKeyPressed(KeyboardKey.C))
        {
            _cursorInfoFollowsMouse = !_cursorInfoFollowsMouse;
        }

        // Layer hotkeys: 1-9 to activate, Ctrl+1-9 to toggle visibility
        bool ctrlForLayers = IsKeyDown(KeyboardKey.LeftControl) || IsKeyDown(KeyboardKey.RightControl);
        KeyboardKey[] numKeys = {
            KeyboardKey.One, KeyboardKey.Two, KeyboardKey.Three,
            KeyboardKey.Four, KeyboardKey.Five, KeyboardKey.Six,
            KeyboardKey.Seven, KeyboardKey.Eight, KeyboardKey.Nine
        };
        for (int i = 0; i < numKeys.Length && i < _layers.Count; i++)
        {
            if (IsKeyPressed(numKeys[i]))
            {
                if (ctrlForLayers)
                {
                    _layers[i].IsVisible = !_layers[i].IsVisible;
                }
                else
                {
                    _activeLayerIndex = i;
                }
            }
        }

        // Status message timer
        if (_statusTimer > 0)
        {
            _statusTimer -= deltaTime;
        }

        // Toggle simulation with P key
        if (IsKeyPressed(KeyboardKey.P))
        {
            _isSimulating = !_isSimulating;
            if (_isSimulating)
            {
                // Rebuild enemies from current editor data before simulating
                _enemySystem.Rebuild(_mapData.Enemies);
            }
        }

        // Tick enemy system when simulating
        if (_isSimulating)
        {
            _enemySystem.Update(deltaTime);
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

        // Patrol path editing mode: clicks add waypoints, Enter confirms, Escape cancels
        if (_isEditingPatrolPath)
        {
            if (!imGuiWantsMouse && IsMouseButtonPressed(MouseButton.Left))
            {
                var paintPos = ScreenToWorld(GetMousePosition());
                int px = (int)MathF.Floor(paintPos.X);
                int py = (int)MathF.Floor(paintPos.Y);
                if (px >= 0 && px < _mapData.Width && py >= 0 && py < _mapData.Height)
                {
                    _patrolPathInProgress.Add(new PatrolWaypoint { TileX = px, TileY = py });
                }
            }

            if (IsKeyPressed(KeyboardKey.Enter) || IsKeyPressed(KeyboardKey.KpEnter))
            {
                // Confirm patrol path
                if (_patrolEditEnemyIndex >= 0 && _patrolEditEnemyIndex < _mapData.Enemies.Count)
                {
                    _mapData.Enemies[_patrolEditEnemyIndex].PatrolPath = new List<PatrolWaypoint>(_patrolPathInProgress);
                }
                _isEditingPatrolPath = false;
                _patrolPathInProgress.Clear();
                _patrolEditEnemyIndex = -1;
            }

            if (IsKeyPressed(KeyboardKey.Escape))
            {
                // Cancel patrol path editing
                _isEditingPatrolPath = false;
                _patrolPathInProgress.Clear();
                _patrolEditEnemyIndex = -1;
            }

            // Skip normal input handling while editing path
        }
        else
        {

        // Paint tiles / place enemies with left mouse button
        bool isEnemyLayer = _layers[_activeLayerIndex].Name == EnemiesLayerName;

        // If clicking on a hovered enemy while another layer is active, auto-switch to enemy layer
        if (!imGuiWantsMouse && !isEnemyLayer && _hoveredEnemyIndex >= 0 && IsMouseButtonPressed(MouseButton.Left))
        {
            // Find and activate the enemies layer
            for (int li = 0; li < _layers.Count; li++)
            {
                if (_layers[li].Name == EnemiesLayerName)
                {
                    _activeLayerIndex = li;
                    break;
                }
            }
            isEnemyLayer = true;
            _selectedEnemyIndex = _hoveredEnemyIndex;
            _isDraggingEnemy = true;
        }

        if (!imGuiWantsMouse && isEnemyLayer)
        {
            if (IsMouseButtonPressed(MouseButton.Left))
            {
                // Enemy layer: click to select existing or place new enemy
                if (_hoveredEnemyIndex >= 0)
                {
                    _selectedEnemyIndex = _hoveredEnemyIndex;
                    _isDraggingEnemy = true;
                }
                else
                {
                    var paintPos = ScreenToWorld(GetMousePosition());
                    int px = (int)MathF.Floor(paintPos.X);
                    int py = (int)MathF.Floor(paintPos.Y);
                    if (px >= 0 && px < _mapData.Width && py >= 0 && py < _mapData.Height)
                    {
                        _mapData.Enemies.Add(new EnemyPlacement
                        {
                            TileX = px,
                            TileY = py,
                            Rotation = 0,
                            EnemyType = "Guard"
                        });
                        _selectedEnemyIndex = _mapData.Enemies.Count - 1;
                        _isDraggingEnemy = true;
                    }
                }
            }

            // Drag selected enemy to new tile while holding LMB
            if (_isDraggingEnemy && IsMouseButtonDown(MouseButton.Left)
                && _selectedEnemyIndex >= 0 && _selectedEnemyIndex < _mapData.Enemies.Count)
            {
                var dragPos = ScreenToWorld(GetMousePosition());
                int dx = (int)MathF.Floor(dragPos.X);
                int dy = (int)MathF.Floor(dragPos.Y);
                if (dx >= 0 && dx < _mapData.Width && dy >= 0 && dy < _mapData.Height)
                {
                    _mapData.Enemies[_selectedEnemyIndex].TileX = dx;
                    _mapData.Enemies[_selectedEnemyIndex].TileY = dy;
                }
            }

            if (IsMouseButtonReleased(MouseButton.Left))
            {
                _isDraggingEnemy = false;
            }
        }
        else if (!imGuiWantsMouse && IsMouseButtonDown(MouseButton.Left) && !isEnemyLayer)
        {
            // Tile layer: paint tiles (click or drag)
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

        // Delete selected enemy with Delete key
        if (isEnemyLayer && _selectedEnemyIndex >= 0 && _selectedEnemyIndex < _mapData.Enemies.Count
            && IsKeyPressed(KeyboardKey.Delete))
        {
            _mapData.Enemies.RemoveAt(_selectedEnemyIndex);
            _selectedEnemyIndex = -1;
        }

        } // end of: else (not editing patrol path)

        // Ctrl+/- for GUI scaling, plain +/- for zoom
        bool ctrlHeld = IsKeyDown(KeyboardKey.LeftControl) || IsKeyDown(KeyboardKey.RightControl);

        if (ctrlHeld)
        {
            if (IsKeyPressed(KeyboardKey.Equal) || IsKeyPressed(KeyboardKey.KpAdd))
            {
                _guiScale = Math.Clamp(_guiScale + GuiScaleStep, MinGuiScale, MaxGuiScale);
            }
            else if (IsKeyPressed(KeyboardKey.Minus) || IsKeyPressed(KeyboardKey.KpSubtract))
            {
                _guiScale = Math.Clamp(_guiScale - GuiScaleStep, MinGuiScale, MaxGuiScale);
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

        if (!ctrlHeld && (IsKeyDown(KeyboardKey.Equal) || IsKeyDown(KeyboardKey.KpAdd)))
        {
            zoomDelta = 1f * deltaTime * 5f;
        }
        else if (!ctrlHeld && (IsKeyDown(KeyboardKey.Minus) || IsKeyDown(KeyboardKey.KpSubtract)))
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

            if (layer.Name == EnemiesLayerName)
            {
                RenderEnemyLayer(tileSize);
            }
            else
            {
                RenderLayer(layer, tileSize);
            }
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
        RenderEnemyPropertiesPanel();
        rlImGui.End();

        DrawText("Level Editor - F1 to return to game", 10, GetScreenHeight() - 70, 20, Color.White);
        DrawText($"Zoom: {_zoom:F2}x", 10, GetScreenHeight() - 45, 20, Color.LightGray);

        if (_isEditingPatrolPath)
        {
            const string msg = "EDITING PATROL PATH - LMB: Add waypoint | Enter: Confirm | Esc: Cancel";
            int msgW = MeasureText(msg, 24);
            DrawText(msg, (GetScreenWidth() - msgW) / 2, GetScreenHeight() - 100, 24, Color.Yellow);
        }

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
        ImGui.SetWindowFontScale(_guiScale);

        if (ImGui.BeginMainMenuBar())
        {
            ImGui.SetWindowFontScale(_guiScale);

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

            if (ImGui.BeginMenu("GUI"))
            {
                if (ImGui.MenuItem("Increase Scaling", "Ctrl++"))
                {
                    _guiScale = Math.Clamp(_guiScale + GuiScaleStep, MinGuiScale, MaxGuiScale);
                }

                if (ImGui.MenuItem("Decrease Scaling", "Ctrl+-"))
                {
                    _guiScale = Math.Clamp(_guiScale - GuiScaleStep, MinGuiScale, MaxGuiScale);
                }

                if (ImGui.MenuItem("Reset Scaling"))
                {
                    _guiScale = 1.5f;
                }

                ImGui.Separator();
                ImGui.Text($"Scale: {_guiScale:F2}x");

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Simulation"))
            {
                if (ImGui.MenuItem(_isSimulating ? "Stop Simulation" : "Start Simulation", "P"))
                {
                    _isSimulating = !_isSimulating;
                    if (_isSimulating)
                    {
                        _enemySystem.Rebuild(_mapData.Enemies);
                    }
                }
                ImGui.EndMenu();
            }

            // Show simulation status in menu bar
            if (_isSimulating)
            {
                ImGui.SameLine(ImGui.GetWindowWidth() - 250);
                ImGui.TextColored(new Vector4(0.3f, 1f, 0.3f, 1f), "SIMULATING");
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
            ImGui.SetWindowFontScale(_guiScale);
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
            ImGui.SetWindowFontScale(_guiScale);
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
            ImGui.SetWindowFontScale(_guiScale);
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
        _mapData.Enemies.Clear();
        _selectedEnemyIndex = -1;
        _hoveredEnemyIndex = -1;
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
        _selectedEnemyIndex = -1;
        _hoveredEnemyIndex = -1;

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
        ImGui.SetWindowFontScale(_guiScale);

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
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "RMB drag: Pan");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "LMB: Paint tile / Place enemy");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "Scroll / +/-: Zoom");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "1-9: Activate layer");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "Ctrl+1-9: Toggle visibility");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "C: Toggle cursor follow");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "Del: Delete selected enemy");

        ImGui.End();
    }

    private void RenderTilePalette()
    {
        ImGui.SetNextWindowPos(new Vector2(10, 45), ImGuiCond.FirstUseEver);
        ImGui.Begin("Tile Palette", ImGuiWindowFlags.AlwaysAutoResize);
        ImGui.SetWindowFontScale(_guiScale);

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
        ImGui.SetWindowFontScale(_guiScale);

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
                if (layer.Name == EnemiesLayerName)
                {
                    // Show enemy info for this tile
                    var enemyHere = _mapData.Enemies.FindIndex(e => e.TileX == tileX && e.TileY == tileY);
                    string status = enemyHere >= 0
                        ? $"{_mapData.Enemies[enemyHere].EnemyType} (#{enemyHere})"
                        : "empty";
                    var color = enemyHere >= 0
                        ? new Vector4(1f, 0.3f, 0.3f, 1f)
                        : new Vector4(0.5f, 0.5f, 0.5f, 1f);
                    ImGui.TextColored(color, $"  {layer.Name}: {status}");
                }
                else
                {
                    uint tileId = _mapData.GetTile(layer.Tiles, tileX, tileY);
                    string status = tileId > 0 ? $"ID {tileId}" : "empty";
                    var color = tileId > 0
                        ? new Vector4(0.3f, 1f, 0.3f, 1f)
                        : new Vector4(0.5f, 0.5f, 0.5f, 1f);
                    ImGui.TextColored(color, $"  {layer.Name}: {status}");
                }
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "Outside map bounds");
        }

        ImGui.End();
    }

    /// <summary>
    /// Render enemies as red circles on the map. Handles hover highlight and selection.
    /// </summary>
    private void RenderEnemyLayer(float tileSize)
    {
        var mouseScreen = GetMousePosition();
        bool imGuiWantsMouse = ImGui.GetIO().WantCaptureMouse;
        _hoveredEnemyIndex = -1;

        float radius = tileSize * 0.35f;

        for (int i = 0; i < _mapData.Enemies.Count; i++)
        {
            var enemy = _mapData.Enemies[i];

            // Center of the tile in screen space
            float centerX = (enemy.TileX + 0.5f) * tileSize + _cameraOffset.X;
            float centerY = (enemy.TileY + 0.5f) * tileSize + _cameraOffset.Y;

            // Check if mouse is hovering this enemy circle
            if (!imGuiWantsMouse)
            {
                float dx = mouseScreen.X - centerX;
                float dy = mouseScreen.Y - centerY;
                if (dx * dx + dy * dy <= radius * radius)
                {
                    _hoveredEnemyIndex = i;
                }
            }

            // Draw filled red circle
            DrawCircle((int)centerX, (int)centerY, radius, new Color(200, 40, 40, 200));

            // Draw yellow outline if hovered
            if (i == _hoveredEnemyIndex)
            {
                DrawCircleLines((int)centerX, (int)centerY, radius + 2f, Color.Yellow);
                DrawCircleLines((int)centerX, (int)centerY, radius + 3f, Color.Yellow);
            }

            // Draw white outline if selected
            if (i == _selectedEnemyIndex)
            {
                DrawCircleLines((int)centerX, (int)centerY, radius + 1f, Color.White);
                DrawCircleLines((int)centerX, (int)centerY, radius + 4f, Color.White);
            }

            // Draw a small direction indicator line from center
            // Screen Y increases downward, matching the game world's Z axis
            float dirLen = radius * 0.8f;
            float angle = enemy.Rotation;
            float endX = centerX + MathF.Cos(angle) * dirLen;
            float endY = centerY + MathF.Sin(angle) * dirLen;
            DrawLineEx(new Vector2(centerX, centerY), new Vector2(endX, endY), 2f, Color.White);

            // Draw saved patrol path
            if (enemy.ShowPatrolPath && enemy.PatrolPath.Count > 0)
            {
                DrawPatrolPath(enemy, enemy.PatrolPath, tileSize, new Color(0, 200, 255, 200));
            }

            // Draw in-progress patrol path for the enemy being edited
            if (_isEditingPatrolPath && _patrolEditEnemyIndex == i && _patrolPathInProgress.Count > 0)
            {
                DrawPatrolPath(enemy, _patrolPathInProgress, tileSize, new Color(255, 200, 0, 220));

                // Draw a dashed line from the last waypoint to the mouse cursor
                var lastWp = _patrolPathInProgress[^1];
                float lastWpX = (lastWp.TileX + 0.5f) * tileSize + _cameraOffset.X;
                float lastWpY = (lastWp.TileY + 0.5f) * tileSize + _cameraOffset.Y;
                DrawLineEx(new Vector2(lastWpX, lastWpY), mouseScreen, 1f, new Color(255, 200, 0, 120));
            }
        }

        // When simulating, draw live enemy positions as green circles
        if (_isSimulating && _enemySystem.Enemies != null)
        {
            float liveRadius = tileSize * 0.3f;
            float quadSize = Utilities.LevelData.QuadSize;

            foreach (var liveEnemy in _enemySystem.Enemies)
            {
                // Convert world-space position back to tile-space for the 2D editor view
                float tilePosX = liveEnemy.Position.X / quadSize;
                float tilePosZ = liveEnemy.Position.Z / quadSize;

                float liveCX = (tilePosX + 0.5f) * tileSize + _cameraOffset.X;
                float liveCY = (tilePosZ + 0.5f) * tileSize + _cameraOffset.Y;

                // Green filled circle for live position
                DrawCircle((int)liveCX, (int)liveCY, liveRadius, new Color(40, 200, 40, 180));
                DrawCircleLines((int)liveCX, (int)liveCY, liveRadius, new Color(40, 255, 40, 255));

                // Direction indicator
                float liveDirLen = liveRadius * 0.8f;
                float liveAngle = liveEnemy.Rotation; // Undo the -π/2 offset from EnemySystem
                float liveEndX = liveCX + MathF.Cos(liveAngle) * liveDirLen;
                float liveEndY = liveCY + MathF.Sin(liveAngle) * liveDirLen;
                DrawLineEx(new Vector2(liveCX, liveCY), new Vector2(liveEndX, liveEndY), 2f, Color.White);

                // State label
                string stateText = liveEnemy.EnemyState.ToString();
                int stateW = MeasureText(stateText, 14);
                var stateColor = liveEnemy.EnemyState == Entities.EnemyState.COLLIDING
                    ? new Color(255, 40, 40, 255)
                    : new Color(40, 255, 40, 255);
                DrawText(stateText, (int)(liveCX - stateW / 2f), (int)(liveCY - liveRadius - 16), 14, stateColor);
            }
        }
    }

    /// <summary>
    /// Draw a patrol path as lines and waypoint dots from the enemy's position through each waypoint.
    /// </summary>
    private void DrawPatrolPath(EnemyPlacement enemy, List<PatrolWaypoint> path, float tileSize, Color color)
    {
        float prevX = (enemy.TileX + 0.5f) * tileSize + _cameraOffset.X;
        float prevY = (enemy.TileY + 0.5f) * tileSize + _cameraOffset.Y;

        for (int w = 0; w < path.Count; w++)
        {
            float wpX = (path[w].TileX + 0.5f) * tileSize + _cameraOffset.X;
            float wpY = (path[w].TileY + 0.5f) * tileSize + _cameraOffset.Y;

            DrawLineEx(new Vector2(prevX, prevY), new Vector2(wpX, wpY), 2f, color);
            DrawCircle((int)wpX, (int)wpY, tileSize * 0.12f, color);

            prevX = wpX;
            prevY = wpY;
        }
    }

    /// <summary>
    /// Render ImGui panel showing properties of the selected enemy.
    /// </summary>
    private void RenderEnemyPropertiesPanel()
    {
        if (_selectedEnemyIndex < 0 || _selectedEnemyIndex >= _mapData.Enemies.Count)
            return;

        var enemy = _mapData.Enemies[_selectedEnemyIndex];

        ImGui.SetNextWindowPos(new Vector2(GetScreenWidth() - 300, 500), ImGuiCond.FirstUseEver);
        ImGui.Begin("Enemy Properties", ImGuiWindowFlags.AlwaysAutoResize);
        ImGui.SetWindowFontScale(_guiScale);

        ImGui.Text($"Enemy #{_selectedEnemyIndex}");
        ImGui.Separator();

        // Tile position
        int tileX = enemy.TileX;
        int tileY = enemy.TileY;
        if (ImGui.InputInt("Tile X", ref tileX))
        {
            enemy.TileX = Math.Clamp(tileX, 0, _mapData.Width - 1);
        }
        if (ImGui.InputInt("Tile Y", ref tileY))
        {
            enemy.TileY = Math.Clamp(tileY, 0, _mapData.Height - 1);
        }

        ImGui.Spacing();

        // World position (read-only, calculated from tile)
        float worldX = enemy.TileX * Utilities.LevelData.QuadSize;
        float worldZ = enemy.TileY * Utilities.LevelData.QuadSize;
        ImGui.Text("World Position");
        ImGui.Text($"  X: {worldX:F1}  Y: 2.0  Z: {worldZ:F1}");

        ImGui.Spacing();

        // Rotation (snapped to 45-degree increments)
        const float step = MathF.PI / 4f; // 45 degrees
        int rotIndex = (int)MathF.Round(enemy.Rotation / step);
        rotIndex = Math.Clamp(rotIndex, 0, 7);
        string[] labels = { "0°", "45°", "90°", "135°", "180°", "225°", "270°", "315°" };
        if (ImGui.SliderInt("Rotation", ref rotIndex, 0, 7, labels[rotIndex]))
        {
            enemy.Rotation = rotIndex * step;
        }

        ImGui.Spacing();

        // Enemy type
        ImGui.Text($"Type: {enemy.EnemyType}");
        if (ImGui.Button("Guard")) enemy.EnemyType = "Guard";

        ImGui.Spacing();
        ImGui.Separator();

        // Patrol path
        ImGui.Text("Patrol Path");

        bool showPath = enemy.ShowPatrolPath;
        if (ImGui.Checkbox("Show Path", ref showPath))
        {
            enemy.ShowPatrolPath = showPath;
        }

        if (_isEditingPatrolPath && _patrolEditEnemyIndex == _selectedEnemyIndex)
        {
            ImGui.TextColored(new Vector4(1f, 0.8f, 0f, 1f),
                $"Editing... ({_patrolPathInProgress.Count} waypoints)");
            ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "LMB: Add waypoint");
            ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "Enter: Confirm path");
            ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "Escape: Cancel");

            if (ImGui.Button("Cancel Editing"))
            {
                _isEditingPatrolPath = false;
                _patrolPathInProgress.Clear();
                _patrolEditEnemyIndex = -1;
            }
        }
        else
        {
            if (enemy.PatrolPath.Count > 0)
            {
                ImGui.Text($"{enemy.PatrolPath.Count} waypoints");
                for (int w = 0; w < enemy.PatrolPath.Count; w++)
                {
                    var wp = enemy.PatrolPath[w];
                    ImGui.TextColored(new Vector4(0, 0.8f, 1f, 1f),
                        $"  {w + 1}: ({wp.TileX}, {wp.TileY})");
                }

                if (ImGui.Button("Clear Path"))
                {
                    enemy.PatrolPath.Clear();
                }
                ImGui.SameLine();
            }

            if (ImGui.Button("Add Path"))
            {
                _isEditingPatrolPath = true;
                _patrolEditEnemyIndex = _selectedEnemyIndex;
                _patrolPathInProgress.Clear();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();

        // Delete button
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1f));
        if (ImGui.Button("Delete Enemy", new Vector2(-1, 0)))
        {
            _mapData.Enemies.RemoveAt(_selectedEnemyIndex);
            _selectedEnemyIndex = -1;
        }
        ImGui.PopStyleColor(2);

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
