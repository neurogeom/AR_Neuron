using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra.Double;
using Numpy;
using UnityEditor;

public class FastMarching : MonoBehaviour
{
    static Texture3D SDF;
    static double max_intensity;
    static double min_intensity;
    static float[] gsdt;
    static float[] phi;
    static int[] parent;
    static int[] parent_oc;
    static States[] state;
    static HeapElemX[] elems;
    enum States { ALIVE = -1, TRIAL = 0, FAR = 1, REPAIRED = 2 };

    static ConcurrentBag<int> targetBag;

    static HashSet<int> results;
    static Dictionary<int, Marker> markers;

    static byte[] gsdt_float;

    public static double[] FastMarching_dt(byte[] img, int sz0, int sz1, int sz2, int bkg_thresh = 0)
    {
        int tol_sz = sz0 * sz1 * sz2;
        int sz01 = sz0 * sz1;
        States[] state = new States[tol_sz];
        int bkg_count = 0;
        int bdr_count = 0;
        double[] gsdt = new double[tol_sz];
        for (int i = 0; i < sz0; i++)
        {
            for (int j = 0; j < sz1; j++)
            {
                for (int k = 0; k < sz2; k++)
                {
                    int index = k * sz01 + j * sz0 + i;
                    if (index > img.Length) Debug.Log(index);
                    if (img[index] < bkg_thresh)
                    {
                        gsdt[index] = img[index];
                        state[index] = States.ALIVE;
                        bkg_count++;
                    }
                    else
                    {
                        gsdt[index] = double.MaxValue;
                        //gsdt[index] = 0;
                        state[index] = States.FAR;
                    }
                }
            }
        }
        int alive = 0;
        Heap<HeapElem> heap = new Heap<HeapElem>();
        Dictionary<int, HeapElem> elems = new Dictionary<int, HeapElem>();
        for (int i = 0; i < sz0; i++)
        {
            for (int j = 0; j < sz1; j++)
            {
                for (int k = 0; k < sz2; k++)
                {
                    int index = k * sz01 + j * sz0 + i;
                    if (state[index] == States.ALIVE)
                    {
                        alive++;
                        for (int ii = -1; ii <= 1; ii++)
                        {
                            int i2 = i + ii;
                            if (i2 < 0 || i2 >= sz0) continue;
                            for (int jj = -1; jj <= 1; jj++)
                            {
                                int j2 = j + jj;
                                if (j2 < 0 || j2 >= sz1) continue;
                                for (int kk = -1; kk <= 1; kk++)
                                {
                                    int k2 = k + kk;
                                    if (k2 < 0 || k2 >= sz2) continue;
                                    int offset = Math.Abs(ii) + Math.Abs(jj) + Math.Abs(kk);
                                    if (offset > 2) continue;   //connection type=2?
                                    int index_2 = k2 * sz01 + j2 * sz0 + i2;
                                    if (state[index_2] == States.FAR)
                                    {
                                        int mini = i, minj = j, mink = k;
                                        int index_min = mink * sz01 + minj * sz0 + mini;
                                        if (gsdt[index_min] > 0)
                                        {
                                            for (int iii = -1; iii <= 1; iii++)
                                            {
                                                int i3 = i2 + iii;
                                                if (i3 < 0 || i3 >= sz0) continue;
                                                for (int jjj = -1; jjj <= 1; jjj++)
                                                {
                                                    int j3 = j2 + jjj;
                                                    if (j3 < 0 || j3 >= sz1) continue;
                                                    for (int kkk = -1; kkk <= 1; kkk++)
                                                    {
                                                        int k3 = k2 + kkk;
                                                        if (k3 < 0 || k3 >= sz2) continue;
                                                        int offset2 = Mathf.Abs(iii) + Mathf.Abs(jjj) + Mathf.Abs(kkk);
                                                        if (offset2 > 2) continue;   //connection type=2?
                                                        int index_3 = k3 * sz01 + j3 * sz0 + i3;
                                                        if (state[index_3] == States.ALIVE && gsdt[index_3] < gsdt[index_min])
                                                        {
                                                            index_min = index_3;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        gsdt[index_2] = gsdt[index_min] + img[index_2];
                                        state[index_2] = States.TRIAL;
                                        HeapElem elem = new HeapElem(index_2, gsdt[index_2]);
                                        heap.insert(elem);

                                        elems.Add(index_2, elem);
                                        bdr_count++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        Debug.Log("bkg_count: " + bkg_count);
        Debug.Log("bdr_count: " + bdr_count);

        int time_counter = bkg_count;
        double process1 = 0;
        while (!heap.empty())
        {
            double process2 = (time_counter++) * 100000.0 / tol_sz;
            //heap.RemoveAt(heap.keys[0]);
            HeapElem min_elem = heap.delete_min();
            elems.Remove(min_elem.img_index);

            int min_index = min_elem.img_index;
            state[min_index] = States.ALIVE;
            int i = (int)(min_index % sz0);
            int j = (int)((min_index / sz0) % sz1);
            int k = (int)((min_index / sz01) % sz2);
            for (int ii = -1; ii <= 1; ii++)
            {
                int w = i + ii;
                if (w < 0 || w >= sz0) continue;
                for (int jj = -1; jj <= 1; jj++)
                {
                    int h = j + jj;
                    if (h < 0 || h >= sz1) continue;
                    for (int kk = -1; kk <= 1; kk++)
                    {
                        int d = k + kk;
                        if (d < 0 || d >= sz2) continue;
                        int offset = Mathf.Abs(ii) + Mathf.Abs(jj) + Mathf.Abs(kk);
                        if (offset > 2) continue;   //connection type=2?
                        int index = d * sz01 + h * sz0 + w;

                        if (state[index] != States.ALIVE)
                        {
                            double new_dist = gsdt[min_index] + img[index] * Mathf.Sqrt(offset);

                            if (state[index] == States.FAR)
                            {
                                gsdt[index] = new_dist;
                                HeapElem elem = new HeapElem(index, new_dist);
                                heap.insert(elem);
                                elems.Add(index, elem);
                                state[index] = States.TRIAL;
                            }
                            else if (state[index] == States.TRIAL)
                            {
                                if (gsdt[index] > new_dist)
                                {
                                    gsdt[index] = new_dist;
                                    HeapElem elem;
                                    elems.TryGetValue(index, out elem);
                                    heap.adjust(elem.heap_id, gsdt[index]);
                                }
                            }
                        }
                    }
                }
            }
        }
        return gsdt;
    }

    public static float[] FastMarching_dt_parallel(byte[] img,int sz0, int sz1, int sz2, int bkg_thresh = 0)
    {
        int tol_sz = sz0 * sz1 * sz2;
        int sz01 = sz0 * sz1;
        States[] state = new States[tol_sz];
        int bkg_count = 0;
        int bdr_count = 0;
        Debug.Log(tol_sz);
        float[] gsdt = new float[tol_sz];
        Parallel.For(0, tol_sz, index =>
        {
            if (index > img.Length) Debug.Log(index);
            if (img[index] < bkg_thresh)
            {
                gsdt[index] = img[index];
                state[index] = States.ALIVE;
            }
            else
            {
                gsdt[index] = float.MaxValue;
                //gsdt[index] = 0;
                state[index] = States.FAR;
            }
        });

        int alive = 0;
        Heap<HeapElem> heap = new Heap<HeapElem>();
        ConcurrentDictionary<int, HeapElem> elems = new ConcurrentDictionary<int, HeapElem>();
        ConcurrentBag<HeapElem> concurrentBag = new ConcurrentBag<HeapElem>();

        Parallel.For(0, sz0, i =>
        {
            for (int j = 0; j < sz1; j++)
            {
                for (int k = 0; k < sz2; k++)
                {
                    int index = k * sz01 + j * sz0 + i;
                    if (state[index] == States.ALIVE)
                    {
                        for (int ii = -1; ii <= 1; ii++)
                        {
                            int i2 = (int)i + ii;
                            if (i2 < 0 || i2 >= sz0) continue;
                            for (int jj = -1; jj <= 1; jj++)
                            {
                                int j2 = j + jj;
                                if (j2 < 0 || j2 >= sz1) continue;
                                for (int kk = -1; kk <= 1; kk++)
                                {
                                    int k2 = k + kk;
                                    if (k2 < 0 || k2 >= sz2) continue;
                                    int offset = Math.Abs(ii) + Math.Abs(jj) + Math.Abs(kk);
                                    if (offset > 2) continue;   //connection type=2?
                                    int index_2 = k2 * sz01 + j2 * sz0 + i2;
                                    if (state[index_2] == States.FAR)
                                    {
                                        int mini = (int)i, minj = j, mink = k;
                                        int index_min = mink * sz01 + minj * sz0 + mini;
                                        if (gsdt[index_min] > 0)
                                        {
                                            for (int iii = -1; iii <= 1; iii++)
                                            {
                                                int i3 = i2 + iii;
                                                if (i3 < 0 || i3 >= sz0) continue;
                                                for (int jjj = -1; jjj <= 1; jjj++)
                                                {
                                                    int j3 = j2 + jjj;
                                                    if (j3 < 0 || j3 >= sz1) continue;
                                                    for (int kkk = -1; kkk <= 1; kkk++)
                                                    {
                                                        int k3 = k2 + kkk;
                                                        if (k3 < 0 || k3 >= sz2) continue;
                                                        int offset2 = Mathf.Abs(iii) + Mathf.Abs(jjj) + Mathf.Abs(kkk);
                                                        if (offset2 > 2) continue;   //connection type=2?
                                                        int index_3 = k3 * sz01 + j3 * sz0 + i3;
                                                        if (state[index_3] == States.ALIVE && gsdt[index_3] < gsdt[index_min])
                                                        {
                                                            index_min = index_3;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        gsdt[index_2] = gsdt[index_min] + img[index_2];
                                        state[index_2] = States.TRIAL;
                                        HeapElem elem = new HeapElem(index_2, gsdt[index_2]);

                                        //heap.insert(elem);

                                        if (elems.TryAdd(index_2, elem)) concurrentBag.Add(elem);
                                        //elems.Add(index_2, elem);

                                        //bdr_count++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });


        Debug.Log("bkg_count: " + bkg_count);
        Debug.Log("bdr_count: " + bdr_count);
        Debug.Log(concurrentBag.Count);
        foreach (var elem in concurrentBag)
        {
            heap.insert(elem);
        }

        int time_counter = bkg_count;
        double process1 = 0;
        while (!heap.empty())
        {
            double process2 = (time_counter++) * 100000.0 / tol_sz;
            //heap.RemoveAt(heap.keys[0]);
            HeapElem min_elem = heap.delete_min();
            HeapElem temp;
            elems.TryRemove(min_elem.img_index, out temp);
            //elems.Remove(min_elem.img_index);

            int min_index = min_elem.img_index;
            state[min_index] = States.ALIVE;
            int i = (int)(min_index % sz0);
            int j = (int)((min_index / sz0) % sz1);
            int k = (int)((min_index / sz01) % sz2);
            for (int ii = -1; ii <= 1; ii++)
            {
                int w = i + ii;
                if (w < 0 || w >= sz0) continue;
                for (int jj = -1; jj <= 1; jj++)
                {
                    int h = j + jj;
                    if (h < 0 || h >= sz1) continue;
                    for (int kk = -1; kk <= 1; kk++)
                    {
                        int d = k + kk;
                        if (d < 0 || d >= sz2) continue;
                        int offset = Mathf.Abs(ii) + Mathf.Abs(jj) + Mathf.Abs(kk);
                        if (offset > 2) continue;   //connection type=2?
                        int index = d * sz01 + h * sz0 + w;

                        if (state[index] != States.ALIVE)
                        {
                            float new_dist = gsdt[min_index] + img[index] * Mathf.Sqrt(offset);

                            if (state[index] == States.FAR)
                            {
                                gsdt[index] = new_dist;
                                HeapElem elem = new HeapElem(index, new_dist);
                                heap.insert(elem);
                                elems.TryAdd(index, elem);
                                state[index] = States.TRIAL;
                            }
                            else if (state[index] == States.TRIAL)
                            {
                                if (gsdt[index] > new_dist)
                                {
                                    gsdt[index] = new_dist;
                                    HeapElem elem;
                                    elems.TryGetValue(index, out elem);
                                    heap.adjust(elem.heap_id, gsdt[index]);
                                }
                            }
                        }
                    }
                }
            }
        }

        return gsdt;
    }

    public static double[] MSFM_dt_parallel(byte[] img, int sz0, int sz1, int sz2, int bkg_thresh = 0)
    {
        int tol_sz = sz0 * sz1 * sz2;
        int sz01 = sz0 * sz1;
        States[] state = new States[tol_sz];
        int bkg_count = 0;
        int bdr_count = 0;
        double[] gsdt = new double[tol_sz];
        targetBag = new ConcurrentBag<int>();

        Debug.Log(img.Length);
        Debug.Log(tol_sz);

        Parallel.For(0, tol_sz, index =>
        {
            if (index > img.Length) Debug.Log(index);
            if (img[index] < bkg_thresh)
            {
                gsdt[index] = img[index];
                state[index] = States.ALIVE;
                bkg_count++;
            }
            else
            {
                gsdt[index] = double.MaxValue;
                //gsdt[index] = 0;
                state[index] = States.FAR;
                targetBag.Add(index);
            }
        });

        int alive = 0;
        Heap<HeapElem> heap = new Heap<HeapElem>();
        ConcurrentDictionary<int, HeapElem> elems = new ConcurrentDictionary<int, HeapElem>();
        ConcurrentBag<HeapElem> concurrentBag = new ConcurrentBag<HeapElem>();

        Parallel.For(0, sz0, i =>
        {
            for (int j = 0; j < sz1; j++)
            {
                for (int k = 0; k < sz2; k++)
                {
                    int index = k * sz01 + j * sz0 + i;
                    if (state[index] == States.ALIVE)
                    {
                        for (int ii = -1; ii <= 1; ii++)
                        {
                            int i2 = (int)i + ii;
                            if (i2 < 0 || i2 >= sz0) continue;
                            for (int jj = -1; jj <= 1; jj++)
                            {
                                int j2 = j + jj;
                                if (j2 < 0 || j2 >= sz1) continue;
                                for (int kk = -1; kk <= 1; kk++)
                                {
                                    int k2 = k + kk;
                                    if (k2 < 0 || k2 >= sz2) continue;
                                    int offset = Math.Abs(ii) + Math.Abs(jj) + Math.Abs(kk);
                                    if (offset > 2) continue;   //connection type=2?
                                    int index_2 = k2 * sz01 + j2 * sz0 + i2;
                                    if (state[index_2] == States.FAR)
                                    {
                                        int mini = (int)i, minj = j, mink = k;
                                        int index_min = mink * sz01 + minj * sz0 + mini;
                                        if (gsdt[index_min] > 0)
                                        {
                                            for (int iii = -1; iii <= 1; iii++)
                                            {
                                                int i3 = i2 + iii;
                                                if (i3 < 0 || i3 >= sz0) continue;
                                                for (int jjj = -1; jjj <= 1; jjj++)
                                                {
                                                    int j3 = j2 + jjj;
                                                    if (j3 < 0 || j3 >= sz1) continue;
                                                    for (int kkk = -1; kkk <= 1; kkk++)
                                                    {
                                                        int k3 = k2 + kkk;
                                                        if (k3 < 0 || k3 >= sz2) continue;
                                                        int offset2 = Mathf.Abs(iii) + Mathf.Abs(jjj) + Mathf.Abs(kkk);
                                                        if (offset2 > 2) continue;   //connection type=2?
                                                        int index_3 = k3 * sz01 + j3 * sz0 + i3;
                                                        if (state[index_3] == States.ALIVE && gsdt[index_3] < gsdt[index_min])
                                                        {
                                                            index_min = index_3;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        gsdt[index_2] = gsdt[index_min] + img[index_2];
                                        state[index_2] = States.TRIAL;
                                        HeapElem elem = new HeapElem(index_2, gsdt[index_2]);

                                        //heap.insert(elem);

                                        if (elems.TryAdd(index_2, elem))
                                        {
                                            concurrentBag.Add(elem);
                                            bdr_count++;
                                        }
                                        //elems.Add(index_2, elem);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });


        Debug.Log("bkg_count: " + bkg_count);
        Debug.Log("bdr_count: " + bdr_count);
        Debug.Log(concurrentBag.Count);
        foreach (var elem in concurrentBag)
        {
            heap.insert(elem);
        }

        int time_counter = bkg_count;
        double process1 = 0;
        while (!heap.empty())
        {
            double process2 = (time_counter++) * 100000.0 / tol_sz;
            //heap.RemoveAt(heap.keys[0]);
            HeapElem min_elem = heap.delete_min();
            HeapElem temp;
            elems.TryRemove(min_elem.img_index, out temp);
            //elems.Remove(min_elem.img_index);

            int min_index = min_elem.img_index;
            state[min_index] = States.ALIVE;
            int i = (int)(min_index % sz0);
            int j = (int)((min_index / sz0) % sz1);
            int k = (int)((min_index / sz01) % sz2);
            for (int ii = -1; ii <= 1; ii++)
            {
                int w = i + ii;
                if (w < 0 || w >= sz0) continue;
                for (int jj = -1; jj <= 1; jj++)
                {
                    int h = j + jj;
                    if (h < 0 || h >= sz1) continue;
                    for (int kk = -1; kk <= 1; kk++)
                    {
                        int d = k + kk;
                        if (d < 0 || d >= sz2) continue;
                        int offset = Mathf.Abs(ii) + Mathf.Abs(jj) + Mathf.Abs(kk);
                        if (offset > 2) continue;   //connection type=2?
                        int index = d * sz01 + h * sz0 + w;

                        if (state[index] != States.ALIVE)
                        {
                            double new_dist = gsdt[min_index] + img[index] * Mathf.Sqrt(offset);

                            if (state[index] == States.FAR)
                            {
                                gsdt[index] = new_dist;
                                HeapElem elem = new HeapElem(index, new_dist);
                                heap.insert(elem);
                                elems.TryAdd(index, elem);
                                state[index] = States.TRIAL;
                            }
                            else if (state[index] == States.TRIAL)
                            {
                                if (gsdt[index] > new_dist)
                                {
                                    gsdt[index] = new_dist;
                                    HeapElem elem;
                                    elems.TryGetValue(index, out elem);
                                    heap.adjust(elem.heap_id, gsdt[index]);
                                }
                            }
                        }
                    }
                }
            }
        }
        return gsdt;
    }

    public static bool FastMarching_tree(Marker root, float[] img, out List<Marker> outTree, int sz0, int sz1, int sz2, int cnn_type = 3, int bkg_thresh = 30,
                                         bool is_break_accept = false)
    {
        double higher_thresh = 150;
        int tol_sz = sz0 * sz1 * sz2;
        int sz01 = sz0 * sz1;

        float[] gsdt = new float[tol_sz];
        float[] phi = new float[tol_sz];
        parent = new int[tol_sz];
        state = new States[tol_sz];

        img.CopyTo(gsdt, 0);

        outTree = new List<Marker>();

        max_intensity = 0;
        min_intensity = double.MaxValue;

        for (int i = 0; i < tol_sz; i++)
        {
            phi[i] = float.MaxValue;
            parent[i] = i;  // each pixel point to itself at the         statements beginning
            state[i] = States.FAR;
            max_intensity = Math.Max(max_intensity, gsdt[i]);
            min_intensity = Math.Min(min_intensity, gsdt[i]);
        }

        max_intensity -= min_intensity;
        double li = 10;
        //root.pos += new Vector3(0.5f,0.5f,0.5f);
        int root_index = (int)root.position.z * sz01 + (int)root.position.y * sz0 + (int)root.position.x;
        state[root_index] = States.ALIVE;
        phi[root_index] = 0;

        Heap<HeapElemX> heap = new Heap<HeapElemX>();
        Dictionary<int, HeapElemX> elems = new Dictionary<int, HeapElemX>();
        //init heap
        HeapElemX rootElem = new HeapElemX(root_index, phi[root_index]);
        rootElem.prev_index = root_index;
        heap.insert(rootElem);
        elems[root_index] = rootElem;
        HashSet<int> results = new HashSet<int>();

        while (!heap.empty())
        {
            HeapElemX min_elem = heap.delete_min();
            elems.Remove(min_elem.img_index);
            results.Add(min_elem.img_index);
            int min_index = min_elem.img_index;

            parent[min_index] = min_elem.prev_index;

            state[min_index] = States.ALIVE;

            int i = (int)(min_index % sz0);
            int j = (int)((min_index / sz0) % sz1);
            int k = (int)((min_index / sz01) % sz2);

            int w, h, d;
            for (int ii = -1; ii <= 1; ii++)
            {
                w = i + ii;
                if (w < 0 || w >= sz0) continue;
                for (int jj = -1; jj <= 1; jj++)
                {
                    h = j + jj;
                    if (h < 0 || h >= sz1) continue;
                    for (int kk = -1; kk <= 1; kk++)
                    {
                        d = k + kk;
                        if (d < 0 || d >= sz2) continue;
                        int offset = Math.Abs(ii) + Math.Abs(jj) + Math.Abs(kk);
                        if (offset == 0 || offset > cnn_type) continue;
                        double factor = (offset == 1) ? 1.0 : ((offset == 2) ? 1.414214 : ((offset == 3) ? 1.732051 : 0.0));
                        int index = d * sz01 + h * sz0 + w;
                        int marker_distance = (int)Vector3.Distance(root.position, new Vector3(w, h, d));
                        //double true_thresh;
                        //true_thresh = marker_distance <= 50 ? higher_thresh : bkg_thresh;
                        if (is_break_accept)
                        {

                            if (gsdt[index] < bkg_thresh && gsdt[min_index] < bkg_thresh) continue;
                        }
                        else
                        {
                            if (gsdt[index] < bkg_thresh) continue;
                        }
                        if (state[index] != States.ALIVE)
                        {
                            float new_dist = (float)(phi[min_index] + (GI(gsdt[index]) + GI(gsdt[min_index])) * factor * 0.5);
                            int prev_index = min_index;

                            if (state[index] == States.FAR)
                            {
                                phi[index] = new_dist;
                                HeapElemX elem = new HeapElemX(index, phi[index]);
                                elem.prev_index = prev_index;
                                heap.insert(elem);
                                elems[index] = elem;
                                state[index] = States.TRIAL;
                            }
                            else if (state[index] == States.TRIAL)
                            {
                                if (phi[index] > new_dist)
                                {
                                    phi[index] = new_dist;
                                    HeapElemX elem = elems[index];
                                    heap.adjust(elem.heap_id, phi[index]);
                                    elem.prev_index = prev_index;
                                }
                            }
                        }
                    }
                }
            }
        }
        Debug.Log("fast Marching done");

        //save swc tree
        Dictionary<int, Marker> markers = new Dictionary<int, Marker>();

        foreach (var index in results)
        {
            if (state[index] != States.ALIVE) continue;
            int i = (int)(index % sz0);
            int j = (int)((index / sz0) % sz1);
            int k = (int)((index / sz01) % sz2);
            Marker marker = new Marker(new Vector3(i, j, k));
            markers[index] = marker;
            outTree.Add(marker);
        }

        foreach (var index in results)
        {
            if (state[index] != States.ALIVE) continue;
            int index2 = parent[index];
            Marker marker1 = markers[index];
            Marker marker2 = markers[index2];
            if (marker1 == marker2) marker1.parent = null;
            else marker1.parent = marker2;
        }

        Debug.Log("restruction done");
        return true;
    }

    public static bool FastMarching_tree(Marker root, float[] img, out List<Marker> outTree, int sz0, int sz1, int sz2, int o_width, int o_height, int o_depth,int[] targets, 
                                        int cnn_type = 3, int bkg_thresh = 30, bool is_break_accept = false)
    {
        double higher_thresh = 150;
        int tol_sz = sz0 * sz1 * sz2;
        int sz01 = sz0 * sz1;

        gsdt = new float[tol_sz];
        phi = new float[tol_sz];
        parent = new int[tol_sz];
        state = new States[tol_sz];

        img.CopyTo(gsdt, 0);

        outTree = new List<Marker>();

        max_intensity = 0;
        min_intensity = double.MaxValue;

        for (int i = 0; i < tol_sz; i++)
        {
            phi[i] = float.MaxValue;
            parent[i] = i;  // each pixel point to itself at the         statements beginning
            state[i] = States.FAR;
            max_intensity = Math.Max(max_intensity, gsdt[i]);
            min_intensity = Math.Min(min_intensity, gsdt[i]);
        }

        max_intensity -= min_intensity;
        double li = 10;
        //root.pos += new Vector3(0.5f,0.5f,0.5f);
        int root_index = (int)root.position.z * sz01 + (int)root.position.y * sz0 + (int)root.position.x;
        state[root_index] = States.ALIVE;
        phi[root_index] = 0;

        HashSet<int> target_set = new HashSet<int>();
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != root_index) target_set.Add(targets[i]);
        }

        Heap<HeapElemX> heap = new Heap<HeapElemX>();
        Dictionary<int, HeapElemX> elems = new Dictionary<int, HeapElemX>();
        //init heap
        HeapElemX rootElem = new HeapElemX(root_index, phi[root_index]);
        rootElem.prev_index = root_index;
        heap.insert(rootElem);
        elems[root_index] = rootElem;
        results = new HashSet<int>();
        while (!heap.empty())
        {
            HeapElemX min_elem = heap.delete_min();
            elems.Remove(min_elem.img_index);
            results.Add(min_elem.img_index);

            //insert target
            if (target_set.Contains(min_elem.img_index)) target_set.Remove(min_elem.img_index);

            int min_index = min_elem.img_index;

            parent[min_index] = min_elem.prev_index;

            state[min_index] = States.ALIVE;

            int i = (int)(min_index % sz0);
            int j = (int)((min_index / sz0) % sz1);
            int k = (int)((min_index / sz01) % sz2);

            int w, h, d;
            for (int ii = -1; ii <= 1; ii++)
            {
                w = i + ii;
                if (w < 0 || w >= sz0) continue;
                for (int jj = -1; jj <= 1; jj++)
                {
                    h = j + jj;
                    if (h < 0 || h >= sz1) continue;
                    for (int kk = -1; kk <= 1; kk++)
                    {
                        d = k + kk;
                        if (d < 0 || d >= sz2) continue;
                        int offset = Math.Abs(ii) + Math.Abs(jj) + Math.Abs(kk);
                        if (offset == 0 || offset > cnn_type) continue;
                        double factor = (offset == 1) ? 1.0 : ((offset == 2) ? 1.414214 : ((offset == 3) ? 1.732051 : 0.0));
                        int index = d * sz01 + h * sz0 + w;
                        int marker_distance = (int)Vector3.Distance(root.position, new Vector3(w, h, d));
                        //double true_thresh;
                        //true_thresh = marker_distance <= 50 ? higher_thresh : bkg_thresh;
                        if (is_break_accept)
                        {

                            if (gsdt[index] < bkg_thresh && gsdt[min_index] < bkg_thresh) continue;
                        }
                        else
                        {
                            if (gsdt[index] < bkg_thresh) continue;
                        }
                        if (state[index] != States.ALIVE)
                        {
                            float new_dist = (float)(phi[min_index] + (GI(gsdt[index]) + GI(gsdt[min_index])) * factor * 0.5);
                            int prev_index = min_index;

                            if (state[index] == States.FAR)
                            {
                                phi[index] = new_dist;
                                HeapElemX elem = new HeapElemX(index, phi[index]);
                                elem.prev_index = prev_index;
                                heap.insert(elem);
                                elems[index] = elem;
                                state[index] = States.TRIAL;
                            }
                            else if (state[index] == States.TRIAL)
                            {
                                if (phi[index] > new_dist)
                                {
                                    phi[index] = new_dist;
                                    HeapElemX elem = elems[index];
                                    heap.adjust(elem.heap_id, phi[index]);
                                    elem.prev_index = prev_index;
                                }
                            }
                        }
                    }
                }
            }
        }
        Debug.Log("fast Marching done");

        gsdt_float = new byte[gsdt.Length];
        Texture3D texture3D = new Texture3D(sz0, sz1, sz2, TextureFormat.R8, false);
        texture3D.wrapMode = TextureWrapMode.Clamp;
        for (int i = 0; i < gsdt.Length; i++)
        {
            //gsdt[i] = (float)(gsdt[i] / maximum);
            if (state[i] == States.ALIVE)
            {
                gsdt_float[i] = 255;
            }
            else gsdt_float[i] = 0;
        }
        texture3D.SetPixelData(gsdt_float, 0);
        texture3D.Apply();
        AssetDatabase.DeleteAsset("Assets/Textures/" + "initial" + ".Asset");
        AssetDatabase.CreateAsset(texture3D, "Assets/Textures/" + "initial" + ".Asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        HashSet<int> searchSet = new HashSet<int>();
        Dictionary<int, int> connection = new Dictionary<int, int>();
        while (target_set.Count > 0)
        {
            float min_dis = float.MaxValue;
            int target_index = target_set.First();
            foreach (var t_index in target_set)
            {
                int x = (int)(t_index % sz0);
                int y = (int)((t_index / sz0) % sz1);
                int z = (int)((t_index / sz01) % sz2);
                float distance_toseed = Vector3.Distance(new Vector3(x, y, z), root.position);
                if (distance_toseed < min_dis)
                {
                    min_dis = distance_toseed;
                    target_index = t_index;
                }
            }
            //int target_index = target_set.First();
            target_set.Remove(target_index);
            HashSet<Vector3> voxelSet = findSubVoxels(target_index, gsdt, searchSet, results, sz0, sz1, sz2, bkg_thresh);
            if (voxelSet.Count < 3) continue;
            //(Vector3 direction, Vector3 cltAvg) = PCA(voxelSet, sz01, sz0);
            //Debug.Log(maximum_pos + " " + minimum_pos);

            /*
            //WPCA
            //var volxelList = voxelSet.ToList<Vector3>();
            //double[,] weightArr = new double[volxelList.Count, 3];
            //double[,] varArr = new double[volxelList.Count, 3];
            //for (int i = 0; i < volxelList.Count; i++)
            //{
            //    int tmp_index = (int)(volxelList[i].z * sz01 + volxelList[i].y * sz0 + volxelList[i].x);
            //    double weight = gsdt[tmp_index];
            //    weightArr[i, 0] = weight;
            //    weightArr[i, 1] = weight;
            //    weightArr[i, 2] = weight;
            //    varArr[i, 0] = volxelList[i].x;
            //    varArr[i, 1] = volxelList[i].y;
            //    varArr[i, 2] = volxelList[i].z;
            //}
            //NDarray X = np.array(varArr);
            //NDarray W = np.array(weightArr);
            //var wx_sum = np.einsum("ij,ij->j", X, W);
            ////Debug.Log(wx_sum);
            ////Debug.Log(np.sum(W, 0));
            //var mean = wx_sum / np.sum(W, 0);
            //X -= mean;
            //X *= W;

            //var covar = np.dot(X.T, X);
            //covar /= np.dot(W.T, W);
            //covar[np.isnan(covar)] = (NDarray)0;
            ////Debug.Log(covar);
            ////if self.xi != 0:
            ////Ws = weights.sum(0)
            ////covar *= np.outer(Ws, Ws) * *self.xi


            //var eigvals = (X.shape[1] - 3, X.shape[1] - 1);
            //(NDarray evals, NDarray evecs) = np.linalg.eig(covar);
            //var components_ = evecs.T;
            //var explained_variance_ = np.flipud(evals);
            //var explained_variance_ratio_ = np.flipud(evals) / covar.trace();

            //NDarray wpca_vec;
            //if (Math.Abs((double)evals[0]) > Math.Abs((double)evals[1]) && Math.Abs((double)evals[0]) > Math.Abs((double)evals[2])) wpca_vec = evecs[0];
            //else if (Math.Abs((double)evals[1]) > Math.Abs((double)evals[0]) && Math.Abs((double)evals[1]) > Math.Abs((double)evals[2])) wpca_vec = evecs[1];
            //else wpca_vec = evecs[2];
            //Debug.Log(evals);
            //Debug.Log(evecs);
            //Debug.Log(wpca_vec);

            //Vector3 wpca_direction = new Vector3((float)wpca_vec[0], (float)wpca_vec[1], (float)wpca_vec[2]).normalized;
            //Vector3 wpca_position = new Vector3((float)mean[0], (float)mean[1], (float)mean[2]) / 512.0f - new Vector3(0.5f, 0.5f, 0.5f);
            //Debug.Log(direction + " " + wpca_position);
            //var trans = GameObject.Find("PaintingBoard").transform;
            //var wpca_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //wpca_direction = trans.TransformDirection(wpca_direction);
            //wpca_sphere.transform.position = trans.TransformPoint(wpca_position);
            //wpca_sphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            //Debug.DrawLine(wpca_sphere.transform.position, wpca_sphere.transform.position + wpca_direction * 0.25f, Color.red, 1000);
            */
            //createCircle(marker.position, direction, radius);

            
            //PCA
            (Vector3 direction, Vector3 maximum_pos, Vector3 minimum_pos) = PCA(voxelSet, sz0, sz1, sz2);
            Vector3 a = Vector3.Cross(Vector3.forward, direction);
            if (a == Vector3.zero)
            {
                a = Vector3.Cross(Vector3.up, direction).normalized;
            }
            Vector3 b = Vector3.Cross(a, direction).normalized;
            int serachLength = 20;

            HashSet<int> trunk = new HashSet<int>();
            //计算第一个方向
            SearchCluster(maximum_pos, direction, 25, sz0, sz1, sz2, gsdt,phi, searchSet, results, trunk, target_set, heap, elems, connection, bkg_thresh);

            //计算另一个方向
            SearchCluster(minimum_pos, -direction, 25, sz0, sz1, sz2, gsdt,phi, searchSet, results, trunk, target_set, heap, elems, connection, bkg_thresh);
            
            (direction,maximum_pos,minimum_pos) = ParentDir(voxelSet, sz01, sz0, new Vector3Int(sz0,sz1,sz2), new Vector3Int(o_width,o_height,o_depth));
            SearchCluster(maximum_pos, direction, 25, sz0, sz1, sz2, gsdt, phi, searchSet, results, trunk, target_set, heap, elems, connection, bkg_thresh);

            ////计算另一个方向
            //SearchCluster(minimum_pos, -direction, 25, sz0, sz1, sz2, gsdt, phi, searchSet, results,  trunk, target_set, heap, elems, connection, bkg_thresh);

        }
        Debug.Log("==========done" + heap.elems.Count);

        while (!heap.empty())
        {
            HeapElemX min_elem = heap.delete_min();
            elems.Remove(min_elem.img_index);
            results.Add(min_elem.img_index);

            //insert target
            if (target_set.Contains(min_elem.img_index)) target_set.Remove(min_elem.img_index);

            int min_index = min_elem.img_index;

            parent[min_index] = min_elem.prev_index;

            state[min_index] = States.ALIVE;

            int i = (int)(min_index % sz0);
            int j = (int)((min_index / sz0) % sz1);
            int k = (int)((min_index / sz01) % sz2);

            int w, h, d;
            for (int ii = -1; ii <= 1; ii++)
            {
                w = i + ii;
                if (w < 0 || w >= sz0) continue;
                for (int jj = -1; jj <= 1; jj++)
                {
                    h = j + jj;
                    if (h < 0 || h >= sz1) continue;
                    for (int kk = -1; kk <= 1; kk++)
                    {
                        d = k + kk;
                        if (d < 0 || d >= sz2) continue;
                        int offset = Math.Abs(ii) + Math.Abs(jj) + Math.Abs(kk);
                        if (offset == 0 || offset > cnn_type) continue;
                        double factor = (offset == 1) ? 1.0 : ((offset == 2) ? 1.414214 : ((offset == 3) ? 1.732051 : 0.0));
                        int index = d * sz01 + h * sz0 + w;
                        int marker_distance = (int)Vector3.Distance(root.position, new Vector3(w, h, d));
                        //double true_thresh;
                        //true_thresh = marker_distance <= 50 ? higher_thresh : bkg_thresh;
                        if (is_break_accept)
                        {

                            if (gsdt[index] < bkg_thresh && gsdt[min_index] < bkg_thresh) continue;
                        }
                        else
                        {
                            if (gsdt[index] < bkg_thresh) continue;
                        }
                        if (state[index] != States.ALIVE)
                        {
                            float new_dist = (float)(phi[min_index] + (GI(gsdt[index]) + GI(gsdt[min_index])) * factor * 0.5);
                            int prev_index = min_index;

                            if (state[index] == States.FAR)
                            {
                                phi[index] = new_dist;
                                HeapElemX elem = new HeapElemX(index, phi[index]);
                                elem.prev_index = prev_index;
                                heap.insert(elem);
                                elems[index] = elem;
                                state[index] = States.TRIAL;
                            }
                            else if (state[index] == States.TRIAL)
                            {
                                if (phi[index] > new_dist)
                                {
                                    phi[index] = new_dist;
                                    HeapElemX elem = elems[index];
                                    heap.adjust(elem.heap_id, phi[index]);
                                    elem.prev_index = prev_index;
                                }
                            }
                        }
                    }
                }
            }

            if (connection.ContainsKey(min_index))
            {
                //Debug.Log("connection works");
                int index = connection[min_index];
                w = (int)(min_index % sz0);
                h = (int)((min_index / sz0) % sz1);
                d = (int)((min_index / sz01) % sz2);
                double factor = Vector3.Distance(new Vector3(i, j, k), new Vector3(w, h, d));
                if (state[index] != States.ALIVE)
                {
                    float new_dist = (float)(phi[min_index] + (GI(gsdt[index]) + GI(gsdt[min_index])) * factor * 0.5);
                    int prev_index = min_index;

                    if (state[index] == States.FAR)
                    {
                        phi[index] = new_dist;
                        HeapElemX elem = new HeapElemX(index, phi[index]);
                        elem.prev_index = prev_index;
                        heap.insert(elem);
                        elems[index] = elem;
                        state[index] = States.TRIAL;
                    }
                    else if (state[index] == States.TRIAL)
                    {
                        if (phi[index] > new_dist)
                        {
                            phi[index] = new_dist;
                            HeapElemX elem = elems[index];
                            heap.adjust(elem.heap_id, phi[index]);
                            elem.prev_index = prev_index;
                        }
                    }
                }
            }
        }

        texture3D = new Texture3D(sz0, sz1, sz2, TextureFormat.R8, false);
        texture3D.wrapMode = TextureWrapMode.Clamp;
        for (int i = 0; i < gsdt.Length; i++)
        {
            //gsdt[i] = (float)(gsdt[i] / maximum);
            if (gsdt_float[i] == 255) gsdt_float[i] = 128;
            else if (state[i] == States.ALIVE && gsdt_float[i] == 0)
            {
                gsdt_float[i] = 255;
            }
            else gsdt_float[i] = 0;
        }
        texture3D.SetPixelData(gsdt_float, 0);
        texture3D.Apply();
        AssetDatabase.DeleteAsset("Assets/Textures/" + "after" + ".Asset");
        AssetDatabase.CreateAsset(texture3D, "Assets/Textures/" + "after" + ".Asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        for (int i = 0; i < gsdt.Length; i++)
        {
            //gsdt[i] = (float)(gsdt[i] / maximum);
            if (gsdt_float[i] == 255) gsdt_float[i] = 128;
            else if (state[i] == States.ALIVE && gsdt_float[i] == 0)
            {
                gsdt_float[i] = 255;
            }
            else gsdt_float[i] = 0;
        }

        //save swc tree
        markers = new Dictionary<int, Marker>();

        foreach (var index in results)
        {
            if (state[index] != States.ALIVE) continue;
            int i = (int)(index % sz0);
            int j = (int)((index / sz0) % sz1);
            int k = (int)((index / sz01) % sz2);
            Marker marker = new Marker(new Vector3(i, j, k));
            markers[index] = marker;
            outTree.Add(marker);
        }

        foreach (var index in results)
        {
            if (state[index] != States.ALIVE) continue;
            int index2 = parent[index];
            Marker marker1 = markers[index];
            Marker marker2 = markers[index2];
            if (marker1 == marker2) marker1.parent = null;
            else marker1.parent = marker2;
        }

        return true;
    }

    public static bool MSFM_tree(Marker root, double[] img, out List<Marker> outTree, int sz0, int sz1, int sz2,
                                int cnn_type = 3, int bkg_thresh = 30, bool is_break_accept = false)
    {

        double higher_thresh = 150;
        int tol_sz = sz0 * sz1 * sz2;
        int sz01 = sz0 * sz1;

        double[] gsdt = new double[tol_sz];
        double[] phi = new double[tol_sz];
        parent_oc = new int[tol_sz];
        state = new States[tol_sz];

        img.CopyTo(gsdt, 0);

        outTree = new List<Marker>();

        max_intensity = 0;
        min_intensity = double.MaxValue;

        for (int i = 0; i < tol_sz; i++)
        {
            phi[i] = double.MaxValue;
            parent_oc[i] = i;  // each pixel point to itself at the         statements beginning
            state[i] = States.FAR;
            max_intensity = Math.Max(max_intensity, gsdt[i]);
            min_intensity = Math.Min(min_intensity, gsdt[i]);
        }

        //max_intensity -= min_intensity;
        double li = 10;
        //root.pos += new Vector3(0.5f,0.5f,0.5f);
        int root_index = (int)root.position.z * sz01 + (int)root.position.y * sz0 + (int)root.position.x;
        state[root_index] = States.ALIVE;
        phi[root_index] = 0;

        Heap<HeapElemX> heap = new Heap<HeapElemX>();
        heap.elems.Capacity = (int)tol_sz;
        //Dictionary<int, HeapElemX> elems = new Dictionary<int, HeapElemX>();
        elems = new HeapElemX[tol_sz];
        //init heap
        HeapElemX rootElem = new HeapElemX(root_index, phi[root_index]);
        rootElem.prev_index = root_index;
        heap.insert(rootElem);
        elems[root_index] = rootElem;
        HashSet<int> results = new HashSet<int>();
        HashSet<int> targetSet = new HashSet<int>(targetBag.ToHashSet<int>());
        targetBag.Clear();
        double max_dist = 0;
        while (!heap.empty() && targetSet.Count != 0)
        {
            HeapElemX min_elem = heap.delete_min();
            //elems.Remove(min_elem.img_index);
            results.Add(min_elem.img_index);
            if (targetSet.Contains(min_elem.img_index)) targetSet.Remove(min_elem.img_index);
            int min_index = min_elem.img_index;

            parent_oc[min_index] = min_elem.prev_index;

            state[min_index] = States.ALIVE;

            int i = (int)(min_index % sz0);
            int j = (int)((min_index / sz0) % sz1);
            int k = (int)((min_index / sz01) % sz2);

            
            int w, h, d;
            for (int ii = -1; ii <= 1; ii++)
            {
                w = i + ii;
                if (w < 0 || w >= sz0) continue;
                for (int jj = -1; jj <= 1; jj++)
                {
                    h = j + jj;
                    if (h < 0 || h >= sz1) continue;
                    for (int kk = -1; kk <= 1; kk++)
                    {
                        d = k + kk;
                        if (d < 0 || d >= sz2) continue;
                        int offset = Math.Abs(ii) + Math.Abs(jj) + Math.Abs(kk);
                        if (offset == 0 || offset > cnn_type) continue;
                        double factor = (offset == 1) ? 1.0 : ((offset == 2) ? 1.414214 : ((offset == 3) ? 1.732051 : 0.0));
                        int index = d * sz01 + h * sz0 + w;
                        int marker_distance = (int)Vector3.Distance(root.position, new Vector3(w, h, d));
                        //double true_thresh;
                        //true_thresh = marker_distance <= 50 ? higher_thresh : bkg_thresh;

                        //if (is_break_accept)
                        //{

                        //    if (gsdt[index] < bkg_thresh && gsdt[min_index] < bkg_thresh) continue;
                        //}
                        //else
                        //{
                        //    if (gsdt[index] < bkg_thresh) continue;
                        //}

                        if (state[index] != States.ALIVE)
                        {
                            double new_dist;
                            double intensity = gsdt[index];
                            if (gsdt[index] < bkg_thresh)
                            {
                                //new_dist = phi[min_index] + 1/0.0000000001;
                                new_dist = phi[min_index] + 1/ 0.0000000001;

                            }
                            else
                            {
                                new_dist = phi[min_index] + 1/((intensity / max_intensity) * (intensity / max_intensity) * (intensity / max_intensity) * (intensity / max_intensity));
                                //new_dist = phi[min_index] + (GI(gsdt[index]) + GI(gsdt[min_index])) * factor * 0.5;
                                
                            }
                            //double new_dist = phi[min_index] + (GI(gsdt[index]) + GI(gsdt[min_index])) * factor * 0.5;
                            int prev_index = min_index;
                            max_dist = Math.Max(max_dist,new_dist);
                            if (state[index] == States.FAR)
                            {
                                phi[index] = new_dist;
                                HeapElemX elem = new HeapElemX(index, phi[index]);
                                elem.prev_index = prev_index;
                                heap.insert(elem);
                                elems[index] = elem;
                                state[index] = States.TRIAL;
                            }
                            else if (state[index] == States.TRIAL)
                            {
                                if (phi[index] > new_dist)
                                {
                                    phi[index] = new_dist;
                                    HeapElemX elem = elems[index];
                                    heap.adjust(elem.heap_id, phi[index]);
                                    elem.prev_index = prev_index;
                                }
                            }
                        }
                    }
                }
            }
        }
        Debug.Log("Multi-Stencils Fast Marching done");

        float[] gsdt_float = new float[gsdt.Length];
        Texture3D texture3D = new Texture3D(sz0,sz1,sz2, TextureFormat.RFloat, false);
        Debug.Log(max_dist);
        for (int i = 0; i < gsdt.Length; i++)
        {
            //gsdt[i] = (float)(gsdt[i] / maximum);
            gsdt_float[i] = (float)(phi[i] / max_dist);
        }
        texture3D.SetPixelData(gsdt_float, 0);
        texture3D.Apply();
        AssetDatabase.DeleteAsset("Assets/Textures/" + "initial reconstrcution" + ".Asset");
        AssetDatabase.CreateAsset(texture3D, "Assets/Textures/" + "initial reconstrcution" + ".Asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        //save swc tree
        //Dictionary<int, Marker> markers = new Dictionary<int, Marker>();
        //Marker[] markers = new Marker[tol_sz];

        //foreach (var index in results)
        //{
        //    if (state[index] != States.ALIVE) continue;
        //    int i = (int)(index % sz0);
        //    int j = (int)((index / sz0) % sz1);
        //    int k = (int)((index / sz01) % sz2);
        //    Marker marker = new Marker(new Vector3(i, j, k));
        //    markers[index] = marker;
        //    outTree.Add(marker);
        //}

        //foreach (var index in results)
        //{
        //    if (state[index] != States.ALIVE) continue;
        //    int index2 = parent[index];
        //    Marker marker1 = markers[index];
        //    Marker marker2 = markers[index2];
        //    if (marker1 == marker2) marker1.parent = null;
        //    else marker1.parent = marker2;
        //}

        //Debug.Log("restruction done");
        //heap.clear();
        //Array.Resize(ref markers, 0);
        //Array.Resize(ref gsdt, 0);
        //Array.Resize(ref phi, 0);
        //Array.Resize(ref parent, 0);
        //Array.Resize(ref state, 0);
        //Array.Resize(ref elems, 0);

        return true;
    }

    //public static bool MSFM_tree_boost(Marker root, double[] img, out List<Marker> outTree, int sz0, int sz1, int sz2, Texture3D SDF, int cnn_type = 3, int bkg_thresh = 30,
    //                             bool is_break_accept = false)
    //{
    //    double higher_thresh = 150;
    //    int tol_sz = sz0 * sz1 * sz2;
    //    int sz01 = sz0 * sz1;

    //    gsdt = new double[tol_sz];
    //    phi = new double[tol_sz];
    //    parent = new int[tol_sz];
    //    state = new States[tol_sz];

    //    img.CopyTo(gsdt, 0);

    //    outTree = new List<Marker>();

    //    max_intensity = 0;
    //    min_intensity = double.MaxValue;

    //    for (int i = 0; i < tol_sz; i++)
    //    {
    //        phi[i] = double.MaxValue;
    //        parent[i] = i;  // each pixel point to itself at the         statements beginning
    //        state[i] = States.FAR;
    //        max_intensity = Math.Max(max_intensity, gsdt[i]);
    //        min_intensity = Math.Min(min_intensity, gsdt[i]);
    //    }

    //    max_intensity -= min_intensity;
    //    double li = 10;
    //    //root.pos += new Vector3(0.5f,0.5f,0.5f);
    //    int root_index = (int)root.position.z * sz01 + (int)root.position.y * sz0 + (int)root.position.x;
    //    state[root_index] = States.ALIVE;
    //    phi[root_index] = 0;

    //    Heap<HeapElemX> heap = new Heap<HeapElemX>();
    //    elems = new HeapElemX[tol_sz];
    //    //init heap
    //    HeapElemX rootElem = new HeapElemX(root_index, phi[root_index]);
    //    rootElem.prev_index = root_index;
    //    heap.insert(rootElem);
    //    elems[root_index] = rootElem;
    //    HashSet<int> results = new HashSet<int>();
    //    HashSet<int> targetSet = new HashSet<int>(targetBag.ToHashSet<int>());

    //    while (!heap.empty() && targetSet.Count != 0)
    //    {
    //        HeapElemX min_elem = heap.delete_min();
    //        results.Add(min_elem.img_index);
    //        if (targetSet.Contains(min_elem.img_index)) targetSet.Remove(min_elem.img_index);
    //        int min_index = min_elem.img_index;

    //        parent[min_index] = min_elem.prev_index;

    //        state[min_index] = States.ALIVE;

    //        int i = (int)(min_index % sz0);
    //        int j = (int)((min_index / sz0) % sz1);
    //        int k = (int)((min_index / sz01) % sz2);

    //        int w, h, d;
    //        for (int ii = -1; ii <= 1; ii++)
    //        {
    //            w = i + ii;
    //            if (w < 0 || w >= sz0) continue;
    //            for (int jj = -1; jj <= 1; jj++)
    //            {
    //                h = j + jj;
    //                if (h < 0 || h >= sz1) continue;
    //                for (int kk = -1; kk <= 1; kk++)
    //                {
    //                    d = k + kk;
    //                    if (d < 0 || d >= sz2) continue;
    //                    int offset = Math.Abs(ii) + Math.Abs(jj) + Math.Abs(kk);
    //                    if (offset == 0 || offset > cnn_type) continue;
    //                    double factor = (offset == 1) ? 1.0 : ((offset == 2) ? 1.414214 : ((offset == 3) ? 1.732051 : 0.0));
    //                    int index = d * sz01 + h * sz0 + w;
    //                    int marker_distance = (int)Vector3.Distance(root.position, new Vector3(w, h, d));
    //                    //double true_thresh;
    //                    //true_thresh = marker_distance <= 50 ? higher_thresh : bkg_thresh;

    //                    //if (is_break_accept)
    //                    //{

    //                    //    if (gsdt[index] < bkg_thresh && gsdt[min_index] < bkg_thresh) continue;
    //                    //}
    //                    //else
    //                    //{
    //                    //    if (gsdt[index] < bkg_thresh) continue;
    //                    //}

    //                    if (state[index] != States.ALIVE)
    //                    {
    //                        double new_dist;
    //                        double intensity = gsdt[index];
    //                        if (gsdt[index] < bkg_thresh)
    //                        {
    //                            new_dist = 0.0000000001;
    //                        }
    //                        else
    //                        {
    //                            new_dist = phi[min_index] + (intensity / max_intensity) * (intensity / max_intensity) * (intensity / max_intensity) * (intensity / max_intensity);
    //                        }
    //                        new_dist += +SDF.GetPixel(Mathf.RoundToInt(w / 512.0f * 128), Mathf.RoundToInt(h / 512.0f * 128), Mathf.RoundToInt(d / 512.0f * 128)).r;
    //                        int prev_index = min_index;

    //                        if (state[index] == States.FAR)
    //                        {
    //                            phi[index] = new_dist;
    //                            HeapElemX elem = new HeapElemX(index, phi[index]);
    //                            elem.prev_index = prev_index;
    //                            heap.insert(elem);
    //                            elems[index] = elem;
    //                            state[index] = States.TRIAL;
    //                        }
    //                        else if (state[index] == States.TRIAL)
    //                        {
    //                            if (phi[index] > new_dist)
    //                            {
    //                                phi[index] = new_dist;
    //                                HeapElemX elem = elems[index];
    //                                heap.adjust(elem.heap_id, phi[index]);
    //                                elem.prev_index = prev_index;
    //                            }
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    Debug.Log("fast Marching done");

    //    //save swc tree
    //    Marker[] markers = new Marker[tol_sz];

    //    foreach (var index in results)
    //    {
    //        if (state[index] != States.ALIVE) continue;
    //        int i = (int)(index % sz0);
    //        int j = (int)((index / sz0) % sz1);
    //        int k = (int)((index / sz01) % sz2);
    //        Marker marker = new Marker(new Vector3(i, j, k));
    //        markers[index] = marker;
    //        outTree.Add(marker);
    //    }

    //    foreach (var index in results)
    //    {
    //        if (state[index] != States.ALIVE) continue;
    //        int index2 = parent[index];
    //        Marker marker1 = markers[index];
    //        Marker marker2 = markers[index2];
    //        if (marker1 == marker2) marker1.parent = null;
    //        else marker1.parent = marker2;
    //    }

    //    Debug.Log("restruction done");
    //    return true;
    //}

    //public static List<Marker> FastMarching_repair(Marker RepairSeed, Vector3 direction, int sz0, int sz1, int sz2, int cnn_type = 3, double bkg_thresh = 30,
    //                                     bool is_break_accept = false)
    //{
    //    double higher_thresh = 150;
    //    int tol_sz = sz0 * sz1 * sz2;
    //    int sz01 = sz0 * sz1;

    //    var outTree = new List<Marker>();

    //    int root_index = (int)RepairSeed.position.z * sz01 + (int)RepairSeed.position.y * sz0 + (int)RepairSeed.position.x;
    //    state[root_index] = States.ALIVE;
    //    //phi[root_index] = 0;

    //    Heap<HeapElemX> heap = new Heap<HeapElemX>();
    //    Dictionary<int, HeapElemX> elems = new Dictionary<int, HeapElemX>();
    //    //init heap

    //    int diff = 15;
    //    float min_dist = float.MaxValue;
    //    int min_dist_index = -1;
    //    List<int> voxelList = new List<int>();
    //    HashSet<int> set = new HashSet<int>();
    //    Vector3 tmpVox = RepairSeed.position;

    //    for (int ii = -diff; ii <= diff; ii++)
    //    {
    //        int i = (int)RepairSeed.position.x + ii;
    //        if (i < 0 || i >= sz0) continue;
    //        for (int jj = -diff; jj <= diff; jj++)
    //        {
    //            int j = (int)RepairSeed.position.y + jj;
    //            if (j < 0 || j >= sz1) continue;
    //            for (int kk = -diff; kk <= diff; kk++)
    //            {
    //                int k = (int)RepairSeed.position.z + kk;
    //                if (k < 0 || k >= sz2) continue;
    //                Vector3 temp = new Vector3(i, j, k);
    //                if (Vector3.Angle(direction, temp - RepairSeed.position) > 45) continue;
    //                int index = k * sz01 + j * sz0 + i;
    //                if (state[index] == States.FAR && gsdt[index] >= bkg_thresh)
    //                {
    //                    float dist = Vector3.Distance(RepairSeed.position, temp);
    //                    int offset = Math.Abs(ii) + Math.Abs(jj) + Math.Abs(kk);
    //                    double factor = Math.Sqrt(offset);
    //                    double new_dist = Math.Sqrt(offset);
    //                    //double new_dist = phi[root_index] + (GI(gsdt[index]) + GI(gsdt[root_index])) * factor * 0.5;
    //                    if (new_dist < min_dist)
    //                    {
    //                        min_dist_index = index;
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    int x = (int)(min_dist_index % sz0);
    //    int y = (int)(((min_dist_index) / sz0) % sz1);
    //    int z = (int)(min_dist_index / sz01);

    //    Debug.Log(x + " " + y + " " + z + " " + min_dist_index);

    //    phi[min_dist_index] = min_dist;
    //    HeapElemX min_dist_elem = new HeapElemX(min_dist_index, phi[min_dist_index]);
    //    min_dist_elem.prev_index = min_dist_index;

    //    heap.insert(min_dist_elem);
    //    elems[min_dist_index] = min_dist_elem;
    //    state[min_dist_index] = States.TRIAL;

    //    HashSet<int> results = new HashSet<int>();
    //    while (!heap.empty())
    //    {
    //        HeapElemX min_elem = heap.delete_min();
    //        elems.Remove(min_elem.img_index);
    //        results.Add(min_elem.img_index);
    //        int min_index = min_elem.img_index;

    //        parent[min_index] = min_elem.prev_index;

    //        state[min_index] = States.REPAIRED;

    //        int i = (int)(min_index % sz0);
    //        int j = (int)((min_index / sz0) % sz1);
    //        int k = (int)((min_index / sz01) % sz2);

    //        int w, h, d;
    //        for (int ii = -1; ii <= 1; ii++)
    //        {
    //            w = i + ii;
    //            if (w < 0 || w >= sz0) continue;
    //            for (int jj = -1; jj <= 1; jj++)
    //            {
    //                h = j + jj;
    //                if (h < 0 || h >= sz1) continue;
    //                for (int kk = -1; kk <= 1; kk++)
    //                {
    //                    d = k + kk;
    //                    if (d < 0 || d >= sz2) continue;
    //                    int offset = Math.Abs(ii) + Math.Abs(jj) + Math.Abs(kk);
    //                    if (offset == 0 || offset > cnn_type) continue;
    //                    double factor = Math.Sqrt(offset);
    //                    int index = d * sz01 + h * sz0 + w;
    //                    int marker_distance = (int)Vector3.Distance(RepairSeed.position, new Vector3(w, h, d));
    //                    //double true_thresh;
    //                    //true_thresh = marker_distance <= 50 ? higher_thresh : bkg_thresh;
    //                    if (is_break_accept)
    //                    {

    //                        if (gsdt[index] < bkg_thresh && gsdt[min_index] <= bkg_thresh) continue;
    //                    }
    //                    else
    //                    {
    //                        if (gsdt[index] < bkg_thresh) continue;
    //                    }
    //                    if (state[index] != States.ALIVE && state[index] != States.REPAIRED)
    //                    {
    //                        double new_dist = phi[min_index] + (GI(gsdt[index]) + GI(gsdt[min_index])) * factor * 0.5;
    //                        int prev_index = min_index;

    //                        if (state[index] == States.FAR)
    //                        {
    //                            phi[index] = new_dist;
    //                            HeapElemX elem = new HeapElemX(index, phi[index]);
    //                            elem.prev_index = prev_index;
    //                            heap.insert(elem);
    //                            elems[index] = elem;
    //                            state[index] = States.TRIAL;
    //                        }
    //                        else if (state[index] == States.TRIAL)
    //                        {
    //                            if (phi[index] > new_dist)
    //                            {
    //                                phi[index] = new_dist;
    //                                HeapElemX elem = elems[index];
    //                                heap.adjust(elem.heap_id, phi[index]);
    //                                elem.prev_index = prev_index;
    //                            }
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    //save swc tree
    //    Dictionary<int, Marker> markers = new Dictionary<int, Marker>();
    //    //for (int i = 0; i < sz0; i++)
    //    //{
    //    //    for (int j = 0; j < sz1; j++)
    //    //    {
    //    //        for (int k = 0; k < sz2; k++)
    //    //        {
    //    //            int index = k * sz01 + j * sz0 + i;
    //    //            if (state[index] != States.REPAIRED) continue;
    //    //            Marker marker = new Marker(new Vector3(i, j, k));
    //    //            marker.radius = 1;
    //    //            markers[index] = marker;
    //    //            outTree.Add(marker);

    //    //        }
    //    //    }
    //    //}
    //    foreach (var index in results)
    //    {
    //        int i = (int)(index % sz0);
    //        int j = (int)((index / sz0) % sz1);
    //        int k = (int)((index / sz01) % sz2);
    //        if (state[index] != States.REPAIRED) continue;
    //        Marker marker = new Marker(new Vector3(i, j, k));
    //        marker.radius = 1;
    //        markers[index] = marker;
    //        outTree.Add(marker);

    //    }
    //    outTree.Add(RepairSeed);

    //    foreach (var index in results)
    //    {
    //        if (state[index] != States.REPAIRED) continue;
    //        int index2 = parent[index];
    //        Marker marker1 = markers[index];
    //        Marker marker2 = markers[index2];
    //        if (marker1 == marker2) marker1.parent = RepairSeed;
    //        else marker1.parent = marker2;

    //        state[index] = States.ALIVE;
    //    }

    //    //for (int i = 0; i < sz0; i++)
    //    //{
    //    //    for (int j = 0; j < sz1; j++)
    //    //    {
    //    //        for (int k = 0; k < sz2; k++)
    //    //        {
    //    //            int index = k * sz01 + j * sz0 + i;
    //    //            if (state[index] != States.REPAIRED) continue;
    //    //            int index2 = parent[index];
    //    //            Marker marker1 = markers[index];
    //    //            Marker marker2 = markers[index2];
    //    //            if (marker1 == marker2) marker1.parent = RepairSeed;
    //    //            else marker1.parent = marker2;

    //    //            state[index] = States.ALIVE;
    //    //        }
    //    //    }
    //    //}
    //    Debug.Log("repair done");
    //    return outTree;
    //}

    public static void TraceTarget(ref List<Marker> outTree, Marker root, int targetIndex,int sz0,int sz1,int sz2, int o_width,int o_height, int o_depth, int cnn_type = 3, int bkg_thresh = 30, bool is_break_accept = false)
    { 
        int sz01 = sz0 * sz1;
        HashSet<int> target_set = new HashSet<int>();
        HashSet<int> searchSet = new HashSet<int>();
        Heap<HeapElemX> heap = new Heap<HeapElemX>();
        Dictionary<int, int> connection = new Dictionary<int, int>();
        Dictionary<int, HeapElemX> elems = new Dictionary<int, HeapElemX>();
        target_set.Add(targetIndex);
        while (target_set.Count > 0)
        {
            float min_dis = float.MaxValue;
            int target_index = target_set.First();
            foreach (var t_index in target_set)
            {
                int x = (int)(t_index % sz0);
                int y = (int)((t_index / sz0) % sz1);
                int z = (int)((t_index / sz01) % sz2);
                float distance_toseed = Vector3.Distance(new Vector3(x, y, z), root.position);
                if (distance_toseed < min_dis)
                {
                    min_dis = distance_toseed;
                    target_index = t_index;
                }
            }
            //int target_index = target_set.First();
            target_set.Remove(target_index);
            HashSet<Vector3> voxelSet = findSubVoxels(target_index, gsdt, searchSet, results, sz0, sz1, sz2, bkg_thresh);
            if (voxelSet.Count < 3) continue;
            //PCA
            (Vector3 direction, Vector3 maximum_pos, Vector3 minimum_pos) = PCA(voxelSet, sz0, sz1, sz2);
            Vector3 a = Vector3.Cross(Vector3.forward, direction);
            if (a == Vector3.zero)
            {
                a = Vector3.Cross(Vector3.up, direction).normalized;
            }
            Vector3 b = Vector3.Cross(a, direction).normalized;
            int serachLength = 20;

            HashSet<int> trunk = new HashSet<int>();
            //计算第一个方向
            SearchCluster(maximum_pos, direction, 25, sz0, sz1, sz2, gsdt, phi, searchSet, results, trunk, target_set, heap, elems, connection, bkg_thresh);

            //计算另一个方向
            SearchCluster(minimum_pos, -direction, 25, sz0, sz1, sz2, gsdt, phi, searchSet, results, trunk, target_set, heap, elems, connection, bkg_thresh);

            (direction, maximum_pos, minimum_pos) = ParentDir(voxelSet, sz01, sz0, new Vector3Int(sz0, sz1, sz2), new Vector3Int(o_width, o_height, o_depth));
            SearchCluster(maximum_pos, direction, 25, sz0, sz1, sz2, gsdt, phi, searchSet, results, trunk, target_set, heap, elems, connection, bkg_thresh);

            ////计算另一个方向
            //SearchCluster(minimum_pos, -direction, 25, sz0, sz1, sz2, gsdt, phi, searchSet, results, trunk, target_set, heap, elems, connection, bkg_thresh);

        }

        Debug.Log("==========done" + heap.elems.Count);

        foreach(var elem in heap.elems)
        {
            Debug.Log(elem.img_index);
        }

        HashSet<int> addResults = new HashSet<int>();
        while (!heap.empty())
        {
            HeapElemX min_elem = heap.delete_min();
            elems.Remove(min_elem.img_index);
            addResults.Add(min_elem.img_index);

            //insert target
            if (target_set.Contains(min_elem.img_index)) target_set.Remove(min_elem.img_index);

            int min_index = min_elem.img_index;

            parent[min_index] = min_elem.prev_index;

            state[min_index] = States.ALIVE;

            int i = (int)(min_index % sz0);
            int j = (int)((min_index / sz0) % sz1);
            int k = (int)((min_index / sz01) % sz2);

            int w, h, d;
            for (int ii = -1; ii <= 1; ii++)
            {
                w = i + ii;
                if (w < 0 || w >= sz0) continue;
                for (int jj = -1; jj <= 1; jj++)
                {
                    h = j + jj;
                    if (h < 0 || h >= sz1) continue;
                    for (int kk = -1; kk <= 1; kk++)
                    {
                        d = k + kk;
                        if (d < 0 || d >= sz2) continue;
                        int offset = Math.Abs(ii) + Math.Abs(jj) + Math.Abs(kk);
                        if (offset == 0 || offset > cnn_type) continue;
                        double factor = (offset == 1) ? 1.0 : ((offset == 2) ? 1.414214 : ((offset == 3) ? 1.732051 : 0.0));
                        int index = d * sz01 + h * sz0 + w;
                        int marker_distance = (int)Vector3.Distance(root.position, new Vector3(w, h, d));
                        //double true_thresh;
                        //true_thresh = marker_distance <= 50 ? higher_thresh : bkg_thresh;
                        if (is_break_accept)
                        {

                            if (gsdt[index] < bkg_thresh && gsdt[min_index] < bkg_thresh) continue;
                        }
                        else
                        {
                            if (gsdt[index] < bkg_thresh) continue;
                        }
                        if (state[index] != States.ALIVE)
                        {
                            float new_dist = (float)(phi[min_index] + (GI(gsdt[index]) + GI(gsdt[min_index])) * factor * 0.5);
                            int prev_index = min_index;

                            if (state[index] == States.FAR)
                            {
                                phi[index] = new_dist;
                                HeapElemX elem = new HeapElemX(index, phi[index]);
                                elem.prev_index = prev_index;
                                heap.insert(elem);
                                elems[index] = elem;
                                state[index] = States.TRIAL;
                            }
                            else if (state[index] == States.TRIAL)
                            {
                                if (phi[index] > new_dist)
                                {
                                    phi[index] = new_dist;
                                    HeapElemX elem = elems[index];
                                    heap.adjust(elem.heap_id, phi[index]);
                                    elem.prev_index = prev_index;
                                }
                            }
                        }
                    }
                }
            }

            if (connection.ContainsKey(min_index))
            {
                //Debug.Log("connection works");
                int index = connection[min_index];
                w = (int)(min_index % sz0);
                h = (int)((min_index / sz0) % sz1);
                d = (int)((min_index / sz01) % sz2);
                double factor = Vector3.Distance(new Vector3(i, j, k), new Vector3(w, h, d));
                if (state[index] != States.ALIVE)
                {
                    float new_dist = (float)(phi[min_index] + (GI(gsdt[index]) + GI(gsdt[min_index])) * factor * 0.5);
                    int prev_index = min_index;

                    if (state[index] == States.FAR)
                    {
                        phi[index] = new_dist;
                        HeapElemX elem = new HeapElemX(index, phi[index]);
                        elem.prev_index = prev_index;
                        heap.insert(elem);
                        elems[index] = elem;
                        state[index] = States.TRIAL;
                    }
                    else if (state[index] == States.TRIAL)
                    {
                        if (phi[index] > new_dist)
                        {
                            phi[index] = new_dist;
                            HeapElemX elem = elems[index];
                            heap.adjust(elem.heap_id, phi[index]);
                            elem.prev_index = prev_index;
                        }
                    }
                }
            }
        }

        //Texture3D texture3D = new Texture3D(sz0, sz1, sz2, TextureFormat.R8, false);
        //texture3D.wrapMode = TextureWrapMode.Clamp;
        //for (int i = 0; i < gsdt.Length; i++)
        //{
        //    //gsdt[i] = (float)(gsdt[i] / maximum);
        //    if (gsdt_float[i] == 255) gsdt_float[i] = 255;
        //    else if (state[i] == States.ALIVE && gsdt_float[i] == 0)
        //    {
        //        gsdt_float[i] = 255;
        //    }
        //    else gsdt_float[i] = 0;
        //}
        //texture3D.SetPixelData(gsdt_float, 0);
        //texture3D.Apply();
        //AssetDatabase.DeleteAsset("Assets/Textures/" + "after" + ".Asset");
        //AssetDatabase.CreateAsset(texture3D, "Assets/Textures/" + "after" + ".Asset");
        //AssetDatabase.SaveAssets();
        //AssetDatabase.Refresh();

        //save swc tree
        Debug.Log("addResults count:"+addResults.Count);
        Debug.Log("Results count:" + results.Count);
        
        results.UnionWith(addResults);

        Debug.Log(results.Count);

        markers.Clear();

        outTree = new List<Marker>();
        foreach (var index in results)
        {
            if (state[index] != States.ALIVE) continue;
            int i = (int)(index % sz0);
            int j = (int)((index / sz0) % sz1);
            int k = (int)((index / sz01) % sz2);
            Marker marker = new Marker(new Vector3(i, j, k));
            markers[index] = marker;
            outTree.Add(marker);
        }

        foreach (var index in results)
        {
            if (state[index] != States.ALIVE) continue;
            int index2 = parent[index];
            Marker marker1 = markers[index];
            Marker marker2 = markers[index2];
            if (marker2 == null) Debug.Log(index+" "+ index2);
            if (marker1 == marker2)
            {
                marker1.parent = null;
                Debug.Log(marker1);
            }
            else marker1.parent = marker2;
        }
        Debug.Log("Results count:" + results.Count);
        Debug.Log("repair done");
    }


    public static double GI(double intensity)
    {
        double lamda = 10;
        double ret = Math.Exp(lamda * (1 - intensity / max_intensity) * (1 - intensity / max_intensity));
        return ret;
    }

    private static HashSet<Vector3> findSubVoxels(int index, float[] gsdt, HashSet<int> searchSet, HashSet<int> results, int sz0, int sz1, int sz2, int bkg_thresh = 30)
    {
        int sz01 = sz0 * sz1;
        HashSet<Vector3> tmpClt = new HashSet<Vector3>();
        Queue<Vector3> voxelList = new Queue<Vector3>();
        int x = (int)(index % sz0);
        int y = (int)((index / sz0) % sz1);
        int z = (int)((index / sz01) % sz2);
        Vector3 tmpVox = new Vector3(x, y, z);
        voxelList.Enqueue(tmpVox);
        int offset = 1;
        while (voxelList.Count != 0 && voxelList.Count < 1000)
        {
            tmpVox = voxelList.Dequeue();
            tmpClt.Add(tmpVox);

            for (int i = (int)tmpVox.x - offset; i <= tmpVox.x + offset; i++)
            {
                if (i < 0 || i >= sz0) continue;
                for (int j = (int)tmpVox.y - offset; j <= tmpVox.y + offset; j++)
                {
                    if (j < 0 || j >= sz1) continue;
                    for (int k = (int)tmpVox.z - offset; k <= tmpVox.z + offset; k++)
                    {
                        if (k < 0 || k >= sz2) continue;
                        // append this voxel
                        int tmp_index = k * sz01 + j * sz0 + i;
                        if (gsdt[tmp_index] >= bkg_thresh && !searchSet.Contains(tmp_index) && !results.Contains(tmp_index))
                        {
                            searchSet.Add(tmp_index);
                            voxelList.Enqueue(new Vector3(i, j, k));
                        }
                    }
                }
            }
        }
        return tmpClt;
    }

    private static (Vector3, Vector3, Vector3) PCA(HashSet<Vector3> voxelSet, int sz0, int sz1, int sz2)
    {
        int sz01 = sz0 * sz1;
        Vector3 average = Vector3.zero;
        List<Vector3> subVoxels = new List<Vector3>();

        foreach (var subvoxel in voxelSet)
        {
            average += subvoxel;
            int tmp_index = (int)(subvoxel.z * sz01 + subvoxel.y * sz0 + subvoxel.x);
        }
        average /= voxelSet.Count;

        foreach (var subvoxel in voxelSet)
        {
            var temp = subvoxel - average;
            subVoxels.Add(temp);
        }

        var M = new DenseMatrix(3, subVoxels.Count);

        for (int i = 0; i < subVoxels.Count; i++)
        {
            M[0, i] = subVoxels[i].x;
            M[1, i] = subVoxels[i].y;
            M[2, i] = subVoxels[i].z;
        }

        var c = (1.0 / subVoxels.Count) * M * M.Transpose();
        var cc = c.Evd();

        var eigenValues = cc.EigenValues;
        var eigenVectors = cc.EigenVectors;

        var val0 = Math.Abs(eigenValues[0].Real);
        var val1 = Math.Abs(eigenValues[1].Real);
        var val2 = Math.Abs(eigenValues[2].Real);

        var vec0 = eigenVectors.Column(0);
        var vec1 = eigenVectors.Column(1);
        var vec2 = eigenVectors.Column(2);

        var vec = vec0;
        if (val0 > val1 && val0 > val2) vec = vec0;
        else if (val1 > val0 && val1 > val2) vec = vec1;
        else vec = vec2;

        Vector3 direction = new Vector3((float)vec[0], (float)vec[1], (float)vec[2]).normalized;
        Vector3 position = new Vector3(average.x / sz0, average.y / sz1, average.z / sz2) - new Vector3(0.5f, 0.5f, 0.5f);
        //Debug.Log(direction + " " + position);
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var trans = GameObject.Find("Cube").transform;
        sphere.transform.position = trans.TransformPoint(position);
        sphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        sphere.transform.parent = GameObject.Find("SerachPoints").transform;
        Debug.DrawLine(sphere.transform.position, sphere.transform.position + trans.TransformDirection(direction) * 0.25f, Color.yellow, 1000);

        //计算点在主方向上一维投影坐标
        List<float> projections = new List<float>();
        foreach (var subVoxel in subVoxels)
        {
            projections.Add(direction.x * subVoxel.x + direction.y * subVoxel.y + direction.z * subVoxel.z);
        }

        float maximum = float.MinValue, minimum = float.MaxValue;
        int max_i = 0, min_i = 0;
        for (int i = 0; i < projections.Count; i++)
        {
            if (projections[i] > maximum)
            {
                maximum = Math.Max(maximum, projections[i]);
                max_i = i;
            }
            if (projections[i] < minimum)
            {
                minimum = Math.Min(minimum, projections[i]);
                min_i = i;
            }

        }

        Vector3 maximum_pos = subVoxels[max_i] + average;
        Vector3 minimum_pos = subVoxels[min_i] + average;

        int maximum_index = (int)(maximum_pos.z * sz01 + maximum_pos.y * sz0 + maximum_pos.x);
        int minimum_index = (int)(minimum_pos.z * sz01 + minimum_pos.y * sz0 + minimum_pos.x);
        return (direction, maximum_pos, minimum_pos);
    }

    private static void SearchCluster(Vector3 baseVoxel, Vector3 direction, int searchLength, int sz0, int sz1, int sz2, float[] gsdt, float[] phi, HashSet<int> searchSet, HashSet<int> results, HashSet<int> trunk, HashSet<int> target_set, Heap<HeapElemX> heap, Dictionary<int, HeapElemX> elems, Dictionary<int, int> connection, int bkg_thresh)
    {
        Vector3 a = Vector3.Cross(Vector3.forward, direction);
        if (a == Vector3.zero)
        {
            a = Vector3.Cross(Vector3.up, direction).normalized;
        }
        Vector3 b = Vector3.Cross(a, direction).normalized;

        int sz01 = sz0 * sz1;
        Vector3 circleCenter = baseVoxel;
        bool is_break = false;
        for (int length = 1; length < searchLength && !is_break; length++)
        {
            circleCenter += direction;
            int radius = (int)Math.Round(length * Math.Tan(Math.PI / 6));
            //GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //circle.GetComponent<MeshRenderer>().material.color = new Color(1, 0, 0, 0.5f);
            //var trans = GameObject.Find("PaintingBoard").transform;
            //Vector3 pos = circleCenter / 512.0f - new Vector3(0.5f, 0.5f, 0.5f);
            //circle.transform.position = trans.TransformPoint(pos);
            //circle.transform.localScale = new Vector3(radius*2 / (float)sz0, 0.0001f, radius*2 / (float)sz2);
            //circle.transform.up = trans.TransformDirection(direction);
            for (int r = 1; r <= radius && !is_break; r++)
            {
                for (float theta = 0; theta < 2 * Mathf.PI; theta += Mathf.PI / 36)
                {
                    Vector3 tmp = circleCenter + r * (Mathf.Cos(theta) * a + Mathf.Sin(theta) * b);
                    tmp = new Vector3(Mathf.Round(tmp.x), Mathf.Round(tmp.y), Mathf.Round(tmp.z));
                    if (tmp.x >= 0 && tmp.x < sz0 && tmp.y >= 0 && tmp.y < sz1 && tmp.z >= 0 && tmp.z < sz2)
                    {
                        int tmp_index = (int)(tmp.z * sz01 + tmp.y * sz0 + tmp.x);
                        if (gsdt[tmp_index] >= bkg_thresh)
                        {
                            int base_index = (int)(baseVoxel.z * sz01 + baseVoxel.y * sz0 + baseVoxel.x);

                            //接入重建主结构
                           
                            if (results.Contains(tmp_index))
                            {
                                Debug.Log("find main construction");
                                if (!trunk.Contains(tmp_index))
                                {
                                    trunk.Add(tmp_index);
                                    double factor = Vector3.Distance(tmp, baseVoxel);
                                    float new_dist = (float)(phi[tmp_index] + (GI(gsdt[tmp_index]) + GI(gsdt[base_index])) * factor * 0.5);
                                    phi[base_index] = new_dist;
                                    HeapElemX elem = new HeapElemX(base_index, phi[base_index]);
                                    elem.prev_index = tmp_index;
                                    heap.insert(elem);
                                    elems[base_index] = elem;
                                    //state[maximum_index] = States.TRIAL;

                                }
                                is_break = true;
                                break;
                            }
                            else if (searchSet.Contains(tmp_index))
                            {
                                connection[base_index] = tmp_index;
                                connection[tmp_index] = base_index;
                            }
                            else
                            {
                                //var tmpVoxels = findSubVoxels(tmp_index, results, sz0, sz1, sz2);
                                //if (tmpVoxels.Count < 10) continue;
                                target_set.Add(tmp_index);
                                connection[base_index] = tmp_index;
                                connection[tmp_index] = base_index;
                            }
                            //连接非连通区域
                        }
                    }
                }
            }
        }
    }

    private static (Vector3, Vector3, Vector3) ParentDir(HashSet<Vector3> voxelSet, int sz01, int sz0, Vector3Int volumeDim, Vector3Int occupancyDim)
    {
        (int v_width, int v_height, int v_depth) = (volumeDim.x,volumeDim.y,volumeDim.z);
        (int o_width, int o_height, int o_depth) = (occupancyDim.x, occupancyDim.y, occupancyDim.z);
        Vector3 average = Vector3.zero;
        List<Vector3> subVoxels = new List<Vector3>();


        foreach (var subvoxel in voxelSet)
        {
            average += subvoxel;
            int tmp_index = (int)(subvoxel.z * sz01 + subvoxel.y * sz0 + subvoxel.x);
        }
        average /= voxelSet.Count;
        foreach (var subvoxel in voxelSet)
        {
            var temp = subvoxel - average;
            subVoxels.Add(temp);
        }
        average.x = average.x / v_width * o_width; 
        average.y = average.y / v_height * o_height;
        average.z = average.z / v_depth * o_depth;
        int index_oc = ((int)average.x + (int)average.y * o_width + (int)average.z * o_width * o_height);
        int parent_index_oc = parent_oc[index_oc];
        int pp_index_oc = parent_oc[parent_index_oc];
        //Debug.Log(index_oc + " " + parent_index_oc + " " + pp_index_oc);
        Vector3 parent_pos = new Vector3(parent_index_oc % o_width, (parent_index_oc / o_width) % o_height, (parent_index_oc / o_width / o_height) % o_depth);
        Vector3 pparent_pos = new Vector3(pp_index_oc % o_width, (pp_index_oc / o_width) % o_height, (pp_index_oc / o_width / o_height) % o_depth);

        Vector3 direction = (parent_pos-average).normalized;
        Vector3 direction2 = ((pparent_pos - parent_pos) / 2 + (parent_pos - average) / 2).normalized;
        direction2 = (pparent_pos - average).normalized;
        Vector3 position = new Vector3(average.x / o_width, average.y / o_height, average.z / o_depth) - new Vector3(0.5f, 0.5f, 0.5f);
        //Debug.Log(direction + " " + position);
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var trans = GameObject.Find("Cube").transform;
        sphere.transform.position = trans.TransformPoint(position);
        sphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        sphere.transform.parent = GameObject.Find("SerachPoints").transform;

        Debug.DrawLine(sphere.transform.position, sphere.transform.position + trans.TransformDirection(direction) * 0.25f, Color.blue, 1000);
        Debug.DrawLine(sphere.transform.position, sphere.transform.position + trans.TransformDirection(direction2) * 0.25f, Color.green, 1000);

        List<float> projections = new List<float>();
        foreach (var subVoxel in subVoxels)
        {
            projections.Add(direction.x * subVoxel.x + direction.y * subVoxel.y + direction.z * subVoxel.z);
        }
        //Debug.Log(projections.Count);
        average = new Vector3(average.x / o_width * v_width, average.y / o_height * v_height, average.z / o_depth * v_depth);
        float maximum = float.MinValue, minimum = float.MaxValue;
        int max_i = 0, min_i = 0;
        for (int i = 0; i < projections.Count; i++)
        {
            if (projections[i] > maximum)
            {
                maximum = Math.Max(maximum, projections[i]);
                max_i = i;
            }
            if (projections[i] < minimum)
            {
                minimum = Math.Min(minimum, projections[i]);
                min_i = i;
            }

        }
        //Debug.Log(max_i + " " + min_i);
        Vector3 maximum_pos = subVoxels[max_i] + average;
        Vector3 minimum_pos = subVoxels[min_i] + average;

        int maximum_index = (int)(maximum_pos.z * sz01 + maximum_pos.y * sz0 + maximum_pos.x);
        int minimum_index = (int)(minimum_pos.z * sz01 + minimum_pos.y * sz0 + minimum_pos.x);
        return (direction2, maximum_pos, minimum_pos);
    }
}

