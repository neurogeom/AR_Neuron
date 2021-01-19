using Microsoft.MixedReality.Toolkit.Experimental.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Persistence;
using UnityEngine.XR.WSA.Sharing;
#if WINDOWS_UWP
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;
using Windows.Storage;
#endif

public class AnchorMaker : MonoBehaviour
{
    public WorldAnchorManager worldAnchorManager;
    bool awaitingStore = true;
    WorldAnchorStore store = null;
    bool savedRoot;
    string[] anchors;
    int numOnExport;
#if WINDOWS_UWP
    DataWriter writer;
    StreamSocket streamSocket;
    float totalMB;
#endif
    // Start is called before the first frame update
    async void Awake()
    {
#if WINDOWS_UWP

        streamSocket=new StreamSocket();
        var hostName=new HostName("192.168.1.2");
        streamSocket.ConnectAsync(hostName,"8088");
        
        writer=new DataWriter(streamSocket.OutputStream);
        totalMB=0.0f;
#endif
        numOnExport = 0;
        Debug.Log("AnchorMaker: Looking for anchors...");
        WorldAnchorStore.GetAsync(StoreLoaded);
    }

    private void StoreLoaded(WorldAnchorStore store)
    {
        this.store = store;
        awaitingStore = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!awaitingStore)
        {
            Debug.Log("AnchorMaker: Store loaded.");
            awaitingStore = true;
            anchors = this.store.GetAllIds();
            Debug.Log("Found anchors: " + anchors.Length);
            for (int index = 0; index < anchors.Length; index++)
            {
                Debug.Log(anchors[index]);
            }
            LoadGame();
            if (anchors.Length > 0)
            {
                WorldAnchorTransferBatch transferBatch = new WorldAnchorTransferBatch();
                WorldAnchor anchor = this.gameObject.GetComponent<WorldAnchor>();
                transferBatch.AddWorldAnchor("rootGameObject", anchor);
                WorldAnchorTransferBatch.ExportAsync(transferBatch, OnExportDataAvailable, OnExportComplete);
                
            }
        }
        
    }

    private void OnExportComplete(SerializationCompletionReason completionReason)
    {
        if (completionReason != SerializationCompletionReason.Succeeded)
        {
            Debug.Log("OnExportComplete: Succeeded!");
            Debug.Log("Total " + numOnExport.ToString() + " times of Calling OnExportDataAvailable!");
        }
        else
        {
            Debug.Log("OnExportComplete: Failed!");
            Debug.Log("Total " + numOnExport.ToString() + " times of Calling OnExportDataAvailable!");
        }
#if WINDOWS_UWP
        writer.DetachStream();
        writer.Dispose();
        Debug.Log("Storage of AnchorInfo: "+totalMB.ToString()+" MB");
#endif
    }

    private async void OnExportDataAvailable(byte[] data)
    {
        //Debug.Log("OnExportDataAvailable: ");
        Debug.Log(data.Length.ToString());
#if WINDOWS_UWP
        writer.WriteBytes(data);
        writer.StoreAsync();
        //writer.DetachStream();
        //writer.Dispose();
        totalMB+=data.Length/1024.0f/1024.0f;
#endif
        numOnExport++;
    }

    private void LoadGame()
    {
        // Save data about holograms positioned by this world anchor
        this.savedRoot = this.store.Load("rootGameObject", this.gameObject);
        if (!this.savedRoot)
        {
            // We didn't actually have the game root saved! We have to re-place our objects or start over
            Debug.Log("LoadGame: No saved anchors found");
        }
    }

    public void OnManipulationEnded()
    {
        WorldAnchor anchor = this.gameObject.GetComponent<WorldAnchor>();
        if (anchor == null)
        {
            string name = worldAnchorManager.AttachAnchor(this.gameObject);
            Debug.Log("Added anchor: " + name);
        }
        else
        {
            if (anchor.name != null)
            {
                Debug.Log("Updating anchor: " + anchor.name);
            }
        }
        SaveGame();
    }
    public void OnManipulationStarted()
    {
        if (anchors != null)
        {
            foreach (string a in anchors)
            {
                Debug.Log("Deleting anchor: " + a);
                store.Delete(a);
            }
            this.savedRoot = false;
        }
    }

    private void SaveGame()
    {
        WorldAnchor anchor = this.gameObject.GetComponent<WorldAnchor>();
        // Save data about holograms positioned by this world anchor
        if (!this.savedRoot && anchor != null) // Only Save the root once
        {
            this.savedRoot = this.store.Save("rootGameObject", anchor);
            //Assert(this.savedRoot);
            Debug.Log("Saved anchor: " + this.savedRoot);
        }
        else
        {
            Debug.Log("Already saved anchor: " + this.savedRoot);
        }
    }
}
