using UnityEngine;
using System.Collections;

public class Cube : MonoBehaviour {

    public int type;
    public int row;
    public int column;

    public void Init(int cubeType, Vector2 pos) {
        type = cubeType;
        row = (int)pos.x;
        column = (int)pos.y;
    }
}
