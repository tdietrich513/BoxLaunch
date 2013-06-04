using System;
using System.Collections.Generic;
using System.Linq;

using NDesk.Options;

namespace BoxLaunch
{    
    public class Program
    {        
        internal static Dictionary<string, Func<ICommand>> SubCommands;

        public static void Main(string[] args)
        {
            SubCommands = new Dictionary<string, Func<ICommand>>
                              {
                                  { "sync-and-run", () => new SyncAndRunCommand() },
                                  { "sync", () => new SyncCommand() },
                                  { "hash", () => new HashCommand() }
                              };


            var showHelp = false;
            var showVersion = false;
            var pauseAfterRunning = false;

            var p = new OptionSet() {
                { "h|?|help", v => showHelp = v != null },
                { "version", v => showVersion = v != null},
                { "pause", v => pauseAfterRunning = v != null}
            };
            
            var extra = p.Parse(args);

            if (extra.Count == 0) {
                Console.WriteLine("Use 'BoxLaunch help' for usage.");
                return;
            }

            if (showVersion) {
                Console.WriteLine("BoxLaunch 1.0");
                return;
            }

            if (showHelp) {
                extra.Add("--help");
            }

            if (!SubCommands.ContainsKey(extra[0]))
            {
                Console.WriteLine("{0} is not a recognized command. See 'BoxLaunch help' for usage.", extra[0]);
                return;
            }
            
            var subCommand = SubCommands[extra[0]]();
            subCommand.Run(extra);

            if (pauseAfterRunning) Console.ReadLine();
        }      
    }
}
