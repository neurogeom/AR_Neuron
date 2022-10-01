
using UnityEditor;
using UnityEngine;

public class Config : MonoBehaviour 
{
    public string path;
    public Texture3D volume;
    public Texture3D occupancy;
    public int backgroundThreshold;
    public int blockSize;
    public GameObject seed;
    public GameObject cube;
    public GameObject paintingBoard;

    private void awake()
    {
        if (volume == null) return;
        string volumeName = volume.name;
        string filter = volumeName + "_occupancy";
        string[] searchfolders = {"Assets/Textures"};
        string[] result = AssetDatabase.FindAssets(filter,searchfolders);
        if(result.Length == 0)
        {
            OccupancyMapCompute compute = GameObject.Find("computeMap").GetComponent<OccupancyMapCompute>();
            compute.bkgThreshold = backgroundThreshold;
            compute.BlockSize = blockSize;
            compute.Volume = volume;
            occupancy = compute.computeOccupancyMap();
        }
            Debug.Log("find:" + result[0]);
            occupancy = AssetDatabase.LoadAssetAtPath("Assets/Textures/"+filter+".Asset",typeof(Texture3D)) as Texture3D;
            Debug.Log(occupancy.name);

    }
}
