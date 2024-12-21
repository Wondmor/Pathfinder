using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单例A星寻路管理器
/// </summary>
public class AStarManager
{
    private static AStarManager instance;

    public static AStarManager Instance
    {
        get
        {
            if (instance == null)
                instance = new AStarManager();
            return instance;
        }
    }

    //地图数据(宽和高)
    public int mapW;
    public int mapH;
    //地图容器
    public AStarNode[,] nodes;
    //开启列表，使用 SortedSet 实现优先队列
    private SortedSet<AStarNode> openList;
    //关闭列表
    private HashSet<AStarNode> closeList;

    /// <summary>
    /// 初始化地图信息
    /// </summary>
    /// <param name="w"></param>
    /// <param name="h"></param>
    public void InitMapInfo(int w, int h, int blockProbability)
    {
        this.mapW = w;
        this.mapH = h;
        //根据宽高创建地图
        nodes = new AStarNode[w, h];
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                //从配置文件读取格子类型（暂时用随机阻挡来代替）
                AStarNode node = new AStarNode(i, j, Random.Range(0, 100) < blockProbability ? NodeType.Blocked : NodeType.Walkable);
                nodes[i, j] = node;
            }
        }

        //初始化列表
        openList = new SortedSet<AStarNode>(new AStarNodeComparer());
        closeList = new HashSet<AStarNode>();
    }

    /// <summary>
    /// 寻路方法
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    public List<AStarNode> FindPath(Vector2 startPos, Vector2 endPos)
    {
        //检查起点和终点是否合法
        if (startPos.x < 0 || startPos.x >= mapW || startPos.y < 0 || startPos.y >= mapH ||
            endPos.x < 0 || endPos.x >= mapW || endPos.y < 0 || endPos.y >= mapH)
        {
            Debug.Log("起点或终点不合法");
            return null;
        }

        AStarNode start = nodes[(int)startPos.x, (int)startPos.y];
        AStarNode end = nodes[(int)endPos.x, (int)endPos.y];

        if (start.nodeType == NodeType.Blocked || end.nodeType == NodeType.Blocked)
        {
            Debug.Log("起点或终点为阻挡点");
            return null;
        }

        //重置关闭和开启列表
        closeList.Clear();
        openList.Clear();

        //将起点加入关闭列表
        start.father = null;
        start.f = start.g = start.h = 0;
        closeList.Add(start);

        while (true)
        {
            //检查相邻节点并加入开启列表
            AddNodeToOpenList(start.x, start.y - 1, 1f, start, end);
            AddNodeToOpenList(start.x - 1, start.y, 1f, start, end);
            AddNodeToOpenList(start.x + 1, start.y, 1f, start, end);
            AddNodeToOpenList(start.x, start.y + 1, 1f, start, end);

            //如果开启列表为空，则无法找到路径
            if (openList.Count == 0)
            {
                Debug.Log("无法找到路径");
                return null;
            }

            //取出优先队列中代价最小的节点
            start = openList.Min;
            openList.Remove(start);
            closeList.Add(start);

            //如果找到终点，返回路径
            if (start == end)
            {
                List<AStarNode> path = new List<AStarNode>();
                while (start != null)
                {
                    path.Add(start);
                    start = start.father;
                }
                path.Reverse();
                return path;
            }
        }
    }

    /// <summary>
    /// 把点加入开启列表
    /// </summary>
    private void AddNodeToOpenList(int x, int y, float g, AStarNode father, AStarNode end)
    {
        //边界检查
        if (x < 0 || x >= mapW || y < 0 || y >= mapH) return;

        AStarNode node = nodes[x, y];
        if (node == null || node.nodeType == NodeType.Blocked || closeList.Contains(node) || openList.Contains(node)) return;

        float newG = father.g + g;
        node.father = father;
        node.g = newG;
        node.h = Mathf.Abs(end.x - node.x) + Mathf.Abs(end.y - node.y);
        node.f = node.g + node.h;

        openList.Add(node);
    }
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>
    /// 寻路方法（融合射线检测）
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    public List<AStarNode> FindPathWithRaycast(Vector2 startPos, Vector2 endPos)
    {
        // 检查起点和终点是否合法
        if (startPos.x < 0 || startPos.x >= mapW || startPos.y < 0 || startPos.y >= mapH ||
            endPos.x < 0 || endPos.x >= mapW || endPos.y < 0 || endPos.y >= mapH)
        {
            Debug.Log("起点或终点不合法");
            return null;
        }

        AStarNode start = nodes[(int)startPos.x, (int)startPos.y];
        AStarNode end = nodes[(int)endPos.x, (int)endPos.y];

        if (start.nodeType == NodeType.Blocked || end.nodeType == NodeType.Blocked)
        {
            Debug.Log("起点或终点为阻挡点");
            return null;
        }

        // 重置关闭和开启列表
        closeList.Clear();
        openList.Clear();

        // 初始化
        start.father = null;
        start.f = start.g = start.h = 0;
        closeList.Add(start);

        int layerMask = LayerMask.GetMask("Obstacles");

        while (true)
        {
            // 射线检测：检查当前节点到目标节点是否有阻挡
            if (!Physics.Raycast(
                    ConvertToWorldPosition(start), // 当前节点的世界坐标
                    (ConvertToWorldPosition(end) - ConvertToWorldPosition(start)).normalized, // 射线方向
                    Vector3.Distance(ConvertToWorldPosition(start), ConvertToWorldPosition(end)), // 射线长度
                    layerMask)) // 指定检测层
            {
                // 如果射线检测无阻挡，直接返回终点路径
                end.father = start;
                return BuildPath(end);
            }

            // 检查相邻节点并加入开启列表
            AddNodeToOpenList(start.x, start.y - 1, 1f, start, end);
            AddNodeToOpenList(start.x - 1, start.y, 1f, start, end);
            AddNodeToOpenList(start.x + 1, start.y, 1f, start, end);
            AddNodeToOpenList(start.x, start.y + 1, 1f, start, end);

            // 如果开启列表为空，则无法找到路径
            if (openList.Count == 0)
            {
                Debug.Log("无法找到路径");
                return null;
            }

            // 取出优先队列中代价最小的节点
            start = openList.Min;
            openList.Remove(start);
            closeList.Add(start);

            // 如果找到终点，返回路径
            if (start == end)
            {
                return BuildPath(end);
            }
        }
    }

    /// <summary>
    /// 根据终点回溯路径
    /// </summary>
    private List<AStarNode> BuildPath(AStarNode endNode)
    {
        List<AStarNode> path = new List<AStarNode>();
        while (endNode != null)
        {
            path.Add(endNode);
            endNode = endNode.father;
        }
        path.Reverse();
        return path;
    }

    /// <summary>
    /// 转换节点坐标为世界坐标
    /// </summary>
    private Vector3 ConvertToWorldPosition(AStarNode node)
    {
        float worldX = TesterManager.Instance.beginX + node.x * TesterManager.Instance.offsetX;
        float worldY = TesterManager.Instance.beginY + node.y * TesterManager.Instance.offsetY;
        return new Vector3(worldX, worldY, 0);
    }

}

/// <summary>
/// 优先队列比较器
/// </summary>
public class AStarNodeComparer : IComparer<AStarNode>
{
    public int Compare(AStarNode a, AStarNode b)
    {
        if (a.f != b.f) 
            return a.f.CompareTo(b.f);

        // 如果 f 值相等，进一步用坐标比较，确保唯一性
        int xCompare = a.x.CompareTo(b.x);
        if (xCompare != 0) 
            return xCompare;

        return a.y.CompareTo(b.y); // 用 y 坐标比较
    }
}

