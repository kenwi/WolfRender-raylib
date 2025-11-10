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

    public static void DrawSpriteTexture(
        Texture2D texture,
        Vector3 position,
        Vector3 cameraPosition,
        Color color,
        float width = 4f,
        float height = 4f,
        float angle = 4f)
    {
        // Calculate direction from sprite to camera (for billboard effect)
        var directionToCamera = cameraPosition - position;
        directionToCamera.Y = 0; // Keep sprite vertical (only rotate around Y-axis)
        
        // Normalize the direction
        var dirLength = directionToCamera.Length();
        if (dirLength < 0.001f)
        {
            directionToCamera = new Vector3(0, 0, 1); // Default forward if too close
        }
        else
        {
            directionToCamera = directionToCamera / dirLength;
        }

        // Calculate right and up vectors for the billboard
        var right = Vector3.Cross(directionToCamera, Vector3.UnitY);
        var rightLength = right.Length();
        if (rightLength > 0.001f)
        {
            right = right / rightLength;
        }
        else
        {
            right = Vector3.UnitX; // Fallback
        }

        var up = Vector3.UnitY;

        // Apply rotation around the up vector (Y-axis)
        var cosAngle = MathF.Cos(angle);
        var sinAngle = MathF.Sin(angle);
        
        // Rotate the right vector around the up vector
        var rotatedRight = new Vector3(
            right.X * cosAngle - right.Z * sinAngle,
            right.Y,
            right.X * sinAngle + right.Z * cosAngle
        );

        // Calculate the four corners of the sprite quad
        var halfWidth = rotatedRight * (width / 2);
        var halfHeight = up * (height / 2);

        var topLeft = position - halfWidth + halfHeight;
        var topRight = position + halfWidth + halfHeight;
        var bottomRight = position + halfWidth - halfHeight;
        var bottomLeft = position - halfWidth - halfHeight;

        // Draw the quad
        Rlgl.SetTexture(texture.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(color.R, color.G, color.B, color.A);

        // Calculate normal (facing camera)
        var normal = directionToCamera;
        Rlgl.Normal3f(normal.X, normal.Y, normal.Z);

        // Top-left
        Rlgl.TexCoord2f(0.0f, 0.0f);
        Rlgl.Vertex3f(topLeft.X, topLeft.Y, topLeft.Z);

        // Top-right
        Rlgl.TexCoord2f(1.0f, 0.0f);
        Rlgl.Vertex3f(topRight.X, topRight.Y, topRight.Z);

        // Bottom-right
        Rlgl.TexCoord2f(1.0f, 1.0f);
        Rlgl.Vertex3f(bottomRight.X, bottomRight.Y, bottomRight.Z);

        // Bottom-left
        Rlgl.TexCoord2f(0.0f, 1.0f);
        Rlgl.Vertex3f(bottomLeft.X, bottomLeft.Y, bottomLeft.Z);

        Rlgl.End();
        Rlgl.SetTexture(0);
    }
}

