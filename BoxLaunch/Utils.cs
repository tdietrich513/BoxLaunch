using System;
using System.Collections.Generic;
using System.IO;
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

        public static Dictionary<int, List<T>> SplitList<T>(this IEnumerable<T> list, int splitCount, bool randomize = true)
        {
            var returnList = new Dictionary<int, List<T>>(splitCount);
            int x;
            for (x = 0; x < splitCount; x++)
            {
                returnList[x] = new List<T>();
            }

            x = 0;
            foreach (var item in list)
            {
                returnList[x % splitCount].Add(item);
                x += 1;
            }
            
            if (!randomize) return returnList;

            for (x = 0; x < splitCount; x++)
            {
                returnList[x] = returnList[x].OrderBy(a => Guid.NewGuid()).ToList();
            }
            return returnList;
        }
    }
}