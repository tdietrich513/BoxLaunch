﻿namespace BoxLaunch
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

		private static bool ArgsAreValid(string[] args)
		{
			if (args.Contains("-help") || args.Count() == 0)
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

			if (targetDir.GetFiles().Count() > 0) Console.WriteLine("Checking for updates...");			

			var updates = new List<UpdateItem>();
			foreach (var sourceFile in sourceDir.GetFiles())
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
			var updateSize = updates.Sum(x => x.FileSize);
			
			var completed = 0M;			
			
			var startTime = DateTime.Now;
			var failure = false;
			if (updateSize > 0M)
			{
				Console.CursorVisible = false;
				Console.WriteLine("Downloading current version...");

				foreach (var update in updates)
				{
					var progressPct = decimal.Round((completed / updateSize) * 100M, 2);

					var progressText = ProgressText(progressPct, completed, updateSize);

					var displayText = string.Format(
						"Getting {0} ({1}KB)...", update.Source.Name.Left(52), Decimal.Round(update.Source.Length / BytesToKiloBytes, 0));

					Console.SetCursorPosition(0, Console.CursorTop);
					Console.WriteLine(SpaceRight(progressText));
					Console.Write(SpaceRight(displayText));
					Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 1);

					try
					{
						update.Source.CopyTo(update.Target.FullName, true);
					}
					catch
					{
						failure = true;
						Console.WriteLine();
						Console.WriteLine("Update failed! Is this program already open elsewhere?");
						Console.WriteLine("Press enter to try to launch anyway, note that the program may not function properly.");
						Console.CursorVisible = true;
						Console.ReadLine();
						break;
					}

					completed += update.Source.Length;
				}

				var finalProgressText = ProgressText(100M, updateSize, updateSize);

				Console.SetCursorPosition(0, Console.CursorTop);
				Console.WriteLine(SpaceRight(finalProgressText));
				var elapsedTime = Convert.ToDecimal(DateTime.Now.Subtract(startTime).TotalMilliseconds / 1000D);
				var completeText = string.Format(
					"Process complete, time: {0} seconds, rate {1} MB/s",
					decimal.Round(elapsedTime, 2),
					decimal.Round((completed / BytesToMegaBytes) / elapsedTime, 2)
					);
				if (!failure)
				{
					Console.WriteLine(SpaceRight(completeText));
					System.Threading.Thread.Sleep(5000);
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

	internal class UpdateItem
	{
		public FileInfo Source { get; set; }
		public FileInfo Target { get; set; }
		public decimal FileSize { get; set; }
	}

	public static class Utils
	{
		public static string Left(this string str, int length)
		{
			return str.Substring(0, Math.Min(length, str.Length));
		}
	}
}
