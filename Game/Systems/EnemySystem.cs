using System.Numerics;
using Game.Entities;
using Game.Utilities;

namespace Game.Systems;

public class EnemySystem
{
    private readonly Player _player;
    private readonly List<Enemy> _enemies;
    private readonly InputSystem _inputSystem;
    public List<Enemy> Enemies => _enemies;
    
    public EnemySystem(Player player, InputSystem inputSystem)
    {
        _inputSystem = inputSystem;
        _player = player;
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
            var enemy = new EnemyGuard
            {
                Position = new Vector3(
                    placement.TileX * LevelData.QuadSize,
                    2f,
                    placement.TileY * LevelData.QuadSize),
                Rotation = placement.Rotation,
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
            // Patrol path check (placeholder for future movement logic)
            if (enemy.HasPatrolPath)
            {
                // Enemy has a patrol path to follow
                // TODO: Implement patrol movement
                ;
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