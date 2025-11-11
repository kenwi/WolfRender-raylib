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
    
    public EnemySystem(Player player, InputSystem inputSystem /* remove this */)
    {
        _inputSystem = inputSystem;
        _player = player;
        
        // Static implementation to be enhanced
        _enemies = new List<Enemy>()
        {
            new EnemyGuard()
            {
                Position = new Vector3(27 * 4, 2, 27 * 4),
                Rotation = 0
            },
            
            new EnemyGuard()
            {
                Position = new Vector3(30 * 4, 2, 26 * 4),
                Rotation = 0
            }
        };
    }

    public void Update(float deltaTime)
    {
        foreach (var enemy in _enemies)
        {
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