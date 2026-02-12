using System.Numerics;
using Raylib_cs;

namespace Game.Entities;

public enum EnemyState
{
    IDLE,
    WALKING,
    NOTICING,
    FLEEING,
    ATTACKING,
    DYING,
    COLLIDING
}

public class Enemy
{
    public Vector3 Position { get; set; }
    public float Rotation { get; set; }
    public float MoveSpeed { get; set; }
    public Rectangle FrameRect { get; set; }
    public int FrameColumnIndex { get; set; }
    public int FrameRowIndex { get; set; }
    public float AnimationTimer { get; set; }
    public int ShootingAnimationIndex { get; set; } = 1;
    public int DyingAnimationIndex { get; set; }
    public float AngleToPlayer  { get; set; }
    public float DistanceFromPlayer { get; set; }
    public EnemyState EnemyState { get; set; }

    /// <summary>
    /// Time the enemy has spent in the current state (seconds).
    /// Reset to 0 on every state transition via <see cref="TransitionTo"/>.
    /// </summary>
    public float StateTimer { get; set; }

    /// <summary>
    /// Transition to a new state and reset the state timer.
    /// </summary>
    public void TransitionTo(EnemyState newState)
    {
        if (EnemyState == newState) return;
        EnemyState = newState;
        StateTimer = 0f;
    }

    /// <summary>
    /// Patrol path as world-space waypoints. Empty list means no patrol.
    /// </summary>
    public List<Vector3> PatrolPath { get; set; } = new();

    /// <summary>
    /// Whether this enemy has a patrol path to follow.
    /// </summary>
    public bool HasPatrolPath => PatrolPath.Count > 0;

    /// <summary>
    /// Index of the current target waypoint in the patrol path.
    /// </summary>
    public int CurrentWaypointIndex { get; set; }

    /// <summary>
    /// The enemy's starting position, used to return after completing the patrol loop.
    /// </summary>
    public Vector3 PatrolOrigin { get; set; }

    // --- Line of Sight ---

    /// <summary>
    /// Whether the enemy can currently see the player.
    /// </summary>
    public bool CanSeePlayer { get; set; }

    /// <summary>
    /// The enemy's field-of-view half-angle in radians (total FOV = 2 * this).
    /// </summary>
    public float FovHalfAngle { get; set; } = MathF.PI / 3f; // 60 degrees half = 120 total

    /// <summary>
    /// Maximum sight range in tile units.
    /// </summary>
    public float SightRange { get; set; } = 12f;

    /// <summary>
    /// FOV polygon endpoints in tile-space for editor visualization.
    /// First point is the enemy's origin, subsequent points are ray hit positions.
    /// </summary>
    public List<Vector2> FovPolygon { get; set; } = new();
}

public class EnemyGuard : Enemy
{
    public EnemyGuard()
    {
        
    }
}