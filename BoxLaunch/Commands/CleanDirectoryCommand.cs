using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoxLaunch.Actions;
using NDesk.Options;

namespace BoxLaunch.Commands
{
    public class CleanDirectoryCommand : BaseCommand
    {
        public string TargetPath { get; set; }

        public override void Run(IEnumerable<string> args)
        {
            var p = new OptionSet
            {
                {
                    "t|target=", "The {TARGET DIRECTORY} that should be purged.",
                    v => TargetPath = v.EndsWith("\\") ? v : v + "\\"
                }
            };

            var extra = Parse(
                p,
                args,
                "clean-directory",
                "-t={TARGET DIRECTORY}",
                "Purges the contents of a directory."
                );

            if (extra == null) return;

            var action = new CleanDirectoryAction {
                TargetPath = TargetPath
            };

            action.Execute();
        }
    }
}
