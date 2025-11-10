namespace Game.Utilities;
using rlImGui_cs;
using Game.Entities;
using ImGuiNET;

public static class Debug
{
    public static List<Door> Doors;
    public static Player Player;
    
    public static void Setup(List<Door> doors, Player player)
    {
        Doors = doors;
        Player = player;
        
        rlImGui.Setup(true);
    }

    public static void Draw()
    {
        rlImGui.Begin();
        
        // Main debug window
        ImGui.Begin("Debug Info", ImGuiWindowFlags.AlwaysAutoResize);
        
        // Increase font size for better readability
        ImGui.SetWindowFontScale(2.0f);
        
        // Player Information Section
        if (ImGui.CollapsingHeader("Player", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (Player != null)
            {
                ImGui.Text("Position:");
                ImGui.SameLine();
                ImGui.Text($"X: {Player.Position.X:F2}, Y: {Player.Position.Y:F2}, Z: {Player.Position.Z:F2}");
                
                ImGui.Text("Old Position:");
                ImGui.SameLine();
                ImGui.Text($"X: {Player.OldPosition.X:F2}, Y: {Player.OldPosition.Y:F2}, Z: {Player.OldPosition.Z:F2}");
                
                ImGui.Text("Velocity:");
                ImGui.SameLine();
                ImGui.Text($"X: {Player.Velocity.X:F2}, Y: {Player.Velocity.Y:F2}, Z: {Player.Velocity.Z:F2}");
                
                ImGui.Separator();
                
                ImGui.Text("Collision Radius:");
                ImGui.SameLine();
                ImGui.Text($"{Player.CollisionRadius:F2}");
                
                ImGui.Text("Move Speed:");
                ImGui.SameLine();
                ImGui.Text($"{Player.MoveSpeed:F2}");
                
                ImGui.Separator();
                
                if (Player.Camera.Position.X != 0 || Player.Camera.Position.Y != 0 || Player.Camera.Position.Z != 0)
                {
                    ImGui.Text("Camera Position:");
                    ImGui.SameLine();
                    ImGui.Text($"X: {Player.Camera.Position.X:F2}, Y: {Player.Camera.Position.Y:F2}, Z: {Player.Camera.Position.Z:F2}");
                    
                    ImGui.Text("Camera Target:");
                    ImGui.SameLine();
                    ImGui.Text($"X: {Player.Camera.Target.X:F2}, Y: {Player.Camera.Target.Y:F2}, Z: {Player.Camera.Target.Z:F2}");
                    
                    ImGui.Text("Camera FOV:");
                    ImGui.SameLine();
                    ImGui.Text($"{Player.Camera.FovY:F2}°");
                }
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "Player is null");
            }
        }
        
        // Doors Information Section
        if (ImGui.CollapsingHeader($"Doors ({Doors?.Count ?? 0})", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (Doors != null && Doors.Count > 0)
            {
                // Table header
                // Calculate scaled column widths based on font scale
                float fontScale = 2;//ImGui.GetWindowFontScale();
                if (ImGui.BeginTable("DoorsTable", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchProp))
                {
                    ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, 40 * fontScale);
                    ImGui.TableSetupColumn("State", ImGuiTableColumnFlags.WidthFixed, 80 * fontScale);
                    ImGui.TableSetupColumn("Rotation", ImGuiTableColumnFlags.WidthFixed, 80 * fontScale);
                    ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthFixed, 120 * fontScale);
                    ImGui.TableSetupColumn("Start Pos", ImGuiTableColumnFlags.WidthFixed, 120 * fontScale);
                    ImGui.TableSetupColumn("Time Open", ImGuiTableColumnFlags.WidthFixed, 80 * fontScale);
                    ImGui.TableSetupColumn("Time Opening", ImGuiTableColumnFlags.WidthFixed, 90 * fontScale);
                    ImGui.TableSetupScrollFreeze(0, 1); // Make row 0 always visible
                    ImGui.TableHeadersRow();
                    
                    for (int i = 0; i < Doors.Count; i++)
                    {
                        var door = Doors[i];
                        ImGui.TableNextRow();
                        
                        // ID
                        ImGui.TableNextColumn();
                        ImGui.Text($"{i}");
                        
                        // State (with color coding)
                        ImGui.TableNextColumn();
                        var stateColor = door.DoorState switch
                        {
                            DoorState.CLOSED => new System.Numerics.Vector4(1, 0.3f, 0.3f, 1), // Red
                            DoorState.OPENING => new System.Numerics.Vector4(1, 1, 0.3f, 1), // Yellow
                            DoorState.OPEN => new System.Numerics.Vector4(0.3f, 1, 0.3f, 1), // Green
                            DoorState.CLOSING => new System.Numerics.Vector4(1, 0.7f, 0.3f, 1), // Orange
                            _ => new System.Numerics.Vector4(1, 1, 1, 1) // White
                        };
                        ImGui.TextColored(stateColor, door.DoorState.ToString());
                        
                        // Rotation
                        ImGui.TableNextColumn();
                        ImGui.Text(door.DoorRotation.ToString());
                        
                        // Position
                        ImGui.TableNextColumn();
                        ImGui.Text($"({door.Position.X:F2}, {door.Position.Y:F2})");
                        
                        // Start Position
                        ImGui.TableNextColumn();
                        ImGui.Text($"({door.StartPosition.X:F2}, {door.StartPosition.Y:F2})");
                        
                        // Time Open
                        ImGui.TableNextColumn();
                        ImGui.Text($"{door.TimeDoorHasBeenOpen:F2}s");
                        
                        // Time Opening
                        ImGui.TableNextColumn();
                        ImGui.Text($"{door.TimeDoorHasBeenOpening:F2}s");
                    }
                    
                    ImGui.EndTable();
                }
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "No doors available");
            }
        }
        
        ImGui.End();
        
        rlImGui.End();
    }
}