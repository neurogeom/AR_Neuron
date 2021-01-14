using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
/*
Created by BlackFJ
*/
namespace Command
{
    public enum CommandType
    {
        Error,
        Useless,
        Connection_MobilePhone,
        Connection_Server,
        Connection_HoloLens,
        Connection_HoloLens_UWP,
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
            if (commandContent == null)
            {
                commandType = CommandType.Useless;
                return;
            }
            int i = commandContent.IndexOf(':');
            if (i == -1)
            {
                commandType = CommandType.Useless;
                return;
            }
            string prefix = commandContent.Substring(0, i);
            string suffix = commandContent.Substring(i + 1);
            if (prefix == "ConnectionMobilePhone")
            {
                commandType = CommandType.Connection_MobilePhone;
                ConnectionRegex(suffix);
            }
            else if (prefix == "ConnectionServer")
            {
                commandType = CommandType.Connection_Server;
                ConnectionRegex(suffix);
            }
            else if (prefix == "ConnectionHoloLens")
            {
                commandType = CommandType.Connection_HoloLens;
                ConnectionRegex(suffix);
            }
            else if (prefix == "ConnectionHoloLens_UWP")
            {
                commandType = CommandType.Connection_HoloLens_UWP;
                ConnectionRegex(suffix);
            }
            else if (prefix == "ConnectionOtherClient")
            {
                commandType = CommandType.Connection_OtherClient;
                ConnectionRegex(suffix);
            }
            else if (prefix == "FileList")
            {
                commandType = CommandType.FileListShow;
                FileListRegex(suffix);
            }
            else if (prefix == "FileSelection")
            {
                commandType = CommandType.FileSelection;
                FileSelectionRegex(suffix);
            }
            else if (prefix == "SWCTransmition")
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
                Console.WriteLine("Error Command!");
            }
            else if (commandType == CommandType.Useless)
            {
                Console.WriteLine("UseLess Command!");
            }
            else if (commandType == CommandType.Connection_MobilePhone)
            {
                Console.WriteLine("ConnectionMobilePhone: ip {0}, port {1}", ip, port);
            }
            else if (commandType == CommandType.Connection_HoloLens)
            {
                Console.WriteLine("ConnectionHoloLens: ip {0}, port {1}", ip, port);
            }
            else if (commandType == CommandType.Connection_HoloLens_UWP)
            {
                Console.WriteLine("ConnectionHoloLens_UWP: ip {0}, port {1}", ip, port);
            }
            else if (commandType == CommandType.Connection_Server)
            {
                Console.WriteLine("ConnectionServer: ip {0}, port {1}", ip, port);
            }
            else if (commandType == CommandType.Connection_OtherClient)
            {
                Console.WriteLine("ConnectionOtherClient: ip {0}, port {1}", ip, port);
            }
            else if (commandType == CommandType.FileSelection)
            {
                Console.WriteLine("FileSelecion: {0}", selectedFile);
            }
            else if (commandType == CommandType.FileListShow)
            {
                Console.WriteLine("FileList: ");
                for (int i = 0; i < fileList.Length; ++i)
                {
                    Console.WriteLine(fileList[i]);
                }
            }
        }
    }
}
