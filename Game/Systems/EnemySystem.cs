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
    private const int FovRayCount = 48;

    // Movement / behavior constants
    private const float ArrivalThreshold = 0.5f;
    private const float EnemyCollisionRadius = 1.0f;
    private const float TurnSpeed = 4f; // radians per second when facing player
    private const float NoticingDuration = 0.8f; // seconds before transitioning to ATTACKING

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
        // Throttled line-of-sight and FOV polygon update
        _losAccumulator += deltaTime;
        if (_losAccumulator >= LosInterval)
        {
            _losAccumulator -= LosInterval;
            UpdateLineOfSight();
        }

        foreach (var enemy in _enemies)
        {
            UpdateBehavior(enemy, deltaTime);
            UpdateSpriteFrame(enemy);
        }

        // Debug: cycle enemy state
        if (_inputSystem.GetInputState().IsChangeStatePressed)
        {
            foreach (var enemy in _enemies)
            {
                enemy.EnemyState++;
                var state = (int)enemy.EnemyState;
                enemy.EnemyState = (EnemyState)(state % 6);
            }
        }
    }

    private void UpdateBehavior(Enemy enemy, float deltaTime)
    {
        enemy.StateTimer += deltaTime;

        switch (enemy.EnemyState)
        {
            case EnemyState.IDLE:
                OnIdle(enemy, deltaTime);
                break;

            case EnemyState.WALKING:
                OnWalking(enemy, deltaTime);
                break;

            case EnemyState.NOTICING:
                OnNoticing(enemy, deltaTime);
                break;

            case EnemyState.ATTACKING:
                OnAttacking(enemy, deltaTime);
                break;

            case EnemyState.COLLIDING:
                // Colliding is transient — patrol will overwrite it next frame
                if (enemy.HasPatrolPath)
                    UpdatePatrol(enemy, deltaTime);
                break;

            default:
                break;
        }
    }

    private void OnIdle(Enemy enemy, float deltaTime)
    {
        if (enemy.CanSeePlayer)
        {
            enemy.TransitionTo(EnemyState.NOTICING);
            return;
        }

        // Start patrolling if we have a path
        if (enemy.HasPatrolPath)
            enemy.TransitionTo(EnemyState.WALKING);
    }

    private void OnWalking(Enemy enemy, float deltaTime)
    {
        if (enemy.CanSeePlayer)
        {
            enemy.TransitionTo(EnemyState.NOTICING);
            return;
        }

        if (enemy.HasPatrolPath)
            UpdatePatrol(enemy, deltaTime);
    }

    private void OnNoticing(Enemy enemy, float deltaTime)
    {
        RotateTowardPlayer(enemy, deltaTime);

        if (!enemy.CanSeePlayer)
        {
            // Lost sight — return to normal behavior
            enemy.TransitionTo(enemy.HasPatrolPath ? EnemyState.WALKING : EnemyState.IDLE);
            return;
        }

        if (enemy.StateTimer >= NoticingDuration)
        {
            enemy.TransitionTo(EnemyState.ATTACKING);
        }
    }

    private void OnAttacking(Enemy enemy, float deltaTime)
    {
        RotateTowardPlayer(enemy, deltaTime);

        if (!enemy.CanSeePlayer)
        {
            // Lost sight — return to normal behavior
            enemy.TransitionTo(enemy.HasPatrolPath ? EnemyState.WALKING : EnemyState.IDLE);
        }

        // TODO: fire at player, play attack animation, etc.
    }

    /// <summary>
    /// Smoothly rotate the enemy to face the player's current position.
    /// </summary>
    private void RotateTowardPlayer(Enemy enemy, float deltaTime)
    {
        Vector3 toPlayer = _player.Position - enemy.Position;
        float targetAngle = MathF.Atan2(toPlayer.X, -toPlayer.Z) - MathF.PI / 2f;

        float diff = NormalizeAngle(targetAngle - enemy.Rotation);
        float maxStep = TurnSpeed * deltaTime;

        if (MathF.Abs(diff) <= maxStep)
            enemy.Rotation = targetAngle;
        else
            enemy.Rotation += MathF.Sign(diff) * maxStep;
    }

    private void UpdatePatrol(Enemy enemy, float deltaTime)
    {
        // Build the full loop: origin -> waypoints -> back to origin
        int totalStops = enemy.PatrolPath.Count + 1;
        int idx = enemy.CurrentWaypointIndex % totalStops;

        Vector3 target = idx < enemy.PatrolPath.Count
            ? enemy.PatrolPath[idx]
            : enemy.PatrolOrigin;

        Vector3 toTarget = target - enemy.Position;
        float distXZ = MathF.Sqrt(toTarget.X * toTarget.X + toTarget.Z * toTarget.Z);

        if (distXZ > ArrivalThreshold)
        {
            MoveToward(enemy, toTarget, distXZ, deltaTime);
        }
        else
        {
            // Snap to target and advance to next waypoint
            enemy.Position = new Vector3(target.X, enemy.Position.Y, target.Z);
            enemy.CurrentWaypointIndex = (enemy.CurrentWaypointIndex + 1) % totalStops;
        }
    }

    /// <summary>
    /// Move the enemy toward a target direction, handling wall and door collisions.
    /// </summary>
    private void MoveToward(Enemy enemy, Vector3 toTarget, float distXZ, float deltaTime)
    {
        Vector3 direction = new Vector3(toTarget.X / distXZ, 0, toTarget.Z / distXZ);
        float step = MathF.Min(enemy.MoveSpeed * deltaTime, distXZ);
        Vector3 nextPosition = enemy.Position + direction * step;

        if (_collisionSystem.CheckCollisionAtPosition(nextPosition, EnemyCollisionRadius))
        {
            enemy.EnemyState = EnemyState.COLLIDING;
            TryOpenBlockingDoor(nextPosition);
        }
        else
        {
            enemy.Position = nextPosition;
            enemy.Rotation = MathF.Atan2(direction.X, -direction.Z) - MathF.PI / 2f;
            enemy.EnemyState = EnemyState.WALKING;
        }
    }

    /// <summary>
    /// If a door is blocking the enemy's path, try to open it.
    /// </summary>
    private void TryOpenBlockingDoor(Vector3 position)
    {
        if (!_doorSystem.IsDoorBlocking(position, EnemyCollisionRadius))
            return;

        var doorSearchPos = new Vector2(
            position.X / LevelData.QuadSize,
            position.Z / LevelData.QuadSize);
        var closestDoor = _doorSystem.FindClosestDoor(doorSearchPos);
        if (closestDoor != null)
        {
            _doorSystem.OpenDoor(closestDoor);
        }
    }

    /// <summary>
    /// Calculate the sprite column index based on the angle between the player's
    /// view direction and the enemy's facing direction.
    /// </summary>
    private void UpdateSpriteFrame(Enemy enemy)
    {
        Vector2 playerEnemyVector = new Vector2(
            enemy.Position.X - _player.Position.X,
            enemy.Position.Z - _player.Position.Z);

        var playerToEntityAngle = Math.Atan2(playerEnemyVector.X, playerEnemyVector.Y);

        // Normalize to [0, 2π)
        while (playerToEntityAngle < 0) playerToEntityAngle += 2 * Math.PI;
        while (playerToEntityAngle >= 2 * Math.PI) playerToEntityAngle -= 2 * Math.PI;

        var relativeDirection = enemy.Rotation + playerToEntityAngle;

        // Normalize to [0, 2π)
        while (relativeDirection < 0) relativeDirection += 2 * Math.PI;
        while (relativeDirection >= 2 * Math.PI) relativeDirection -= 2 * Math.PI;

        // Rotate 90 degrees to align sprite sheet columns
        var rotatedAngle = relativeDirection + Math.PI / 2;
        while (rotatedAngle >= 2 * Math.PI) rotatedAngle -= 2 * Math.PI;

        var spriteIndex = (int)Math.Round(rotatedAngle / (Math.PI * 2) * 8) % 8;

        enemy.FrameColumnIndex = spriteIndex;
        enemy.AngleToPlayer = (float)rotatedAngle;
        enemy.DistanceFromPlayer = playerEnemyVector.Length() / LevelData.QuadSize;
    }

    /// <summary>
    /// Check line of sight for all enemies and regenerate their FOV polygons.
    /// Called at a throttled rate (LosInterval).
    /// </summary>
    private void UpdateLineOfSight()
    {
        if (_mapData == null) return;

        float quadSize = LevelData.QuadSize;
        var playerTile = new Vector2(
            _player.Position.X / quadSize + 0.5f,
            _player.Position.Z / quadSize + 0.5f);

        var doors = _doorSystem.Doors;

        foreach (var enemy in _enemies)
        {
            var enemyTile = new Vector2(
                enemy.Position.X / quadSize + 0.5f,
                enemy.Position.Z / quadSize + 0.5f);

            float facingAngle = enemy.Rotation;

            // Generate FOV polygon for visualization
            enemy.FovPolygon = LineOfSight.GenerateFovPolygon(
                _mapData, doors, enemyTile, facingAngle,
                enemy.FovHalfAngle * 2f, enemy.SightRange, FovRayCount);

            // Distance check
            float distTiles = Vector2.Distance(enemyTile, playerTile);
            if (distTiles > enemy.SightRange)
            {
                enemy.CanSeePlayer = false;
                continue;
            }

            // FOV angle check
            Vector2 toPlayer = playerTile - enemyTile;
            float angleToPlayer = MathF.Atan2(toPlayer.Y, toPlayer.X);
            float angleDiff = NormalizeAngle(angleToPlayer - facingAngle);
            if (MathF.Abs(angleDiff) > enemy.FovHalfAngle)
            {
                enemy.CanSeePlayer = false;
                continue;
            }

            var hasSeenPlayer = enemy.CanSeePlayer;
            // DDA ray check
            enemy.CanSeePlayer = LineOfSight.CanSee(_mapData, doors, enemyTile, playerTile);
            if (hasSeenPlayer == true && enemy.CanSeePlayer == false)
            {
                // lost track of the player
                ;
            }
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
