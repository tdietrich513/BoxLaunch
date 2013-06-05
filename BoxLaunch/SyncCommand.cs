﻿using System.Collections.Generic;

using NDesk.Options;

namespace BoxLaunch
{
    public class SyncCommand : BaseCommand
    {
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }

        public override void Run(IEnumerable<string> args)
        {            
            var p = new OptionSet {                
                                      { "s|source=", "The {SOURCE DIRECTORY} that contains the files to be copied.", v => SourcePath = v},
                                      { "t|target=", "The {TARGET DIRECTORY} that the files should be copied to.", v => TargetPath = v}                
                                  };

            var extra = Parse(
                p,
                args,
                "sync-and-run",
                "-s={SOURCE DIRECTORY} -t={TARGET DIRECTORY}",
                "Downloads updates to a directory.");

            if (extra == null) return;

            var syncAction = new SyncDirectoriesAction { SourcePath = SourcePath, TargetPath = TargetPath };
            syncAction.Execute();
        }
    }
}