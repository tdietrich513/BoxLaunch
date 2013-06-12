using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BoxLaunch
{
    public class HashCache
    {
        private readonly FileInfo _location;
        
        public List<FileHash> Hashes { get; set; }

        public HashCache(FileInfo location)
        {
            _location = location;
            Hashes = new List<FileHash>();
            

            if (!_location.Exists) return;

            Load();
        }      

        public bool ContainsFile(string fileName)
        {
            return Hashes.Any(fh => string.Equals(fileName, fh.FileName, StringComparison.OrdinalIgnoreCase));
        }

        public FileHash GetHash(string fileName)
        {
            return Hashes.First(fh => string.Equals(fileName, fh.FileName, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<FileHash> Differences(HashCache other)
        {
            foreach (var sourceHash in Hashes)
            {
                if (!other.ContainsFile(sourceHash.FileName))
                {
                    yield return sourceHash;
                    continue;
                }
                var otherHash = other.GetHash(sourceHash.FileName);
                if (otherHash.Hash != sourceHash.Hash)
                {
                    yield return sourceHash;                    
                }                
            }
        }

        public bool HashMatches(HashCache other, string fileName)
        {
            if (!ContainsFile(fileName) || other.ContainsFile(fileName)) return false;

            var sourceHash = GetHash(fileName);
            var targetHash = GetHash(fileName);

            return sourceHash == targetHash;
        }
    
        public void Create()
        {
            var dirInfo = _location.Directory;
            Console.WriteLine("Opening {0}", dirInfo.FullName);
            var query = new GetFolderContentsQuery { Folder = dirInfo };
            var files = query.Execute().Where(fi => fi.Name != ".blhash").ToList();
            Console.WriteLine("Found {0} file(s)", files.Count);                        

            var hashResults = new Dictionary<string, string>(files.Count);
            var procCount = Environment.ProcessorCount;
            var splitFiles = files.OrderByDescending(ui => ui.Length).SplitList(procCount);

            var processStart = DateTime.Now;
            Parallel.ForEach(
                splitFiles,
                new ParallelOptions { MaxDegreeOfParallelism = procCount },
                (list, state) =>
                {
                    var hasher = MD5.Create();
                    foreach (var file in list.Value)
                    {
                        var fileStart = DateTime.Now;
                        var hash = string.Join("", hasher.ComputeHash(file.OpenRead()).Select(b => b.ToString("x2")));
                        Console.WriteLine("  (Thread {2}) Hashed {0} in {1} ms", file.Name, Math.Floor((DateTime.Now - fileStart).TotalMilliseconds), list.Key);
                        hashResults[file.Name] = hash;
                    }
                });
          
            Console.WriteLine("Done! ({0} s)", (DateTime.Now - processStart).TotalSeconds);
            Hashes.RemoveAll(fh => hashResults.Keys.Contains(fh.FileName));
            Hashes.AddRange(hashResults.Select(kvp => new FileHash { FileName = kvp.Key, Hash = kvp.Value}));
        }

        public void Create(string singleFileName)
        {
            var fileInfo = new FileInfo(_location.Directory.FullName + "\\" + singleFileName);
            var dirInfo = fileInfo.Directory;
            if (dirInfo == null) return;

            var hasher = MD5.Create();
            Console.Write("  hashing {0}...", fileInfo.FullName);
            var fileStart = DateTime.Now;
            var hash = string.Join("", hasher.ComputeHash(fileInfo.OpenRead()).Select(b => b.ToString("x2")));
            Console.WriteLine("Complete! ({0} ms)", (DateTime.Now - fileStart).TotalMilliseconds);
            Hashes.RemoveAll(fh => fh.FileName == singleFileName);
            Hashes.Add(new FileHash { FileName = singleFileName, Hash = hash});            
        }

        public void Load()
        {
            Hashes.Clear();
            var hashRx = new Regex(@"^(?<file>[^:]+):\s(?<hash>.+)$");

            string[] sourceLines;
            using (var sr = new StreamReader(_location.FullName))
            {
                var contents = sr.ReadToEnd();
                sourceLines = contents.Split(Environment.NewLine.ToArray(), StringSplitOptions.RemoveEmptyEntries);
            }

            foreach (var line in sourceLines)
            {
                if (hashRx.IsMatch(line))
                {
                    var match = hashRx.Match(line);
                    Hashes.Add(new FileHash
                    {
                        FileName = match.Groups["file"].Value,
                        Hash = match.Groups["hash"].Value
                    });
                }
            }
        }
   
        public void Save()
        {
            var tempFile = Path.GetTempFileName();
            using (var sr = new StreamWriter(tempFile))
            {
                foreach (var fileHash in Hashes.OrderBy(fh => fh.FileName))
                {
                    sr.Write(string.Format("{0}: {1}{2}", fileHash.FileName, fileHash.Hash, Environment.NewLine));
                }
            }

            if(_location.Exists) _location.Delete();

            File.Move(tempFile, _location.FullName);
            Console.WriteLine("Saved hash values to {0}", _location.FullName);
        }

        public void Update(HashCache source)
        {
            foreach (var fh in source.Differences(this))
            {
                Hashes.RemoveAll(x => string.Equals(fh.FileName, x.FileName, StringComparison.OrdinalIgnoreCase));
                Hashes.Add(fh);
            }
        }

        public void Update(HashCache source, string fileName)
        {
            Hashes.RemoveAll(fh => string.Equals(fh.FileName, fileName));
            Hashes.Add(source.GetHash(fileName));
        }
    }
}
