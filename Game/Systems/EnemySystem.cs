using System.Numerics;
using Game.Entities;
using Game.Utilities;

namespace Game.Systems;

public class EnemySystem
{
    private readonly Player _player;
    private readonly List<Enemy> _enemies;
    private readonly InputSystem _inputSystem;
    private readonly CollisionSystem _collisionSystem;
    private readonly DoorSystem _doorSystem;
    private MapData _mapData = null!;

    // Throttled LOS update
    private float _losAccumulator;
    private const float LosInterval = 1f / 11f; // ~11 checks per second
    private const int FovRayCount = 48; // Number of rays for FOV polygon
    private static readonly Random _random = new();

    public List<Enemy> Enemies => _enemies;
    public EnemySystem(Player player, InputSystem inputSystem, CollisionSystem collisionSystem, DoorSystem doorSystem)
    {
        _inputSystem = inputSystem;
        _player = player;
        _collisionSystem = collisionSystem;
        _doorSystem = doorSystem;
        _enemies = new List<Enemy>();
    }

    /// <summary>
    /// Rebuild the enemy list from MapData enemy placements.
    /// Call this when the level data has changed (e.g. after editing in the level editor).
    /// </summary>
    public void Rebuild(List<EnemyPlacement> placements, MapData? mapData = null)
    {
        if (mapData != null)
            _mapData = mapData;

        _enemies.Clear();
        _losAccumulator = 0f;
        
        foreach (var placement in placements)
        {
            var startPos = new Vector3(
                placement.TileX * LevelData.QuadSize,
                2f,
                placement.TileY * LevelData.QuadSize);

            var enemy = new EnemyGuard
            {
                Position = startPos,
                PatrolOrigin = startPos,
                Rotation = placement.Rotation,
                MoveSpeed = 2f,
                CurrentWaypointIndex = 0,
                PatrolPath = placement.PatrolPath.Select(wp => new Vector3(
                    wp.TileX * LevelData.QuadSize,
                    2f,
                    wp.TileY * LevelData.QuadSize)).ToList()
            };
            _enemies.Add(enemy);
        }
    }

    public void Update(float deltaTime)
    {
        foreach (var enemy in _enemies)
        {
            // Patrol movement: walk through waypoints, then back to origin, and loop
            if (enemy.HasPatrolPath)
            {
                // Build the full loop: origin -> waypoints -> back to origin
                // CurrentWaypointIndex 0..N-1 = patrol waypoints, N = returning to origin
                int totalStops = enemy.PatrolPath.Count + 1; // +1 for return to origin
                int idx = enemy.CurrentWaypointIndex % totalStops;

                Vector3 target = idx < enemy.PatrolPath.Count
                    ? enemy.PatrolPath[idx]
                    : enemy.PatrolOrigin;

                Vector3 toTarget = target - enemy.Position;
                float distXZ = MathF.Sqrt(toTarget.X * toTarget.X + toTarget.Z * toTarget.Z);

                const float arrivalThreshold = 0.5f;

                if (distXZ > arrivalThreshold)
                {
                    // Move toward the target
                    Vector3 direction = new Vector3(toTarget.X / distXZ, 0, toTarget.Z / distXZ);
                    float step = enemy.MoveSpeed * deltaTime;
                    if (step > distXZ) step = distXZ; // Don't overshoot

                    Vector3 nextPosition = enemy.Position + direction * step;

                    // Check if the next position would collide with a wall
                    const float enemyRadius = 1.0f;
                    if (_collisionSystem.CheckCollisionAtPosition(nextPosition, enemyRadius))
                    {
                        // Wall ahead — stop and set colliding state
                        enemy.EnemyState = EnemyState.COLLIDING;

                        // Check if the next position would collide with a door
                        if (_doorSystem.IsDoorBlocking(nextPosition, enemyRadius))
                        {
                            var doorSearchPos = new Vector2(nextPosition.X / LevelData.QuadSize, nextPosition.Z / LevelData.QuadSize);
                            var closestDoor = _doorSystem.FindClosestDoor(doorSearchPos);
                            if (closestDoor != null)
                            {
                                _doorSystem.OpenDoor(closestDoor);
                            }
                        }
                    }
                    else
                    {
                        enemy.Position = nextPosition;

                        // Update rotation to face movement direction
                        enemy.Rotation = MathF.Atan2(direction.X, -direction.Z) - MathF.PI / 2f;
                        enemy.EnemyState = EnemyState.WALKING;
                    }
                }
                else
                {
                    // Snap to target and advance to next waypoint
                    enemy.Position = new Vector3(target.X, enemy.Position.Y, target.Z);
                    enemy.CurrentWaypointIndex = (enemy.CurrentWaypointIndex + 1) % totalStops;
                }
            }

            Vector2 playerEnemyVector = new Vector2(enemy.Position.X - _player.Position.X,
                enemy.Position.Z - _player.Position.Z);

            var playerToEntityAngle = Math.Atan2(playerEnemyVector.X, playerEnemyVector.Y);
            
            // Normalize to [0, 2π) - Atan2 returns [-π, π], so negative values need wrapping
            while (playerToEntityAngle < 0) playerToEntityAngle += 2 * Math.PI;
            while (playerToEntityAngle >= 2 * Math.PI) playerToEntityAngle -= 2 * Math.PI;
            
            var relativeDirection = enemy.Rotation + playerToEntityAngle;

            // Normalize to [0, 2π) - ensures consistent range after addition
            while (relativeDirection < 0) relativeDirection += 2 * Math.PI;
            while (relativeDirection >= 2 * Math.PI) relativeDirection -= 2 * Math.PI;
            
            // Rotate 90 degrees to the right (π/2 radians) to align sprite sheet columns
            var rotatedAngle = relativeDirection + Math.PI / 2;

            // Normalize again after rotation (simpler than the +8 trick)
            while (rotatedAngle >= 2 * Math.PI) rotatedAngle -= 2 * Math.PI;
            var spriteIndex = (int)Math.Round(rotatedAngle / (Math.PI * 2) * 8) % 8;
            
            enemy.FrameColumnIndex = spriteIndex;
            enemy.AngleToPlayer = (float)rotatedAngle;
            enemy.DistanceFromPlayer = playerEnemyVector.Length() / LevelData.QuadSize;

            // For debugging
            if (_inputSystem.GetInputState().IsChangeStatePressed)
            {
                enemy.EnemyState++;
                var state = (int)enemy.EnemyState;
                enemy.EnemyState = (EnemyState)(state % 6);
            }
        }

        // Throttled line-of-sight and FOV polygon update
        _losAccumulator += deltaTime;
        if (_losAccumulator >= LosInterval)
        {
            _losAccumulator -= LosInterval;
            UpdateLineOfSight();
        }
    }

