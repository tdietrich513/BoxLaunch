using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace BoxLaunch
{
    using System.Diagnostics;
    using System.IO;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class Program
    {
        private const decimal BytesToMegaBytes = 1048506M;
        private const decimal BytesToKiloBytes = 1024M;
        private static decimal _completed;
        private static decimal _updateSize;
        private static readonly object ConsoleLock = new object();
        private const int MessageLine = 0;
        private const int ProgressLine = 1;
        private const int ThreadsStartAt = 3;

        private static bool ArgsAreValid(string[] args)
        {
            if (args.Contains("-help") || !args.Any())
            {
                PrintHelp();
                return false;
            }

            if (args.Count() < 3)
            {
                Console.WriteLine("ERROR: Must provide source, target, and executable arguments!");
                PrintHelp();
                return false;
            }

            return true;
        }

        private static bool PathsAreValid(string sourcePath, string targetPath)
        {
            if (!Directory.Exists(sourcePath))
            {
                Console.WriteLine("ERROR: Source directory ({0}) does not exist!", sourcePath);
                return false;
            }

            if (Directory.Exists(targetPath)) return true;

            //Directory does not exist, must be created to precede. 

            try
            {
                Console.WriteLine("Target Directory not found, creating directory...");
                Directory.CreateDirectory(targetPath);
                return true;
            }
            catch
            {
                Console.WriteLine(
                    "ERROR: Could not create target directory!/r/nMake sure you have write rights to {0} and try again.", targetPath);
                return false;
            }
        }

        private static List<UpdateItem> GetUpdates(string sourcePath, string targetPath)
        {
            var sourceDir = new DirectoryInfo(sourcePath);
            var targetDir = new DirectoryInfo(targetPath);
            
            var excludeList = new List<string> { ".blignore" };
            if (sourceDir.GetFiles(".blignore").Length > 0)
            {
                var ignoreFi = sourceDir.GetFiles(".blignore")[0];
                string ignoreData;
                using (var sr = ignoreFi.OpenText())
                {
                    ignoreData = sr.ReadToEnd();
                }

                foreach (var ignorePattern in ignoreData.Split('\n'))
                {
                    var filesThatAreIgnored = sourceDir.GetFiles(ignorePattern);
                    if (filesThatAreIgnored.Length > 0)
                    {
                        excludeList.AddRange(filesThatAreIgnored.Select(fi => fi.Name));
                    }
                }
            }

            if (targetDir.GetFiles().Any()) Console.WriteLine("Checking for updates...");

            var updates = new List<UpdateItem>();
            foreach (var sourceFile in sourceDir.GetFiles().Where(fi => !excludeList.Contains(fi.Name)))
            {
                var targetFile = new FileInfo(targetPath + "\\" + sourceFile.Name);

                if (!targetFile.Exists || sourceFile.LastWriteTime != targetFile.LastWriteTime)
                {
                    updates.Add(new UpdateItem { Source = sourceFile, Target = targetFile, FileSize = sourceFile.Length });
                }
            }
            return updates;
        }

        static void Main(string[] args)
        {
            if (!ArgsAreValid(args)) return;

            var sourcePath = args[0];
            var targetPath = args[1];
            var executableName = args[2];
            string arguments = string.Empty;
            if (args.Length > 3) arguments = args[3];

            if (!sourcePath.EndsWith("\\")) sourcePath += "\\";
            if (!targetPath.EndsWith("\\")) targetPath += "\\";
            if (!PathsAreValid(sourcePath, targetPath)) return;

            if (!File.Exists(sourcePath + executableName))
            {
                Console.WriteLine("ERROR: Executable ({0}) does not exist in source directory!", executableName);
                return;
            }

            var updates = GetUpdates(sourcePath, targetPath);

            _completed = 0M;
            _updateSize = updates.Sum(x => x.FileSize);

            var startTime = DateTime.Now;
            var failure = false;
            if (_updateSize > 0M)
            {
                Console.CursorVisible = false;
                var procCount = Environment.ProcessorCount;

                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Downloading current version using {0} threads...", procCount);                

                var splitUpdates = SplitUpdates(procCount, updates);

                Parallel.ForEach(splitUpdates, new ParallelOptions { MaxDegreeOfParallelism = procCount},
                    (list, state) =>
                    {
                        foreach (var update in list.Value)
                        {
                            var displayText = string.Format(
                                "Getting {0} ({1}KB)...",
                                update.Source.Name.Left(Console.WindowWidth / 2),
                                Decimal.Round(update.Source.Length / BytesToKiloBytes, 0));

                            lock (ConsoleLock)
                            {
                                Console.SetCursorPosition(0, ThreadsStartAt + list.Key);
                                Console.Write(SpaceRight(displayText));
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
                            UpdateProgressText();
                        }
                        lock (ConsoleLock)
                        {
                            Console.SetCursorPosition(0, ThreadsStartAt + list.Key);
                            Console.Write(SpaceRight(""));
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
                }

                Console.Clear();

                Console.SetCursorPosition(0, MessageLine);
                var elapsedTime = Convert.ToDecimal(DateTime.Now.Subtract(startTime).TotalMilliseconds / 1000D);
                var completeText = string.Format(
                    "Process complete, time: {0} seconds, rate {1} MB/s",
                    decimal.Round(elapsedTime, 2),
                    decimal.Round((_completed / BytesToMegaBytes) / elapsedTime, 2)
                    );
                if (!failure)
                {
                    Console.WriteLine(SpaceRight(completeText));
                    Thread.Sleep(3000);
                }
            }
            else
            {
                Console.WriteLine("Program is up to date...");
            }

            if (!File.Exists(targetPath + executableName))
            {
                Console.WriteLine("ERROR: Executable ({0}) does not exist in target directory!", executableName);
                return;
            }

            var executableLocation = "\"" + targetPath + executableName + "\"";
            Console.WriteLine("Launching Program...");
            var psi = new ProcessStartInfo { FileName = executableLocation, Arguments = arguments };
            Process.Start(psi);
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

        private static string SpaceRight(string forText)
        {
            return forText + new string(Enumerable.Repeat(' ', Console.WindowWidth - forText.Length - 1).ToArray());
        }

        private static object ProgressBar(decimal progressPct, int length)
        {
            var dotsToShow = Convert.ToInt32(Math.Round(length * (progressPct / 100)));
            return string.Format(
                "[{0}{1}]",
                new string(Enumerable.Repeat('#', dotsToShow).ToArray()),
                new string(Enumerable.Repeat(' ', length - dotsToShow).ToArray())
                );
        }

        private static void PrintHelp()
        {
            Console.WriteLine("SYNTAX");
            Console.WriteLine("\t BoxLaunch <source> <target> <executable> <arguments>");
        }

        private static void UpdateProgressText()
        {
            var progressPct = decimal.Round((_completed / _updateSize) * 100M, 2);
            var progressText = ProgressText(progressPct, _completed, _updateSize);
            lock (ConsoleLock)
            {
                Console.SetCursorPosition(0, ProgressLine);
                Console.Write(SpaceRight(progressText));
            }
        }

        private static string ProgressText(decimal progressPct, decimal completed, decimal updateSize)
        {
            return string.Format(
                        "{0}% {3} ({1}/{2}MB)",
                        progressPct,
                        decimal.Round(completed / BytesToMegaBytes, 2),
                        decimal.Round(updateSize / BytesToMegaBytes, 2),
                        ProgressBar(progressPct, 25)
                        );
        }
    }
}
