using Raylib_cs;

namespace Game;

/// <summary>
/// Shared level data stored as raw tile ID arrays.
/// Both the game and editor scenes reference this, and serialization operates on these arrays directly.
/// </summary>
public class MapData
{
    public uint[] Floor { get; set; } = Array.Empty<uint>();
    public uint[] Walls { get; set; } = Array.Empty<uint>();
    public uint[] Ceiling { get; set; } = Array.Empty<uint>();
    public uint[] Doors { get; set; } = Array.Empty<uint>();
    public List<Texture2D> Textures { get; set; } = new();
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>
    /// Get the tile ID at a given position for a specific layer array.
    /// Returns 0 if out of bounds.
    /// </summary>
    public uint GetTile(uint[] layer, int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0;

        return layer[Width * y + x];
    }
}
