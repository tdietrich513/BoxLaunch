using System;
using System.Collections.Generic;
using System.Linq;
using NDesk.Options;

namespace BoxLaunch.Commands
{
    public interface ICommand
    {
        void Run(IEnumerable<string> args);
    }

    public abstract class BaseCommand : ICommand
    {
        public abstract void Run(IEnumerable<string> args);

        protected List<string> Parse(OptionSet p, IEnumerable<string> args, string command, string prototype, string description)
        {
            var showHelp = false;
            p.Add("h|?|help", "Show this message and exit.", v => showHelp = v != null);

            List<string> extra = null;
            if (args != null)
            {
                extra = p.Parse(args.Skip(1));
            }
            if (args == null || showHelp)
            {
                Console.WriteLine("usage: BoxLaunch {0} {1}", args == null ? command : args.First(), prototype);
                Console.WriteLine();
                Console.WriteLine(description);
                Console.WriteLine();
                Console.WriteLine("Available Options:");
                p.WriteOptionDescriptions(Console.Out);
                return null;
            }
            return extra;
        }
    }
}