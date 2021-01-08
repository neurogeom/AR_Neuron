using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class PrefabInstantiation : MonoBehaviour, IPunInstantiateMagicCallback
{
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        GameObject parentObject = GameObject.Find("SWCParent(Clone)");
        this.transform.parent = parentObject.transform;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
