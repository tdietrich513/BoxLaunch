using System;
using System.Collections.Generic;
using System.Linq;
using BoxLaunch.Commands;
using NDesk.Options;

namespace BoxLaunch
{    
    public class Program
    {        
        internal static Dictionary<string, Func<ICommand>> SubCommands;

        public static void Main(string[] args)
        {
            SubCommands = new Dictionary<string, Func<ICommand>> {
                                  { "sync-and-run", () => new SyncAndRunCommand() },
                                  { "sync", () => new SyncCommand() },
                                  { "hash", () => new HashCommand() },
                                  { "copy-and-run", () => new CopyAndRunCommand() },
                                  { "help", () => new ProgramHelpCommand() },
                                  { "clean-directory", () => new CleanDirectoryCommand() },
                                  { "run-program", () => new ExecuteProgramCommand() }
                              };


            var showHelp = false;
            var showVersion = false;
            var pauseAfterRunning = false;

            var p = new OptionSet {
                { "h|?|help", v => showHelp = v != null },
                { "version", v => showVersion = v != null},
                { "pause", v => pauseAfterRunning = v != null}
            };
            
            var extra = p.Parse(args);
        
            if (showVersion) {
                Console.WriteLine("BoxLaunch 1.0");
                return;
            }

            if (extra.Count == 0) {
                Console.WriteLine("See 'BoxLaunch help' for usage.");
                return;
            }

            if (showHelp) {
                extra.Add("--help");
            }
            
          
            if (!SubCommands.ContainsKey(extra[0]))
            {
                if (args.Count() < 3) return;
                var defaultCommand = new SyncAndRunCommand();
                var translatedArgs = new List<string>()
                                         {
                                             "sync-and-run",
                                             "-s=" + args[0],
                                             "-t=" + args[1],
                                             "-p=" + args[2]
                                         };
                if (args.Count() > 3)
                    translatedArgs.AddRange(args.Skip(3).Select(a => "-a=" + a));

                defaultCommand.Run(translatedArgs);
                return;
            }
            
            var subCommand = SubCommands[extra[0]]();
            subCommand.Run(extra);

            if (pauseAfterRunning) Console.ReadLine();
        }      
    }
}
