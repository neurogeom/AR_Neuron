using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRepair : MonoBehaviour
{
    public GameObject repairSeed;
    public GameObject paintingBoard;
    [SerializeField] Texture3D volume;

    // Start is called before the first frame update
    void Start()
    {
        byte[] img1d = volume.GetPixelData<byte>(0).ToArray();
        Vector3 position = repairSeed.transform.position;
        position = paintingBoard.transform.InverseTransformPoint(position);
        position += new Vector3(0.5f, 0.5f, 0.5f);
        position.x *= volume.width;
        position.y *= volume.height;
        position.z *= volume.depth;
        Marker seed = new Marker(position);
        List<Marker> Tree = MostRepair.trace_single_seed(img1d, volume.width, volume.height, volume.depth, seed, 30, 1);
        Debug.Log(Tree.Count);
        Primitive.CreateTree(Tree, paintingBoard.transform, volume.width, volume.height, volume.depth);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
