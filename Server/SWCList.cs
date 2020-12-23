using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace net_csharp
{
    public class SWCList
    {
        private string swcFolder;
        private List<string> swcFiles = new List<string>();

        public SWCList(string FolderPath)
        {
            swcFolder = FolderPath;
            string []swcFilesArray = Directory.GetFiles(swcFolder,"*.swc");
            for(int i = 0; i < swcFilesArray.Length; ++i)
            {
                int index = swcFilesArray[i].IndexOf('\\');
                swcFiles.Add(swcFilesArray[i].Substring(index + 1));
            }
        }

        public void Show()
        {
            foreach(string swcFile in swcFiles)
            {
                Console.WriteLine(swcFile);
            }
        }

        public List<string> GetSWCFilesName()
        {
            return swcFiles;
        }

        public string ReadSelectedSWC(string swcFile)
        {
            if (swcFiles.IndexOf(swcFile) == -1)
            {
                return null;
            }
            
            string ans = "";
            using(StreamReader sr=new StreamReader(swcFolder + "/" + swcFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    ans += line + "\n";
                }
            }
            return ans;
        }
        
    }
}
