using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes;
using System.Text;
using System;
using Photon.Pun;

/*
#if WINDOWS_UWP//UWP下编译
using Windows.Storage;
using System.Threading.Tasks;
using System;
using System.Threading;
#endif
*/

/*
Created by BlackFJ
*/

///<summary>
///
///</summary>
public class SWCLoader : MonoBehaviour,IPunInstantiateMagicCallback
{
    public string swc_path;
    private List<Vector3> swc_list;
    private Vector3 swc_boundary_min;
    private Vector3 swc_boundary_max;
    private Vector3 swc_average;
    private Dictionary<int, int> swc_map;
    private float scale;
    private bool needScript;
    //private string scriptName;
    private BoundsControl boundsControl;
    public int justShowSphere;
    public int neuronFollowing;
    public GameObject SWCPrefab;

    private void ReadSWC()
    {
        swc_average = new Vector3(0, 0, 0);
        swc_list = new List<Vector3>();
        swc_map = new Dictionary<int, int>();

        /*
#if WINDOWS_UWP
        string fileContent="Start";
        Stream fileStream=null;

        Task task = new Task(
        async () => {    
            StorageFolder modelFolder = KnownFolders.PicturesLibrary;
            StorageFile modelFile = await modelFolder.GetFileAsync(swc_path);
            fileStream = await modelFile.OpenStreamForReadAsync();

            byte[] data = new byte[fileStream.Length];
            fileStream.Read(data, 0, data.Length);
            fileContent = Encoding.ASCII.GetString(data);                     
            
        });
        task.Start();
        string []strs=fileContent.Split('\n');

#else
*/
        if (!File.Exists(swc_path))
        {
            GameObject debugTip = GameObject.Find("DebugTip");
            if (debugTip != null)
            {
                debugTip.GetComponent<ToolTip>().ToolTipText = "Error on reading swc file!";
            }
            return;
        }
        string[] strs = File.ReadAllLines(swc_path);
//#endif
        GameObject.Find("DebugTip").GetComponent<ToolTip>().ToolTipText = "Reading swc Succeed!";
        for (int i = 0; i < strs.Length; ++i)
        {
            if (strs[i].StartsWith("#")) continue;
            string[] words = strs[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Vector3 swc = new Vector3(float.Parse(words[2]), float.Parse(words[3]), float.Parse(words[4]));
            swc_list.Add(swc);
            
            swc_map.Add(int.Parse(words[0]), int.Parse(words[6]));
            swc_average += swc;
            if (swc[0] > swc_boundary_max[0]) swc_boundary_max[0] = swc[0];
            if (swc[1] > swc_boundary_max[1]) swc_boundary_max[1] = swc[1];
            if (swc[2] > swc_boundary_max[2]) swc_boundary_max[2] = swc[2];
            if (swc[0] < swc_boundary_min[0]) swc_boundary_min[0] = swc[0];
            if (swc[1] < swc_boundary_min[1]) swc_boundary_min[1] = swc[1];
            if (swc[2] < swc_boundary_min[2]) swc_boundary_min[2] = swc[2];
        }
        swc_average /= swc_list.Count;
        print("success");
    }

    /*
    private void ReadSWC_UWP()
    {
#if WINDOWS_UWP
        Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        Windows.Storage.StorageFile sampleFile=await storageFolder.GetFileAsync(swc_path);
        var contents=await Windows.Storage.FileIO.ReadLinesAsync(sampleFile);
        GameObject.Find("DebugTip").GetComponent<ToolTip>().ToolTipText = "Reading swc Succeed!";
        for (int i = 0; i <contents.Count; ++i)
        {
            if (contents[i].StartsWith("#")) continue;
            string[] words = contents[i].Split();
            Vector3 swc = new Vector3(float.Parse(words[2]), float.Parse(words[3]), float.Parse(words[4]));
            swc_list.Add(swc);
            swc_map.Add(int.Parse(words[0]), int.Parse(words[6]));
            swc_average += swc;
            if (swc[0] > swc_boundary_max[0]) swc_boundary_max[0] = swc[0];
            if (swc[1] > swc_boundary_max[1]) swc_boundary_max[1] = swc[1];
            if (swc[2] > swc_boundary_max[2]) swc_boundary_max[2] = swc[2];
            if (swc[0] < swc_boundary_min[0]) swc_boundary_min[0] = swc[0];
            if (swc[1] < swc_boundary_min[1]) swc_boundary_min[1] = swc[1];
            if (swc[2] < swc_boundary_min[2]) swc_boundary_min[2] = swc[2];
        }
        swc_average /= swc_list.Count;
#endif
    }
    */

    private void CreateCylinder(Vector3 a, Vector3 b, float radius,GameObject parentObject,int flag=0)
    {
        GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        //GameObject newObj = PhotonNetwork.Instantiate("Cylinder", Vector3.zero, Quaternion.identity);
        float length = Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z));
        Vector3 ab = (a - b).normalized;
        Vector3 y_axis = new Vector3(0, 1, 0);
        newObj.transform.localScale = new Vector3(radius, length / 2, radius);
        newObj.transform.Rotate(Vector3.Cross(ab, y_axis), -Mathf.Acos(Vector3.Dot(ab, y_axis)) * 180 / Mathf.PI);
        newObj.transform.position = (a + b) / 2;
        
