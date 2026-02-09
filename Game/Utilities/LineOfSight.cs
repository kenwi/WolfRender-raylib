using System.Numerics;
using Game.Entities;

namespace Game.Utilities;

/// <summary>
/// Tile-based DDA raycasting for line-of-sight checks and FOV visualization.
/// </summary>
public static class LineOfSight
{
    /// <summary>
    /// Cast a ray from start to end in tile space using DDA.
    /// Returns true if the ray reaches the end without hitting a wall or closed door.
    /// </summary>
    public static bool CanSee(MapData mapData, List<Door> doors, Vector2 startTile, Vector2 endTile)
    {
        var hitPos = CastRay(mapData, doors, startTile, endTile - startTile, Vector2.Distance(startTile, endTile));
        // If the ray traveled the full distance (within tolerance), line of sight is clear
        return Vector2.Distance(startTile, hitPos) >= Vector2.Distance(startTile, endTile) - 0.1f;
    }

    /// <summary>
    /// Cast a single ray from origin in the given direction using DDA.
    /// Returns the tile-space position where the ray hit a wall/door or reached maxDistance.
    /// </summary>
    public static Vector2 CastRay(MapData mapData, List<Door> doors, Vector2 origin, Vector2 direction, float maxDistance)
    {
        if (direction.LengthSquared() < 0.0001f)
            return origin;

        direction = Vector2.Normalize(direction);

        float posX = origin.X;
        float posY = origin.Y;

        // Current tile
        int mapX = (int)MathF.Floor(posX);
        int mapY = (int)MathF.Floor(posY);

        // Direction of step (+1 or -1)
        int stepX = direction.X >= 0 ? 1 : -1;
        int stepY = direction.Y >= 0 ? 1 : -1;

        // Distance along ray to cross one full tile in X or Y
        float deltaDistX = MathF.Abs(direction.X) > 0.0001f ? MathF.Abs(1f / direction.X) : float.MaxValue;
        float deltaDistY = MathF.Abs(direction.Y) > 0.0001f ? MathF.Abs(1f / direction.Y) : float.MaxValue;

        // Distance to the first X and Y tile boundary
        float sideDistX, sideDistY;
        if (direction.X < 0)
            sideDistX = (posX - mapX) * deltaDistX;
        else
            sideDistX = (mapX + 1.0f - posX) * deltaDistX;

        if (direction.Y < 0)
            sideDistY = (posY - mapY) * deltaDistY;
        else
            sideDistY = (mapY + 1.0f - posY) * deltaDistY;

        // DDA loop
        float distanceTraveled = 0f;
        while (distanceTraveled < maxDistance)
        {
            // Step to the next tile boundary
            if (sideDistX < sideDistY)
            {
                distanceTraveled = sideDistX;
                sideDistX += deltaDistX;
                mapX += stepX;
            }
            else
            {
                distanceTraveled = sideDistY;
                sideDistY += deltaDistY;
                mapY += stepY;
            }

            if (distanceTraveled > maxDistance)
                break;

            // Check bounds
            if (mapX < 0 || mapX >= mapData.Width || mapY < 0 || mapY >= mapData.Height)
                return origin + direction * distanceTraveled;

            // Check wall
            if (mapData.GetTile(mapData.Walls, mapX, mapY) > 0)
                return origin + direction * distanceTraveled;

            // Check closed doors at this tile
            if (IsDoorBlockingTile(doors, mapX, mapY))
                return origin + direction * distanceTraveled;
        }

        // Ray reached max distance without hitting anything
        return origin + direction * maxDistance;
    }

    /// <summary>
    /// Check if there is a non-open door at the given tile coordinates.
    /// </summary>
    private static bool IsDoorBlockingTile(List<Door> doors, int tileX, int tileY)
    {
        foreach (var door in doors)
        {
            if (door.DoorState == DoorState.OPEN)
                continue;

            int doorTileX = (int)MathF.Round(door.StartPosition.X);
            int doorTileY = (int)MathF.Round(door.StartPosition.Y);

            if (doorTileX == tileX && doorTileY == tileY)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Generate FOV polygon endpoints by casting rays in a fan.
    /// Returns tile-space endpoints that form the FOV polygon (first point is the origin).
    /// </summary>
    public static List<Vector2> GenerateFovPolygon(
        MapData mapData, List<Door> doors,
        Vector2 originTile, float facingAngle,
        float fovAngle, float maxDistance, int rayCount)
    {
        var points = new List<Vector2>(rayCount + 1);
        points.Add(originTile); // Origin is the first vertex

        float halfFov = fovAngle / 2f;
        float startAngle = facingAngle - halfFov;
        float angleStep = fovAngle / (rayCount - 1);

        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + angleStep * i;
            var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var hitPoint = CastRay(mapData, doors, originTile, dir, maxDistance);
            points.Add(hitPoint);
        }

        return points;
    }
}
