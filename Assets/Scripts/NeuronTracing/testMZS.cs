using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class testMZS : MonoBehaviour
{
    public Texture3D volume;
    public ComputeShader cs;
    // Start is called before the first frame update
    void Start()
    {
        byte[] img1d = volume.GetPixelData<byte>(0).ToArray();
        JunctionPointsDetect(img1d, 512, 512, 512, 30);
    }

    // Update is called once per frame
    void Update()
    {

    }

    List<Marker> JunctionPointsDetect(byte[] img, int sz0, int sz1, int sz2, int bkg_thresh = 30)
    {
        List<Marker> junctionPoints = new List<Marker>();
        int thickness = 2;
        long sz01 = sz0 * sz1;
        int[] sz = { sz0, sz1, sz2 };
        int slice = 244;
        int sz_x = sz[1], sz_y = sz[2];
        int[] img_slice = new int[sz_x * sz_y];
        for (int m = 0; m < sz_x; m++)     //get the MIP image of subvolume
        {
            for (int n = 0; n < sz_y; n++)
            {
                byte maximum = 0;
                for (int t = 0; t < thickness; t++)
                {
                    long index = 0;
                    int slice_index = slice + t;
                    index = n * sz01 + m * sz0 + slice_index;
                    maximum = Math.Max(maximum, img[index]);


                }
                if (maximum >= bkg_thresh) maximum = 255;
                else maximum = 0;
                img_slice[m + n * sz_x] = maximum;
            }
        }

        Texture2D tex = new Texture2D(512, 512, TextureFormat.R8, false);
        byte[] temp = new byte[img_slice.Length];
        for (int t = 0; t < img_slice.Length; t++)
        {
            temp[t] = (byte)img_slice[t];
        }
        tex.SetPixelData<byte>(temp, 0, 0);
        string fileName = "Assets/Textures/" + "BeforeMZST" + ".Asset";
        AssetDatabase.DeleteAsset(fileName);
        AssetDatabase.CreateAsset(tex, fileName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //int count1 = 1;
        //int count2 = 0;
        //int[] temp_img = new int[img_slice.Length];
        //while (count1 + count2 > 0) { 
        //    count1 = 0;
        //    count2 = 0;
        //    int[,] offset = { { 0, 0 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 }, { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 } };
        //    temp_img = new int[img_slice.Length];
        //    for (int x = 0; x < 512; x++)
        //    {
        //        for (int y = 0; y < 512; y++)
        //        {
        //            int index = x + y * 512;
        //            if (img_slice[index] != 0)
        //            {
        //                int[] p = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        //                for (int t = 0; t < 9; t++)
        //                {
        //                    int i = x + offset[t, 0];
        //                    int j = y + offset[t, 1];
        //                    if (i < 0 || i >= 512 || j < 0 || j >= 512)
        //                        p[t] = 0;
        //                    else
        //                    {
        //                        int index2 = i + j * 512;
        //                        p[t] = img_slice[index2] > 0 ? 1 : 0;
        //                    }
        //                }
        //                int C1 = (p[1] == 0 && (p[2] != 0 || p[3] != 0)) ? 1 : 0;
        //                int C2 = (p[3] == 0 && (p[4] != 0 || p[5] != 0)) ? 1 : 0;
        //                int C3 = (p[5] == 0 && (p[6] != 0 || p[7] != 0)) ? 1 : 0;
        //                int C4 = (p[7] == 0 && (p[8] != 0 || p[1] != 0)) ? 1 : 0;
        //                int C = C1 + C2 + C3 + C4;
        //                int B = p[1] + p[2] + p[3] + p[4] + p[5] + p[6] + p[7] + p[8];
        //                if ((x + y) % 2 == 0 && C == 1 && B >= 2 && B <= 7 && p[1] * p[3] * p[5] == 0 && p[3] * p[5] * p[7] == 0)
        //                {
        //                    temp_img[index] = 0;
        //                    count1++;
        //                }
        //                else temp_img[index] = 255;
        //            }
        //        }
        //    }
        //    temp_img.CopyTo(img_slice, 0);
        //    for (int x = 0; x < 512; x++)
        //    {
        //        for (int y = 0; y < 512; y++)
        //        {
        //            int index = x + y * 512;

        //            if (img_slice[index] != 0)
        //            {
        //                int[] p = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        //                for (int t = 0; t < 9; t++)
        //                {
        //                    int i = x + offset[t, 0];
        //                    int j = y + offset[t, 1];
        //                    if (i < 0 || i >= 512 || j < 0 || j >= 512)
        //                        p[t] = 0;
        //                    else
        //                    {
        //                        int index2 = i + j * 512;
        //                        p[t] = img_slice[index2] > 0 ? 1 : 0;
        //                    }
        //                }
        //                int C1 = (p[1] == 0 && (p[2] != 0 || p[3] != 0)) ? 1 : 0;
        //                int C2 = (p[3] == 0 && (p[4] != 0 || p[5] != 0)) ? 1 : 0;
        //                int C3 = (p[5] == 0 && (p[6] != 0 || p[7] != 0)) ? 1 : 0;
        //                int C4 = (p[7] == 0 && (p[8] != 0 || p[1] != 0)) ? 1 : 0;
        //                int C = C1 + C2 + C3 + C4;
        //                //int C = !p[1] && (p[2] || p[3]) + !p[3] && (p[4] || p[5]) + !p[5] && (p[6] || p[7]) + !p[7] && (p[8] || p[1]);
        //                int B = p[1] + p[2] + p[3] + p[4] + p[5] + p[6] + p[7] + p[8];
        //                if ((x + y) % 2 != 0 && C == 1 && B >= 1 && B <= 7 && p[1] * p[3] * p[7] == 0 && p[1] * p[5] * p[7] == 0)
        //                {
        //                    temp_img[index] = 0;
        //                    count2++;
        //                }
        //                else temp_img[index] = 255;
        //            }
        //        }
        //    }
        //    temp_img.CopyTo(img_slice, 0);

        //}


        //mzs thining

        //byte[] temp = new byte[img_slice.Length];
        //for (int t = 0; t < img_slice.Length; t++)
        //{
        //    temp[t] = (byte)img_slice[t];
        //}
        //Texture2D tex = new Texture2D(512, 512, TextureFormat.R8, false);
        //tex.SetPixelData<byte>(temp, 0, 0);
        //string fileName = "Assets/Textures/" + "MZSTBefore.Asset";
        //AssetDatabase.DeleteAsset(fileName);
        //AssetDatabase.CreateAsset(tex, fileName);
        //AssetDatabase.SaveAssets();
        //AssetDatabase.Refresh();


        int kernel = cs.FindKernel("CSMain");
        ComputeBuffer imgBuffer = new ComputeBuffer(sz_x * sz_y, 4);
        imgBuffer.SetData(img_slice);
        cs.SetBuffer(kernel, "img", imgBuffer);
        cs.SetInts("size", new int[] { sz_x, sz_y });
        cs.Dispatch(kernel, sz_x / 32, sz_y / 32, 1);
        imgBuffer.GetData(img_slice);

        tex = new Texture2D(512, 512, TextureFormat.R8, false);
        temp = new byte[img_slice.Length];
        for (int t = 0; t < img_slice.Length; t++)
        {
            temp[t] = (byte)img_slice[t];
        }
        tex.SetPixelData<byte>(temp, 0, 0);
        fileName = "Assets/Textures/" + "MZST" + ".Asset";
        AssetDatabase.DeleteAsset(fileName);
        AssetDatabase.CreateAsset(tex, fileName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //tex = new Texture2D(512, 512, TextureFormat.R8, false);
        //temp = new byte[img_slice.Length];
        //for (int t = 0; t < img_slice.Length; t++)
        //{
        //    temp[t] = (byte)img_slice[t];
        //}
        //tex.SetPixelData<byte>(temp, 0, 0);
        //fileName = "Assets/Textures/" + "MZST.Asset";
        //AssetDatabase.DeleteAsset(fileName);
        //AssetDatabase.CreateAsset(tex, fileName);
        //AssetDatabase.SaveAssets();
        //AssetDatabase.Refresh();
        //imgBuffer.Release();

        //int iteration1 = cs.FindKernel("Iteration1");
        //int iteration2 = cs.FindKernel("Iteration2");
        //ComputeBuffer input = new ComputeBuffer(sz_x * sz_y, 4);
        //ComputeBuffer output = new ComputeBuffer(sz_x * sz_y, 4);
        //int[] size = { sz_x, sz_y };
        //input.SetData(img_slice);
        //cs.SetBuffer(iteration1, "input", input);
        //cs.SetBuffer(iteration1, "output", output);
        //cs.SetInts("size", size);
        //for (int time = 0; time < 20; time++)
        //{
        //    cs.SetBuffer(iteration1, "input", input);
        //    cs.SetBuffer(iteration1, "output", output);
        //    cs.Dispatch(iteration1, sz_x / 8, sz_y / 8, 1);
        //    output.GetData(img_slice);
        //    input.SetData(img_slice);
        //    cs.SetBuffer(iteration2, "input", input);
        //    cs.SetBuffer(iteration2, "output", output);
        //    cs.Dispatch(iteration2, sz_x / 8, sz_y / 8, 1);
        //    output.GetData(img_slice);
        //    input.SetData(img_slice);
        //    tex = new Texture2D(512, 512, TextureFormat.R8, false);
        //    temp = new byte[img_slice.Length];
        //    for (int t = 0; t < img_slice.Length; t++)
        //    {
        //        temp[t] = (byte)img_slice[t];
        //    }
        //    tex.SetPixelData<byte>(temp, 0, 0);
        //    fileName = "Assets/Textures/" + "MZST" + time + ".Asset";
        //    AssetDatabase.DeleteAsset(fileName);
        //    AssetDatabase.CreateAsset(tex, fileName);
        //    AssetDatabase.SaveAssets();
        //    AssetDatabase.Refresh();
        //}
        //input.Release();
        //input.Dispose();
        //output.Release();
        //output.Dispose();
        return junctionPoints;
    }
}
