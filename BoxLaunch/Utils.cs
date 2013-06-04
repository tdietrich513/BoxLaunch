using System;
using System.Linq;

namespace BoxLaunch
{
    public static class Utils
    {
        public static string Left(this string str, int length)
        {
            return str.Substring(0, Math.Min(length, str.Length));
        }

        public static string SpaceRight(this string str)
        {
            return str.PadRight(Console.WindowWidth - 1);
        }

        public static string ProgressBar(this decimal pct, int length, char symbol)
        {
            var dotsToShow = Convert.ToInt32(Math.Round(length * (pct / 100)));
            return string.Format(
                "[{0}{1}]",
                new string(Enumerable.Repeat(symbol, dotsToShow).ToArray()),
                new string(Enumerable.Repeat(' ', length - dotsToShow).ToArray()));
        }
    }
}