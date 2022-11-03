using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TestFIM : MonoBehaviour
{
    public RenderTexture gwdt;
    public RenderTexture state;
    public RenderTexture parent;
    public RenderTexture phi;
    public RenderTexture visualize;
    private RenderTexture[] rts;
    public ComputeShader computeShader;
    public Texture3D volume;
    public Vector3Int dims;
    private ComputeBuffer sourceSet;
    private ComputeBuffer remedySet;
    private ComputeBuffer iterateListBufferTo;
    private ComputeBuffer iterateListArgBuffer;
    private ComputeBuffer iterateListArgBufferTo;
    private const int READ = 0;
    private const int WRITE = 1;

    public int bkgThreshold =30;
    private int[] seed = new int[3];
    private int seedIndex;
    private float maxIntensity;

    
    // Start is called before the first frame update
    void Start()
    {
        //float time = Time.realtimeSinceStartup;
        //Debug.Log(time);
        //PrepareDatas();
        //FIMDT();
        //Debug.Log("FIMDT cost time:"+(Time.realtimeSinceStartup - time));
        //time = Time.realtimeSinceStartup;
        //FIMTree();
        //Debug.Log("FIMTree cost time:" + (Time.realtimeSinceStartup - time));

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PrepareDatas()
    {
        dims = new Vector3Int(volume.width, volume.height, volume.depth);
        gwdt = InitRenderTexture3D(dims.x, dims.y, dims.z, RenderTextureFormat.RFloat, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat);
        state = InitRenderTexture3D(dims.x, dims.y, dims.z, RenderTextureFormat.R8, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UInt);
        parent = InitRenderTexture3D(dims.x, dims.y, dims.z, RenderTextureFormat.RInt, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_UInt);
        phi = InitRenderTexture3D(dims.x, dims.y, dims.z, RenderTextureFormat.RFloat, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat);
        visualize = InitRenderTexture3D(dims.x, dims.y, dims.z, RenderTextureFormat.R8, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm);
        
        //rts = new RenderTexture[2];

        //int iterateListBufferNum = 128 * 128;
        //iterateListBufferTo = new ComputeBuffer(iterateListBufferNum, 2 * sizeof(uint), ComputeBufferType.Append);

        //iterateListArgBuffer = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
        //iterateListArgBufferTo = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
        //int[] initarg = new int[] { 0, 1, 1 };
        //iterateListArgBuffer.SetData(initarg);
        //iterateListArgBufferTo.SetData(initarg);
    }

    //Fast Iterative Method Distance Transform Using Compute Shader
    public void FIMDT()
    {
        int kernel = computeShader.FindKernel("InitBound");
        computeShader.SetTexture(kernel, "state", state);
        computeShader.SetTexture(kernel, "gwdt", gwdt);
        computeShader.SetTexture(kernel, "volume", volume);
        computeShader.SetInt("bkgThreshold", bkgThreshold);
        computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

        int[] dimsArray = new int[3] { dims.x, dims.y, dims.z };
        uint sourceCount;
        computeShader.SetInts("dims", dimsArray);

        //Update Step
        sourceSet = new ComputeBuffer(100000000, sizeof(uint), ComputeBufferType.Append);
        do
        {
            sourceSet.SetCounterValue(0);

            kernel = computeShader.FindKernel("UpdateFarState");
            computeShader.SetTexture(kernel, "state", state);
            computeShader.SetTexture(kernel, "visualize", visualize);
            computeShader.SetBuffer(kernel, "sourceSet", sourceSet);
            computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

            kernel = computeShader.FindKernel("UpdateSourceValue");
            computeShader.SetTexture(kernel, "state", state);
            computeShader.SetTexture(kernel, "gwdt", gwdt);
            computeShader.SetTexture(kernel, "volume", volume);
            computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

            kernel = computeShader.FindKernel("UpdateSourceState");
            computeShader.SetTexture(kernel, "state", state);
            computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

            //Get Source Set Count
            sourceCount = GetAppendBufferSize(sourceSet);
        } while (sourceCount > 0);
        sourceSet.Release();

        uint remedyCount = 0;
        //Remedy Step
        remedySet = new ComputeBuffer(100000000, sizeof(uint), ComputeBufferType.Append);
        remedySet.SetCounterValue(0);
        kernel = computeShader.FindKernel("InitRemedy");
        computeShader.SetBuffer(kernel, "remedySet", remedySet);
        computeShader.SetTexture(kernel, "volume", volume);
        computeShader.SetTexture(kernel, "gwdt", gwdt);
        computeShader.SetTexture(kernel, "state", state);
        computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

        remedyCount = GetAppendBufferSize(remedySet);
        //remedySet.Release();
        while (remedyCount > 0)
        {
            //remedySet = new ComputeBuffer(dims.x * dims.y * dims.z, sizeof(uint), ComputeBufferType.Append);
            remedySet.SetCounterValue(0);

            kernel = computeShader.FindKernel("UpdateRemedy");
            computeShader.SetBuffer(kernel, "remedySet", remedySet);
            computeShader.SetTexture(kernel, "volume", volume);
            computeShader.SetTexture(kernel, "gwdt", gwdt);
            computeShader.SetTexture(kernel, "state", state);
            computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

            remedyCount = GetAppendBufferSize(remedySet);

            kernel = computeShader.FindKernel("UpdateRemedyNeighbor");
            computeShader.SetBuffer(kernel, "remedySet", remedySet);
            computeShader.SetTexture(kernel, "state", state);
            computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);


            //Get Remedy Set Count
            remedyCount = GetAppendBufferSize(remedySet);
            //remedySet.Release();
        }
        remedySet.Release();

        ComputeBuffer gwdtBuffer1 = new ComputeBuffer(dims.x * dims.y * dims.z / 2, sizeof(float), ComputeBufferType.Default);
        ComputeBuffer gwdtBuffer2 = new ComputeBuffer(dims.x * dims.y * dims.z / 2, sizeof(float), ComputeBufferType.Default);
        kernel = computeShader.FindKernel("visualizeTexture");
        computeShader.SetTexture(kernel, "gwdt", gwdt);
        computeShader.SetBuffer(kernel, "gwdtBuffer1", gwdtBuffer1);
        computeShader.SetBuffer(kernel, "gwdtBuffer2", gwdtBuffer2);
        computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

        float[] gwdtBufferData = new float[gwdtBuffer1.count + gwdtBuffer2.count];
        gwdtBuffer1.GetData(gwdtBufferData, 0, 0, gwdtBuffer1.count);
        gwdtBuffer2.GetData(gwdtBufferData, gwdtBuffer1.count, 0, gwdtBuffer2.count);
        //gwdtBuffer2.GetData(gwdtBufferData,gwdtBuffer1.count,0,gwdtBuffer2.count);

        int maxIndex = 0;
        for (int i = 0; i < gwdtBufferData.Length; i++)
        {
            if (gwdtBufferData[i] > gwdtBufferData[maxIndex])
                maxIndex = i;
        }
        seed[0] = maxIndex % dims.x;
        seed[1] = (maxIndex / dims.x) % dims.y;
        seed[2] = (maxIndex / dims.x / dims.y) % dims.z;
        seedIndex = maxIndex;
        maxIntensity = gwdtBufferData[maxIndex];
        Debug.Log($"{seed[0]} {seed[1]} {seed[2]}");
        Debug.Log(maxIndex + " " + gwdtBufferData[maxIndex]);
        
        gwdtBuffer1.Release();
        gwdtBuffer2.Release();
        //AssetDatabase.DeleteAsset("Assets/Textures/FIMTest/BoundInit.Asset");
        //AssetDatabase.CreateAsset(gwdt, "Assets/Textures/FIMTest/BoundInit.Asset");
        //AssetDatabase.SaveAssets();
        //AssetDatabase.Refresh();


    }

    public List<Marker> FIMTree()
    {
        int kernel = computeShader.FindKernel("InitSeed");
        computeShader.SetTexture(kernel, "state", state);
        computeShader.SetTexture(kernel, "parent", parent);
        computeShader.SetTexture(kernel, "phi", phi);
        computeShader.SetTexture(kernel, "gwdt", gwdt);
        computeShader.SetFloat("maxIntensity", maxIntensity);
        computeShader.SetInt("bkgThreshold", bkgThreshold);
        computeShader.SetInt("seedIndex", seedIndex);
        computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

        uint sourceCount;
        HashSet<uint> results = new HashSet<uint>();
        results.Add((uint)seedIndex);
        //Update Steps
        sourceSet = new ComputeBuffer(100000000, sizeof(uint), ComputeBufferType.Append);
        do
        {
            sourceSet.SetCounterValue(0);

            kernel = computeShader.FindKernel("UpdateFarStateTree");
            computeShader.SetTexture(kernel, "state", state);
            computeShader.SetTexture(kernel, "gwdt", gwdt);
            computeShader.SetBuffer(kernel, "sourceSet", sourceSet);
            computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

            kernel = computeShader.FindKernel("UpdateSourceValueTree");
            computeShader.SetTexture(kernel, "state", state);
            computeShader.SetTexture(kernel, "gwdt", gwdt);
            computeShader.SetTexture(kernel, "phi", phi);
            computeShader.SetTexture(kernel, "parent", parent);
            computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

            kernel = computeShader.FindKernel("UpdateSourceStateTree");
            computeShader.SetTexture(kernel, "state", state);
            computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

            //Get Source Set Count
            sourceCount = GetAppendBufferSize(sourceSet);

            uint[] sourceData = new uint[sourceCount];
            sourceSet.GetData(sourceData);
            results.UnionWith(sourceData);
        } while (sourceCount > 0);
        sourceSet.Release();
        Debug.Log($"results count: {results.Count}");

        //Remedy Step
        uint remedyCount = 0;
        remedySet = new ComputeBuffer(100000000, sizeof(uint), ComputeBufferType.Append);
        remedySet.SetCounterValue(0);
        kernel = computeShader.FindKernel("InitRemedyTree");
        computeShader.SetBuffer(kernel, "remedySet", remedySet);
        computeShader.SetTexture(kernel, "gwdt", gwdt);
        computeShader.SetTexture(kernel, "phi", phi);
        computeShader.SetTexture(kernel, "state", state);
        computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

        remedyCount = GetAppendBufferSize(remedySet);

        //remedySet.Release();
        while (remedyCount > 0)
        {
            //remedySet = new ComputeBuffer(dims.x * dims.y * dims.z, sizeof(uint), ComputeBufferType.Append);
            remedySet.SetCounterValue(0);

            kernel = computeShader.FindKernel("UpdateRemedyTree");
            computeShader.SetBuffer(kernel, "remedySet", remedySet);
            computeShader.SetTexture(kernel, "state", state);
            computeShader.SetTexture(kernel, "gwdt", gwdt);
            computeShader.SetTexture(kernel, "phi", phi);
            computeShader.SetTexture(kernel, "parent", parent);
            computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

            remedyCount = GetAppendBufferSize(remedySet);

            kernel = computeShader.FindKernel("UpdateRemedyNeighborTree");
            computeShader.SetBuffer(kernel, "remedySet", remedySet);
            computeShader.SetTexture(kernel, "state", state);
            computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

            //Get Remedy Set Count
            remedyCount = GetAppendBufferSize(remedySet);
            //remedySet.Release();
        }


        ComputeBuffer parentBuffer1 = new ComputeBuffer(dims.x * dims.y * dims.z / 2, sizeof(float), ComputeBufferType.Default);
        ComputeBuffer parentBuffer2 = new ComputeBuffer(dims.x * dims.y * dims.z / 2, sizeof(float), ComputeBufferType.Default);
        kernel = computeShader.FindKernel("GetParent");
        computeShader.SetTexture(kernel, "parent", parent);
        computeShader.SetBuffer(kernel, "parentBuffer1", parentBuffer1);
        computeShader.SetBuffer(kernel, "parentBuffer2", parentBuffer2);
        computeShader.Dispatch(kernel, dims.x / 4, dims.y / 4, dims.z / 4);

        uint[] parentBufferData = new uint[parentBuffer1.count + parentBuffer2.count];
        parentBuffer1.GetData(parentBufferData, 0, 0, parentBuffer1.count);
        parentBuffer2.GetData(parentBufferData, parentBuffer1.count, 0, parentBuffer2.count);

        parentBuffer1.Release();
        parentBuffer2.Release();

        var markers = new Dictionary<uint, Marker>();
        var completeTree = new List<Marker>();
        return completeTree;

        foreach (var index in results)
        {
            int i = (int)(index % dims.x);
            int j = (int)((index / dims.x) % dims.y);
            int k = (int)((index / dims.x / dims.y) % dims.z);
            Marker marker = new Marker(new Vector3(i, j, k));
            markers[index] = marker;
            completeTree.Add(marker);
        }

        foreach (var index in results)
        {
            uint index2 = parentBufferData[index];
            Marker marker1 = markers[index];
            Marker marker2 = markers[index2];
            if (marker1 == marker2) marker1.parent = null;
            else marker1.parent = marker2;
        }

        return completeTree;
        //AssetDatabase.DeleteAsset("Assets/Textures/FIMTest/BoundInit.Asset");
        //AssetDatabase.CreateAsset(gwdt, "Assets/Textures/FIMTest/BoundInit.Asset");
        //AssetDatabase.SaveAssets();
        //AssetDatabase.Refresh();


    }

    RenderTexture InitRenderTexture3D(int width, int height, int depth, RenderTextureFormat format, UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat)
    {
        RenderTexture renderTexture = new RenderTexture(width, height, 0, format);
        renderTexture.graphicsFormat = graphicsFormat;
        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        renderTexture.volumeDepth = depth;
        renderTexture.enableRandomWrite = true;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        //renderTexture.Create();
        return renderTexture;
    }

    uint GetAppendBufferSize(ComputeBuffer appendBuffer)
    {
        uint[] countBufferData = new uint[1];
        var countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);
        ComputeBuffer.CopyCount(appendBuffer, countBuffer, 0);
        countBuffer.GetData(countBufferData);
        uint count = countBufferData[0];
        countBuffer.Release();
        //Debug.Log(count);
        return count;
    }
}
