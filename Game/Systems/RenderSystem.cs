using System.Numerics;
using Game.Entities;
using Game.Utilities;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Game.Systems;

public class RenderSystem
{
    private readonly LevelData _level;
    private readonly List<Texture2D> _textures;
    private readonly float _drawDistance;
    private readonly float _maxAngleDot;
    private const float TileSize = 4.0f;

    public RenderSystem(LevelData level, List<Texture2D> textures, float drawDistance = 15.0f, float maxAngleDot = 0.4f)
    {
        _level = level;
        _textures = textures;
        _drawDistance = drawDistance;
        _maxAngleDot = maxAngleDot;
    }

    public void Render(Player player)
    {
        Vector3 cameraForward = Vector3.Normalize(player.Camera.Target - player.Camera.Position);
        Vector3 cameraPosXZ = new Vector3(player.Camera.Position.X, 0, player.Camera.Position.Z);
        float drawDistanceWorld = _drawDistance * TileSize;

        int cameraTileX = (int)(player.Camera.Position.X / TileSize + 0.5f);
        int cameraTileY = (int)(player.Camera.Position.Z / TileSize + 0.5f);
        int minX = Math.Max(0, cameraTileX - (int)_drawDistance);
        int maxX = Math.Min(_level.Width - 1, cameraTileX + (int)_drawDistance);
        int minY = Math.Max(0, cameraTileY - (int)_drawDistance);
        int maxY = Math.Min(_level.Height - 1, cameraTileY + (int)_drawDistance);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector3 tilePos = new Vector3(x * TileSize, 0, y * TileSize);
                Vector3 toTile = tilePos - cameraPosXZ;
                float distance = toTile.Length();

                if (distance > drawDistanceWorld) continue;

                Vector3 toTileNormalized = Vector3.Normalize(toTile);
                float dot = Vector3.Dot(cameraForward, toTileNormalized);

                if (dot > _maxAngleDot || distance < 10)
                {
                    RenderTile(x, y, tilePos);
                }
            }
        }

   }

    private void RenderTile(int x, int y, Vector3 worldPos)
    {
        // Draw walls
        var wallTile = _level.GetWallTile(x, y);
        if (wallTile != 0 && wallTile <= _textures.Count)
        {
            PrimitiveRenderer.DrawCubeTexture(
                _textures[(int)wallTile - 1],
                new Vector3(worldPos.X, 2, worldPos.Z),
                4.0f, 4.0f, 4.0f,
                Color.White
            );
        }

        // Draw floors
        var floorTile = _level.GetFloorTile(x, y);
        if (floorTile != 0 && floorTile <= _textures.Count)
        {
            PrimitiveRenderer.DrawFloorTexture(
                _textures[(int)floorTile - 1],
                new Vector3(worldPos.X, -2, worldPos.Z),
                4.0f, 4.0f, 4.0f,
                Color.White
            );
        }

        // Draw ceilings
        var ceilingTile = _level.GetCeilingTile(x, y);
        if (ceilingTile != 0 && ceilingTile <= _textures.Count)
        {
            PrimitiveRenderer.DrawCeilingTexture(
                _textures[(int)ceilingTile - 1],
                new Vector3(worldPos.X, 6f, worldPos.Z),
                4.0f, 4.0f, 4.0f,
                Color.White
            );
        }
    }
}

