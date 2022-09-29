using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MostRepair
{
    static bool[] visited;
    static byte[] img;
    static long sz0, sz1, sz2;
    static int threshold;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static List<Marker> trace_single_seed(byte[]img1d, long szx, long szy, long szz, Marker seed,int thresh, int slipszie)
    {
        img = img1d;
        sz0 = szx;
        sz1 = szy;
        sz2 = szz;
        threshold = thresh;
        visited = new bool[sz0 * sz1 * sz2];
        Marker Tmp = new Marker(new Vector3(Mathf.Round(seed.position.x), Mathf.Round(seed.position.y), Mathf.Round(seed.position.z)));
        Tmp.radius = GetRadius(Tmp);
        Debug.Log(Tmp.position + " " + Tmp.radius);
        recenter(Tmp, threshold);
        Tmp.radius = GetRadius(Tmp);
        Debug.Log(Tmp.position + " " + Tmp.radius);
        VoxelCluster tmpClt = VoxelScooping(Tmp, threshold);
        tmpClt.marker = Tmp;
        var conCltList = new List<VoxelCluster>();
        var clusterList = new List<VoxelCluster>();
        var preCltList = new List<VoxelCluster>();
        var preNodeList = new List<Marker>();
        var tempNodeList = new List<Marker>();
        Marker preNode;
        VoxelCluster preClt;
        clusterList.Add(tmpClt);
        int is_seed = 0;
        preNodeList.Add(Tmp);
        int times = 0;
        while (clusterList.Count != 0)
        {
            if (times++ > 100) break;
            tmpClt = FindUnvisitedNeighbor(clusterList[0]);
            clusterList.Remove(clusterList[0]);
            //if (is_seed == 0)
            //{
            //    preCltList = tmpClt.split(slipszie);
            //    preNodeList.Add(Tmp);
            //    is_seed++;
            //}
            conCltList.Clear();
            conCltList = tmpClt.split(slipszie);
            for(int i = conCltList.Count-1;i>=0;i--)
            {
                Tmp = createNode(conCltList[i]);
                if (Tmp.radius <= 1) continue;
                clusterList.Add(conCltList[i]);
                tempNodeList.Add(Tmp);
            }
            //preCltList.Remove(preCltList.Last());
            //preNodeList.Remove(preNodeList.Last());
            //for (int j = conCltList.Count- 1; j >= 0; j--)
            //    preCltList.Add(conCltList[j]);
            for (int i = 0; i < tempNodeList.Count; i++)
                preNodeList.Add(tempNodeList[i]);
            //for (int i = tempNodeList.Count - 1; i >= 0; i--)
            //    tempNodeList.Remove(tempNodeList.Last());
            tempNodeList.Clear();
        }
        return preNodeList;
    }

    public static float GetRadius(Marker marker)
    {
        long sz01 = sz0 * sz1;
        double max_r = sz0 / 2;
        max_r = Math.Max(max_r, sz1 / 2);
        max_r = Math.Max(max_r, sz2 / 2);

        double tol_num = 0, bkg_num = 0;
        float ir;
        for (ir = 1; ir < max_r; ir++)
        {
            //tol_num = 0;
            //bkg_num = 0;
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
                            if (img[k * sz01 + j * sz0 + i] < threshold)
                            {
                                bkg_num++;
                                //if (bkg_num>10) return ir;
                            }
                        }

                        //double r = Math.Sqrt(dx * dx + dy * dy + dz * dz);


                    }
                }
            }
            if ((bkg_num / tol_num > 0.01)) return ir;
        }
        return ir;
    }

    private static Marker createNode(VoxelCluster clt)
    {
        Marker marker = new Marker(clt.getCenter());
        Debug.Log(marker.position);
        marker.radius = GetRadius(marker);
        marker.parent = clt.parent;
        clt.marker = marker;
        if (marker.parent!=null&&Mathf.Abs(marker.parent.radius - marker.radius) >= 2)
        {
            marker.radius = (marker.radius + marker.parent.radius) / 2;
        }
        return marker;
    }

    private static VoxelCluster FindUnvisitedNeighbor(VoxelCluster clt)
    {
        VoxelCluster uNbClt = new VoxelCluster();
        uNbClt.parent = clt.marker;
        
        foreach(var voxel in clt.voxels)
        {
            //long x = voxel % sz0;
            //long y = ((voxel - x) / sz0) % sz1;
            //long z = voxel / (sz0 * sz1);
            long x = (long)voxel.x;
            long y = (long)voxel.y;
            long z = (long)voxel.z;
            int diff = 4;
            for (long i = x - diff; i <= x + diff; i++)
            {
                if (i < 0 || i >= sz0) continue;
                for (long j = y - diff; j <= y + diff; j++)
                {
                    if (j < 0 || j >= sz1) continue;
                    for (long k = z - diff; k <= z + diff; k++)
                    {
                        if (k < 0 || k >= sz2) continue;
                        long index = k * sz0 * sz1 + j * sz0 + i;
                        if (!visited[index] && img[index] >= clt.threshold)
                        {
                            uNbClt.AppendVoxel(i,j,k);
                            visited[index] = true;
                        }
                    }
                }
            }
        }
        uNbClt.threshold = clt.threshold;
        return uNbClt;
        
    }

    public static void recenter(Marker seed, int threshold)
    {
        
        int rBox = 2 * (int)(Mathf.CeilToInt(seed.radius));
        int xb = seed.position.x - rBox >= 0 ? (int)seed.position.x - rBox : 0;
        int xe = seed.position.x + rBox < sz0 ? (int)seed.position.x + rBox : (int)sz0;
        int yb = seed.position.y - rBox >= 0 ? (int)seed.position.y - rBox : 0;
        int ye = seed.position.y + rBox < sz1 ? (int)seed.position.y + rBox : (int)sz1;
        int zb = seed.position.z - rBox >= 0 ? (int)seed.position.z - rBox : 0;
        int ze = seed.position.z + rBox < sz2 ? (int)seed.position.z + rBox : (int)sz2;

        float xsum = 0, ysum = 0, zsum = 0, gsum = 0;
        for(int i = xb; i < xe; i++)
        {
            for (int j = yb; j < ye; j++)
            {
                for (int k = zb; k < ze; k++)
                {
                    long index = k * sz0 * sz1 + j * sz0 + i;
                    if (img[index] >= threshold)
                    {
                        xsum += i;
                        ysum += j;
                        zsum += k;
                        gsum++;
                    }
                }
            }
        }
        seed.position.Set(xsum / gsum, ysum / gsum, zsum / gsum);
    }

    public static VoxelCluster VoxelScooping(Marker pivot, int threshold)
    {
        var clt = new VoxelCluster();
        int rBox = 2*Mathf.RoundToInt(pivot.radius);
        int xb = pivot.position.x - rBox >= 0 ? (int)pivot.position.x - rBox : 0;
        int xe = pivot.position.x + rBox < sz0 ? (int)pivot.position.x + rBox : (int)sz0-1;
        int yb = pivot.position.y - rBox >= 0 ? (int)pivot.position.y - rBox : 0;
        int ye = pivot.position.y + rBox < sz1 ? (int)pivot.position.y + rBox : (int)sz1-1;
        int zb = pivot.position.z - rBox >= 0 ? (int)pivot.position.z - rBox : 0;
        int ze = pivot.position.z + rBox < sz2 ? (int)pivot.position.z + rBox : (int)sz2-1;

        for (int i = xb; i <= xe; i++)
        {
            for (int j = yb; j <= ye; j++)
            {
                for (int k = zb; k <= ze; k++)
                {
                    long index = k * sz0 * sz1 + j * sz0 + i;
                    if (img[index] >= threshold && !visited[index])
                    {
                        double d_sq = (i - pivot.position.x) * (i - pivot.position.x) + (j - pivot.position.y) * (j - pivot.position.y) + (k - pivot.position.z) * (k - pivot.position.z);
                        if (d_sq < pivot.radius * pivot.radius)
                        {
                            clt.AppendVoxel(i,j,k);
                            visited[index] = true;
                        }
                    }
                }
            }
        }
        clt.threshold = threshold;
        return clt;
    }

}

