using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class OccupancyMapCompute : MonoBehaviour
{
    [SerializeReference] public ComputeShader occupancyCompute;
    [SerializeReference] public ComputeShader distanceCompute;
    [SerializeReference] public int BlockSize;
    [SerializeReference] public Vector3Int Dimensions;
    [SerializeReference] public Texture3D Volume;
    [SerializeReference] public Texture3D OccupancyMap;
    private RenderTexture resultTexture;
    private RenderTexture dist_swap;
    private Vector3Int Dmap;
    // Start is called before the first frame update
    void Start()
    {
        Dimensions = new Vector3Int(Volume.width, Volume.height, Volume.depth);
        Dmap = Dimensions / BlockSize;
        computeOccupancyMap2();
        //computeOccupancyMap();
        
        //computeDistanceMap2();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void computeOccupancyMap()
    {
        int kernelKey = occupancyCompute.FindKernel("OccupancyMap");
        occupancyCompute.SetInt("BlockSize", BlockSize);
        occupancyCompute.SetFloats("Dimensions", Dimensions.x, Dimensions.y, Dimensions.z);
        occupancyCompute.SetTexture(kernelKey, "Volume", Volume);

        Texture3D texture3D = new Texture3D(Dmap.x, Dmap.y, Dmap.z, TextureFormat.R8, false);

        Texture2D[] texture2Ds = new Texture2D[Dmap.z];
        List<Color> tex = new List<Color>();

        for (int i = 0; i < Dmap.z; i++)
        {
            resultTexture = new RenderTexture(Dmap.x, Dmap.y, 1, RenderTextureFormat.R8);
            resultTexture.enableRandomWrite = true;
            resultTexture.Create();
            occupancyCompute.SetTexture(kernelKey, "Result", resultTexture);
            occupancyCompute.SetInt("depth", i);
            occupancyCompute.Dispatch(kernelKey, Dmap.x / 8, Dmap.y / 8, 1);

            RenderTexture.active = resultTexture;
            texture2Ds[i] = new Texture2D(Dmap.x, Dmap.y, TextureFormat.R8, false);
            texture2Ds[i].ReadPixels(new Rect(0, 0, Dmap.x, Dmap.y), 0, 0);
            texture2Ds[i].Apply();
            Color[] temp = texture2Ds[i].GetPixels();
            tex.AddRange(temp);
            Debug.Log("compute depth:" + i);
        }
        texture3D.SetPixels(tex.ToArray());
        texture3D.Apply();
        string name = Volume.name;
        AssetDatabase.DeleteAsset("Assets/Textures/" + name + "_occupancy" + ".Asset");
        AssetDatabase.CreateAsset(texture3D, "Assets/Textures/" + name + "_occupancy" + ".Asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SavingTexture succeeded!");
        OccupancyMap = texture3D;
    }

    void computeOccupancyMap2()
    {
        int kernelKey = occupancyCompute.FindKernel("OccupancyMap");
        occupancyCompute.SetInt("BlockSize", BlockSize);
        occupancyCompute.SetFloats("Dimensions", Dimensions.x, Dimensions.y, Dimensions.z);
        occupancyCompute.SetTexture(kernelKey, "Volume", Volume);

        resultTexture = new RenderTexture(Dmap.x, Dmap.y, 0, RenderTextureFormat.R8);
        resultTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        resultTexture.volumeDepth = Dmap.z;
        resultTexture.enableRandomWrite = true;

        dist_swap = new RenderTexture(Dmap.x, Dmap.y, 0, RenderTextureFormat.RInt);
        dist_swap.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        dist_swap.volumeDepth = Dmap.z;
        dist_swap.enableRandomWrite = true;

        occupancyCompute.SetTexture(kernelKey, "Result", resultTexture);
        occupancyCompute.SetTexture(kernelKey, "dist_swap", dist_swap);
        occupancyCompute.Dispatch(kernelKey, Dmap.x / 8, Dmap.y / 8, Dmap.z / 8);

        RenderTexture.active = resultTexture;
        Texture2D tex2d = new Texture2D(Dmap.x, Dmap.y, TextureFormat.R8, false);

        var texture3D = readRenderTexture3D(resultTexture);

        string name = Volume.name;
        AssetDatabase.DeleteAsset("Assets/Textures/" + name + "_occupancy" + ".Asset");
        AssetDatabase.CreateAsset(texture3D, "Assets/Textures/" + name + "_occupancy" + ".Asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SavingTexture succeeded!");
        OccupancyMap = texture3D;
    }

    void computeDistanceMap2()
    {
        var dist = new RenderTexture(Dmap.x, Dmap.y, 0, RenderTextureFormat.RInt);
        dist.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        dist.volumeDepth = Dmap.z;
        dist.enableRandomWrite = true;

        var result = new RenderTexture(Dmap.x, Dmap.y, 0, RenderTextureFormat.R8);
        result.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        result.volumeDepth = Dmap.z;
        result.enableRandomWrite = true;

        int kernelKey = distanceCompute.FindKernel("DistanceMap");
        distanceCompute.SetInts("dM", Dmap.x, Dmap.y, Dmap.z);
        distanceCompute.SetTexture(kernelKey, "dist_swap", dist_swap);
        distanceCompute.SetTexture(kernelKey, "dist", dist);

        distanceCompute.SetInt("stage", 0);
        distanceCompute.Dispatch(kernelKey, Dmap.y / 8, Dmap.z / 8, 1);
        distanceCompute.SetInt("stage", 1);
        distanceCompute.Dispatch(kernelKey, Dmap.x / 8, Dmap.z / 8, 1);
        distanceCompute.SetInt("stage", 2);
        distanceCompute.Dispatch(kernelKey, Dmap.x / 8, Dmap.y / 8, 1);

        kernelKey = distanceCompute.FindKernel("ReadTexture");
        distanceCompute.SetTexture(kernelKey, "Result", result);
        distanceCompute.SetTexture(kernelKey, "dist", dist);
        distanceCompute.Dispatch(kernelKey, Dmap.x / 8, Dmap.y / 8, Dmap.z / 8);

        var texture3D = readRenderTexture3D(result);

        AssetDatabase.DeleteAsset("Assets/Textures/" + "Distance2" + ".Asset");
        AssetDatabase.CreateAsset(texture3D, "Assets/Textures/" + "Distance2" + ".Asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SavingTexture succeeded!");


    }

    void computeDistanceMap()
    {
        int kernelKey = distanceCompute.FindKernel("DistanceMap");
        distanceCompute.SetFloats("dM", Dmap.x, Dmap.y, Dmap.z);
        distanceCompute.SetTexture(kernelKey, "OccupancyMap", OccupancyMap);

        Texture3D texture3D = new Texture3D(Dmap.x, Dmap.y, Dmap.z, TextureFormat.ARGB32, false);

        Texture2D[] texture2Ds = new Texture2D[Dmap.z];
        List<Color> tex = new List<Color>();

        for (int i = 0; i < Dmap.z; i++)
        {
            resultTexture = new RenderTexture(Dmap.x, Dmap.y, 1, RenderTextureFormat.ARGB32);
            resultTexture.enableRandomWrite = true;
            resultTexture.Create();
            distanceCompute.SetTexture(kernelKey, "Result", resultTexture);
            distanceCompute.SetInt("depth", i);
            distanceCompute.Dispatch(kernelKey, Dmap.x / 8, Dmap.y / 8, 1);

            RenderTexture.active = resultTexture;
            texture2Ds[i] = new Texture2D(Dmap.x, Dmap.y, TextureFormat.ARGB32, false);
            texture2Ds[i].ReadPixels(new Rect(0, 0, Dmap.x, Dmap.y), 0, 0);
            texture2Ds[i].Apply();
            Color[] temp = texture2Ds[i].GetPixels();
            tex.AddRange(temp);
            Debug.Log("compute depth:" + i);
        }
        texture3D.SetPixels(tex.ToArray());
        texture3D.Apply();
        AssetDatabase.DeleteAsset("Assets/Textures/" + "Distance_compare_new" + ".Asset");
        AssetDatabase.CreateAsset(texture3D, "Assets/Textures/" + "Distance_compare_new" + ".Asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SavingTexture succeeded!");


    }

    Texture3D readRenderTexture3D(RenderTexture source)
    {
        RenderTexture.active = source;
        Texture2D tex2d = new Texture2D(Dmap.x, Dmap.y, TextureFormat.R8, false);
        Texture3D texture3D = new Texture3D(Dmap.x, Dmap.y, Dmap.z, TextureFormat.R8, false);
        List<Color> texColors = new List<Color>();

        for (int i = 0; i < Dmap.z; i++)
        {
            var target = new RenderTexture(Dmap.x, Dmap.y, 0, RenderTextureFormat.R8);
            //tmp.Create();
            Graphics.CopyTexture(source, i, target, 0);
            RenderTexture.active = target;
            tex2d.ReadPixels(new Rect(0, 0, Dmap.x, Dmap.y), 0, 0);
            tex2d.Apply();
            texColors.AddRange(tex2d.GetPixels());
        }

        texture3D.SetPixels(texColors.ToArray());
        texture3D.filterMode = FilterMode.Point;
        texture3D.wrapMode = TextureWrapMode.Clamp;
        texture3D.Apply();

        return texture3D;
    }

}
