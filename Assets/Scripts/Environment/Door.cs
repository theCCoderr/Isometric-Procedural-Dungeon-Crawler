using Amr;
using UnityEngine;

public class Door : MonoBehaviour
{
    public DoorPos pos;
    [SerializeField] private bool upDoor;
    [SerializeField] private bool downDoor;
    [SerializeField] private bool rightDoor;
    [SerializeField] private bool leftDoor;
    private PolygonCollider2D doorCollider;

    private void Awake()
    {
        DungeonGenerator.ONTilesGenerated.AddListener(DeterminePos);
        doorCollider = GetComponent<PolygonCollider2D>();
        GameManager.ONChangeDoorsState.AddListener(ChangeDoorState);
    }

    private void ChangeDoorState(bool openDoor) => doorCollider.isTrigger = openDoor;

    private void DeterminePos()
    {
        var grid = DungeonGenerator.grid;

        Vector2 position = transform.position;

        upDoor = grid[(int) (position.x - (int) RoomGenerator.xMin - 0.5f) + 2][(int) (position.y - RoomGenerator.yMin - 0.5f) + 3] == (int) TT.OgDoor;

        downDoor = grid[(int) (position.x - (int) RoomGenerator.xMin - 0.5f) + 2][(int) (position.y - RoomGenerator.yMin - 0.5f) + 1] == (int) TT.OgDoor;

        rightDoor = grid[(int) (position.x - (int) RoomGenerator.xMin - 0.5f) + 3][(int) (position.y - RoomGenerator.yMin - 0.5f) + 2] == (int) TT.OgDoor;

        leftDoor = grid[(int) (position.x - (int) RoomGenerator.xMin - 0.5f) + 1][(int) (position.y - RoomGenerator.yMin - 0.5f) + 2] == (int) TT.OgDoor;

        pos = rightDoor || leftDoor ? pos = rightDoor ? leftDoor ? DoorPos.HMiddle : DoorPos.HLeft : DoorPos.HRight
            : upDoor ? downDoor ? DoorPos.Middle : DoorPos.Bottom : DoorPos.Top;
    }
}

public enum DoorPos
{
    Top = 1,
    Bottom = 2,
    HMiddle = 3,
    Middle = 4,
    HRight = 5,
    HLeft = 6
}