using System.ComponentModel;
using DotTiled;

namespace Game.Utilities;

public class LevelData
{
    private readonly TileLayer _walls;
    private readonly TileLayer _floor;
    private readonly TileLayer _ceiling;
    private const int MapWidth = 64;
    public int Width => (int)_walls.Width;
    public int Height => (int)_walls.Height;
    public static int QuadSize => 4;
    public static int DrawedQuads = 0;
    public static int TileCount = MapWidth * MapWidth;

    public LevelData(TileLayer walls, TileLayer floor, TileLayer ceiling)
    {
        _walls = walls;
        _floor = floor;
        _ceiling = ceiling;
    }

    public static int GetIndex(int col, int row, int width = LevelData.MapWidth) => width * row + col;
    
    public static (int col, int row) GetColRow(int index, int width = LevelData.MapWidth) => (index % width, index / width);
    
    public bool IsWallAt(float worldX, float worldZ)
    {
        int tileX = (int)(worldX / 4 + 0.5f);
        int tileY = (int)(worldZ / 4 + 0.5f);

        if (tileX < 0 || tileX >= Width || tileY < 0 || tileY >= Height)
            return true;

        int index = GetIndex(tileX, tileY, MapWidth);
        return _walls?.Data?.Value?.GlobalTileIDs?.Value?[index] != 0;
    }

    public uint GetWallTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0;

        int index = GetIndex(x, y, MapWidth);
        return _walls?.Data?.Value?.GlobalTileIDs?.Value?[index] ?? 0;
    }

    public uint GetFloorTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0;

        int index = GetIndex(x, y, MapWidth);
        return _floor?.Data?.Value?.GlobalTileIDs?.Value?[index] ?? 0;
    }

    public uint GetCeilingTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0;

        int index = GetIndex(x, y, MapWidth);
        return _ceiling?.Data?.Value?.GlobalTileIDs?.Value?[index] ?? 0;
    }
}

