using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/**
 * 棋局数据和动画处理
 */
public class StarMap : MonoBehaviour {

    private static StarMap uniqueInstance;
    private GameObject[,] cubeList = new GameObject[9, 9];
    public Vector3 startPt = new Vector3(-2f, 7f, 0f);
    public GameObject selectedCube;

    public static StarMap getInstance() {
        if (null == uniqueInstance) {
            uniqueInstance = new StarMap();
        }
        return uniqueInstance;
    }

    private List<GameObject> openList;
    private List<GameObject> closeList;
    public List<GameObject> eliminableList;

    /**
     * 搜索成块的相连星星
     */
    public void searchCubeChunk() {
        if (null == selectedCube) return;
        openList = new List<GameObject>();
        closeList = new List<GameObject>();
        eliminableList = new List<GameObject>();

        openList.Add(selectedCube);
        eliminableList.Add(selectedCube);
        while (openList.Count > 0) {
            checkOpenList();
        }
    }

    /**
     * 对单个星星搜索上下左右四个位置的邻居，看是否相同颜色
     */
    private void checkOpenList() {
        GameObject popCube = popOpenList();
        if (null == popCube) return;
        Cube cube = popCube.GetComponent<Cube>();
        if (closeList.IndexOf(popCube) != -1) return;
        if (cube.type != selectedCube.GetComponent<Cube>().type) return;

        closeList.Add(popCube);
        int curRow = cube.row;
        int curColumn = cube.column;
        GameObject contiguousCube;

        //上边星星
        if (curRow - 1 >= 0) {
            contiguousCube = cubeList[curRow - 1, curColumn];
            pushOpenList(contiguousCube);
        }
        //右边星星
        if (curColumn + 1 < 9) {
            contiguousCube = cubeList[curRow, curColumn + 1];
            pushOpenList(contiguousCube);
        }
        //下边星星
        if (curRow + 1 < 9) {
            contiguousCube = cubeList[curRow + 1, curColumn];
            pushOpenList(contiguousCube);
        }
        //左边星星
        if (curColumn - 1 >= 0) {
            contiguousCube = cubeList[curRow, curColumn - 1];
            pushOpenList(contiguousCube);
        }
    }


    /**
     * 对开放列表的push做校验
     */
    private void pushOpenList(GameObject cube) {
        if (null == cube) return;
        if (closeList.IndexOf(cube) != -1) return;
        if (cube.GetComponent<Cube>().type == selectedCube.GetComponent<Cube>().type) {
            openList.Add(cube);
            if (eliminableList.IndexOf(cube) == -1) {
                eliminableList.Add(cube);
            }
        }
    }


    /**
     * 删除openList的最后一个元素并返回
     */
    private GameObject popOpenList() {
        if (openList.Count == 0) {
            return null;
        }
        int index = openList.Count - 1;
        GameObject popCube = openList[index];
        openList.RemoveAt(index);
        return popCube;
    }

    /**
     * 可消除的星星块发光效果
     */
    public void lightEliminableCubes() {
        if (null == eliminableList || eliminableList.Count == 0) return;
        eliminableList.ForEach(lightSelectedCube);
    }

    /**
     * 可消除的星星块取消发光
     */
    public void unlightEliminableCubes() {
        if (null == eliminableList || eliminableList.Count == 0) return;
        eliminableList.ForEach(normalizeSelectedCube);
        eliminableList = new List<GameObject>();
    }

    /**
     * 对选中的cube应用外发光着色器
     */
    void lightSelectedCube(GameObject cube) {
        cube.GetComponent<Renderer>().material.shader = Shader.Find("Custom/OutLight");
    }

    /**
     * 还原选中的cube着色器
     */
    void normalizeSelectedCube(GameObject cube) {
        cube.GetComponent<Renderer>().material.shader = Shader.Find("Unlit/Transparent Cutout");
    }

    /**
     * 从场景中删除指定星星格子
     */
    void destroyCube(GameObject cube)
    {
        int row = cube.GetComponent<Cube>().row;
        int column = cube.GetComponent<Cube>().column;
        setCubeToMap(row, column, null);
        Destroy(cube);
    }

    /**
     * 当次点击格子后，从场景中销毁全部可消除的格子
     */
    public void destroyEliminableList() {
        eliminableList.ForEach(destroyCube);
    }

    public void setCubeToMap(int r, int c, GameObject cube) {
        cubeList[r, c] = cube;
    }

    public GameObject getCubeFromMap(int r, int c) {
        return cubeList[r, c];
    }


    /**
     * ------------------------------------
     * 格子的下落和左移计算和播放处理
     * ------------------------------------
     */

    public event EventHandler GameOverHandler;
    private List<Hashtable> verticalMoveList;

    public IEnumerator cubesFallDown()
    {
        verticalMoveList = new List<Hashtable>();
        for (int c = 0; c < 9; c++) {
            for (int r = 8; r >= 0; r--) {
                checkCubeVertically(r, c);
            }
        }

        foreach (Hashtable hash in verticalMoveList) {
            GameObject obj = hash["gameobject"] as GameObject;
            float newy = (float)hash["newy"];
            iTween.MoveTo(obj, iTween.Hash("y", newy, "delay", .1));
        }
        if (verticalMoveList.Count > 0) {
            yield return new WaitForSeconds(.5f);
        }
        cubesLeftMove();
        if (checkGameOver()) {
            if (GameOverHandler != null) {
                GameOverHandler(this, new EventArgs());
            }
        }
    }


