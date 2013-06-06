using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BoxLaunch
{
    public class HashDirectoryAction
    {
        public string HashPath { get; set; }

        public bool Execute()
        {
            var dirInfo = new DirectoryInfo(HashPath);
            Console.WriteLine("Opening {0}", dirInfo.FullName);
            var query = new GetFolderContentsQuery { Folder = dirInfo };
            var files = query.Execute().Where(fi => fi.Name != ".blhash").ToList();
            Console.WriteLine("Found {0} file(s)", files.Count);

            var hfInfo = new FileInfo(HashPath + ".blhash");                        
            
            var tempFile = Path.GetTempFileName();

            var hashResults = new Dictionary<string, string>(files.Count);
            var procCount = Environment.ProcessorCount;
            var splitFiles = SplitFiles(procCount, files);

            var processStart = DateTime.Now;
            Parallel.ForEach(
                splitFiles,
                new ParallelOptions { MaxDegreeOfParallelism = procCount },
                (list, state) => {
                    var hasher = MD5.Create();
                    foreach (var file in list.Value)
                    {                        
                        var fileStart = DateTime.Now;
                        var hash = string.Join("", hasher.ComputeHash(file.OpenRead()).Select(b => b.ToString("x2")));
                        Console.WriteLine("\tHashed {0} in {1} ms", file.Name, (DateTime.Now - fileStart).TotalMilliseconds);
                        hashResults[file.Name] = hash;
                    }
                });
            
            using (var sr = new StreamWriter(tempFile))
            {
                foreach (var kvp in hashResults.OrderBy(kvp => kvp.Key)) {                
                    sr.Write(string.Format("{0}: {1}{2}", kvp.Key, kvp.Value, Environment.NewLine ));                    
                }                
            }
            Console.WriteLine("Done! ({0} s)", (DateTime.Now - processStart).TotalSeconds);

            hfInfo.Delete();
            File.Move(tempFile, hfInfo.FullName);

            return true;
        }

        private static Dictionary<int, List<FileInfo>> SplitFiles(int splitCount, IEnumerable<FileInfo> files)
        {
            var splitUpdates = new Dictionary<int, List<FileInfo>>(splitCount);
            int x;
            for (x = 0; x < splitCount; x++)
            {
                splitUpdates[x] = new List<FileInfo>();
            }

            x = 0;
            foreach (var file in files.OrderByDescending(ui => ui.Length))
            {
                splitUpdates[x % splitCount].Add(file);
                x += 1;
            }

            for (x = 0; x < splitCount; x++)
            {
                splitUpdates[x] = splitUpdates[x].OrderBy(a => Guid.NewGuid()).ToList();
            }
            return splitUpdates;
        }
    }
}