    /// <summary>
    /// Check line of sight for all enemies and regenerate their FOV polygons.
    /// Called at a throttled rate (LosInterval).
    /// </summary>
    private void UpdateLineOfSight()
    {
        if (_mapData == null) return;

        float quadSize = LevelData.QuadSize;
        // Enemy world positions are at tile corners (TileX * QuadSize).
        // Shift by +0.5 to cast from tile centers, matching the visual position.
        var playerTile = new Vector2(
            _player.Position.X / quadSize + 0.5f,
            _player.Position.Z / quadSize + 0.5f);

        var doors = _doorSystem.Doors;

        foreach (var enemy in _enemies)
        {
            var enemyTile = new Vector2(
                enemy.Position.X / quadSize + 0.5f,
                enemy.Position.Z / quadSize + 0.5f);

            // The enemy's facing angle in 2D tile space
            // Enemy.Rotation is set via Atan2(dir.X, -dir.Z) - PI/2 during movement
            // For tile-space (X right, Y down), we use Rotation directly
            float facingAngle = enemy.Rotation;

            // 1. Generate FOV polygon for visualization
            enemy.FovPolygon = LineOfSight.GenerateFovPolygon(
                _mapData, doors, enemyTile, facingAngle,
                enemy.FovHalfAngle * 2f, enemy.SightRange, FovRayCount);

            // 2. Distance check: skip if too far
            float distTiles = Vector2.Distance(enemyTile, playerTile);
            if (distTiles > enemy.SightRange)
            {
                enemy.CanSeePlayer = false;
                continue;
            }

            // 3. FOV angle check: is the player within the enemy's field of view?
            Vector2 toPlayer = playerTile - enemyTile;
            float angleToPlayer = MathF.Atan2(toPlayer.Y, toPlayer.X);
            float angleDiff = NormalizeAngle(angleToPlayer - facingAngle);
            if (MathF.Abs(angleDiff) > enemy.FovHalfAngle)
            {
                enemy.CanSeePlayer = false;
                continue;
            }

            // 4. DDA ray check: is there a clear path?
            bool clearPath = LineOfSight.CanSee(_mapData, doors, enemyTile, playerTile);
            if (!clearPath)
            {
                enemy.CanSeePlayer = false;
                continue;
            }
            enemy.CanSeePlayer = true;
            enemy.EnemyState = EnemyState.NOTICING;
            // 5. Distance-based probability
            // At distance 0 -> 100% chance, at max range -> ~5% chance (linear falloff)
            // float detectionChance = 1f - 0.95f * (distTiles / enemy.SightRange);
            // detectionChance = Math.Clamp(detectionChance, 0.05f, 1f);

            // enemy.CanSeePlayer = _random.NextSingle() <= detectionChance;
        }
    }

    /// <summary>
    /// Normalize an angle to [-PI, PI] range.
    /// </summary>
    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI) angle -= 2f * MathF.PI;
        while (angle < -MathF.PI) angle += 2f * MathF.PI;
        return angle;
    }
}