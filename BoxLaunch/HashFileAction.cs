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

            var hasher = MD5.Create();
            Console.Write("\t hashing {0}...", fileInfo.FullName);
            var fileStart = DateTime.Now;
            var hash = string.Join("", hasher.ComputeHash(fileInfo.OpenRead()).Select(b => b.ToString("x2")));
            Console.WriteLine(" Complete! ({0} ms)", (DateTime.Now - fileStart).TotalMilliseconds);

            return new UpdateHashFileAction(dirInfo, fileInfo.Name, hash).Execute();            
        }

    }
}