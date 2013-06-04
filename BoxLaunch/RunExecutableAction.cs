using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace BoxLaunch
{
    public class RunExecutableAction
    {
        public string TargetPath { get; set; }
        public string ExecutableName { get; set; }
        public List<string> ExecutableArgs { get; set; }
   
        public bool Execute()
        {
            if (!File.Exists(TargetPath + ExecutableName))
            {
                Console.WriteLine("ERROR: Executable ({0}) does not exist in target directory!", ExecutableName);
                return false;
            }

            var executableLocation = "\"" + TargetPath + ExecutableName + "\"";
            Console.WriteLine("Launching Program...");
            var psi = new ProcessStartInfo { FileName = executableLocation, Arguments = string.Join(" ", ExecutableArgs) };
            Process.Start(psi);
            return true;
        }
    }
}