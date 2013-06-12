using System.Collections.Generic;
using System.IO;

using NDesk.Options;

namespace BoxLaunch
{
    public class HashCommand : BaseCommand
    {
        public string Path { get; set; }
        public string File { get; set; }

        public override void Run(IEnumerable<string> args)
        {
            var p = new OptionSet {
                                      { "d|directory=", "The {DIRECTORY} that needs to be hashed.", v => Path = v.EndsWith("\\") ? v : v + "\\" },
                                      { "f|file=", "A {FILE} to hash.", v => File = v}
                                  };

            var extra = Parse(p, args, "hash", "-d={DIRECTORY}", "Creates a hash cache for a directory.");

            if (extra == null) return;

            if (Path != null)
            {
                var hashCache = new HashCache(new FileInfo(Path + "\\.blhash"));
                hashCache.Create();
                hashCache.Save();
                return;
            }

            var fileInfo = new FileInfo(File);
            var hashcache = new HashCache( new FileInfo(fileInfo.DirectoryName + "\\.blhash"));
            hashcache.Create(fileInfo.Name);
            hashcache.Save();
        }
    }
}