using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BoxLaunch.Actions
{
    public class CleanDirectoryAction : IAction
    {
        public string TargetPath { get; set; }
        public DirectoryInfo TargetDir { get; set; }


        public bool Execute()
        {
            if (string.IsNullOrWhiteSpace(TargetPath))
            {
                Console.WriteLine("ERROR: No target path was provided!");
                return false;
            }

            if (!Directory.Exists(TargetPath))
            {
                Console.WriteLine("ERROR: Target directory ({0}) does not exist!", TargetPath);
                return false;
            }

            TargetDir = new DirectoryInfo(TargetPath);
            var files = TargetDir.GetFiles().ToList();

            try
            {
                foreach (var file in files)
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Exception cleaning directory: " + ex.ToString());
                return false;
            }
            

            return true;
        }
    }
}
