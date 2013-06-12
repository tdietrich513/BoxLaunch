using System;
using System.IO;

namespace BoxLaunch
{
    public class UpdateHashFileAction
    {    
        public UpdateHashFileAction(DirectoryInfo dirInfo, string fileName, string hash)
        {
            DirInfo = dirInfo;
            FileName = fileName;
            Hash = hash;
        }

        public DirectoryInfo DirInfo { get; set; }    
        public string FileName { get; set; }
        public string Hash { get; set; }

        public bool Execute()
        {
            var tempFile = Path.GetTempFileName();

            var hfInfo = new FileInfo(DirInfo.FullName + "\\.blhash");
            if (!hfInfo.Exists)
            {
                hfInfo.Create().Dispose();
            }

            using (var sr = new StreamReader(hfInfo.FullName))
            using (var sw = new StreamWriter(tempFile))
            {
                string line;
                var replacedline = false;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith(FileName))
                    {
                        sw.Write(string.Format("{0}: {1}{2}", FileName, Hash, Environment.NewLine));
                        replacedline = true;
                    }
                    else
                    {
                        sw.Write(line + Environment.NewLine);
                    }
                }
                if (!replacedline)
                {
                    sw.Write(string.Format("{0}: {1}{2}", FileName, Hash, Environment.NewLine));
                }
            }

            hfInfo.Delete();
            File.Move(tempFile, hfInfo.FullName);

            return true;
        }
    }
}