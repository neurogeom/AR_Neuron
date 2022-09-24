using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
/*
Created by BlackFJ
*/

///<summary>
///
///</summary>
public class TestComputeShaderScript : MonoBehaviour
{
    public ComputeShader shader;
    RenderTexture tex;
    
    void RunShader()
    {
        int kernelHandle = shader.FindKernel("CSMain");
        
        tex.enableRandomWrite = true;
        tex.Create();

        shader.SetTexture(kernelHandle, "Result", tex);
        shader.Dispatch(kernelHandle, 256 / 8, 256 / 8, 1);
    }

    private void Awake()
    {   
        tex = new RenderTexture(256, 256, 24);
        RunShader();


        Texture2D texture2D = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        RenderTexture.active = tex;
        texture2D.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        texture2D.Apply();
        //AssetDatabase.CreateAsset(texture2D, "Assets/test.renderTexture");
        //AssetDatabase.SaveAssets();
        //AssetDatabase.Refresh();
    }
}
