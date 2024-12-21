using System.Collections.Generic;
using UnityEngine;

public class BFSWithRayDetection
{
    /// <summary>
    /// 使用BFS与射线检测混合算法
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    public List<AStarNode> FindPathWithRaycast(Vector2 startPos, Vector2 endPos)
    {
        // 获取 AStarManager 的实例
        AStarManager manager = AStarManager.Instance;

        // 获取地图的节点数组
        AStarNode[,] nodes = manager.nodes;

        // 初始化 BFS 队列和访问记录
        Queue<AStarNode> openQueue = new Queue<AStarNode>();
        HashSet<AStarNode> visited = new HashSet<AStarNode>();

        // 起点和终点节点
        AStarNode startNode = nodes[(int)startPos.x, (int)startPos.y];
        AStarNode endNode = nodes[(int)endPos.x, (int)endPos.y];

        // 检查起点和终点是否合法
        if (startNode.nodeType == NodeType.Blocked || endNode.nodeType == NodeType.Blocked)
        {
            Debug.LogError("Start or end node is blocked!");
            return null;
        }

        // 起点入队并标记为已访问
        openQueue.Enqueue(startNode);
        visited.Add(startNode);

        int layerMask = LayerMask.GetMask("Obstacles");

        // BFS 主循环
        while (openQueue.Count > 0)
        {
            // 当前节点出队
            AStarNode currentNode = openQueue.Dequeue();

            // 射线检测：检查当前节点到目标节点之间是否有阻挡
            if (!Physics.Raycast(
                    ConvertToWorldPosition(currentNode), // 当前节点的世界坐标
                    (ConvertToWorldPosition(endNode) - ConvertToWorldPosition(currentNode)).normalized, // 射线方向
                    Vector3.Distance(ConvertToWorldPosition(currentNode), ConvertToWorldPosition(endNode)), // 射线长度
                    layerMask)) // 指定检测层
            {
                // 如果射线检测无阻挡，直接将目标节点连接到当前路径
                endNode.father = currentNode;
                return BuildPath(endNode);
            }

            // 遍历当前节点的邻居
            foreach (var neighbor in GetNeighbors(currentNode, nodes))
            {
                // 检查是否已访问或是阻挡节点
                if (visited.Contains(neighbor) || neighbor.nodeType == NodeType.Blocked)
                    continue;

                // 设置父节点，加入队列，并标记为已访问
                neighbor.father = currentNode;
                openQueue.Enqueue(neighbor);
                visited.Add(neighbor);
            }
        }

        // 如果队列为空且未找到路径，返回 null
        return null;
    }

    /// <summary>
    /// 根据终点回溯路径
    /// </summary>
    private List<AStarNode> BuildPath(AStarNode endNode)
    {
        List<AStarNode> path = new List<AStarNode>();
        HashSet<AStarNode> visitedNodes = new HashSet<AStarNode>(); // 用于检测循环

        while (endNode != null)
        {
            // 检查是否已访问过当前节点（防止循环）
            if (visitedNodes.Contains(endNode))
            {
                break;
            }

            path.Add(endNode);
            visitedNodes.Add(endNode);

            endNode = endNode.father; // 回溯父节点
        }

        path.Reverse(); // 反转路径顺序
        return path;
    }

    /// <summary>
    /// 获取当前节点的邻居
    /// </summary>
    private List<AStarNode> GetNeighbors(AStarNode currentNode, AStarNode[,] nodes)
    {
        List<AStarNode> neighbors = new List<AStarNode>();
        int x = currentNode.x;
        int y = currentNode.y;

        // 检查四个方向（上下左右）
        if (x > 0) neighbors.Add(nodes[x - 1, y]); // 左
        if (x < nodes.GetLength(0) - 1) neighbors.Add(nodes[x + 1, y]); // 右
        if (y > 0) neighbors.Add(nodes[x, y - 1]); // 下
        if (y < nodes.GetLength(1) - 1) neighbors.Add(nodes[x, y + 1]); // 上

        return neighbors;
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
