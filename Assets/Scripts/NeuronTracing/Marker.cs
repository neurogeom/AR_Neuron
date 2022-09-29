using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Marker 
{
    public Vector3 position;
    public Marker parent;
    public int type;
    public float radius;
    public float angle;
    public bool isSegment_root = false;
    public bool isBranch_root = false;
    public bool isLeaf = false;
    public Marker()
    {
        position = Vector3.zero;
        type = 3;
        parent = null;
    }
    public Marker(Vector3 vector)
    {
        position = vector;
        type = 3;
        parent = null;
    }

    public long img_index(long sz0,long sz01)
    {
        return (long)position.z*sz01+ (long)position.y*sz0+ (long)position.x;
    }

    public static float markerRadius(byte[] img, long sz0, long sz1, long sz2, Marker marker, double thresh)
    {
        long sz01 = sz0 * sz1;
        double max_r = sz0 / 2;
        max_r = Math.Max(max_r, sz1 / 2);
        max_r = Math.Max(max_r, sz2 / 2);

        double tol_num = 0, bkg_num = 0;
        float ir;
        for (ir = 1; ir < max_r; ir++)
        {
            tol_num = 0;
            bkg_num = 0;
            double dz, dy, dx;
            for (dz = -ir; dz <= ir; dz++)
            {
                for (dy = -ir; dy <= ir; dy++)
                {
                    for (dx = -ir; dx <= ir; dx++)
                    {
                        double r = Math.Sqrt(dz * dz + dy * dy + dx * dx);
                        if (r > ir - 1 && r <= ir)
                        {
                            tol_num++;
                            long i = (long)(marker.position.x + dx);
                            if (i < 0 || i >= sz0) return ir;
                            long j = (long)(marker.position.y + dy);
                            if (j < 0 || j >= sz1) return ir;
                            long k = (long)(marker.position.z + dz);
                            if (k < 0 || k >= sz2) return ir;
                            if (img[k * sz01 + j * sz0 + i] <= thresh)
                            {
                                bkg_num++;
                                if ((bkg_num / tol_num > 0.01)) return ir;
                                //if (bkg_num>10) return ir;
                            }
                        }

                        //double r = Math.Sqrt(dx * dx + dy * dy + dz * dz);


                    }
                }
            }
        }
        return ir;
    }
}

//public class SwcSoma : Marker
//{
//    public List<SwcNode> children;

//    public SwcSoma(Vector3 pos, float r)
//    {
//        position = pos;
//        radius = r;
//        Parent = null;
//        children = new List<SwcNode>();
//    }

//    public new void AddChild(SwcNode child)
//    {
//        children.Add(child);
//        child.Parent = this;
//    }
//}
