using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BoxLaunch
{
    public class CopyFileAction
    {
        private const decimal BytesToMegaBytes = 1048506M;
        private const decimal BytesToKiloBytes = 1024M;

        private const int ProgressLine = 1;
        private const int ThreadsStartAt = 3;

        private decimal _completed;
        private decimal _updateSize;
        private readonly object _consoleLock = new object();

        public string SourceFile { get; set; }
        public string TargetPath { get; set; }
        public FileInfo SourceFileInfo { get; set; }
        public DirectoryInfo SourceDir { get; set; }
        public DirectoryInfo TargetDir { get; set; }

        private List<UpdateItem> GetUpdates()
        {
            if (TargetDir.GetFiles().Any()) Console.WriteLine("Checking for changes...");

            // if we've got a hash file in both target and source, use that to compute update.
            if (File.Exists(SourceDir.FullName + "\\.blhash") && File.Exists(TargetDir.FullName + "\\.blhash")) return UpdatesFromHash();

            // fall back to comparing file dates.
            return ForceUpdates();
        }

        private List<UpdateItem> ForceUpdates()
        {            
            var updates = new List<UpdateItem>();
            updates.Add(new UpdateItem() {Source = SourceFileInfo, Target = new FileInfo(TargetPath + "\\" + SourceFileInfo.Name), FileSize = SourceFileInfo.Length});
            if (File.Exists(SourceDir.FullName + "\\.blhash"))
            {
                var sourceHashFi = new FileInfo(SourceFileInfo.Directory.FullName + "\\.blhash");
                var targetHashFi = new FileInfo(TargetPath + "\\.blhash");
                updates.Add(new UpdateItem { Source = sourceHashFi, Target = targetHashFi, FileSize = sourceHashFi.Length});
            }
            return updates;
        }

        private List<UpdateItem> UpdatesFromHash()
        {
            var sourceHashFi = new FileInfo(SourceDir.FullName + "\\.blhash");
            var targetHashFi = new FileInfo(TargetPath + "\\.blhash");
            var hashRx = new Regex(@"^(?<file>[^:]+):\s(?<hash>.+)$");

            string[] sourceLines;
            using (var sr = new StreamReader(sourceHashFi.FullName))
            {
                var contents = sr.ReadToEnd();
                sourceLines = contents.Split('\n');
            }

            string[] targetLines;
            using (var sr = new StreamReader(targetHashFi.FullName))
            {
                var contents = sr.ReadToEnd();
                targetLines = contents.Split('\n');
            }

            var sourceHashes = sourceLines.ToDictionary(
                line => hashRx.Match(line).Groups["file"].Value,
                line => hashRx.Match(line).Groups["hash"].Value, StringComparer.OrdinalIgnoreCase);

            var targetHashes = targetLines.ToDictionary(
                line => hashRx.Match(line).Groups["file"].Value,
                line => hashRx.Match(line).Groups["hash"].Value, StringComparer.OrdinalIgnoreCase);

            var updates = new List<UpdateItem>();

            var buildUpdateItem = new Func<string, UpdateItem>(
                fileName =>
                {
                    var source = new FileInfo(SourceDir.FullName + "\\" + fileName);
                    return new UpdateItem
                    {
                        Source = source,
                        Target = new FileInfo(TargetPath + "\\" + fileName),
                        FileSize = source.Length
                    };
                });

            var sourceHash = sourceHashes[SourceFileInfo.Name];
            if (!targetHashes.ContainsKey(SourceFileInfo.Name))
            {
                // Target does not have a hash.
                updates.Add(buildUpdateItem(SourceFileInfo.Name));
                updates.Add(buildUpdateItem(".blhash"));
                return updates;
            }
            if (sourceHash != targetHashes[SourceFileInfo.Name])
            {
                // Hashes do not match.
                updates.Add(buildUpdateItem(SourceFileInfo.Name));
                updates.Add(buildUpdateItem(".blhash"));
                return updates;
            }
            if (!File.Exists(TargetPath + "\\" + SourceFileInfo.Name))
            {
                // Target is missing a file.
                updates.Add(buildUpdateItem(".blhash"));
                return updates;
            }

            return updates;
        }

        private bool PathsAreValid()
        {
            if (!SourceDir.Exists)
            {
                Console.WriteLine("ERROR: Source directory ({0}) does not exist!", SourceDir.FullName);
                return false;
            }

            if (TargetDir.Exists) return true;

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


        private void UpdateProgressText(int ctop)
        {
            var progressPct = decimal.Round((_completed / _updateSize) * 100M, 2);
            var progressText = ProgressText(progressPct, _completed, _updateSize);
            lock (_consoleLock)
            {
                Console.SetCursorPosition(0, ctop + ProgressLine);
                Console.Write(progressText.SpaceRight());
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
            if (!TargetPath.EndsWith("\\")) TargetPath += "\\";

            SourceFileInfo = new FileInfo(SourceFile);
            SourceDir = SourceFileInfo.Directory;
            TargetDir = new DirectoryInfo(TargetPath);

            if (!PathsAreValid()) return false;
            var updates = GetUpdates();

            _completed = 0M;
            _updateSize = updates.Sum(x => x.FileSize);

            if (_updateSize == 0M)
            {
                Console.WriteLine("Target is up to date...");
                return true;
            }


            var startTime = DateTime.Now;
            var failure = false;

            Console.CursorVisible = false;
            Console.WriteLine("Downloading current version...");
            var ctop = Console.CursorTop;

            Parallel.ForEach(
                updates,
                (update, state) =>
                {

                    var displayText = string.Format(
                        "Getting {0} ({1}KB)...",
                        update.Source.Name.Left(Console.WindowWidth / 2),
                        Decimal.Round(update.Source.Length / BytesToKiloBytes, 0));

                    Console.WriteLine(displayText.SpaceRight());

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

            Console.SetCursorPosition(0, ctop + ThreadsStartAt + updates.Count + 1);
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
