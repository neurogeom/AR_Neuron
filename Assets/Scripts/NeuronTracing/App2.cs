using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class App2 : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("setting")]
    public bool isMsfm = true;
    public int bkg_thresh = 30;

    //vector<string> filelist;
    string inimg_file;
    string inmarker_file;
    string outswc_file;
    bool is_gsdt = true;
    //bool is_coverage_prune = true;//false;
    bool is_break_accept = false;
    bool is_leaf_prune = true;
    bool is_smooth = false;
    double length_thresh = 1.0;
    int cnn_type = 2; // default connection type 2
    int channel = 0;
    double SR_ratio = 3.0 / 9.0;

    [SerializeField] Texture3D volume;
    [SerializeField] GameObject seed;
    [SerializeField] GameObject cube;
    [SerializeField] GameObject PaintingBoard;
    [SerializeField] Texture3D occupancy;

    public HashSet<int> targets;
    byte[] img1d;
    byte[] occupancyData;
    Vector3Int dims;
    public Vector3 rootPos;
    List<Marker> filteredTree = new List<Marker>();
    Marker root;
    Marker msfm_root;
    List<Marker> outTree;

    FastMarching fm;
    HierarchyPrune hp;

    private Vector3Int vDim,oDim;
    private List<Marker> completeTree;
    List<Marker> resampledTree;
    List<Marker> filteredBranch;
    List<Marker> resampledBranch;
    public bool trace = false;

    public TestFIM fim; 
    private void Awake()
    {
        (dims.x, dims.y, dims.z) = (volume.width, volume.height, volume.depth);
        img1d = volume.GetPixelData<byte>(0).ToArray();
        occupancyData = occupancy.GetPixelData<byte>(0).ToArray();

        Vector3 aabbmax = cube.transform.localPosition + cube.transform.localScale * 0.5f;
        Vector3 aabbmin = cube.transform.localPosition - cube.transform.localScale * 0.5f;
        Vector3 somaPos = seed.transform.localPosition;
        Vector3 v1 = new Vector3((somaPos - aabbmin).x / (aabbmax - aabbmin).x, (somaPos - aabbmin).y / (aabbmax - aabbmin).y, (somaPos - aabbmin).z / (aabbmax - aabbmin).z);

        //rootPos = new Vector3(v1.x * dims.x, v1.y * dims.y, v1.z * dims.z);
        Debug.Log(rootPos);

        root = new Marker(rootPos);
        msfm_root = new Marker(rootPos / 4);
        fm = new FastMarching();
        hp = new HierarchyPrune();
        vDim = new Vector3Int(volume.width, volume.height, volume.depth);
        oDim = new Vector3Int(occupancy.width, occupancy.height, occupancy.depth);
        targets = new HashSet<int>();
    }

    async void Start()
    {
        //await Task.Run(() =>
        //{
        //    try
        //    {
        //        AllPathPruning2();

        //    }
        //    catch (Exception ex) { Debug.LogError(ex); }
        //});

        //task.Start();
    }

    private void Update()
    {
        if (trace == true)
        {
            trace = false;
            float startTime = Time.realtimeSinceStartup;
            float time = Time.realtimeSinceStartup;
            Debug.Log(time);
            //AllPathPruning2();

            fim.PrepareDatas();
            fim.FIMDT();
            //float[] gsdt = fm.FastMarching_dt_parallel(img1d, vDim.x, vDim.y, vDim.z, bkg_thresh);

            Debug.Log($"DT cost {Time.realtimeSinceStartup - time}");
            time = Time.realtimeSinceStartup;
            
            //completeTree = fm.FastMarching_tree(root, gsdt, vDim.x, vDim.y, vDim.z, oDim.x, oDim.y, oDim.z, targets, 3, bkg_thresh, true);
            completeTree = fim.FIMTree();
            Debug.Log($"restruction cost {Time.realtimeSinceStartup - time}");
            time = Time.realtimeSinceStartup;

            Debug.Log(completeTree.Count);

            filteredTree = hp.hierarchy_prune(completeTree, img1d, vDim.x, vDim.y, vDim.z, bkg_thresh, 15, true, SR_ratio);
            Debug.Log(filteredTree.Count);

            resampledTree = hp.Resample(filteredTree, img1d, vDim.x, vDim.y, vDim.z);
            Debug.Log(resampledTree.Count);

            Debug.Log($"filter and resample cost {Time.realtimeSinceStartup - time}");
            time = Time.realtimeSinceStartup;

            Primitive.CreateTree(resampledTree, cube.transform, vDim.x, vDim.y, vDim.z);
            Debug.Log($"create tree cost {Time.realtimeSinceStartup - time}");
        }
    }

    void AllPathPruning2()
    {
        Debug.Log("task");
        float[] gsdt;
        double[] msfm;
        //float[] gsdt = new float[volume.width * volume.height * volume.depth];

        if (is_gsdt)
        {
            //float time = Time.time;

            gsdt = fm.FastMarching_dt_parallel(img1d, vDim.x,vDim.y,vDim.z, bkg_thresh);

            return;
            if (isMsfm)
            {
                msfm = fm.MSFM_dt_parallel(occupancyData, oDim.x,oDim.y,oDim.z, bkg_thresh);
                fm.MSFM_tree(msfm_root, msfm, oDim.x,oDim.y,oDim.z, 3, bkg_thresh, false);
            }
            //time = Time.time - time;

            //Debug.Log("gsdt parallel done" + " time cost:" + time);
            Debug.Log("gsdt parallel done" + " time cost:");

            {
                //yield return 0;
                //time = Time.realtimeSinceStartup;
                //gsdt = FastMarching.FastMarching_dt(img1d, volume.width, volume.height, volume.depth, 30);
                //time = Time.realtimeSinceStartup - time;
                //Debug.Log("gsdt done" + " time cost:" + time);


                //float[] gsdt_float = new float[gsdt.Length];
                //Texture3D texture3D = new Texture3D(occupancy.width, occupancy.height, occupancy.depth, TextureFormat.RFloat, false);
                //var maximum = msfm.Max()/4;
                //for (int i = 0; i < msfm.Length; i++)
                //{
                //    //gsdt[i] = (float)(gsdt[i] / maximum);
                //    gsdt_float[i] = (float)(msfm[i] / maximum);
                //}
                //texture3D.SetPixelData(gsdt_float, 0);
                //texture3D.Apply();
                //AssetDatabase.DeleteAsset("Assets/Textures/" + "fmdt" + ".Asset");
                //AssetDatabase.CreateAsset(texture3D, "Assets/Textures/" + "fmdt" + ".Asset");
                //AssetDatabase.SaveAssets();
                //AssetDatabase.Refresh();
                //return;
            }

            //time = Time.time;
            //FastMarching.FastMarching_tree(root, gsdt, out outTree, volume.width, volume.height, volume.depth, 3, 30, false);

            //use eyeData
            //GameObject KeyPoints = GameObject.Find("KeyPoints");
            //Des(KeyPoints);

            //FastMarching.MSFM_tree_boost(root, gsdt, out outTree, volume.width, volume.height, volume.depth, sdf, 3, 30, false);

            //FastMarching.FastMarching_tree(root, gsdt, out outTree, volume.width, volume.height, volume.depth, 3, 30, false);
            completeTree =  fm.FastMarching_tree(root, gsdt, vDim.x,vDim.y,vDim.z, oDim.x, oDim.y, oDim.z, targets, 3, bkg_thresh, true);
            //time = Time.time - time;
            //Debug.Log("restruction done" + " time cost:" + time);
            Debug.Log(completeTree.Count);

            filteredTree = hp.hierarchy_prune(completeTree, img1d, vDim.x, vDim.y, vDim.z, bkg_thresh, 15, true, SR_ratio);
            Debug.Log(filteredTree.Count);

            resampledTree = hp.Resample(filteredTree, img1d, vDim.x, vDim.y, vDim.z);
            Debug.Log(resampledTree.Count);


            //yield return new WaitForSeconds(2);
            
            //GameObject HandTrackingManager = GameObject.Find("HandTrackingManager");
            //HandTrackingManager.GetComponent<IndexTipTrackScript>().enabled = true;



        }
    }


    async public void TargetProcess(int target_index)
    {
        //await Task.Run(()=>TraceTarget(target_index));
        TraceTarget(target_index);
        Primitive.CreateBranch(resampledBranch, cube.transform, vDim.x, vDim.y, vDim.z);
    }

    void TraceTarget(int target_index)   
    {
        Marker branchRoot;
        List<Marker> completeBranch = fm.TraceTarget(filteredTree, out branchRoot, root, target_index, vDim.x, vDim.y, vDim.z, oDim.x, oDim.y, oDim.z, 3, bkg_thresh);

        Debug.Log("Tracing Done");

        filteredBranch = hp.hierarchy_prune_repair(completeBranch, img1d, vDim.x, vDim.y, vDim.z, bkg_thresh, 15, SR_ratio);

        Debug.Log("Pruning Done");

        Debug.Log(filteredTree.Count);
        filteredTree = filteredTree.Union(filteredBranch).ToList();
        Debug.Log(filteredTree.Count);

        resampledBranch = hp.Resample(filteredBranch, img1d, vDim.x, vDim.y, vDim.z);

        foreach(Marker m in resampledBranch)
        {
            if (m.parent == null) m.parent = branchRoot;
        }
        Debug.Log("resample done");

        Debug.Log("filteredTree count:" + filteredTree.Count);


        //Primitive.CreateBranch(branch, realBranchParentMarker, cube.transform, volume.width, volume.height, volume.depth);
        //branchMarker.parent = realBranchParentMarker;
        //Debug.Log(filtered_tree.Count);
        //filtered_tree.AddRange(branch);
        //Debug.Log(filtered_tree.Count);
    }

    // Update is called once per frame
    //IEnumerator All_path_pruning2()
    //{
    //    yield return new WaitForSeconds(1);
    //    byte[] img1d = volume.GetPixelData<byte>(0).ToArray();
    //    double[] gsdt;
    //    //float[] gsdt = new float[volume.width * volume.height * volume.depth];
    //    List<Marker> outTree;
    //    if (is_gsdt)
    //    {
    //        float time = Time.realtimeSinceStartup;
    //        //gsdt = FastMarching.FastMarching_dt_parallel(img1d, volume.width, volume.height, volume.depth, 30);
    //        gsdt = FastMarching.MSFM_dt_parallel(img1d, volume.width, volume.height, volume.depth, 30);
    //        time = Time.realtimeSinceStartup - time;
    //        Debug.Log("gsdt parallel done" + " time cost:" + time);
    //        yield return 0;
    //        //time = Time.realtimeSinceStartup;
    //        //gsdt = FastMarching.FastMarching_dt(img1d, volume.width, volume.height, volume.depth, 30);
    //        //time = Time.realtimeSinceStartup - time;
    //        //Debug.Log("gsdt done" + " time cost:" + time);

    //        //         Texture3D texture3D = new Texture3D(volume.width, volume.height, volume.depth, TextureFormat.RFloat, false);
    //        //var maximum = phi.Max();
    //        //for(int i=0;i<phi.Length;i++)
    //        //         {
    //        //	gsdt[i] = (float)(phi[i] / maximum);
    //        //         }
    //        //         texture3D.SetPixelData(gsdt, 0);
    //        //         texture3D.Apply();
    //        //         AssetDatabase.DeleteAsset("Assets/Textures/" + "gsdt" + ".Asset");
    //        //         AssetDatabase.CreateAsset(texture3D, "Assets/Textures/" + "gsdt" + ".Asset");
    //        //         AssetDatabase.SaveAssets();
    //        //         AssetDatabase.Refresh();

    //        yield return 0;
    //        Vector3 aabbmax = cube.transform.position + transform.localScale * 0.5f;
    //        Vector3 aabbmin = cube.transform.position - transform.localScale * 0.5f;
    //        Vector3 somaPos = seed.transform.position;
    //        Vector3 v1 = new Vector3((somaPos - aabbmin).x / (aabbmax - aabbmin).x, (somaPos - aabbmin).y / (aabbmax - aabbmin).y, (somaPos - aabbmin).z / (aabbmax - aabbmin).z);
    //        Vector3 rootPos = new Vector3(v1.x * volume.width, v1.y * volume.height, v1.z * volume.depth);
    //        Marker root = new Marker(rootPos);

    //        time = Time.realtimeSinceStartup;
    //        //FastMarching.FastMarching_tree(root, gsdt, out outTree, volume.width, volume.height, volume.depth, 3, 30, false);

    //        //use eyeData
    //        //GameObject KeyPoints = GameObject.Find("KeyPoints");
    //        //Des(KeyPoints);

    //        //FastMarching.MSFM_tree_boost(root, gsdt, out outTree, volume.width, volume.height, volume.depth, sdf, 3, 30, false);
    //        FastMarching.MSFM_tree(root, gsdt, out outTree, volume.width, volume.height, volume.depth, 3, 30, false);
    //        //FastMarching.FastMarching_tree(root, gsdt, out outTree, volume.width, volume.height, volume.depth,targets, 3, 30, false);
    //        time = Time.realtimeSinceStartup - time;
    //        Debug.Log("restruction done" + " time cost:" + time);
    //        Debug.Log(outTree.Count);
    //        yield return 0;

    //        //var filtered_tree = new List<Marker>();
    //        //HierarchyPrune.hierarchy_prune(outTree, out filtered_tree, img1d, volume.width, volume.height, volume.depth, 30, 5, true, SR_ratio);
    //        //Debug.Log(filtered_tree.Count);

    //        //yield return new WaitForSeconds(2);
    //        //Primitive.CreateTree(filtered_tree, PaintingBoard.transform);
    //        //GameObject HandTrackingManager = GameObject.Find("HandTrackingManager");
    //        //HandTrackingManager.GetComponent<IndexTipTrackScript>().enabled = true;
    //    }
    //}
}