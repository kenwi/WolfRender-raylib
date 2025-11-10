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
    DYING
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
    public float AngleToPlayer  { get; set; }
    public float DistanceFromPlayer { get; set; }
    public EnemyState EnemyState { get; set; }
}

public class EnemyGuard : Enemy
{
    public EnemyGuard()
    {
        
    }
}