public class VoxelCluster
{
    //public void AppendVoxel(long index)
    //{
    //    voxels.Add(index);
    //}
    public Vector3 getCenter()
    {
        Vector3 center = new Vector3();
        float cx = 0, cy = 0, cz = 0;
        for (int i = 0; i < voxels.Count; i++)
        {
            cx += voxels[i].x;
            cy += voxels[i].y;
            cz += voxels[i].z;
        }
        //average
        center.x = cx / voxels.Count;
        center.y = cy / voxels.Count;
        center.z = cz / voxels.Count;
        return center;
    }

    public void AppendVoxel(long x, long y, long z)
    {
        xb = xb < x ? xb : x;
        xe = xe > x ? xe : x;
        yb = yb < y ? yb : y;
        ye = ye > y ? ye : y;
        zb = zb < z ? zb : z;
        ze = ze > z ? ze : z;
        this.voxels.Add(new Vector3(x,y,z));
    }

    internal void clear()
    {
        voxels.Clear();
        visited.Clear();
        lable.Clear();
    }

    internal List<VoxelCluster> split(int slipszie)
    {
        VoxelCluster tmpClt = new VoxelCluster();
        List<VoxelCluster> cltList = new List<VoxelCluster>();
        int M = (int)(xe - xb + 1);
        int N = (int)(ye - yb + 1);
        int K = (int)(ze - zb + 1);
        visited.Clear();
        lable.Clear();
        for(int i=0;i< M*N*K; i++)
        {
            visited.Add(true);
            lable.Add(0);
        }
        //visited = new List<bool>((int)((xe - xb + 1) * (ye - yb + 1) * (ze - zb + 1))) ;
        //lable = new List<int>((int)((xe - xb + 1) * (ye - yb + 1) * (ze - zb + 1)));
        foreach(var voxel in this.voxels)
        {
            int x = (int)(voxel.x-xb);
            int y = (int)(voxel.y-yb);
            int z = (int)(voxel.z-zb);
            int offset = z * M * N + y * M + x;
            visited[offset] = false;
        }

        int comID = 0;
        bool moreCom = true;
        while (moreCom)
        {
            moreCom = false;
            for(int i = 0; i < M; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    for (int k = 0; k < K; k++)
                    {
                        int offset = k * M * N + j * M + i;
                        if (!visited[offset])
                        {
                            comID++;
                            tmpClt = findSub(i, j, k, comID);

                            if (tmpClt.voxels.Count > MinVoxels * slipszie)
                            {
                                tmpClt.parent = parent;
                                tmpClt.threshold = threshold;
                                cltList.Add(tmpClt);
                            }
                            if (tmpClt.voxels.Count == voxels.Count)
                                return cltList;
                            else
                                moreCom = true;
                        }
                    }
                }
            }
        }
        return cltList;
    }


    private VoxelCluster findSub(int x, int y, int z, int comID)
    {
        int M = (int)(xe - xb + 1);
        int N = (int)(ye - yb + 1);
        int K = (int)(ze - zb + 1);
        VoxelCluster tmpClt = new VoxelCluster();
        List<Vector3> voxelList = new List<Vector3>();
        Vector3 tmpVox = new Vector3(x,y,z);
        voxelList.Add(tmpVox);
        while (voxelList.Count != 0)
        {
            tmpVox = voxelList.Last();
            voxelList.Remove(voxelList.Last());
            int offset = (int)(tmpVox.z * M * N + tmpVox.y * M + tmpVox.x);
            if (visited[offset]) continue;
            visited[offset] = true;
            lable[offset] = comID;
            tmpClt.AppendVoxel((long)tmpVox.x+xb, (long)tmpVox.y+yb, (long)tmpVox.z+zb);
            if (tmpClt.voxels.Count == voxels.Count) return tmpClt;
            for (int i = (int)tmpVox.x - 1; i <= tmpVox.x + 1; i++)
            {
                if (i < 0 || i >= M)
                    continue;
                for (int j = (int)tmpVox.y - 1; j <= tmpVox.y + 1; j++)
                {
                    if (j < 0 || j >= N)
                        continue;
                    for (int k = (int)tmpVox.z - 1; k <= tmpVox.z + 1; k++)
                    {
                        if (k < 0 || k >= K)
                            continue;
                        // append this voxel
                        voxelList.Add(new Vector3(i,j,k));
                    }
                }
            }
        }
        return tmpClt;
    }

    public void getBoundingBox()
    {

    }

    public VoxelCluster()
    {
        voxels = new List<Vector3>();
        xb = yb = zb = long.MaxValue;
        xe = ye = ze = 0;
        visited = new List<bool>();
        lable = new List<int>();
        parent = null;
        marker = null;
        MinVoxels = 5;
    }

    public Marker parent;
    public Marker marker;
    public int threshold;
    long xb, xe, yb, ye, zb, ze;
    int MinVoxels;
    public List<Vector3> voxels;
    List<bool> visited;
    List<int> lable;
    
}


