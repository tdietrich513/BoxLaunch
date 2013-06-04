using System.Collections.Generic;

using NDesk.Options;

namespace BoxLaunch
{
    public class SyncAndRunCommand : BaseCommand
    {       
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        
        public string ExecutableName { get; set; }
        public List<string> ExecutableArgs { get; set; }
   
        public override void Run(IEnumerable<string> args)
        {
            ExecutableArgs = new List<string>();

            var p = new OptionSet {                
                { "s|source=", "The {SOURCE DIRECTORY} that contains the files to be copied.", v => SourcePath = v},
                { "t|target=", "The {TARGET DIRECTORY} that the files should be copied to.", v => TargetPath = v},
                { "p|program=", "The {PROGRAM} to run once the directories are in sync.", v => ExecutableName = v},
                { "a|arg=", "An {ARGUMENT} that should be passed to the executable.", ExecutableArgs.Add}
            };

            var extra = Parse(
                p,
                args,
                "sync-and-run",
                "-s={SOURCE DIRECTORY} -t={TARGET DIRECTORY} -p={PROGRAM}",
                "Downloads updates to a directory then launches an executable.");

            if (extra == null) return;

            var syncAction = new SyncDirectoriesAction { SourcePath = SourcePath, TargetPath = TargetPath };
            if (!syncAction.Execute()) return;

            var executeAction = new RunExecutableAction {
                                        TargetPath = TargetPath,
                                        ExecutableName = ExecutableName,
                                        ExecutableArgs = ExecutableArgs
                                    };
            executeAction.Execute();
        }        
    }
}