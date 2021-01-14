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
                    if (line.StartsWith("#")) continue;
                    ans += line + "\n";
                }
            }
            return ans;
        }

        public string ReadSelectedSWC_UWP(string swcFile)
        {
            if (swcFiles.IndexOf(swcFile) == -1)
            {
                return null;
            }

            string ans = "";
            using (StreamReader sr = new StreamReader(swcFolder + "/" + swcFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("#")) continue;
                    string[] words = line.Split();
                    for(int i = 0; i < words.Length; ++i)
                    {
                    
                        if (i >= 2 && i <= 5)
                        {
                            int num = (int)(float.Parse(words[i]) + 0.5f);
                            ans += num.ToString() + " ";
                        }
                        else
                        {
                            ans += words[i] + " ";
                        }
                       
                        
                    }
                }
                ans += "endSWC\n";
            }
            return ans;
        }

        public List<string> ReadSelectedSWCList_UWP(string swcFile,int limit = 40)
        {
            if (swcFiles.IndexOf(swcFile) == -1)
            {
                return null;
            }

            List<string> ans = new List<string>();
            int count = -1;
            string anss = "";
            using (StreamReader sr = new StreamReader(swcFolder + "/" + swcFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("#")) continue;
                    count++;
                    if (count >= limit)
                    {
                        ans.Add(anss);
                        anss = "";
                        count = 0;
                    }
                    string[] words = line.Split();
                    for (int i = 0; i < words.Length; ++i)
                    {

                        if (i >= 2 && i <= 5)
                        {
                            int num = (int)(float.Parse(words[i]) + 0.5f);
                            anss += num.ToString() + " ";
                        }
                        else
                        {
                            anss += words[i] + " ";
                        }
                    }
                }
            }
            if (count > 0 && count < limit)
            {
                ans.Add(anss);
            }
            for(int i = 0; i < ans.Count - 1; ++i)
            {
                ans[i] += "EndPart\n";
            }
            ans[ans.Count - 1] += "EndSWC\n";
            return ans;
        }
    }
}
