using System;
using System.Collections.Generic;

namespace BoxLaunch.Commands
{
    public class ProgramHelpCommand : ICommand 
    {
        public void Run(IEnumerable<string> args)
        {
            Console.WriteLine("usage: boxlaunch <command> [<args>]");
            Console.WriteLine();
            Console.WriteLine("The most commonly used boxlaunch commands are:");
            Console.WriteLine("   sync-and-run       Downloads updates to a directory then launches an executable.");
            Console.WriteLine("   copy-and-run       Downloads updates to a file then launches it.");
            Console.WriteLine("   sync               Downloads updates to a directory.");            
            Console.WriteLine("   hash               Creates a hash cache for a directory.");
            Console.WriteLine();
            Console.WriteLine("see 'boxlaunch --help <command> for more information on a specific command.");
        }
    }
}