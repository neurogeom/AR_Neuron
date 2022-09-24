using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class App2 : MonoBehaviour
{ 
	// Start is called before the first frame update
	//vector<string> filelist;
	string inimg_file;
	string inmarker_file;
	string outswc_file;
	bool is_gsdt = true;
	//bool is_coverage_prune = true;//false;
	bool is_break_accept = false;
	bool is_leaf_prune = true;
	bool is_smooth = false;
	int bkg_thresh = 30;
	double length_thresh = 1.0;
	int cnn_type = 2; // default connection type 2
	int channel = 0;
	double SR_ratio = 3.0 / 9.0;

	[SerializeField] Texture3D volume;
	[SerializeField] GameObject seed;
	[SerializeField] GameObject cube;
	[SerializeField] GameObject PaintingBoard;
    [SerializeField] Texture3D occupancy;

    static public int[] targets = {};
    byte[] img1d;
    byte[] occupancyData;
    Vector3Int dims;
    Vector3 rootPos;
    List<Marker> filtered_tree = new List<Marker>();
    static Marker root;
    static Marker msfm_root;
    List<Marker> outTree;

    static public int RepairTargetIndex;
    void Start()
    {
        (dims.x,dims.y,dims.z) = (volume.width,volume.height,volume.depth);
        img1d = volume.GetPixelData<byte>(0).ToArray();
        occupancyData = occupancy.GetPixelData<byte>(0).ToArray();

        Vector3 aabbmax = cube.transform.localPosition + cube.transform.localScale * 0.5f;
        Vector3 aabbmin = cube.transform.localPosition - cube.transform.localScale * 0.5f;
        Vector3 somaPos = seed.transform.localPosition;
        Vector3 v1 = new Vector3((somaPos - aabbmin).x / (aabbmax - aabbmin).x, (somaPos - aabbmin).y / (aabbmax - aabbmin).y, (somaPos - aabbmin).z / (aabbmax - aabbmin).z);

        rootPos = new Vector3(v1.x * dims.x, v1.y * dims.y, v1.z * dims.z);
        Debug.Log(rootPos);
        //StartCoroutine(All_path_pruning2());
        //Thread childThread;
        //ThreadStart childRef = new ThreadStart(AllPathPruning2) ;
        //childThread = new Thread(childRef);
        //childThread.Start();
        //AllPathPruning2();

        root = new Marker(rootPos);
        msfm_root = new Marker(rootPos / 4);

        AllPathPruning2();

    }

    void AllPathPruning2()
    {
        
        float[] gsdt;
        double[] msfm;
        //float[] gsdt = new float[volume.width * volume.height * volume.depth];
        
        if (is_gsdt)
        {
            //float time = Time.time;

            gsdt = FastMarching.FastMarching_dt_parallel(img1d, volume.width, volume.height, volume.depth, 30);
            msfm = FastMarching.MSFM_dt_parallel(occupancyData, occupancy.width, occupancy.height, occupancy.depth, 30);
            //time = Time.time - time;

            //Debug.Log("gsdt parallel done" + " time cost:" + time);
            Debug.Log("gsdt parallel done" + " time cost:");

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

            //time = Time.time;
            //FastMarching.FastMarching_tree(root, gsdt, out outTree, volume.width, volume.height, volume.depth, 3, 30, false);

            //use eyeData
            //GameObject KeyPoints = GameObject.Find("KeyPoints");
            //Des(KeyPoints);

            //FastMarching.MSFM_tree_boost(root, gsdt, out outTree, volume.width, volume.height, volume.depth, sdf, 3, 30, false);
            FastMarching.MSFM_tree(msfm_root, msfm, out outTree, occupancy.width, occupancy.height, occupancy.depth,3, 30, false);
            
            //FastMarching.FastMarching_tree(root, gsdt, out outTree, volume.width, volume.height, volume.depth, 3, 30, false);
            FastMarching.FastMarching_tree(root, gsdt, out outTree, volume.width, volume.height, volume.depth, occupancy.width, occupancy.height, occupancy.depth, targets, 3, 30, true);
            //time = Time.time - time;
            //Debug.Log("restruction done" + " time cost:" + time);
            Debug.Log(outTree.Count);
            
            filtered_tree = new List<Marker>();
            HierarchyPrune.hierarchy_prune(outTree, out filtered_tree, img1d, volume.width, volume.height, volume.depth, 30, 15, true, SR_ratio);

            //yield return new WaitForSeconds(2);
            Primitive.CreateTree(filtered_tree, cube.transform, volume.width, volume.height, volume.depth);
            //GameObject HandTrackingManager = GameObject.Find("HandTrackingManager");
            //HandTrackingManager.GetComponent<IndexTipTrackScript>().enabled = true;
        }
    }

    public void TraceTarget(int target_index)
    {
        DestroyImmediate(GameObject.Find("Temp"));
        GameObject temp = new GameObject("Temp");
        temp.transform.position = Vector3.zero;
        Debug.Log(outTree.Count); 
        List<Marker> markers = new List<Marker>(outTree);
        FastMarching.TraceTarget(ref outTree, root, target_index, volume.width, volume.height, volume.depth, occupancy.width, occupancy.height, occupancy.depth);
        Debug.Log(outTree.Count);
        Debug.Log(markers.Count);
        Debug.Log(outTree.ToHashSet().Count);
        Debug.Log(outTree.Except(markers).Count());
        filtered_tree = new List<Marker>();
        HierarchyPrune.hierarchy_prune(outTree, out filtered_tree, img1d, volume.width, volume.height, volume.depth, 30, 15, true, SR_ratio);
        Primitive.CreateTree(filtered_tree, cube.transform, volume.width, volume.height, volume.depth);
    }

	// Update is called once per frame
	IEnumerator All_path_pruning2()
    {
		yield return new WaitForSeconds(1);
		byte[] img1d = volume.GetPixelData<byte>(0).ToArray();
		double[] gsdt;
		//float[] gsdt = new float[volume.width * volume.height * volume.depth];
		List<Marker> outTree;
		if (is_gsdt)
		{
            float time = Time.realtimeSinceStartup;
            //gsdt = FastMarching.FastMarching_dt_parallel(img1d, volume.width, volume.height, volume.depth, 30);
            gsdt = FastMarching.MSFM_dt_parallel(img1d, volume.width, volume.height, volume.depth, 30);
            time = Time.realtimeSinceStartup - time;
            Debug.Log("gsdt parallel done"+" time cost:"+time);
            yield return 0;
            //time = Time.realtimeSinceStartup;
            //gsdt = FastMarching.FastMarching_dt(img1d, volume.width, volume.height, volume.depth, 30);
            //time = Time.realtimeSinceStartup - time;
            //Debug.Log("gsdt done" + " time cost:" + time);

            //         Texture3D texture3D = new Texture3D(volume.width, volume.height, volume.depth, TextureFormat.RFloat, false);
            //var maximum = phi.Max();
            //for(int i=0;i<phi.Length;i++)
            //         {
            //	gsdt[i] = (float)(phi[i] / maximum);
            //         }
            //         texture3D.SetPixelData(gsdt, 0);
            //         texture3D.Apply();
            //         AssetDatabase.DeleteAsset("Assets/Textures/" + "gsdt" + ".Asset");
            //         AssetDatabase.CreateAsset(texture3D, "Assets/Textures/" + "gsdt" + ".Asset");
            //         AssetDatabase.SaveAssets();
            //         AssetDatabase.Refresh();

            yield return 0;
            Vector3 aabbmax = cube.transform.position + transform.localScale * 0.5f;
            Vector3 aabbmin = cube.transform.position - transform.localScale * 0.5f;
            Vector3 somaPos = seed.transform.position;
            Vector3 v1 = new Vector3((somaPos - aabbmin).x / (aabbmax - aabbmin).x, (somaPos - aabbmin).y / (aabbmax - aabbmin).y, (somaPos - aabbmin).z / (aabbmax - aabbmin).z);
            Vector3 rootPos = new Vector3(v1.x * volume.width, v1.y * volume.height, v1.z * volume.depth);
            Marker root = new Marker(rootPos);

            time = Time.realtimeSinceStartup;
            //FastMarching.FastMarching_tree(root, gsdt, out outTree, volume.width, volume.height, volume.depth, 3, 30, false);

            //use eyeData
            //GameObject KeyPoints = GameObject.Find("KeyPoints");
            //Des(KeyPoints);

            //FastMarching.MSFM_tree_boost(root, gsdt, out outTree, volume.width, volume.height, volume.depth, sdf, 3, 30, false);
            FastMarching.MSFM_tree(root, gsdt, out outTree, volume.width, volume.height, volume.depth, 3, 30, false);
            //FastMarching.FastMarching_tree(root, gsdt, out outTree, volume.width, volume.height, volume.depth,targets, 3, 30, false);
            time = Time.realtimeSinceStartup - time;
            Debug.Log("restruction done" + " time cost:" + time);
            Debug.Log(outTree.Count);
            yield return 0;

            //var filtered_tree = new List<Marker>();
            //HierarchyPrune.hierarchy_prune(outTree, out filtered_tree, img1d, volume.width, volume.height, volume.depth, 30, 5, true, SR_ratio);
            //Debug.Log(filtered_tree.Count);

            //yield return new WaitForSeconds(2);
            //Primitive.CreateTree(filtered_tree, PaintingBoard.transform);
            //GameObject HandTrackingManager = GameObject.Find("HandTrackingManager");
            //HandTrackingManager.GetComponent<IndexTipTrackScript>().enabled = true;
        }
	}
}