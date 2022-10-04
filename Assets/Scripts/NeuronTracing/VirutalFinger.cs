using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public class VirutalFinger
{
    enum States { ALIVE = -1, TRIAL = 0, FAR = 1, REPAIRED = 2 };

    byte[] img;
    float[] phi;
    States[] state;
    int[] parent;
    float max_intensity;
    float min_intensity;
    int sz0, sz1, sz2;
    int sz01;
    int tol_sz;
    Dictionary<int, HeapElemX> elems;
    //for (int index= 0; index < tol_sz; index++)
    //{
    //    parent[index] = index;
    //    phi[index] = float.MaxValue;
    //    state[index] = States.FAR;
    //    max_intensity = Math.Max(max_intensity, img[index]);
    //    min_intensity = Math.Min(min_intensity, img[index]);
    //}


    public List<Marker> RefineSketchCurve(List<Marker> markerList, Texture3D volume, int bkg_thresh = 30, int radius = 5, int cnn_type = 2)
    {
        sz0 = volume.width;
        sz1 = volume.height;
        sz2 = volume.depth;
        sz01 = sz0 * sz1;
        tol_sz = sz01 * sz2;
        img = volume.GetPixelData<byte>(0).ToArray();
        phi = new float[tol_sz];
        state = new States[tol_sz];
        parent = new int[tol_sz];
        max_intensity = 255;
        min_intensity = bkg_thresh;
        elems = new Dictionary<int, HeapElemX>();
        UnityEngine.Profiling.Profiler.BeginSample("inital");
        Parallel.For(0, tol_sz, index =>
        {
            parent[index] = index;
            phi[index] = float.MaxValue;
            state[index] = States.FAR;
        });
        UnityEngine.Profiling.Profiler.EndSample();

        Debug.Log(markerList.Count);
        for(int i = 0; i < markerList.Count; i++)
        {
            Debug.Log(markerList[i].position);
        }
        markerList = ResampleMarkers(markerList, new Vector3(sz0, sz1, sz2));
        foreach(Marker marker in markerList)
        {
            Debug.Log(marker.position);
        }
        List<Marker> preList = null;


        for (int i = 1; i < markerList.Count; i++)
        {
            Marker preMarker = markerList[i - 1];
            Marker curMarker = markerList[i];
            var curList = FastMarching_Linker(preMarker, curMarker,bkg_thresh,radius,cnn_type);
            if (preList != null)
            {
                curList[0].parent = preList.Last();
                preList.AddRange(curList);
            }
            else
            {
                preList = curList;
            }
        }
        Debug.Log("total_Marker��" + preList.Count);
        preList = Smooth_Sketch_Curve(preList, 5);
        return preList;
    }
    public List<Marker> FastMarching_Linker(Marker preMarker, Marker curMarker, int bkg_thresh = 30, int radius = 5, int cnn_type = 2)
    {
        List<Marker> outList = new List<Marker>();

        Vector3 direction = (curMarker.position - preMarker.position).normalized;
        var subMarkers = Get_Disk_Markers(preMarker, direction,radius);
        var tarMarkers = Get_Disk_Markers(curMarker, direction,radius);


        Parallel.For(0, tol_sz, index =>
        {
            parent[index] = index;
            phi[index] = float.MaxValue;
            state[index] = States.FAR;
        });

        max_intensity -= min_intensity;
        float li = 10;

        float GI(float intensity)
        {
            float lamda = 10;
            float ret = (float)Math.Exp(lamda * (1 - intensity / max_intensity) * (1 - intensity / max_intensity));
            return ret;
        }
        
        Heap<HeapElemX> heap = new Heap<HeapElemX>();
        
        HashSet<int> tarSet = new HashSet<int>();
        HashSet<int> subSet = new HashSet<int>();
        UnityEngine.Profiling.Profiler.BeginSample("setMarkers");
        foreach (var marker in subMarkers)
        {
            int index = (int)marker.position.z * sz01 + (int)marker.position.y * sz0 + (int)marker.position.x;
            //Debug.Log(index);
            //Debug.Log(marker.position);
            phi[index] = 0;
            state[index] = States.ALIVE;
            HeapElemX elem = new HeapElemX(index, 0);
            elem.prev_index = index;
            heap.insert(elem);
            elems[index] = elem;
            subSet.Add(index);
        }
        foreach(var marker in tarMarkers)
        {
            int index = (int)marker.position.z * sz01 + (int)marker.position.y * sz0 + (int)marker.position.x;
            tarSet.Add(index);
        }
        UnityEngine.Profiling.Profiler.EndSample();

        int stop_index = -1;
        int count = 0;
        while (!heap.empty())
        {
            count=Math.Max(count,elems.Count);
            HeapElemX min_elem = heap.delete_min();
            elems.Remove(min_elem.img_index);

            int min_index = min_elem.img_index;
            parent[min_index] = min_elem.prev_index;

            if (tarSet.Contains(min_index))
            {
                stop_index = min_index;
                Debug.Log("aaaaa");
                break;
            }

            state[min_index] = States.ALIVE;
            int i = min_index % sz0;
            int j = (min_index / sz0) % sz1;
            int k = (min_index / sz01) % sz2;
            
            for(int offset_i = -1; offset_i <= 1; offset_i++)
            {
                int w = i + offset_i;
                if (w < 0 || w >= sz0) continue;
                for (int offset_j = -1; offset_j <= 1; offset_j++)
                {
                    int h = j + offset_j;
                    if (h < 0 || h >= sz1) continue;
                    for (int offset_k = -1; offset_k <= 1; offset_k++)
                    {
                        int d = k + offset_k;
                        if (d < 0 || d >= sz2) continue;
                        int offset = Math.Abs(offset_i) + Math.Abs(offset_j) + Math.Abs(offset_k);
                        if (offset == 0 || offset > cnn_type) continue;
                        int index = d * sz01 + h * sz0 + w;
                        float factor = (offset == 1) ? 1.0f : ((offset == 2) ? 1.414214f : ((offset == 3) ? 1.732051f : 0.0f));
                        if (state[index] != States.ALIVE)
                        {
                            float new_dist = phi[min_index] + (GI(img[index]) + GI(img[min_index])) * factor * 0.5f;
                            int prev_index = min_index;
                            if(state[index] == States.FAR)
                            {
                                phi[index] = new_dist;
                                HeapElemX elem = new HeapElemX(index, phi[index]);
                                elem.prev_index = prev_index;
                                heap.insert(elem);
                                elems[index] = elem;
                                state[index] = States.TRIAL;
                            }
                            else if(state[index] == States.TRIAL)
                            {
                                if (phi[index] > new_dist)
                                {
                                    phi[index] = new_dist;
                                    var elem = elems[index];
                                    heap.adjust(elem.heap_id, phi[index]);
                                    elem.prev_index = prev_index;
                                }
                            }
                        }
                    }
                }
            }  
        }
        Debug.Log(count);
        UnityEngine.Profiling.Profiler.EndSample();
        UnityEngine.Profiling.Profiler.BeginSample("idont know");
        {
            int index = stop_index;
            Debug.Log(index);
            Marker sonMarker = null;
            while (!subSet.Contains(index))
            {
                int i = index % sz0;
                int j = index / sz0 % sz1;
                int k = index / sz01 % sz2;
                Marker marker = new Marker(new Vector3(i, j, k));
                marker.radius = 1.0f;
                if (sonMarker != null) sonMarker.parent = marker;
                outList.Add(marker);
                sonMarker = marker;
                if (index == parent[index]) break;
                index = parent[index];
            }
            int w = index % sz0;
            int h = index / sz0 % sz1;
            int d = index / sz01 % sz2;
            var subMarker = new Marker(new Vector3(w, h, d));
            subMarker.radius = 1.0f;
            sonMarker.parent = subMarker;
            outList.Add(subMarker);
            outList.Reverse();
            Debug.Log(outList.Count+"markers linked");
        }
        UnityEngine.Profiling.Profiler.EndSample();
        return outList;
    }

    public List<Marker> Get_Disk_Markers(Marker marker, Vector3 direction, int radius = 5)
    {
        Debug.Log("circle center" + marker.position);
        //createCircle(marker.position, direction, radius);

        Vector3 a = Vector3.Cross(Vector3.forward, direction);
        if (a == Vector3.zero)
        {
            a = Vector3.Cross(Vector3.up, direction).normalized;
        }
        Vector3 b = Vector3.Cross(a, direction).normalized;
        HashSet<Vector3> set = new HashSet<Vector3>();
        set.Add(marker.position);
        for (int r = 1; r <= radius; r++)
        {
            for (float theta = 0; theta < 2*Mathf.PI; theta += Mathf.PI/36)
            {
                Vector3 tmp = marker.position + r * (Mathf.Cos(theta) * a + Mathf.Sin(theta) * b);
                tmp = new Vector3(Mathf.Round(tmp.x), Mathf.Round(tmp.y), Mathf.Round(tmp.z));
                set.Add(tmp);
            }
        }
        Debug.Log(set.Count);
        var markers = new List<Marker>();
        foreach(var pos in set)
        {
            markers.Add(new Marker(pos));
        }

        return markers;
    }

    public void createCircle(Vector3 pos,Vector3 dir,float radius)
    {
        GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var meshRenderer = circle.GetComponent<MeshRenderer>();
        meshRenderer.material.SetColor("_Color", new Color(0, 1, 0, 0.5f));
        circle.transform.parent = GameObject.Find("PaintingBoard").transform;
        circle.transform.localPosition = pos / 512 - new Vector3(0.5f, 0.5f, 0.5f);
        circle.transform.localScale = new Vector3(1/512.0f, 1 / 512.0f, 1 / 512.0f);

        Vector3 a = Vector3.Cross(Vector3.forward, dir);
        if (a == Vector3.zero)
        {
            a = Vector3.Cross(Vector3.up, dir).normalized;
        }
        Vector3 b = Vector3.Cross(a, dir).normalized;

        int Segments = 144;
        int vertices_count = Segments+1;
        Vector3[] vertices = new Vector3[vertices_count];
        vertices[0] = Vector3.zero;
        float angledegree = 360.0f;
        float angleRad = Mathf.Deg2Rad * angledegree;
        float angleCur = angleRad;
        float angledelta = angleRad / Segments;
        for (int i = 1; i < vertices_count; i++)
        {
            vertices[i] = radius * (Mathf.Cos(angleCur) * a + Mathf.Sin(angleCur) * b) ;
            angleCur -= angledelta;
        }

        //triangles
        int triangle_count = Segments * 3;
        int[] triangles = new int[triangle_count];
        for (int i = 0, vi = 1; i <= triangle_count - 1; i += 3, vi++)     //��Ϊ�ð����ָ���60�������Σ������һ������˳��Ӧ���ǣ�0 60 1��������Ҫ��������
        {
            triangles[i] = 0;
            triangles[i + 1] = vi;
            triangles[i + 2] = vi + 1;
        }
        triangles[triangle_count - 3] = 0;
        triangles[triangle_count - 2] = vertices_count - 1;
        triangles[triangle_count - 1] = 1;                  //Ϊ����ɱջ��������һ�������ε��������

        //uv:
        Vector2[] uvs = new Vector2[vertices_count];
        for (int i = 0; i < vertices_count; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / radius / 2 + 0.5f, vertices[i].z / radius / 2 + 0.5f);
        }

        //����������mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        var meshFilter = circle.GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

    }

    private List<Marker> ResampleMarkers(List<Marker> markerList, Vector3 scale)
    {
        Debug.Log("start resample");
        List<Marker> result = new List<Marker>();
        float dist_sum = 0;
        for(int i=0;i<markerList.Count;i++)
        {
            var pos = markerList[i].position;
            pos = new Vector3((int)(pos.x * scale.x), (int)(pos.y * scale.y), (int)(pos.z * scale.z));
            markerList[i] = new Marker(pos);
        }
        var preMarker = markerList[0];
        result.Add(preMarker);
        for (int i = 1; i < markerList.Count; i++)
        {
            var curMarker = markerList[i];
            dist_sum += Vector3.Distance(preMarker.position, curMarker.position);
            if (dist_sum > 10)
            {
                result.Add(curMarker);
                dist_sum = 0;
            }
            preMarker = curMarker;
        }
        if (dist_sum > 3)
        {
            result.Add(preMarker);
        }
        return result;
    }

    private List<Marker> Smooth_Sketch_Curve(List<Marker> markers, int winsize = 5)
    {
        if (winsize < 2) return null;
        int n = markers.Count;
        int halfWin = winsize / 2;

        for(int i = 1; i < n - 1; i++)
        {
            List<Marker> temp = new List<Marker>();
            List<float> weights = new List<float>();

            temp.Add(markers[i]);
            weights.Add(1.0f + halfWin);
            for(int j = 1; j <= halfWin; j++)
            {
                int k1 = Mathf.Clamp(i + j, 0, n - 1);
                int k2 = Mathf.Clamp(i - j, 0, n - 1);
                temp.Add(markers[k1]);
                temp.Add(markers[k2]);
                weights.Add(1 + halfWin - j);
                weights.Add(1 + halfWin - j);
            }

            float s = 0;
            Vector3 vec = Vector3.zero;
            for(int i2 = 0; i2 < temp.Count; i2++)
            {
                vec += weights[i2] * temp[i2].position;
                s += weights[i2];
            }
            if (s > 0)
            {
                vec /= s;
            }
            markers[i].position = vec;
        }
        return markers;
    }
}
