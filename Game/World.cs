using System.Numerics;
using Game.Entities;
using Game.Systems;
using Game.Utilities;
using ImGuiNET;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Game;

public class World
{
    private readonly Player _player;
    private readonly LevelData _level;
    private readonly List<Texture2D> _textures;

    // Systems
    private readonly InputSystem _inputSystem;
    private readonly MovementSystem _movementSystem;
    private readonly CollisionSystem _collisionSystem;
    private readonly CameraSystem _cameraSystem;
    private readonly DoorSystem _doorSystem;
    private readonly RenderSystem _renderSystem;
    private readonly HudSystem _hudSystem;
    private readonly AnimationSystem _animationSystem;
    private readonly MinimapSystem _minimapSystem;

    // Rendering
    private readonly RenderTexture2D _sceneRenderTexture;
    private InputState _inputState;
    private readonly EnemySystem _enemySystem;

    public World()
    {
        // Initialize Raylib
        // SetConfigFlags(ConfigFlags.VSyncHint);
        SetTargetFPS(120);
        InitWindow(0, 0, "");
        SetWindowState(ConfigFlags.ResizableWindow);
        
        RenderData.Resolution = new Vector2(GetScreenWidth(), GetScreenHeight());
        int screenWidth = (int)RenderData.Resolution.X / RenderData.ResolutionDownScaleMultiplier;
        int screenHeight = (int)RenderData.Resolution.Y / RenderData.ResolutionDownScaleMultiplier;

        // Load level
        var loader = DotTiled.Serialization.Loader.Default();
        var map = loader.LoadMap("resources/map1.tmx");
        var floor = map.Layers[0] as DotTiled.TileLayer;
        var walls = map.Layers[1] as DotTiled.TileLayer;
        var ceiling = map.Layers[2] as DotTiled.TileLayer;
        var doors = map.Layers[3] as DotTiled.TileLayer;

        if (walls == null || floor == null || ceiling == null || doors == null)
            throw new InvalidOperationException("Level must have walls, floor, and ceiling layers");
        
        _level = new LevelData(walls, floor, ceiling);

        // Load textures
        _textures = new List<Texture2D>
        {
            LoadTexture("resources/greystone.png"),
            LoadTexture("resources/bluestone.png"),
            LoadTexture("resources/colorstone.png"),
            LoadTexture("resources/mossy.png"),
            LoadTexture("resources/redbrick.png"),
            LoadTexture("resources/wood.png"),
            LoadTexture("resources/door.png"),
            LoadTexture("resources/enemy_guard2.png")
        };

        // Initialize player
        _player = new Player
        {
            Position = new Vector3(30.0f * 4, 2.0f, 28f * 4),
            Camera = new Camera3D
            {
                Position = new Vector3(30.0f * 4, 2.0f, 30f * 4),
                Target = new Vector3(120.0f, 2.0f, 119.0f),
                Up = new Vector3(0.0f, 1.0f, 0.0f),
                FovY = 60.0f,
                Projection = CameraProjection.Perspective
            }
        };

        // Initialize systems (note: collision system is created first as camera system depends on it)
        _inputSystem = new InputSystem();
        _movementSystem = new MovementSystem();
        _doorSystem = new DoorSystem(doors, _textures);
        _collisionSystem = new CollisionSystem(_level, _doorSystem);
        _cameraSystem = new CameraSystem(_collisionSystem);
        _renderSystem = new RenderSystem(_level, _textures);
        _hudSystem = new HudSystem(screenWidth, screenHeight);
        _minimapSystem = new MinimapSystem(_level, _renderSystem);
        
        _enemySystem = new EnemySystem(_player, _inputSystem);
        _animationSystem = new AnimationSystem(_textures[7], _player, _enemySystem);
        

        // Initialize render textures
        _sceneRenderTexture = LoadRenderTexture(screenWidth, screenHeight);
        Debug.Setup(_doorSystem.Doors, _player, _animationSystem, _enemySystem);
    }

    public void Update(float deltaTime)
    {
        var mouseDelta = _inputState.MouseDelta;
        
        _inputSystem.LockMouse();
        _inputSystem.Update();

        _player.Velocity = _inputSystem.GetMoveDirection(_player) * _player.MoveSpeed;

        _movementSystem.Update(_player, deltaTime);
        _collisionSystem.Update(_player, deltaTime);
        _cameraSystem.Update(_player, _inputState.IsMouseFree, mouseDelta);
        _doorSystem.Update(deltaTime, _inputState, _player.Position);
        _animationSystem.Update(deltaTime);
        _enemySystem.Update(deltaTime);
    }

    public void Render()
    {
        // Update lighting shader with current player position
        PrimitiveRenderer.SetLightingParameters(_player.Position, maxDistance: 50.0f, minBrightness: 0.1f);
        var lightingShader = PrimitiveRenderer.GetLightingShader();
        
        // Render 3D scene
        BeginTextureMode(_sceneRenderTexture);
        BeginMode3D(_player.Camera);
        ClearBackground(Color.Black);
        
        // Enable lighting shader
        if (lightingShader.HasValue)
        {
            BeginShaderMode(lightingShader.Value);
        }

        _renderSystem.Render(_player);
        _doorSystem.Render();
        _animationSystem.Render();
        
        // Disable lighting shader
        if (lightingShader.HasValue)
        {
            EndShaderMode();
        }
        
        EndMode3D();
        EndTextureMode();

        // Render HUD
        // _hudSystem.Begin();
        // _hudSystem.Render(_player, _level);
        // _hudSystem.End();

        // Composite to screen
        BeginDrawing();
        ClearBackground(Color.Black);

        DrawTexturePro(
            _sceneRenderTexture.Texture,
            new Rectangle(0, 0, (float)_sceneRenderTexture.Texture.Width, (float)-_sceneRenderTexture.Texture.Height),
            new Rectangle(0, 0, GetScreenWidth(), GetScreenHeight()),
            new Vector2(0, 0),
            0,
            Color.White);

        // _hudSystem.DrawToScreen(screenWidth, screenHeight);
        DrawFPS(10, GetScreenHeight() - 120);
        
        if (_inputState.IsMinimapEnabled)
        {
            // Render minimap
            _minimapSystem.Render(_player);
        }
        
        Debug.Draw(_inputState.IsDebugEnabled);
        
        EndDrawing();
    }

    public void Run()
    {
        while (!WindowShouldClose())
        {
            var deltaTime = GetFrameTime();
            _inputState =  _inputSystem.GetInputState();
            
            if (_inputState.IsGamePaused)
            {
                Update(deltaTime);
            }
            else
            {
                _inputSystem.Update();
            }
            Render();
        }

        Cleanup();
    }

    private void Cleanup()
    {
        _hudSystem.Dispose();
        UnloadRenderTexture(_sceneRenderTexture);
        CloseWindow();
    }
}

