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

    public List<Enemy> Enemies => _enemies;
    public EnemySystem(Player player,InputSystem inputSystem, CollisionSystem collisionSystem, DoorSystem doorSystem)
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
    public void Rebuild(List<EnemyPlacement> placements)
    {
        _enemies.Clear();
        
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
    }
}