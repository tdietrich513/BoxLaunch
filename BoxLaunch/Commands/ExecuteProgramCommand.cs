using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoxLaunch.Actions;
using NDesk.Options;

namespace BoxLaunch.Commands
{
    public class ExecuteProgramCommand : BaseCommand
    {
        public string TargetPath { get; set; }

        public string ExecutableName { get; set; }
        public List<string> ExecutableArgs { get; set; }

        public override void Run(IEnumerable<string> args)
        {
            ExecutableArgs = new List<string>();

            var p = new OptionSet
            {
                { "t|target=", "The {TARGET DIRECTORY} containing the program to be executed", v => TargetPath = v.EndsWith("\\") ? v : v + "\\" },
                { "p|program=", "The {PROGRAM} to execute.", v => ExecutableName = v },
                { "a|arg=", "An {ARGUMENT} that should be passed to the executable.", ExecutableArgs.Add }
            };

            var extra = Parse(
                p,
                args,
                "execute-program",
                "-t={TARGET DIRECTORY} -p={PROGRAM}",
                "Executes a program.");

            if (extra == null) return;

            var executeAction = new RunExecutableAction
            {
                TargetPath = TargetPath,
                ExecutableName = ExecutableName,
                ExecutableArgs = ExecutableArgs
            };

            executeAction.Execute();
        }
    }
}