        newObj.GetComponent<MeshRenderer>().material.color = Color.red;
        newObj.transform.parent = parentObject.transform;
        if (flag == 1)
        {
            var mf=newObj.GetComponent<MeshFilter>();
            SaveOBJ(mf, new Vector3(1, 1, 1), "1");
        }
        if (needScript)
        {
            //newObj.AddComponent<FollowMoving>();
            //newObj.AddComponent<ObjectManipulator>();
            newObj.AddComponent<NearInteractionGrabbable>();
            newObj.AddComponent<Interactable>();
            newObj.GetComponent<Interactable>().States = Interactable.GetDefaultInteractableStates();
            
            
            /*var profiles=newObj.GetComponent<Interactable>().Profiles;
            var newProfile = new InteractableProfileItem();
            newProfile.Target = newObj;
            
            var newThemeType = ThemeDefinition.GetDefaultThemeDefinition<InteractableColorTheme>().Value;
            newThemeType.StateProperties[0].Values = new List<ThemePropertyValue>()
            {
                    new ThemePropertyValue() { Color = Color.black},  // Default
                     new ThemePropertyValue() { Color = Color.black}, // Focus
                     new ThemePropertyValue() { Color = Random.ColorHSV()},   // Pressed
                     new ThemePropertyValue() { Color = Color.black},   // Disabled
            };
            Theme newTheme = ScriptableObject.CreateInstance<Theme>();
            newTheme.States = Interactable.GetDefaultInteractableStates();
            newTheme.Definitions = new List<ThemeDefinition>() { newThemeType };

            newProfile.Themes.Add(newTheme);
            //var events = newObj.GetComponent<Interactable>().InteractableEvents;
            //InteractableEvent newEvent = new InteractableEvent();
            //newEvent.Receiver=
            */
        }
        
    }
    private void CreateSphere(Vector3 a,float radius,GameObject parentObject)
    {
        GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //GameObject newObj = PhotonNetwork.Instantiate("Sphere", Vector3.zero,Quaternion.identity);
        newObj.transform.position = a;
        newObj.GetComponent<MeshRenderer>().material.color = Color.green;
        newObj.transform.parent = parentObject.transform;
        newObj.transform.localScale = new Vector3(1, 1, 1) * radius;
        if (needScript)
        {
            //newObj.AddComponent<FollowMoving>();
            //newObj.AddComponent<ObjectManipulator>();
            newObj.AddComponent<NearInteractionGrabbable>();
            newObj.AddComponent<Interactable>();
        }
    }

    private void CreateNeuron()
    {
        //GameObject parentObject = GameObject.Find("SWCParent(Clone)");
        int numS = 0;
        int numC = 0;
        for (int i = 0; i < swc_list.Count; ++i)
        {
            CreateSphere((swc_list[i]-swc_average)*scale, scale, this.gameObject);
            numS += 1;
            int pid = swc_map[i + 1];
            if (pid == -1)
            {
                print(i);
                continue;
            }
            
            CreateCylinder((swc_list[i]- swc_average)*scale, (swc_list[pid - 1]-swc_average)*scale,scale, this.gameObject);
            numC += 1;
        }
        print(numS);
        print(numC);
        //CreateBoundingBox(parentObject);

    }

    private void CombineMesh(GameObject parentObject)
    {
        //var matrix = parentObject.transform.worldToLocalMatrix;
        MeshFilter[] meshFilters = parentObject.GetComponentsInChildren<MeshFilter>();
        //var transformList = parentObject.GetComponentsInChildren<Transform>();
        //Matrix4x4[] matrixList = new Matrix4x4[transformList.Length];
        /*for(int i = 0; i < transformList.Length; ++i)
        {
            matrixList[i] = transformList[i].localToWorldMatrix;
        }*/
        print(meshFilters.Length);
        CombineInstance[] combiners = new CombineInstance[meshFilters.Length];
        for(int i = 0; i < meshFilters.Length; ++i)
        {
            combiners[i].mesh = meshFilters[i].sharedMesh;
            combiners[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
        }
        MeshFilter meshFilter = parentObject.GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter=parentObject.AddComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();
        //meshFilter.name = "Neuron";
        parentObject.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combiners);
        if (parentObject.GetComponent<MeshRenderer>() == null)
        {
            parentObject.AddComponent<MeshRenderer>();
        }
        parentObject.GetComponent<MeshRenderer>().material.color = Color.red;
        parentObject.SetActive(true);
    }


    private void SaveOBJ(MeshFilter mf,Vector3 scale,string objName)
    {
        Mesh mesh = mf.mesh;
        Material[] sharedMaterials = mf.GetComponent<Renderer>().sharedMaterials;
        Vector2 textureOffset = mf.GetComponent<Renderer>().material.GetTextureOffset("_MainTex");
        Vector2 textureScale = mf.GetComponent<Renderer>().material.GetTextureScale("_MainTex");
        StringBuilder stringBuilder = new StringBuilder().Append("mtllib design.mtl\ng ").Append(mf.name).Append("\n");

        Vector3[] vertices = mesh.vertices;
        for(int i = 0; i < vertices.Length; ++i)
        {
            Vector3 vector = vertices[i];
            stringBuilder.Append(string.Format("v {0} {1} {2}\n", vector.x * scale.x, vector.y * scale.y, vector.z * scale.z));
        }
        stringBuilder.Append("\n");

        Dictionary<int, int> dictionary = new Dictionary<int, int>();
        if (mesh.subMeshCount > 1)
        {
            int[] triangles = mesh.GetTriangles(1);
            for (int j = 0; j < triangles.Length; j += 3)
            {
                if (!dictionary.ContainsKey(triangles[j]))
                {
                    dictionary.Add(triangles[j], 1);
                }

                if (!dictionary.ContainsKey(triangles[j + 1]))
                {
                    dictionary.Add(triangles[j + 1], 1);
                }

                if (!dictionary.ContainsKey(triangles[j + 2]))
                {
                    dictionary.Add(triangles[j + 2], 1);
                }
            }
        }
        for (int num = 0; num != mesh.uv.Length; num++)
        {
            Vector2 vector2 = Vector2.Scale(mesh.uv[num], textureScale) + textureOffset;

            if (dictionary.ContainsKey(num))
            {
                stringBuilder.Append(string.Format("vt {0} {1}\n", mesh.uv[num].x, mesh.uv[num].y));
            }
            else
            {
                stringBuilder.Append(string.Format("vt {0} {1}\n", vector2.x, vector2.y));
            }
        }

        for (int k = 0; k < mesh.subMeshCount; k++)
        {
            stringBuilder.Append("\n");

            if (k == 0)
            {
                stringBuilder.Append("usemtl ").Append("Material_design").Append("\n");
            }

            if (k == 1)
            {
                stringBuilder.Append("usemtl ").Append("Material_logo").Append("\n");
            }

            int[] triangles2 = mesh.GetTriangles(k);

            for (int l = 0; l < triangles2.Length; l += 3)
            {
                stringBuilder.Append(string.Format("f {0}/{0} {1}/{1} {2}/{2}\n", triangles2[l] + 1, triangles2[l + 2] + 1, triangles2[l + 1] + 1));
            }
        }
        using(StreamWriter sw=new StreamWriter(objName + ".obj"))
        {
            sw.Write(stringBuilder.ToString());
        }
    }

    private void CreateBoundingBox(GameObject parentObject)
    {
        //GameObject boundaryBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //boundsControl=boundaryBox.AddComponent<BoundsControl>();
        boundsControl = parentObject.AddComponent<BoundsControl>();
        //boundsControl.BoundsControlActivation= BoundsControlActivationType.ActivateByProximityAndPointer;
        boundsControl.BoundsControlActivation = BoundsControlActivationType.ActivateOnStart;
        boundsControl.ScaleHandlesConfig.HandleSize = 0.1f;
        boundsControl.RotationHandlesConfig.HandleSize = 0.1f;
        boundsControl.TranslationHandlesConfig.HandleSize = 0.1f;

        boundsControl.TranslateStarted.AddListener(TransferOwnership);
        boundsControl.RotateStarted.AddListener(TransferOwnership);
        boundsControl.ScaleStarted.AddListener(TransferOwnership);

        print("count"+boundsControl.TranslateStarted.GetPersistentEventCount());
        print("rotate count" + boundsControl.RotateStarted.GetPersistentEventCount());
        print("Scale count" + boundsControl.ScaleStarted.GetPersistentEventCount());

        ObjectManipulator objectManipulator = parentObject.GetComponent<ObjectManipulator>();
        objectManipulator.OnManipulationStarted.AddListener((ManipulationEventData data)=> { TransferOwnership(); });



        /*BoxDisplayConfiguration boxConfiguration = boundsControl.BoxDisplayConfig;
        boxConfiguration.BoxMaterial = [Assign BoundingBox.mat]
        boxConfiguration.BoxGrabbedMaterial = [Assign BoundingBoxGrabbed.mat]
        ScaleHandlesConfiguration scaleHandleConfiguration = boundsControl.ScaleHandlesConfig;
        scaleHandleConfiguration.HandleMaterial = [Assign BoundingBoxHandleWhite.mat]
        scaleHandleConfiguration.HandleGrabbedMaterial = [Assign BoundingBoxHandleBlueGrabbed.mat]
        scaleHandleConfiguration.HandlePrefab = [Assign MRTK_BoundingBox_ScaleHandle.prefab]
        scaleHandleConfiguration.HandleSlatePrefab = [Assign MRTK_BoundingBox_ScaleHandle_Slate.prefab]
        scaleHandleConfiguration.HandleSize = 0.016f;
        scaleHandleConfiguration.ColliderPadding = 0.016f;
        RotationHandlesConfiguration rotationHandleConfiguration = boundsControl.RotationHandlesConfig;
        rotationHandleConfiguration.HandleMaterial = [Assign BoundingBoxHandleWhite.mat]
        rotationHandleConfiguration.HandleGrabbedMaterial = [Assign BoundingBoxHandleBlueGrabbed.mat]
        rotationHandleConfiguration.HandlePrefab = [Assign MRTK_BoundingBox_RotateHandle.prefab]
        rotationHandleConfiguration.HandleSize = 0.016f;
        rotationHandleConfiguration.ColliderPadding = 0.016f;*/


        /*boundaryBox.name = "BoundaryBox";
        boundaryBox.transform.localScale = ((swc_boundary_max - swc_boundary_min)+new Vector3(10,10,10))*scale;
        boundaryBox.transform.position = ((swc_boundary_max - swc_boundary_min) / 2+swc_boundary_min-swc_average)*scale;
        print(boundaryBox.transform.position);
        boundaryBox.transform.parent = parentObject.transform;
        boundaryBox.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Transparent/Diffuse"));
        boundaryBox.GetComponent<MeshRenderer>().material.color = new Color(0.0f, 0.5f, 0.8f, 0.0f);
        boundaryBox.AddComponent<NearInteractionGrabbable>();
        boundaryBox.AddComponent<Interactable>();
        boundaryBox.GetComponent<Interactable>().States = Interactable.GetDefaultInteractableStates();
        boundaryBox.AddComponent<FollowMoving>();*/
    }

    private void Awake()
    {
        
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {

        swc_path = "E:\\SWC\\0003-8_1.CNG.swc";
        //swc_path = "1.swc";
        needScript = false;
        //scriptName = "FollowMoving";

        swc_boundary_min = new Vector3(10000, 10000, 10000);
        swc_boundary_max = new Vector3(-10000, -10000, -10000);
        ReadSWC();
        print(swc_boundary_min);
        print(swc_boundary_max);
        scale = 0.01f;
        //print(swc_list.Count);
        //print(swc_list[10]);
        GameObject pObject = this.gameObject;
        CreateNeuron();
        //CombineMesh(pObject);
        CreateBoundingBox(pObject);
        justShowSphere = 0;
        neuronFollowing = 0;
    }

    public void TransferOwnership()
    {
        this.gameObject.GetPhotonView().TransferOwnership(PhotonNetwork.LocalPlayer);
    }

    public void test()
    {
        print("whyyyyy");
        print(boundsControl.TranslateStarted.GetPersistentEventCount());
    }
}
