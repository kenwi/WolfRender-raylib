using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Game.Utilities;

public static class PrimitiveRenderer
{
    public static void DrawCubeTexture(
        Texture2D texture,
        Vector3 position,
        float width,
        float height,
        float length,
        Color color)
    {
        float x = position.X;
        float y = position.Y;
        float z = position.Z;

        Rlgl.SetTexture(texture.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(color.R, color.G, color.B, color.A);

        // Front Face
        Rlgl.Normal3f(0.0f, 0.0f, 1.0f);
        Rlgl.TexCoord2f(0.0f, 0.0f);
        Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
        Rlgl.TexCoord2f(1.0f, 0.0f);
        Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
        Rlgl.TexCoord2f(1.0f, 1.0f);
        Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
        Rlgl.TexCoord2f(0.0f, 1.0f);
        Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);

        // Back Face
        Rlgl.Normal3f(0.0f, 0.0f, -1.0f);
        Rlgl.TexCoord2f(1.0f, 0.0f);
        Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);
        Rlgl.TexCoord2f(1.0f, 1.0f);
        Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);
        Rlgl.TexCoord2f(0.0f, 1.0f);
        Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);
        Rlgl.TexCoord2f(0.0f, 0.0f);
        Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);

        // // Top Face
        // Rlgl.Normal3f(0.0f, 1.0f, 0.0f);
        // Rlgl.TexCoord2f(0.0f, 1.0f);
        // Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);
        // Rlgl.TexCoord2f(0.0f, 0.0f);
        // Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
        // Rlgl.TexCoord2f(1.0f, 0.0f);
        // Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
        // Rlgl.TexCoord2f(1.0f, 1.0f);
        // Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);

        // // Bottom Face
        // Rlgl.Normal3f(0.0f, -1.0f, 0.0f);
        // Rlgl.TexCoord2f(1.0f, 1.0f);
        // Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);
        // Rlgl.TexCoord2f(0.0f, 1.0f);
        // Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
        // Rlgl.TexCoord2f(0.0f, 0.0f);
        // Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
        // Rlgl.TexCoord2f(1.0f, 0.0f);
        // Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);

        // Right face
        Rlgl.Normal3f(1.0f, 0.0f, 0.0f);
        Rlgl.TexCoord2f(1.0f, 0.0f);
        Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
        Rlgl.TexCoord2f(1.0f, 1.0f);
        Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);
        Rlgl.TexCoord2f(0.0f, 1.0f);
        Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
        Rlgl.TexCoord2f(0.0f, 0.0f);
        Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);

        // Left Face
        Rlgl.Normal3f(-1.0f, 0.0f, 0.0f);
        Rlgl.TexCoord2f(0.0f, 0.0f);
        Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);
        Rlgl.TexCoord2f(1.0f, 0.0f);
        Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
        Rlgl.TexCoord2f(1.0f, 1.0f);
        Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
        Rlgl.TexCoord2f(0.0f, 1.0f);
        Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);

        Rlgl.End();
        Rlgl.SetTexture(0);
    }

    public static void DrawFloorTexture(
        Texture2D texture,
        Vector3 position,
        float width,
        float height,
        float length,
        Color color)
    {
        float x = position.X;
        float y = position.Y;
        float z = position.Z;

        Rlgl.SetTexture(texture.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(color.R, color.G, color.B, color.A);

        // Top Face
        Rlgl.Normal3f(0.0f, 1.0f, 0.0f);
        Rlgl.TexCoord2f(0.0f, 1.0f);
        Rlgl.Vertex3f(x - width / 2, y + height / 2, z - length / 2);
        Rlgl.TexCoord2f(0.0f, 0.0f);
        Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
        Rlgl.TexCoord2f(1.0f, 0.0f);
        Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
        Rlgl.TexCoord2f(1.0f, 1.0f);
        Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);

        Rlgl.End();
        Rlgl.SetTexture(0);
    }

    public static void DrawCeilingTexture(
        Texture2D texture,
        Vector3 position,
        float width,
        float height,
        float length,
        Color color)
    {
        float x = position.X;
        float y = position.Y;
        float z = position.Z;

        Rlgl.SetTexture(texture.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(color.R, color.G, color.B, color.A);

        // Bottom Face
        Rlgl.Normal3f(0.0f, -1.0f, 0.0f);
        Rlgl.TexCoord2f(1.0f, 1.0f);
        Rlgl.Vertex3f(x - width / 2, y - height / 2, z - length / 2);
        Rlgl.TexCoord2f(0.0f, 1.0f);
        Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
        Rlgl.TexCoord2f(0.0f, 0.0f);
        Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
        Rlgl.TexCoord2f(1.0f, 0.0f);
        Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);

        Rlgl.End();
        Rlgl.SetTexture(0);
    }

    public static void DrawDoorTextureH(
        Texture2D texture,
        Vector3 position,
        float width,
        float height,
        float length,
        Color color)
    {
        float x = position.X;
        float y = position.Y;
        float z = position.Z + (0.5f * 4);

        Rlgl.SetTexture(texture.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(color.R, color.G, color.B, color.A);

        // Front Face
        Rlgl.Normal3f(0.0f, 0.0f, 1.0f);
        Rlgl.TexCoord2f(0.0f, 0.0f);
        Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
        Rlgl.TexCoord2f(1.0f, 0.0f);
        Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
        Rlgl.TexCoord2f(1.0f, 1.0f);
        Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
        Rlgl.TexCoord2f(0.0f, 1.0f);
        Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);

        // Back Face
        Rlgl.Normal3f(0.0f, 0.0f, -1.0f);
        Rlgl.TexCoord2f(1.0f, 0.0f);
        Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
        Rlgl.TexCoord2f(1.0f, 1.0f);
        Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
        Rlgl.TexCoord2f(0.0f, 1.0f);
        Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
        Rlgl.TexCoord2f(0.0f, 0.0f);
        Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);

        Rlgl.End();
        Rlgl.SetTexture(0);
    }

    public static void DrawDoorTextureV(
        Texture2D texture,
        Vector3 position,
        float width,
        float height,
        float length,
        Color color)
    {
        float x = position.X - (0.5f * 4);
        float y = position.Y;
        float z = position.Z;

        Rlgl.SetTexture(texture.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(color.R, color.G, color.B, color.A);


        // Right face
        Rlgl.Normal3f(1.0f, 0.0f, 0.0f);
        Rlgl.TexCoord2f(1.0f, 0.0f);
        Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
        Rlgl.TexCoord2f(1.0f, 1.0f);
        Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);
        Rlgl.TexCoord2f(0.0f, 1.0f);
        Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
        Rlgl.TexCoord2f(0.0f, 0.0f);
        Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);

        // // Left Face
        Rlgl.Normal3f(-1.0f, 0.0f, 0.0f);
        Rlgl.TexCoord2f(0.0f, 0.0f);
        Rlgl.Vertex3f(x + width / 2, y - height / 2, z - length / 2);
        Rlgl.TexCoord2f(1.0f, 0.0f);
        Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
        Rlgl.TexCoord2f(1.0f, 1.0f);
        Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
        Rlgl.TexCoord2f(0.0f, 1.0f);
        Rlgl.Vertex3f(x + width / 2, y + height / 2, z - length / 2);

        // // Front Face
        // Rlgl.Normal3f(0.0f, 0.0f, 1.0f);
        // Rlgl.TexCoord2f(0.0f, 0.0f);
        // Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
        // Rlgl.TexCoord2f(1.0f, 0.0f);
        // Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);
        // Rlgl.TexCoord2f(1.0f, 1.0f);
        // Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
        // Rlgl.TexCoord2f(0.0f, 1.0f);
        // Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);

        // // Back Face
        // Rlgl.Normal3f(0.0f, 0.0f, -1.0f);
        // Rlgl.TexCoord2f(1.0f, 0.0f);
        // Rlgl.Vertex3f(x - width / 2, y - height / 2, z + length / 2);
        // Rlgl.TexCoord2f(1.0f, 1.0f);
        // Rlgl.Vertex3f(x - width / 2, y + height / 2, z + length / 2);
        // Rlgl.TexCoord2f(0.0f, 1.0f);
        // Rlgl.Vertex3f(x + width / 2, y + height / 2, z + length / 2);
        // Rlgl.TexCoord2f(0.0f, 0.0f);
        // Rlgl.Vertex3f(x + width / 2, y - height / 2, z + length / 2);

        Rlgl.End();
        Rlgl.SetTexture(0);
    }
}

