using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 节点类型
/// </summary>
public enum NodeType
{
    Walkable,
    Blocked,
}

/// <summary>
/// A星的节点类
/// </summary>
public class AStarNode
{
    //节点对象的坐标
    public int x;
    public int y;
    
    //寻路总消耗
    public float f;
    //到起点的代价
    public float g;
    //到终点的代价
    public float h;
    
    //父对象
    public AStarNode father;
    
    //节点类型
    public NodeType nodeType;

    /// <summary>
    /// 传入类型的构造
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="nodeType"></param>
    public AStarNode(int x, int y, NodeType nodeType)
    {
        this.x = x;
        this.y = y;
        this.nodeType = nodeType;
    }

}
