using System.Numerics;
using Game.Entities;
using Game.Utilities;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Game.Systems;

public class CameraSystem
{
    private readonly CollisionSystem _collisionSystem;
    private const float MouseSensitivity = 0.003f;

    public CameraSystem(CollisionSystem collisionSystem)
    {
        _collisionSystem = collisionSystem;
    }

    public void Update(Player player, bool isMouseFree, Vector2 mouseDelta)
    {
        var camera = player.Camera;

        // Store old position before movement
        Vector3 oldPosition = camera.Position;
        Vector3 oldTarget = camera.Target;

        // Handle rotation from mouse input (only if not mouse-free)
        if (!isMouseFree)
        {
            // Calculate rotation angles from mouse delta
            float yaw = -mouseDelta.X * MouseSensitivity;
            float pitch = -mouseDelta.Y * MouseSensitivity;

            // Get current look direction
            Vector3 forward = Vector3.Normalize(camera.Target - camera.Position);
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, camera.Up));

            // Apply yaw (horizontal rotation)
            Matrix4x4 yawMatrix = Matrix4x4.CreateFromAxisAngle(camera.Up, yaw);
            forward = Vector3.Transform(forward, yawMatrix);

            // Apply pitch (vertical rotation) - limit pitch to avoid gimbal lock
            Vector3 pitchAxis = Vector3.Cross(forward, camera.Up);
            if (pitchAxis.Length() > 0.001f)
            {
                pitchAxis = Vector3.Normalize(pitchAxis);
                Matrix4x4 pitchMatrix = Matrix4x4.CreateFromAxisAngle(pitchAxis, pitch);
                forward = Vector3.Transform(forward, pitchMatrix);

                // Limit pitch to prevent flipping
                float dot = Vector3.Dot(forward, camera.Up);
                if (Math.Abs(dot) > 0.98f)
                {
                    // Revert pitch if too steep
                    forward = Vector3.Normalize(camera.Target - camera.Position);
                    forward = Vector3.Transform(forward, yawMatrix);
                }
            }

            // Update camera target based on new forward direction
            float lookDistance = Vector3.Distance(camera.Target, camera.Position);
            if (lookDistance < 0.001f)
                lookDistance = 1.0f;

            camera.Target = camera.Position + forward * lookDistance;
        }

        // Handle movement from keyboard input (calculated separately)
        // Movement is handled by InputSystem + MovementSystem + CollisionSystem
        // We just sync the camera position with player position after collision

        // Sync camera position with player position (after collision resolution)
        camera.Position = player.Position;

        // Update target to maintain relative look direction
        // Only update if position changed (to preserve rotation from mouse)
        Vector3 positionDelta = player.Position - oldPosition;
        if (positionDelta.Length() > 0.001f)
        {
            // Position changed, maintain look direction relative to new position
            Vector3 lookDirection = camera.Target - oldPosition;
            float lookDistance = lookDirection.Length();

            if (lookDistance > 0.001f)
            {
                Vector3 normalizedDirection = lookDirection / lookDistance;
                camera.Target = player.Position + normalizedDirection * lookDistance;
            }
        }
        // If position didn't change, keep target as-is (mouse rotation was already applied above)

        // Sync back
        player.Camera = camera;
    }
}

