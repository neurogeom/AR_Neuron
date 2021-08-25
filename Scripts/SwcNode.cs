using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwcNode
{
    public SwcNode left { get; set; }
    public SwcNode right {get;set;}
    public Vector3 Position { get; set; }
    public float radius { get; set; }
    public SwcNode Parent { get; set; }
    public int Index;

    public SwcNode(Vector3 pos,float r)
    {
        Position = pos;
        radius = r;
    }

    public void AddChild(SwcNode child)
    {
        if (left == null) left = child;
        else
        {
            right = child;
        }
        child.Parent = this;
    }

    public SwcNode() { }
}

public class SwcSoma:SwcNode
{
    public List<SwcNode> children;

    public SwcSoma(Vector3 pos, float r)
    {
        Position = pos;
        radius = r;
        Parent = null;
        children = new List<SwcNode>();
    }

    public new void AddChild(SwcNode child)
    {
        children.Add(child);
        child.Parent = this;
    }
}
