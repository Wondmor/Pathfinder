using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using UnityEngine.Profiling;
public enum PathfindingMode
{
    AStar,
    AStarWithRayDetection,
    BFSWithRayDetection
}

public class TesterManager : MonoBehaviour
{
    private static TesterManager instance;

    public static TesterManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<TesterManager>();
                if (instance == null)
                {
                    UnityEngine.Debug.LogError("TesterManager instance is not present in the scene!");
                }
            }
            return instance;
        }
    }
    //坐标原点的位置
    [Header("Origin Position")]
    public int beginX;
    public int beginY;
    //偏移距离
    [Header("Block Offset")]
    public float offsetX;
    public float offsetY;
    //地图节点大小
    [Header("Map Size")]
    public int mapW = 5;
    public int mapH = 5;
    //随机生成地图中阻挡的概率
    [Header("Blocking Probability(%)")] public int blockP = 10;
    //为了测试算法性能而重复进行的寻路次数
    [Header("Repeat Time")] public int repeatTime;
    //不同类型节点的材质
    [Header("Block Materials")]
    public Material red;
    public Material yellow;
    public Material green;
    public Material white;
    //选择寻路模式
    [Header("Pathfinding Mode")]
    public PathfindingMode pathfindingMode; 
    
    private Dictionary<string, GameObject> cubes = new Dictionary<string, GameObject>();
    
    //起点和终点
    private Vector2 startPos = Vector2.right * -1;
    private Vector2 endPos = Vector2.right * -1;
    //寻路结果
    private List<AStarNode> path = new List<AStarNode>();
    
    // 新增一个 BFS 与射线检测实例
    private BFSWithRayDetection bfsWithRayDetection;
    
    void Start()
    {
        //初始化 BFSWithRayDetection
        bfsWithRayDetection = new BFSWithRayDetection();
        //初始化A*寻路地图
        AStarManager.Instance.InitMapInfo(mapW,mapH,blockP);
        for (int i = 0; i < mapW; i++)
        {
            for (int j = 0; j < mapH; j++)
            {
                //创建立方体作为地图格子
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.position = new Vector3(beginX + i * offsetX, beginY + j * offsetY,0);
                
                //设置立方体名字为坐标，方便查找
                obj.name = i + "-" + j;
                cubes.Add(obj.name, obj);
                
                AStarNode node = AStarManager.Instance.nodes[i, j];
                if (node.nodeType == NodeType.Blocked)
                {
                    // 设置阻挡节点的材质颜色
                    obj.GetComponent<MeshRenderer>().material = red;

                    // 添加或检查阻挡节点的碰撞体
                    BoxCollider collider = obj.GetComponent<BoxCollider>();
                    if (collider == null)
                    {
                        collider = obj.AddComponent<BoxCollider>();
                    }

                    // 将阻挡节点分配到特定的物理层，便于射线检测
                    obj.layer = LayerMask.NameToLayer("Obstacles");
                }
                else
                {
                    // 设置非阻挡节点的材质颜色
                    obj.GetComponent<MeshRenderer>().material = white;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //检测鼠标左键
        if (Input.GetMouseButtonDown(0))
        {
            //射线检测检查点击的位置
            RaycastHit info;
            //得到鼠标位置
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //射线检测
            if (Physics.Raycast(ray, out info, 1000))
            {
                //选择起点
                if (startPos == Vector2.right * -1)
                {
                    //清理之前的寻路结果
                    if (path != null)
                    {
                        for (int i = 0; i < path.Count; i++)
                        {
                            cubes[path[i].x + "-" + path[i].y].GetComponent<MeshRenderer>().material = white;
                        }
                    }
                    
                    string[] strs = info.collider.gameObject.name.Split("-");
                    startPos = new Vector2(int.Parse(strs[0]), int.Parse(strs[1]));
                    info.collider.gameObject.GetComponent<MeshRenderer>().material = yellow;
                }
                //选择终点
                else if(endPos == Vector2.right * -1)
                {
                    string[] strs = info.collider.gameObject.name.Split("-");
                    endPos = new Vector2(int.Parse(strs[0]), int.Parse(strs[1]));
                    info.collider.gameObject.GetComponent<MeshRenderer>().material = yellow;
                    //开始寻路
                    Stopwatch timer = new Stopwatch();
                    // 获取初始内存分配
                    long initialMemory = Profiler.GetTotalAllocatedMemoryLong();
                    timer.Start();
                    if (pathfindingMode == PathfindingMode.AStar)
                    {
                        for (int i = 0; i < repeatTime; i++)
                        {
                            path = AStarManager.Instance.FindPath(startPos, endPos);
                        }
                    }
                    else if (pathfindingMode == PathfindingMode.AStarWithRayDetection)
                    {
                        for (int i = 0; i < repeatTime; i++)
                        {
                            path = AStarManager.Instance.FindPathWithRaycast(startPos, endPos);
                        }
                    }
                    else if (pathfindingMode == PathfindingMode.BFSWithRayDetection)
                    {
                        for (int i = 0; i < repeatTime; i++)
                        {
                            path = bfsWithRayDetection.FindPathWithRaycast(startPos, endPos);
                        }
                    }
                    timer.Stop();
                    // 获取最终内存分配
                    long finalMemory = Profiler.GetTotalAllocatedMemoryLong();
                    // 计算内存消耗
                    long memoryConsumed = finalMemory - initialMemory;
                    UnityEngine.Debug.Log(pathfindingMode + "Pathfinder consume time:" + timer.ElapsedMilliseconds + "ms");
                    //UnityEngine.Debug.Log(pathfindingMode + " Pathfinder consume memory: " + (memoryConsumed / (1024f * 1024f)) + " MB");
                    if (path != null)
                    {
                        for (int i = 1; i < path.Count - 1; i++)
                        {
                            cubes[path[i].x + "-" + path[i].y].GetComponent<MeshRenderer>().material = green;
                        }
                    }
                    
                    //寻路完成重置起点和终点
                    startPos = Vector2.right * -1;
                    endPos = Vector2.right * -1;
                    if(cubes.ContainsKey((int)startPos.x + "-" + (int)startPos.y))
                        cubes[(int)startPos.x + "-" + (int)startPos.y].GetComponent<MeshRenderer>().material = white;
                    if(cubes.ContainsKey((int)endPos.x + "-" + (int)endPos.y))
                        cubes[(int)endPos.x + "-" + (int)endPos.y].GetComponent<MeshRenderer>().material = white;
                }
            }
        }
    }

    public void ResetTester()
    {
        //重置起点和终点
        startPos = Vector2.right * -1;
        endPos = Vector2.right * -1;
    }
        
}
