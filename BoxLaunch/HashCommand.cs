using System.Collections.Generic;

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
                                      { "d|directory=", "The {DIRECTORY} that needs to be hashed.", v => Path = v },
                                      { "f|file=", "A {FILE} to hash.", v => File = v}
                                  };

            var extra = Parse(p, args, "hash", "-d={DIRECTORY}", "Creates a hash cache for a directory.");

            if (extra == null) return;

            if (Path != null)
            {
                var hashDirectoryAction = new HashDirectoryAction { HashPath = Path };
                hashDirectoryAction.Execute();
                return;
            }

            var hashFileAction = new HashFileAction { FileName = File };
            hashFileAction.Execute();
        }
    }
}