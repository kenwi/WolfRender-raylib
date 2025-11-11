using System.Data;
using System.Numerics;
using Game.Entities;
using Game.Utilities;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Game.Systems;

public class AnimationSystem
{
    private readonly Texture2D _texture;
    private readonly Player _player;
    private readonly EnemySystem _enemySystem;
    
    // change this
    private int _shootingAnimationIndex = 0;
    private int _dyingAnimationIndex = 0;
    
    public AnimationSystem(Texture2D texture, Player player, EnemySystem enemySystem)
    {
        _texture = texture;
        _player = player;
        _enemySystem = enemySystem;
    }

    public void Update(float deltaTime)
    {
        var spriteSize = 64;
        var padding = 1;

        foreach (var enemy in _enemySystem.Enemies)
        {
            var frameColumnIndex = enemy.FrameColumnIndex;
            var frameRowIndex = enemy.FrameRowIndex;
            
            var currentColumnPixel = (frameColumnIndex % 8)  * (spriteSize + padding);
            var currentRowPixel = (frameRowIndex % 5) * (spriteSize + padding);
            
            // Animations have different number of frames and are located in different rows
            // so we need to adjust the indexes and calculate correct row and column pixel
            switch (enemy.EnemyState)
            {
                case EnemyState.IDLE:
                    enemy.FrameRowIndex = 0;
                    break;
                case EnemyState.WALKING:
                    if (enemy.AnimationTimer >= 1)
                    {
                        enemy.FrameRowIndex++;
                        enemy.AnimationTimer = 0;
                    }
                    currentRowPixel = (1 + frameRowIndex % 4) * (spriteSize + padding);
                    break;
                case EnemyState.NOTICING:
                    currentColumnPixel = 0  * (spriteSize + padding);
                    currentRowPixel = 6 * (spriteSize + padding);
                    break;
                case EnemyState.FLEEING:
                    break;
                case EnemyState.ATTACKING:
                    if (enemy.AnimationTimer >= 1)
                    {
                        enemy.AnimationTimer = 0;
                        _shootingAnimationIndex++;
                    }
                    currentColumnPixel = (_shootingAnimationIndex % 3) * (spriteSize + padding);
                    currentRowPixel = 6 * (spriteSize + padding);
                    
                    break;
                case EnemyState.DYING:
                    if (enemy.AnimationTimer >= 1)
                    {
                        enemy.AnimationTimer = 0;
                        _dyingAnimationIndex++;
                    }
                    currentColumnPixel = (_dyingAnimationIndex % 5) * (spriteSize + padding);
                    currentRowPixel = 5 * (spriteSize + padding);
                    break;
            }
            
            // Create the new animation frame and load it into the enemy
            enemy.FrameRect = new Rectangle(currentColumnPixel, currentRowPixel, spriteSize, spriteSize);
            enemy.AnimationTimer += deltaTime * 2;
        }
    }

    public void Render()
    {
        foreach (var enemy in _enemySystem.Enemies)
        {
            PrimitiveRenderer.DrawSpriteTexture(_texture,
                enemy.Position,
                _player.Camera.Position,
                Color.White,
                frameRect: enemy.FrameRect);            
        }
    }
}