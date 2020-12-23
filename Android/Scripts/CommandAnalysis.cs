using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using UnityEngine;
/*
Created by BlackFJ
*/
namespace Command
{
    public enum CommandType
    {
        Error,
        Connection_MobilePhone,
        Connection_Server,
        Connection_HoloLens,
        Connection_OtherClient,
        FileListShow,
        FileSelection,
        SWCTransmition

    }

    ///<summary>
    ///
    ///</summary>
    public class CommandAnalysis
    {
        public CommandType commandType;
        public string ip;
        public int port;
        public string[] fileList;
        public string selectedFile;
        public string swcInfo;

        public CommandAnalysis(string commandContent)
        {
            int i = commandContent.IndexOf(':');
            if (i == -1)
            {
                commandType = CommandType.Error;
                return;
            }
            string prefix = commandContent.Substring(0, i);
            string suffix = commandContent.Substring(i + 1);
            //Debug.Log(prefix);
            //Debug.Log(suffix);
            if (prefix.Equals( "ConnectionMobilePhone"))
            {
                commandType = CommandType.Connection_MobilePhone;
                ConnectionRegex(suffix);
            }
            else if (prefix.Equals("ConnectionHoloLens"))
            {
                commandType = CommandType.Connection_Server;
                ConnectionRegex(suffix);
            }
            else if (prefix.Equals("ConnectionHoloLens"))
            {
                commandType = CommandType.Connection_HoloLens;
                ConnectionRegex(suffix);
            }
            else if (prefix.Equals("ConnectionOtherClient"))
            {
                commandType = CommandType.Connection_OtherClient;
                ConnectionRegex(suffix);
            }
            else if (prefix.Equals("FileList"))
            {
                commandType = CommandType.FileListShow;
                FileListRegex(suffix);
            }
            else if (prefix.Equals("FileSelection"))
            {
                commandType = CommandType.FileSelection;
                FileSelectionRegex(suffix);
            }
            else if (prefix.Equals("SWCTransmition"))
            {
                commandType = CommandType.SWCTransmition;
                SWCTransmitionRegex(suffix);
            }
            else
            {
                commandType = CommandType.Error;
            }
        }

        void ConnectionRegex(string suffix)
        {
            
            string[] strs = suffix.Split(':');
            if (strs.Length != 2)
            {
                commandType = CommandType.Error;
                return;
            }
            try
            {
                ip = strs[0];
                port = int.Parse(strs[1]);
            }
            catch
            {
                commandType = CommandType.Error;
            }
        }

        void FileListRegex(string suffix)
        {
            
            string[] strs = suffix.Split(' ');
            /*for(int i = 0; i < strs.Length; ++i)
            {
                Debug.Log(strs[i]);
            }*/
            try
            {
                int fileLen = int.Parse(strs[0]);
                if (fileLen + 1 != strs.Length)
                {
                    commandType = CommandType.Error;
                    return;
                }
                fileList = new string[fileLen];
                for (int i = 0; i < fileLen; ++i)
                {
                    fileList[i] = strs[i + 1];
                }
            }
            catch
            {
                commandType = CommandType.Error;
            }
        }

        void FileSelectionRegex(string suffix)
        {
            selectedFile = suffix;
        }

        void SWCTransmitionRegex(string suffix)
        {
            swcInfo = suffix;
        }

        public void Show()
        {
            if (commandType == CommandType.Error)
            {
                Debug.Log("Error Command!");
            }
            else if (commandType == CommandType.Connection_MobilePhone)
            {
                Debug.Log(string.Format("ConnectionMobilePhone: ip {0}, port {1}", ip, port));
            }
            else if (commandType == CommandType.Connection_HoloLens)
            {
                Debug.Log(string.Format("ConnectionHoloLens: ip {0}, port {1}", ip, port));
            }
            else if (commandType == CommandType.Connection_Server)
            {
                Debug.Log(string.Format("ConnectionServer: ip {0}, port {1}", ip, port));
            }
            else if (commandType == CommandType.Connection_OtherClient)
            {
                Debug.Log(string.Format("ConnectionOtherClient: ip {0}, port {1}", ip, port));
            }
            else if (commandType == CommandType.FileSelection)
            {
                Debug.Log(string.Format("FileSelecion: {0}", selectedFile));
            }
            else if (commandType == CommandType.FileListShow)
            {
                Debug.Log("FileList: ");
                for (int i = 0; i < fileList.Length; ++i)
                {
                    Debug.Log(fileList[i]);
                }
            }
        }
    }
}

