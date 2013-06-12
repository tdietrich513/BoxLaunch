using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace BoxLaunch
{
    public class HashFileAction
    {
        public string FileName { get; set; }

        public bool Execute()
        {
            var fileInfo = new FileInfo(FileName);
            var dirInfo = fileInfo.Directory;
            if (dirInfo == null) return false;

            var tempFile = Path.GetTempFileName();
            var hasher = MD5.Create();
            var hfInfo = new FileInfo(dirInfo.FullName + "\\.blhash");
            if (!hfInfo.Exists) hfInfo.Create().Dispose();

            Console.Write("\t hashing {0}...", fileInfo.FullName);
            var fileStart = DateTime.Now;
            var hash = string.Join("", hasher.ComputeHash(fileInfo.OpenRead()).Select(b => b.ToString("x2")));
            Console.WriteLine(" Complete! ({0} ms)", (DateTime.Now - fileStart).TotalMilliseconds);

            using (var sr = new StreamReader(hfInfo.FullName))
            using (var sw = new StreamWriter(tempFile))
            {
                string line;
                var replacedline = false;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith(fileInfo.Name))
                    {
                        sw.Write(string.Format("{0}: {1}{2}", fileInfo.Name, hash, Environment.NewLine));
                        replacedline = true;
                    }
                    else
                    {
                        sw.Write(line + Environment.NewLine);
                    }                    
                }                
                if (!replacedline)
                {
                    sw.Write(string.Format("{0}: {1}{2}", fileInfo.Name, hash, Environment.NewLine));
                }
            }

            hfInfo.Delete();
            File.Move(tempFile, hfInfo.FullName);

            return true;
        }
    }
}