using System.Collections.Generic;
using System.IO;

using NDesk.Options;

namespace BoxLaunch
{
    public class CopyAndRunCommand : BaseCommand
    {
        public string SourceFile { get; set; }
        public string TargetPath { get; set; }

        public override void Run(IEnumerable<string> args)
        {            
            var p = new OptionSet {                                
                { "p|program=", "The {PROGRAM} to run once the directories are in sync.", v => SourceFile = v},                
                { "t|target=", "The {TARGET DIRECTORY} that the files should be copied to.", v => TargetPath = v.EndsWith("\\") ? v : v + "\\"}                
            };

            var extra = Parse(
                p,
                args,
                "copy-and-run",
                "-s={SOURCE DIRECTORY} -t={TARGET DIRECTORY} -p={PROGRAM}",
                "Downloads updates to a directory then launches an executable.");

            if (extra == null) return;

            var copyFileAction = new CopyFileAction {
                                         SourceFile = SourceFile, 
                                         TargetPath = TargetPath
                                     };

            if (!copyFileAction.Execute()) return;

            var fileName = (new FileInfo(SourceFile)).Name;

            var runExecutable = new RunExecutableAction { ExecutableName = fileName, TargetPath = TargetPath, ExecutableArgs = new List<string>() };
            runExecutable.Execute();
        }
    }
}