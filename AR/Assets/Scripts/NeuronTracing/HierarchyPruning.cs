using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HierarchyPrune
{
    public class HierarchySegment
    {
        public HierarchySegment parent;
        public Marker leaf_marker;
        public Marker root_marker;
        public double length;
        public int level;

        public HierarchySegment()
        {
            leaf_marker = null;
            root_marker = null;
            length = 0;
            level = 1;
            parent = null;
        }

        public void setValue(Marker _leaf, Marker _root, double _len, int _level)
        {
            leaf_marker = _leaf;
            root_marker = _root;
            length = _len;
            level = _level;
            parent = null;
        }

        public List<Marker> get_markers()
        {
            Marker p = leaf_marker;
            List<Marker> markers = new List<Marker>();
            while (p != root_marker)
            {
                markers.Add(p);
                p = p.parent;
            }
            markers.Add(root_marker);
            return markers;
        }
    }
    static void swc2topo_segs(List<Marker> inswc, out List<HierarchySegment> topo_segs, byte[] img, long sz0, long sz1, long sz2)
    {
        int tol_num = inswc.Count;
        Dictionary<Marker, int > swc_map = new Dictionary<Marker, int>();
        for(int i = 0; i < tol_num; i++)
        {
            swc_map[inswc[i]] = i;
        }
        List<Marker> leaf_markers = new List<Marker>();
        int[] childs_num = new int[tol_num];

        //find leaf_markers
        for(int i = 0; i < tol_num; i++)
        {
            childs_num[i] = 0;
        }
        for(int i = 0; i < tol_num; i++)
        {
            if (inswc[i].parent == null) continue;
            try
            {

                int parent_index = swc_map[inswc[i].parent];
                childs_num[parent_index]++;
            }
            catch (Exception e)
            {
                Debug.Log(inswc.Contains(inswc[i].parent));
                GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                newObj.GetComponent<MeshRenderer>().material.color = Color.yellow;
                newObj.transform.localScale = new Vector3(1, 1, 1) * 0.05f;
                newObj.transform.parent = GameObject.Find("Cube").transform;
                newObj.transform.localPosition = new Vector3(inswc[i].parent.position.x / 2048.0f, inswc[i].parent.position.y / 2048.0f, inswc[i].parent.position.z / 140.0f) - new Vector3(0.5f, 0.5f, 0.5f); ;
            }
            
        }
        for(int i = 0; i < tol_num; i++)
        {
            if (childs_num[i] == 0) leaf_markers.Add(inswc[i]);
        }

        int leaf_num = leaf_markers.Count;

        long tol_sz = sz0 * sz1 * sz2;
        long sz01 = sz0 * sz1;

        double[] topo_dists = new double[tol_num];
        Array.Clear(topo_dists, 0, topo_dists.Length);

        Marker[] topo_leafs = new Marker[tol_num];
        
        //calculate distance
        for(int i = 0; i < leaf_num; i++)
        {
            Marker leaf_marker = leaf_markers[i];
            Marker child_node = leaf_markers[i];
            Marker parent_node = child_node.parent;
            int child_index = swc_map[child_node];
            topo_leafs[child_index] = leaf_marker;
            topo_dists[child_index] = img[leaf_marker.img_index(sz0, sz01)] / 255.0;

            while (parent_node != null)
            {
                int parent_index = swc_map[parent_node];
                double tmp_dst = (img[parent_node.img_index(sz0, sz01)]) / 255.0 + topo_dists[child_index];
                if (tmp_dst > topo_dists[parent_index])
                {
                    topo_dists[parent_index] = tmp_dst;
                    topo_leafs[parent_index] = topo_leafs[child_index];
                }
                else break;
                child_index = parent_index;
                parent_node = parent_node.parent;
            }
        }

        //create hierarchy segments
        Dictionary<Marker, int> leaf_index_map = new Dictionary<Marker, int>();
        topo_segs = new List<HierarchySegment>();
        for(int i = 0; i < leaf_num; i++)
        {
            topo_segs.Add(new HierarchySegment());
            leaf_index_map[leaf_markers[i]] = i;
        }

        for(int i = 0; i < leaf_num; i++)
        {
            Marker leaf_marker = leaf_markers[i];
            Marker root_marker = leaf_marker;
            Marker root_parent = root_marker.parent;
            int level = 1;
            while (root_parent != null && topo_leafs[swc_map[root_parent]] == leaf_marker)
            {
                if (childs_num[swc_map[root_marker]] >= 2) level++;
                root_marker = root_parent;
                root_parent = root_marker.parent;
            }

            double dst = topo_dists[swc_map[root_marker]];

            topo_segs[i].setValue(leaf_marker, root_marker, dst, level);


            if (root_parent == null)
            {
                topo_segs[i].parent = null;
            }
            else
            {
                Marker leaf_marker2 = topo_leafs[swc_map[root_parent]];
                int leaf_index2 = leaf_index_map[leaf_marker2];
                topo_segs[i].parent = topo_segs[leaf_index2];
            }
        }
    }

    static List<Marker> topo_segs2swc(List<HierarchySegment> topo_segs, int swc_type)
    {
        var outswc = new List<Marker>();
        double min_dst = double.MaxValue;
        double max_dst = double.MinValue;
        int min_level = int.MaxValue;
        int max_level = int.MinValue;
        foreach (HierarchySegment topo_seg in topo_segs)
        {
            double dst = topo_seg.length;
            min_dst = Math.Min(dst, min_dst);
            max_dst = Math.Max(dst, max_dst);
            int level = topo_seg.level;
            min_level = Math.Min(level, min_level);
            max_level = Math.Max(level, max_level);
        }

        max_level = Math.Min(max_level, 20);

        max_dst -= min_dst;
        if (max_dst == 0) max_dst = 0.0000001;
        max_level -= min_level;
        if (max_level == 0) max_level = 1;
        foreach (HierarchySegment topo_seg in topo_segs)
        {
            double dst = topo_seg.length;
            int level = Math.Min(topo_seg.level, max_level);

            int color_id = (int)((swc_type == 0) ? (dst - min_dst) / max_dst * 254 + 20.5 : (level - min_level) / max_level * 254.0 + 20.5);
            List<Marker> tmp_markers;
            tmp_markers = topo_seg.get_markers();
            foreach(Marker marker in tmp_markers)
            {
                //marker.type = color_id;
            }
            outswc.AddRange(tmp_markers);
        }
        return outswc;
    }

    static void topo_segs2swc(HashSet<HierarchySegment> out_segs, List<HierarchySegment> filtered_segs, out List<Marker> outswc, int swc_type)
    {
        outswc = new List<Marker>();
        foreach (HierarchySegment topo_seg in filtered_segs)
        {
            int color_id = out_segs.Contains(topo_seg) ? 0 : 1;
            List<Marker> tmp_markers;
            tmp_markers = topo_seg.get_markers();
            foreach (Marker marker in tmp_markers)
            {
                marker.type = color_id;
            }
            outswc.AddRange(tmp_markers);
        }
    }

    public static void hierarchy_prune( List<Marker> inswc, out List<Marker> outswc, byte[] img, long sz0, long sz1, long sz2, double bkg_thresh = 30.0, double length_thresh = 5.0, bool isSoma = true, double SR_ratio = 1.0 / 9.0)
    {
        List<HierarchySegment> topo_segs;
        long sz01 = sz0 * sz1;
        long tol_sz = sz01 * sz2;

        swc2topo_segs(inswc, out topo_segs, img, sz0, sz1, sz2);
        Debug.Log(topo_segs.Count);
        

        List<HierarchySegment> filter_segs = new List<HierarchySegment>();
        Marker root = new Marker();
        foreach(Marker marker in inswc)
        {
            if (marker.parent == null)
            {
                root = marker;
                break;
            }
        }

        double real_thresh = 50;
        real_thresh = Math.Max(real_thresh, bkg_thresh);

        float somaR = Marker.markerRadius(img, sz0, sz1, sz2, root, real_thresh);
        Debug.Log(somaR);
        somaR = 50;

        foreach (HierarchySegment topo_seg in topo_segs)
        {
            Marker leaf_marker = topo_seg.leaf_marker;
            if (isSoma && Vector3.Distance(leaf_marker.position, root.position) < 3 * somaR)
            {
                if (topo_seg.length >= somaR * 4)
                {
                    filter_segs.Add(topo_seg);
                }
            }
            else
            {
                if (topo_seg.length >= length_thresh)
                {
                    filter_segs.Add(topo_seg);
                }
            }
        }

        Debug.Log(filter_segs.Count);
       
        //var seg_dist_map = new Dictionary<double, HashSet<HierarchySegment>>();
        //foreach(var seg in filter_segs)
        //{
        //    double dst = seg.length;
        //    if (!seg_dist_map.ContainsKey(dst))
        //    {
        //        seg_dist_map.Add(dst, new HashSet<HierarchySegment>());
        //    }
        //    seg_dist_map[dst].Add(seg);
        //}

        //calculate radius of every node
        foreach(var seg in filter_segs)
        {
            Marker leaf_marker = seg.leaf_marker;
            Marker root_marker = seg.root_marker;
            Marker p = leaf_marker;
            while (p!=root_marker.parent)
            {
                p.radius = Marker.markerRadius(img, sz0, sz1, sz2, p, real_thresh);
                p = p.parent;
            }
        }
        root.radius = somaR;
        Debug.Log("calculate radius done");

        //hierarchy pruning
        byte[] tmpimg = new byte[img.Length];
        img.CopyTo(tmpimg, 0);

        filter_segs.Sort((a, b) => { return -a.length.CompareTo(b.length); });

        var out_segs = new List<HierarchySegment>();
        double tol_sum_sig = 0.0, tol_sum_rdc = 0.0;
        var visited_segs = new HashSet<HierarchySegment>();
        int count = 0;

        foreach (var seg in filter_segs)
        {
            if (seg.parent != null && !visited_segs.Contains(seg.parent)) continue;
            Marker leaf_marker = seg.leaf_marker;
            Marker root_marker = seg.root_marker;

            double sum_sig = 0;
            double sum_rdc = 0;

            Marker p = leaf_marker;
            while (p!=root_marker.parent)
            {
                if (tmpimg[p.img_index(sz0, sz01)] == 0)
                {
                    sum_rdc += img[p.img_index(sz0, sz01)];
                }
                else
                {
                    int r = (int)p.radius;
                    long x = (long)(p.position.x);
                    long y = (long)(p.position.y);
                    long z = (long)(p.position.z);
                    double sum_sphere_size = 0;
                    double sum_delete_size = 0;
                    for(int ii = -r; ii <= r; ii++)
                    {
                        long x2 = x + ii;
                        if (x2 < 0 || x2 >= sz0) continue;
                        for (int jj = -r; jj <= r; jj++)
                        {
                            long y2 = y + jj;
                            if (y2 < 0 || y2 >= sz1) continue;
                            for (int kk = -r; kk <= r; kk++)
                            {
                                long z2 = z + kk;
                                if (z2 < 0 || z2 >= sz2) continue;
                                if (ii * ii + jj * jj + kk * kk > r * r) continue;
                                long index = z2 * sz01 + y2 * sz0 + x2;
                                sum_sphere_size++;
                                if (tmpimg[index] != img[index])
                                {
                                    sum_delete_size++;
                                }
                            }
                        }
                    }

                    if(sum_sphere_size>0&& sum_delete_size / sum_sphere_size > 0.1)
                    {
                        sum_rdc += img[p.img_index(sz0, sz01)];
                    }
                    else sum_sig += img[p.img_index(sz0, sz01)];
                }
                p = p.parent;
            }

            if (seg.parent == null || sum_rdc == 0 || (sum_sig / sum_rdc >= SR_ratio && sum_sig >= byte.MaxValue))
            {
                tol_sum_sig += sum_sig;
                tol_sum_rdc += sum_rdc;
                List<Marker> seg_markers = new List<Marker>();
                p = leaf_marker;
                while (p!=root_marker)
                {
                    if (tmpimg[p.img_index(sz0, sz01)]!= 0)
                    {
                        seg_markers.Add(p);
                    }
                    p = p.parent;
                }

                foreach (var marker in seg_markers)
                {
                    p = marker;
                    int r = (int)p.radius;
                    if (r > 0)
                    {
                        long x = (long)(p.position.x + 0.5);
                        long y = (long)(p.position.y + 0.5);
                        long z = (long)(p.position.z + 0.5);
                        double sum_sphere_size = 0;
                        double sum_delete_size = 0;
                        for (int ii = -r; ii <= r; ii++)
                        {
                            long x2 = x + ii;
                            if (x2 < 0 || x2 >= sz0) continue;
                            for (int jj = -r; jj <= r; jj++)
                            {
                                long y2 = y + jj;
                                if (y2 < 0 || y2 >= sz1) continue;
                                for (int kk = -r; kk <= r; kk++)
                                {
                                    long z2 = z + kk;
                                    if (z2 < 0 || z2 >= sz2) continue;
                                    if (ii * ii + jj * jj + kk * kk > r * r) continue;
                                    long index = z2 * sz01 + y2 * sz0 + x2;
                                    tmpimg[index] = 0;
                                }

                            }
                        }
                    }
                }

                out_segs.Add(seg);
                visited_segs.Add(seg);
            }
        }

        //evaluation
        double tree_sig = 0;
        double covered_sig = 0;
        foreach (var m in inswc)
        {
            tree_sig += img[m.img_index(sz0, sz01)];
            if (tmpimg[m.img_index(sz0, sz01)] == 0) covered_sig += img[m.img_index(sz0, sz01)];
        }
        //for (long i = 0; i < tol_sz; i++)
        //{
        //    if (tmpimg[i] == 0) covered_sig += img[i];
        //}
        Debug.Log("S/T ratio" + covered_sig / tree_sig + "(" + covered_sig + "/" + tree_sig + ")");
        Debug.Log(out_segs.Count);
        
        //topo_segs2swc(out_segs, out outswc, 0);
        //Debug.Log(outswc.Count);
        out_segs = Resample(out_segs,10);
        outswc = topo_segs2swc(out_segs, 0);
        //topo_segs2swc(visited_segs,filter_segs, out outswc, 0);
    }

    public static List<Marker> hierarchy_prune_repair(List<Marker> inswc, byte[] img, long sz0, long sz1, long sz2, double bkg_thresh = 30.0, double length_thresh = 5.0, bool isSoma = true, double SR_ratio = 1.0 / 9.0)
    {
        List<HierarchySegment> topo_segs;
        long sz01 = sz0 * sz1;
        long tol_sz = sz01 * sz2;

        swc2topo_segs(inswc, out topo_segs, img, sz0, sz1, sz2);
        Debug.Log(topo_segs.Count);

        List<HierarchySegment> filter_segs = new List<HierarchySegment>();
        Marker root = new Marker();
        foreach (Marker marker in inswc)
        {
            if (marker.parent == null)
            {
                root = marker;
                break;
            }
        }

        double real_thresh = 50;
        real_thresh = Math.Max(real_thresh, bkg_thresh);

        float somaR = Marker.markerRadius(img, sz0, sz1, sz2, root, real_thresh);
        Debug.Log(somaR);

        foreach (HierarchySegment topo_seg in topo_segs)
        {
            Marker leaf_marker = topo_seg.leaf_marker;
            Debug.Log(topo_seg.length);
            if (isSoma && Vector3.Distance(leaf_marker.position, root.position) < 3 * somaR)
            {
                if (topo_seg.length >= somaR * 4)
                {
                    filter_segs.Add(topo_seg);
                }
            }
            else
            {
                if (topo_seg.length >= length_thresh)
                {
                    filter_segs.Add(topo_seg);
                }
            }
        }

        Debug.Log(filter_segs.Count);
        if (filter_segs.Count == 0)
        {
            filter_segs = topo_segs;
        }

        //var seg_dist_map = new Dictionary<double, HashSet<HierarchySegment>>();
        //foreach(var seg in filter_segs)
        //{
        //    double dst = seg.length;
        //    if (!seg_dist_map.ContainsKey(dst))
        //    {
        //        seg_dist_map.Add(dst, new HashSet<HierarchySegment>());
        //    }
        //    seg_dist_map[dst].Add(seg);
        //}

        //calculate radius of every node
        foreach (var seg in filter_segs)
        {
            Marker leaf_marker = seg.leaf_marker;
            Marker root_marker = seg.root_marker;
            Marker p = leaf_marker;
            while (p != root_marker.parent)
            {
                p.radius = Marker.markerRadius(img, sz0, sz1, sz2, p, real_thresh);
                p = p.parent;
            }
        }
        root.radius = somaR;
        Debug.Log("calculate radius done");

        //hierarchy pruning
        byte[] tmpimg = new byte[img.Length];
        img.CopyTo(tmpimg, 0);

        filter_segs.Sort((a, b) => { return -a.length.CompareTo(b.length); });

        var out_segs = new List<HierarchySegment>();
        double tol_sum_sig = 0.0, tol_sum_rdc = 0.0;
        var visited_segs = new HashSet<HierarchySegment>();
        int count = 0;

        foreach (var seg in filter_segs)
        {
            if (seg.parent != null && !visited_segs.Contains(seg.parent)) continue;
            Marker leaf_marker = seg.leaf_marker;
            Marker root_marker = seg.root_marker;

            double sum_sig = 0;
            double sum_rdc = 0;

            Marker p = leaf_marker;
            while (p != root_marker.parent)
            {
                if (tmpimg[p.img_index(sz0, sz01)] == 0)
                {
                    sum_rdc += img[p.img_index(sz0, sz01)];
                }
                else
                {
                    int r = (int)p.radius;
                    long x = (long)(p.position.x);
                    long y = (long)(p.position.y);
                    long z = (long)(p.position.z);
                    double sum_sphere_size = 0;
                    double sum_delete_size = 0;
                    for (int ii = -r; ii <= r; ii++)
                    {
                        long x2 = x + ii;
                        if (x2 < 0 || x2 >= sz0) continue;
                        for (int jj = -r; jj <= r; jj++)
                        {
                            long y2 = y + jj;
                            if (y2 < 0 || y2 >= sz1) continue;
                            for (int kk = -r; kk <= r; kk++)
                            {
                                long z2 = z + kk;
                                if (z2 < 0 || z2 >= sz2) continue;
                                if (ii * ii + jj * jj + kk * kk > r * r) continue;
                                long index = z2 * sz01 + y2 * sz0 + x2;
                                sum_sphere_size++;
                                if (tmpimg[index] != img[index])
                                {
                                    sum_delete_size++;
                                }
                            }
                        }
                    }

                    if (sum_sphere_size > 0 && sum_delete_size / sum_sphere_size > 0.1)
                    {
                        sum_rdc += img[p.img_index(sz0, sz01)];
                    }
                    else sum_sig += img[p.img_index(sz0, sz01)];
                }
                p = p.parent;
            }

            if (seg.parent == null || sum_rdc == 0 || (sum_sig / sum_rdc >= SR_ratio && sum_sig >= byte.MaxValue))
            {
                tol_sum_sig += sum_sig;
                tol_sum_rdc += sum_rdc;
                List<Marker> seg_markers = new List<Marker>();
                p = leaf_marker;
                while (p != root_marker)
                {
                    if (tmpimg[p.img_index(sz0, sz01)] != 0)
                    {
                        seg_markers.Add(p);
                    }
                    p = p.parent;
                }

                foreach (var marker in seg_markers)
                {
                    p = marker;
                    int r = (int)p.radius;
                    if (r > 0)
                    {
                        long x = (long)(p.position.x + 0.5);
                        long y = (long)(p.position.y + 0.5);
                        long z = (long)(p.position.z + 0.5);
                        double sum_sphere_size = 0;
                        double sum_delete_size = 0;
                        for (int ii = -r; ii <= r; ii++)
                        {
                            long x2 = x + ii;
                            if (x2 < 0 || x2 >= sz0) continue;
                            for (int jj = -r; jj <= r; jj++)
                            {
                                long y2 = y + jj;
                                if (y2 < 0 || y2 >= sz1) continue;
                                for (int kk = -r; kk <= r; kk++)
                                {
                                    long z2 = z + kk;
                                    if (z2 < 0 || z2 >= sz2) continue;
                                    if (ii * ii + jj * jj + kk * kk > r * r) continue;
                                    long index = z2 * sz01 + y2 * sz0 + x2;
                                    tmpimg[index] = 0;
                                }

                            }
                        }
                    }
                }

                out_segs.Add(seg);
                visited_segs.Add(seg);
            }
        }

        //evaluation
        double tree_sig = 0;
        double covered_sig = 0;
        foreach (var m in inswc)
        {
            tree_sig += img[m.img_index(sz0, sz01)];
            if (tmpimg[m.img_index(sz0, sz01)] == 0) covered_sig += img[m.img_index(sz0, sz01)];
        }
        //for (long i = 0; i < tol_sz; i++)
        //{
        //    if (tmpimg[i] == 0) covered_sig += img[i];
        //}
        Debug.Log("S/T ratio" + covered_sig / tree_sig + "(" + covered_sig + "/" + tree_sig + ")");
        Debug.Log(out_segs.Count);

        //topo_segs2swc(out_segs, out outswc, 0);
        //Debug.Log(outswc.Count);
        out_segs = Resample(out_segs, 5);
        var outswc = topo_segs2swc(out_segs, 0);
        return outswc;
        //topo_segs2swc(visited_segs,filter_segs, out outswc, 0);
    }

    //public static float markerRadius(byte[] img, long sz0, long sz1, long sz2, Marker marker, double thresh)
    //{
    //    long sz01 = sz0 * sz1;
    //    double max_r = sz0 / 2;
    //    max_r = Math.Max(max_r, sz1 / 2);
    //    max_r = Math.Max(max_r, sz2 / 2);

    //    double tol_num = 0, bkg_num = 0;
    //    float ir;
    //    for(ir=1;ir<max_r;ir++)
    //    {
    //        tol_num = 0;
    //        bkg_num = 0;
    //        double dz, dy, dx;
    //        for (dz = -ir; dz <= ir; dz++)
    //        {
    //            for(dy = -ir; dy <= ir; dy++)
    //            {
    //                for (dx = -ir; dx <= ir; dx++)
    //                {
    //                    double r = Math.Sqrt(dz * dz + dy * dy + dx * dx);
    //                    if (r <= ir)
    //                    {
    //                        tol_num++;
    //                        long i = (long)(marker.position.x + dx);
    //                        if (i < 0 || i >= sz0) return ir;
    //                        long j = (long)(marker.position.y + dy);
    //                        if (j < 0 || j >= sz1) return ir;
    //                        long k = (long)(marker.position.z + dz);
    //                        if (k < 0 || k >= sz2) return ir;
    //                        if (img[k * sz01 + j * sz0 + i] <= thresh)
    //                        {
    //                            bkg_num++;
    //                            if ((bkg_num / tol_num > 0.01)) return ir;
    //                            //if (bkg_num>10) return ir;
    //                        }
    //                    }

    //                    //double r = Math.Sqrt(dx * dx + dy * dy + dz * dz);


    //                }
    //            }
    //        }
    //    }
    //    return ir;
    //}

    public static List<HierarchySegment> Resample(List<HierarchySegment> in_segs,int factor)
    {
        foreach (var seg in in_segs)
        {
            if (seg.root_marker.parent != null) seg.root_marker.parent.isSegment_root = true;
        }
        foreach(var seg in in_segs)
        {
            Marker marker = seg.leaf_marker;
            Marker leafMarker = seg.leaf_marker;
            Marker rootMarker = seg.root_marker;
            marker.isLeaf = true;
            while (marker!=seg.root_marker)
            {
                double length = 0;
                Marker pre_marker = marker;
                int count_marker = 0;
                while(marker!=seg.root_marker)
                {
                    length += Vector3.Distance(marker.position, marker.parent.position);
                    count_marker++;
                    marker = marker.parent;
                    if (marker.isSegment_root) break;
                }

                int count = count_marker / factor;
                double step = length/count;

                while(pre_marker != marker)
                {
                    Marker temp_marker = pre_marker;
                    double distance = 0;
                    while (distance < step && temp_marker != marker)
                    {
                        if (temp_marker.parent == null)
                        {
                            GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            newObj.GetComponent<MeshRenderer>().material.color = Color.yellow;
                            newObj.transform.localScale = new Vector3(1, 1, 1) * 0.05f;
                            newObj.transform.parent = GameObject.Find("Cube").transform;
                            newObj.transform.localPosition = new Vector3(temp_marker.position.x / 2048.0f, temp_marker.position.y / 2048.0f, temp_marker.position.z / 140.0f) - new Vector3(0.5f, 0.5f, 0.5f);

                            newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            newObj.GetComponent<MeshRenderer>().material.color = Color.yellow;
                            newObj.transform.localScale = new Vector3(1, 1, 1) * 0.05f;
                            newObj.transform.parent = GameObject.Find("Cube").transform;
                            newObj.transform.localPosition = new Vector3(leafMarker.position.x / 2048.0f, leafMarker.position.y / 2048.0f, leafMarker.position.z / 140.0f) - new Vector3(0.5f, 0.5f, 0.5f);

                            newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            newObj.GetComponent<MeshRenderer>().material.color = Color.yellow;
                            newObj.transform.localScale = new Vector3(1, 1, 1) * 0.05f;
                            newObj.transform.parent = GameObject.Find("Cube").transform;
                            newObj.transform.localPosition = new Vector3(rootMarker.position.x / 2048.0f, rootMarker.position.y / 2048.0f, rootMarker.position.z / 140.0f) - new Vector3(0.5f, 0.5f, 0.5f);
                        }
                        distance += Vector3.Distance(temp_marker.position, temp_marker.parent.position);
                        temp_marker = temp_marker.parent;
                    }
                    //double ratio = (distance - step) / Vector3.Distance(temp_marker.position, temp_marker.parent.position);
                    //Vector3 direction = temp_marker.parent.position - temp_marker.position;
                    //var new_maker = new Marker(temp_marker.position + (float)ratio * direction);
                    //new_maker.radius = (float)ratio * temp_marker.radius + (float)(1 - ratio) * temp_marker.parent.radius;
                    //new_maker.parent = temp_marker.parent;
                    pre_marker.parent = temp_marker;
                    pre_marker = temp_marker;
                }
                marker.isBranch_root = true;
            }
        }

        //foreach (var seg in in_segs)
        //{
        //    var leaf_marker = seg.leaf_marker;
        //    var root_marker = seg.root_marker;
        //    var marker = leaf_marker;
        //    var pre_marker = leaf_marker;
        //    float angle_sum=0;
        //    while (marker.parent != root_marker)
        //    {
        //        float angle = Vector3.Angle(marker.parent.position- marker.position, marker.parent.parent.position - marker.parent.position);
        //        angle_sum += angle;
        //        if (angle_sum > 10 ||marker.parent.isSegment_root)
        //        {
        //            pre_marker.angle = angle_sum;
        //            angle_sum = 0;
        //            pre_marker.parent = marker.parent;
        //            pre_marker = marker.parent;
        //        }
        //        marker = marker.parent;
        //    }
        //    pre_marker.parent = marker.parent;
        //}
        return in_segs;
    }
}
