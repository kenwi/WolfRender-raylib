using System.Collections;
using System.Data;
using System.Numerics;
using DotTiled;
using Game.Entities;
using Game.Utilities;
using Raylib_cs;

namespace Game.Systems;

public class DoorSystem
{
    private readonly TileLayer _layer;
    private readonly List<Texture2D> _textures;
    private readonly List<Door> _doors; 
    
    public List<Door> Doors => _doors;

    public DoorSystem(TileLayer layer, List<Texture2D> textures)
    {
        _layer = layer;
        _textures = textures;

        _doors = new List<Door>(20);
        var ids = _layer.Data.Value.GlobalTileIDs.Value;      
        for (int index = 0; index < 64 * 64; index++)
        {
            var value = ids[index];
            if (ids[index] > 0)
            {
                var colRow = LevelData.GetColRow(index, 64);
                var door = new Door()
                {
                    Position = new Vector2(colRow.col, colRow.row),
                    StartPosition = new Vector2(colRow.col, colRow.row),
                    DoorRotation = (DoorRotation)value,
                    DoorState = DoorState.CLOSED
                };
                _doors.Add(door);
            }
        }
    }

    public void Render()
    {
        foreach(var door in _doors)
        {
            if (door.DoorRotation == DoorRotation.HORIZONTAL)
            {
                PrimitiveRenderer.DrawDoorTextureH(_textures[6], new Vector3(door.Position.X * 4, 2, (door.Position.Y - 1) * 4), 4, 4, 4, Raylib_cs.Color.White);
            }
            else
            {
                PrimitiveRenderer.DrawDoorTextureV(_textures[6], new Vector3(door.Position.X * 4, 2, door.Position.Y * 4), 4, 4, 4, Raylib_cs.Color.White);
            }
        }
    }
    
    public void Update(float deltaTime, InputState input, Vector3 playerPosition)
    {
        if (input.IsInteractPressed)
        {
            var position = new Vector2(playerPosition.X / 4, playerPosition.Z / 4);
            var closestDoor = FindClosestDoor(position);
            if (closestDoor != null)
            {
                var distanceFromPlayer = Vector2.Distance(position, closestDoor.Position);
                if (closestDoor != null && distanceFromPlayer < 1.5f)
                {
                    OpenDoor(closestDoor);
                }
            }
        }

        Animate(deltaTime);
    }

    public Door? FindClosestDoor(Vector2 position)
    {
        var closestDoor = _doors.OrderBy(d => Vector2.Distance(d.Position, position)).FirstOrDefault();
        return closestDoor;
    }

    public void OpenDoor(Door door)
    {
        door.DoorState = DoorState.OPENING;
        door.TimeDoorHasBeenOpen = 0;
        door.TimeDoorHasBeenOpening = 0;
    }

    public bool IsDoorBlocking(Vector3 playerPosition, float radius)
    {
        var position = new Vector2(playerPosition.X / 4, playerPosition.Z / 4);
        var closestDoor = FindClosestDoor(position);
        if (closestDoor != null)
        {
            float distanceFromPlayer;
            radius /= 4;
            if (closestDoor.DoorRotation == DoorRotation.HORIZONTAL)
            {
                distanceFromPlayer = Math.Abs(position.Y - closestDoor.Position.Y);
                if (distanceFromPlayer < radius && closestDoor.DoorState != DoorState.OPEN)
                    return true;
            }

            if (closestDoor.DoorRotation == DoorRotation.VERTICAL)
            {
                distanceFromPlayer = Math.Abs(position.X - closestDoor.Position.X);
                if (distanceFromPlayer < radius && closestDoor.DoorState != DoorState.OPEN)
                    return true;
            }

            if (closestDoor.TimeDoorHasBeenOpening > 0.5)
                ;
        }
        return false;
    }

    public void Animate(float deltaTime)
    {
        foreach(var door in _doors)
        {
            var distance = Vector2.Distance(door.StartPosition, door.Position);
            switch (door.DoorState)
            {
                case DoorState.CLOSED:
                    break;
                case DoorState.OPEN:
                    door.TimeDoorHasBeenOpen += deltaTime;
                    if (door.TimeDoorHasBeenOpen > 1f)
                    {
                        door.DoorState = DoorState.CLOSING;
                    }
                    break;
                case DoorState.OPENING:
                    door.TimeDoorHasBeenOpening += deltaTime;
                    if (distance > 1.0f)
                    {
                        door.DoorState = DoorState.OPEN;
                        break;
                    }
                
                    if (door.DoorRotation == DoorRotation.HORIZONTAL)
                    {
                        door.Position += new Vector2(1, 0) * deltaTime;
                    }
                    else
                    {
                        door.Position += new Vector2(0, 1) * deltaTime;
                    }
                    break;
                case DoorState.CLOSING:
                    if (distance < 0.01f)
                    {
                        door.DoorState = DoorState.CLOSED;
                        door.Position = door.StartPosition;
                        door.TimeDoorHasBeenOpen = 0;
                        // door.TimeDoorHasBeenOpening = 0;
                        break;
                    }
                
                    if (door.DoorRotation == DoorRotation.HORIZONTAL)
                    {
                        door.Position -= new Vector2(1, 0) * deltaTime;
                    }
                    else
                    {
                        door.Position -= new Vector2(0, 1) * deltaTime;
                    }
                    break;                    
                default:
                    break;                   
            }
        }
    }
}