    /**
     * 查看指定星星格子是否为空，是的话则往上搜索非空格子，并用来填补当前格
     */
    void checkCubeVertically(int r, int c) {
        if (cubeList[r, c] != null) return;
        for (int uprow = r; uprow >= 0; uprow--) {
            if (cubeList[uprow, c] != null) {
                Hashtable hash = new Hashtable();
                hash.Add("gameobject", cubeList[uprow, c]);
                hash.Add("newy", startPt.y - r * 0.5f);
                verticalMoveList.Add(hash);

                cubeList[r, c] = cubeList[uprow, c];
                cubeList[r, c].GetComponent<Cube>().row = r;
                cubeList[r, c].GetComponent<Cube>().type = cubeList[uprow, c].GetComponent<Cube>().type;
                cubeList[r, c].name = "cube_" + r + "_" + c;
                cubeList[uprow, c] = null;
                break;
            }
        }
    }


    private List<Hashtable> horizontalMoveList;

    void cubesLeftMove() {
        int curColumn = 0;
        while (curColumn < 9) {
            bool isCurColumnEmpty = true;
            horizontalMoveList = new List<Hashtable>();
            for (int curRow = 0; curRow < 9; curRow++) {
                if (cubeList[curRow, curColumn] != null) {
                    isCurColumnEmpty = false;
                    break;
                }
            }
            if (isCurColumnEmpty) {
                // 计算有多少连续空列，再依次往左移动多少列
                int emptyColumnCounts = calcEmptyColumns(curColumn);
                //Debug.Log("start column:" + curColumn + ", empty column:" + emptyColumnCounts);
                moveCubesToLeft(curColumn, emptyColumnCounts);
                curColumn += emptyColumnCounts;
            }
            else {
                curColumn += 1;
            }
            foreach (Hashtable hash in horizontalMoveList) {
                GameObject obj = hash["gameobject"] as GameObject;
                float newx = (float)hash["newx"];
                iTween.MoveTo(obj, iTween.Hash("x", newx, "delay", .1));
            }
        }
    }


    /**
     * 将指定列数右边的格子往左移动
     */
    void moveCubesToLeft(int c, int emptyColumnCounts) {
        for (int rightColumn = c + 1; rightColumn < 9; rightColumn++) {
            for (int r = 0; r < 9; r++) {
                if (cubeList[r, rightColumn] == null) continue;

                int columnAfterMove = rightColumn - emptyColumnCounts;
                Hashtable hash = new Hashtable();
                hash.Add("gameobject", cubeList[r, rightColumn]);
                hash.Add("newx", startPt.x + (columnAfterMove) * 0.5f);
                horizontalMoveList.Add(hash);

                cubeList[r, columnAfterMove] = cubeList[r, rightColumn];
                cubeList[r, columnAfterMove].GetComponent<Cube>().column = columnAfterMove;
                cubeList[r, columnAfterMove].GetComponent<Cube>().type = cubeList[r, rightColumn].GetComponent<Cube>().type;
                cubeList[r, columnAfterMove].name = "cube_" + r + "_" + columnAfterMove;
                cubeList[r, rightColumn] = null;
            }
        }
    }


    /**
     * 计算连续的空列数
     */
    int calcEmptyColumns(int c) {
        int emptyColumns = 1;
        for (int newc = c + 1; newc < 9; newc++) {
            bool isEmptyColumn = true;
            for (int r = 0; r < 9; r++) {
                if (cubeList[r, newc] != null) {
                    isEmptyColumn = false;
                    break;
                }
            }
            if (isEmptyColumn) {
                emptyColumns += 1;
            }
            else {
                // 只计算连续的空列，只要遇到第一个非空列就跳出
                break;
            }
        }
        return emptyColumns;
    }


    bool checkGameOver() {
        bool gameOver = true;
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                bool eliminable = checkCubeEliminable(r, c);
                if (eliminable) {
                    gameOver = false;
                }
            }
        }
        return gameOver;
    }


    bool checkCubeEliminable(int r, int c) {
        bool eliminable = false;
        GameObject cube = cubeList[r, c];
        if (cube == null) return eliminable;

        int targetType = cube.GetComponent<Cube>().type;
        bool targetTypeMatched;
        //上边星星
        if (r - 1 >= 0) {
            targetTypeMatched = isCubeMatchType(r - 1, c, targetType);
            if (targetTypeMatched) return true;
        }
        //右边星星
        if (c + 1 < 9) {
            targetTypeMatched = isCubeMatchType(r, c + 1, targetType);
            if (targetTypeMatched) return true;
        }
        //下边星星
        if (r + 1 < 9) {
            targetTypeMatched = isCubeMatchType(r + 1, c, targetType);
            if (targetTypeMatched) return true;
        }
        //左边星星
        if (c - 1 >= 0) {
            targetTypeMatched = isCubeMatchType(r, c - 1, targetType);
            if (targetTypeMatched) return true;
        }
        return eliminable;
    }


    bool isCubeMatchType(int r, int c, int targetType) {
        if (cubeList[r, c] == null) return false;
        if (cubeList[r, c].GetComponent<Cube>().type == targetType) {
            return true;
        }
        return false;
    }


    public void ClearAllCubes() {
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                if (cubeList[r, c] != null) {
                    destroyCube(cubeList[r, c]);
                }
            }
        }
    }

    public void printCubeList() {
        for (int c = 0; c < 9; c++) {
            for (int r = 0; r < 9; r++) {
                string postfix = "[null]";
                if (cubeList[r, c] != null) {
                    Cube cube = cubeList[r, c].GetComponent<Cube>();
                    postfix = "[" + cube.row + "," + cube.column + "]";
                }
                Debug.Log("(" + r + "," + c + "," + cubeList[r, c] + postfix + ")");
            }
            Debug.Log("\n");
        }
    }
}
