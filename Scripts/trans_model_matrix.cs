using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class trans_model_matrix : MonoBehaviour
{
    [SerializeReference] public Material material;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Matrix4x4 rotation = Matrix4x4.Rotate(Quaternion.Euler(this.transform.eulerAngles));
        Matrix4x4 translation = Matrix4x4.Translate(this.transform.position);
        Matrix4x4 scale = Matrix4x4.Scale(this.transform.localScale);
        this.GetComponent<MeshRenderer>().material.SetMatrix("_Rotation", rotation.inverse);
        this.GetComponent<MeshRenderer>().material.SetMatrix("_Translation", translation.inverse);
        this.GetComponent<MeshRenderer>().material.SetMatrix("_Scale", scale.inverse);
    }
}
