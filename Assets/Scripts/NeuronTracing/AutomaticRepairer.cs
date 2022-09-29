using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AutomaticRepairer : MonoBehaviour
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
        for(int i = 0; i < 3; i++)
        {
            for (int slice = 0; slice < sz[i]; slice += thickness)
            {
                int sz_x = sz[(i + 1) % 3], sz_y = sz[(i + 2) % 3];
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
                            switch (i)
                            {
                                case 0:
                                    {
                                        index = n * sz01 + m * sz0 + slice_index;
                                        break;
                                    }
                                case 1:
                                    {
                                        index = m * sz01 + slice_index * sz0 + n;
                                        break;
                                    }
                                case 2:
                                    {
                                        index = slice_index * sz01 + n * sz0 + m;
                                        break;
                                    }
                            }
                            maximum = Math.Max(maximum, img[index]);


                        }
                        if (maximum >= bkg_thresh) maximum = 255;
                        else maximum = 0;
                        img_slice[m + n * sz_x] = maximum;
                    }
                }
                //mzs thining
                if (i == 0 && slice == 256)
                {
                    byte[] temp = new byte[img_slice.Length];
                    for(int t = 0; t < img_slice.Length; t++)
                    {
                        temp[t] = (byte)img_slice[t];
                    }
                    Texture2D tex = new Texture2D(512, 512, TextureFormat.R8, false);
                    tex.SetPixelData<byte>(temp, 0, 0);
                    string fileName = "Assets/Textures/" + "MZSTBefore.Asset";
                    AssetDatabase.DeleteAsset(fileName);
                    AssetDatabase.CreateAsset(tex, fileName);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                int iteration1 = cs.FindKernel("Iteration1");
                int iteration2 = cs.FindKernel("Iteration2");
                ComputeBuffer input = new ComputeBuffer(sz_x * sz_y, 4);
                ComputeBuffer output = new ComputeBuffer(sz_x * sz_y, 4);
                int[] size = { sz_x, sz_y };
                input.SetData(img_slice);
                cs.SetBuffer(iteration1, "input", input);
                cs.SetBuffer(iteration1, "output", output);
                cs.SetInts("size", size);
                for(int time = 0; time < 10; time++)
                {
                    cs.SetBuffer(iteration1, "input", input);
                    cs.SetBuffer(iteration1, "output", output);
                    cs.Dispatch(iteration1, sz_x / 8, sz_y / 8, 1);
                    output.GetData(img_slice);
                    input.SetData(img_slice);
                    cs.SetBuffer(iteration2, "input", input);
                    cs.SetBuffer(iteration2, "output", output);
                    cs.Dispatch(iteration2, sz_x / 8, sz_y / 8, 1);
                    output.GetData(img_slice);
                    input.SetData(img_slice);
                }
                input.Release();
                output.Release();
                if (i == 0 && slice == 256)
                {
                    Texture2D tex = new Texture2D(512, 512, TextureFormat.R8, false);
                    byte[] temp = new byte[img_slice.Length];
                    for (int t = 0; t < img_slice.Length; t++)
                    {
                        temp[t] = (byte)img_slice[t];
                    }
                    tex.SetPixelData<byte>(temp, 0, 0);
                    string fileName = "Assets/Textures/" + "MZST2.Asset";
                    AssetDatabase.DeleteAsset(fileName);
                    AssetDatabase.CreateAsset(tex, fileName);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

            }
        }
        return junctionPoints;
    }
}
