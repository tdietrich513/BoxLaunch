using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BoxLaunch
{
    public class SyncDirectoriesAction
    {
        private const decimal BytesToMegaBytes = 1048506M;
        private const decimal BytesToKiloBytes = 1024M;
        
        private const int ProgressLine = 1;
        private const int ThreadsStartAt = 3;

        private decimal _completed;
        private decimal _updateSize;
        private readonly object _consoleLock = new object();

        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public DirectoryInfo SourceDir { get; set; }
        public DirectoryInfo TargetDir { get; set; }

        private bool PathsAreValid()
        {
            if (!SourcePath.EndsWith("\\")) SourcePath += "\\";
            if (!TargetPath.EndsWith("\\")) TargetPath += "\\";

            if (!Directory.Exists(SourcePath))
            {
                Console.WriteLine("ERROR: Source directory ({0}) does not exist!", SourcePath);
                return false;
            }

            if (Directory.Exists(TargetPath)) return true;

            //Directory does not exist, must be created to precede. 

            try
            {
                Console.WriteLine("Target Directory not found, creating directory...");
                Directory.CreateDirectory(TargetPath);
                return true;
            }
            catch
            {
                Console.WriteLine(
                    "ERROR: Could not create target directory!/r/nMake sure you have write rights to {0} and try again.", TargetPath);
                return false;
            }
        }

        private List<UpdateItem> GetUpdates()
        {
            if (TargetDir.GetFiles().Any()) Console.WriteLine("Checking for updates...");

            var folderContentsQuery = new GetFolderContentsQuery { Folder = SourceDir };

            var updates = new List<UpdateItem>();
            foreach (var sourceFile in folderContentsQuery.Execute())
            {
                var targetFile = new FileInfo(TargetPath + "\\" + sourceFile.Name);

                if (!targetFile.Exists || sourceFile.LastWriteTime != targetFile.LastWriteTime)
                {
                    updates.Add(new UpdateItem { Source = sourceFile, Target = targetFile, FileSize = sourceFile.Length });
                }
            }
            return updates;
        }
       
        private static Dictionary<int, List<UpdateItem>> SplitUpdates(int splitCount, IEnumerable<UpdateItem> updates)
        {
            var splitUpdates = new Dictionary<int, List<UpdateItem>>(splitCount);
            int x;
            for (x = 0; x < splitCount; x++)
            {
                splitUpdates[x] = new List<UpdateItem>();
            }

            x = 0;
            foreach (var update in updates.OrderByDescending(ui => ui.FileSize))
            {
                splitUpdates[x % splitCount].Add(update);
                x += 1;
            }

            for (x = 0; x < splitCount; x++)
            {
                splitUpdates[x] = splitUpdates[x].OrderBy(a => Guid.NewGuid()).ToList();
            }
            return splitUpdates;
        }

        private void UpdateProgressText(int ctop)
        {
            var progressPct = decimal.Round((_completed / _updateSize) * 100M, 2);
            var progressText = ProgressText(progressPct, _completed, _updateSize);
            lock (_consoleLock)
            {
                Console.SetCursorPosition(0, ctop + ProgressLine);
                Console.Write(progressText.SpaceRight() );
            }
        }

        private string ProgressText(decimal progressPct, decimal completed, decimal updateSize)
        {
            return string.Format(
                "{0}% {3} ({1}/{2}MB)",
                progressPct,
                decimal.Round(completed / BytesToMegaBytes, 2),
                decimal.Round(updateSize / BytesToMegaBytes, 2),
                progressPct.ProgressBar(25, '#')
                );
        }       

        public bool Execute()
        {
            if (!PathsAreValid()) return false;
            SourceDir = new DirectoryInfo(SourcePath);
            TargetDir = new DirectoryInfo(TargetPath);

            var updates = GetUpdates();

            _completed = 0M;
            _updateSize = updates.Sum(x => x.FileSize);

            if (_updateSize == 0M)
            {
                Console.WriteLine("Program is up to date...");
                return true;
            }


            var startTime = DateTime.Now;
            var failure = false;
            
            Console.CursorVisible = false;
            var procCount = Environment.ProcessorCount;                        
            Console.WriteLine("Downloading current version using {0} threads...", procCount);
            var ctop = Console.CursorTop;
            var splitUpdates = SplitUpdates(procCount, updates);

            Parallel.ForEach(
                splitUpdates,
                new ParallelOptions { MaxDegreeOfParallelism = procCount },
                (list, state) => {
                    foreach (var update in list.Value)
                    {
                        var displayText = string.Format(
                            "{2}: Getting {0} ({1}KB)...",
                            update.Source.Name.Left(Console.WindowWidth / 2),
                            Decimal.Round(update.Source.Length / BytesToKiloBytes, 0),
                            list.Key);

                        lock (_consoleLock)
                        {
                            Console.SetCursorPosition(0, ctop + ThreadsStartAt + list.Key);
                            Console.Write(displayText.SpaceRight());
                        }

                        try
                        {
                            update.Source.CopyTo(update.Target.FullName, true);
                        }
                        catch
                        {
                            failure = true;
                            state.Break();
                        }

                        _completed += update.Source.Length;
                        UpdateProgressText(ctop);
                    }
                    lock (_consoleLock)
                    {
                        Console.SetCursorPosition(0, ctop + ThreadsStartAt + list.Key);
                        Console.Write("{0}: Work Complete.".SpaceRight(), list.Key);
                    }
                });

            if (failure)
            {
                Console.WriteLine();
                Console.WriteLine("Update failed! Is this program already open elsewhere?");
                Console.WriteLine("Press enter to try to launch anyway, or close this window to cancel opening.");
                Console.WriteLine("Note that the program may not work properly if you choose to launch it.");
                Console.CursorVisible = true;
                Console.ReadLine();
                return false;
            }

            _completed = _updateSize;
            UpdateProgressText(ctop);
            
            Console.SetCursorPosition(0, ctop + ThreadsStartAt + procCount + 1);
            var elapsedTime = Convert.ToDecimal(DateTime.Now.Subtract(startTime).TotalMilliseconds / 1000D);
            var completeText = string.Format(
                "Process complete, time: {0} seconds, rate {1} MB/s",
                decimal.Round(elapsedTime, 2),
                decimal.Round((_completed / BytesToMegaBytes) / elapsedTime, 2));
            if (!failure)
            {
                Console.WriteLine(completeText.SpaceRight());
                Thread.Sleep(500);
            }

            return true;
        }
    }
}