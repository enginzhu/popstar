using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class main : MonoBehaviour {

    public GameObject[] cubes;
    private StarMap starMap;
    private int predictPoint;
    private int totalPoint;

	void Start () {
        starMap = StarMap.getInstance();
        starMap.GameOverHandler += OnGameOverHandler;
        NewGame();
	}


    void NewGame() {
        totalPoint = 0;
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                int type = Random.Range(0, cubes.Length);
                Vector3 offset = new Vector3(c * 0.5f, -r * 0.5f, 0f);
                Vector3 dest = starMap.startPt + offset;
                GameObject cubeObj = Instantiate(cubes[type], dest, Quaternion.identity) as GameObject;
                cubeObj.name = "cube_" + r + "_" + c;
                Cube cubeInstance = cubeObj.GetComponent<Cube>() as Cube;
                cubeInstance.Init(type, new Vector2(r, c));
                starMap.setCubeToMap(r, c, cubeObj);
            }
        }
    }


	void Update () {
        if (Input.GetButtonDown("Fire1")) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo)) {
                GameObject cube = hitInfo.collider.gameObject;
                if (cube.name.IndexOf("cube") == -1) return;
                OnClickStar(cube);
            }
        }
	}

    void OnGUI() {
        int count = (null == starMap.eliminableList) ? 0 : starMap.eliminableList.Count;
        string text = "星星个数: " + count + "\n"
            + "可获得分数：" + predictPoint + "\n"
            + "总分数：" + totalPoint;
        GUI.Box(new Rect(0, 0, 120, 60), text);
        if (GUI.Button(new Rect(20, 65, 80, 30), "重新开始")) {
            starMap.ClearAllCubes();
            NewGame();
        }
    }


    void OnGameOverHandler(object sender, System.EventArgs args) {
        starMap.ClearAllCubes();
    }

    
    /**
     * 单击选择一个格子，高亮可消除的所有格子，1秒后取消选中状态
     * 双击一个格子，触发消除相连的同颜色格子
     */
    void OnClickStar(GameObject cube)
    {
        if (starMap.selectedCube && starMap.selectedCube == cube) { //double click
            PreprocessPopStar();
            return;
        }
        if (starMap.selectedCube) {
            CancelSelectImmediately();
        }
        starMap.selectedCube = cube;
        starMap.searchCubeChunk();

        if (starMap.eliminableList.Count > 1) {
            predictPoint = (int)(5 * Mathf.Pow(starMap.eliminableList.Count, 2));
        }
        //Debug.Log("可消除星星个数：" + starMap.eliminableList.Count + ", 分数：" + predictPoint);
        starMap.lightEliminableCubes();
        Invoke("CancelSelect", 1);
    }

    /**
     * 消星星预处理，能消除两个或以上的星星时才合法
     */
    void PreprocessPopStar() {
        if (starMap.eliminableList.Count > 1) {
            totalPoint += predictPoint;
            predictPoint = 0;
            popStars();
        }
        else {
            CancelSelectImmediately();
        }
    }


    void CancelSelectImmediately() {
        CancelSelect();
        CancelInvoke("CancelSelect");
    }

    /**
     * 取消选中状态
     */
    void CancelSelect() {
        if (null == starMap.selectedCube) return;
        starMap.unlightEliminableCubes();
        starMap.selectedCube = null;
        predictPoint = 0;
    }

    /**
     * 销毁符合清除规则的星星，余下的星星先往下坠落，再往左移动
     */
    void popStars() {
        starMap.destroyEliminableList();
        StartCoroutine(starMap.cubesFallDown());
        CancelSelectImmediately();
    }
}
