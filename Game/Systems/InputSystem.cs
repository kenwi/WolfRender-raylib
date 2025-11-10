using System.Numerics;
using Game.Entities;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Game.Systems;

public class InputState
{
    public bool MoveForward { get; init; }
    public bool MoveBackward { get; init; }
    public bool MoveLeft { get; init; }
    public bool MoveRight { get; init; }
    public Vector2 MouseDelta { get; init; }
    public bool IsMouseFree { get; init; }
    public bool IsInteractPressed { get; set; }
}

public class InputSystem
{
    private bool _isMouseFree = false;
    private bool _isUpdateEnabled = true;
    private bool _isDebugEnabled = true;

    public bool IsMouseFree => _isMouseFree;
    public bool IsUpdateEnabled => _isUpdateEnabled;
    public bool IsDebugEnabled => _isDebugEnabled;
    
    public void Update()
    {
        if (IsKeyPressed(KeyboardKey.M))
        {
            _isMouseFree = !_isMouseFree;
        }

        if (IsKeyPressed(KeyboardKey.U))
        {
            _isUpdateEnabled = !_isUpdateEnabled;
        }

        if (IsKeyPressed(KeyboardKey.I))
        {
            _isDebugEnabled = !_isDebugEnabled;
        }
    }

    public void LockMouse()
    {
        if (!_isMouseFree)
        {
            var screenCenter = GetScreenCenter();
            SetMousePosition((int)screenCenter.X, (int)screenCenter.Y);
        }
    }

    public InputState GetInputState()
    {
        return new InputState
        {
            MoveForward = IsKeyDown(KeyboardKey.W),
            MoveBackward = IsKeyDown(KeyboardKey.S),
            MoveLeft = IsKeyDown(KeyboardKey.A),
            MoveRight = IsKeyDown(KeyboardKey.D),
            MouseDelta = GetMouseDelta(),
            IsMouseFree = _isMouseFree,
            IsInteractPressed =  IsKeyPressed(KeyboardKey.E)
        };
    }

    public Vector3 GetMoveDirection(Player player)
    {
        Vector3 direction = Vector3.Zero;

        // Calculate forward direction (camera look direction)
        Vector3 targetToPosition = player.Camera.Target - player.Camera.Position;
        float targetDistance = targetToPosition.Length();

        if (targetDistance < 0.001f)
            return Vector3.Zero;

        Vector3 forward = targetToPosition / targetDistance;

        // Project forward onto horizontal plane
        Vector3 forwardHorizontal = new Vector3(forward.X, 0, forward.Z);
        float forwardLength = forwardHorizontal.Length();

        if (forwardLength > 0.001f)
        {
            forwardHorizontal = forwardHorizontal / forwardLength;
        }
        else
        {
            forwardHorizontal = Vector3.UnitZ;
        }

        // Calculate right direction
        Vector3 rightVec = Vector3.Cross(forwardHorizontal, -Vector3.UnitY);
        float rightLength = rightVec.Length();
        Vector3 right = rightLength > 0.001f ? rightVec / rightLength : Vector3.UnitX;

        if (IsKeyDown(KeyboardKey.W))
            direction += forwardHorizontal;
        if (IsKeyDown(KeyboardKey.S))
            direction -= forwardHorizontal;
        if (IsKeyDown(KeyboardKey.D))
            direction -= right;
        if (IsKeyDown(KeyboardKey.A))
            direction += right;

        float directionLength = direction.Length();
        if (directionLength > 0.001f)
        {
            return direction / directionLength;
        }

        return Vector3.Zero;
    }
}

