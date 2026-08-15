using Delaunay.Geo;

public enum ConnectionType
{
    Up = 0,
    Right = 1,
    Down = 2,
    Left = 3
}

public class RoomConnection
{
    public ConnectionType Direction
    {
        get;
    }
    public Room Room
    {
        get;
    }

    public LineSegment line1;
    public LineSegment line2;

    public RoomConnection(Room room, ConnectionType direction)
    {
        Room = room;
        Direction = direction;
    }
}