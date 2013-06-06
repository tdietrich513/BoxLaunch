using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BoxLaunch
{
    public class GetFolderContentsQuery
    {
        public DirectoryInfo Folder { get; set; }

        public IEnumerable<FileInfo> Execute()
        {
            var excludeList = new List<string> { ".blignore" };
            if (Folder.GetFiles(".blignore").Length > 0)
            {
                var ignoreFi = Folder.GetFiles(".blignore")[0];
                string ignoreData;
                using (var sr = ignoreFi.OpenText())
                {
                    ignoreData = sr.ReadToEnd();
                }

                foreach (var ignorePattern in ignoreData.Split(Environment.NewLine.ToArray(), StringSplitOptions.RemoveEmptyEntries))
                {
                    var filesThatAreIgnored = Folder.GetFiles(ignorePattern);
                    if (filesThatAreIgnored.Length > 0)
                    {
                        excludeList.AddRange(filesThatAreIgnored.Select(fi => fi.Name));
                    }
                }
            }

            return Folder.GetFiles().Where(fi => !excludeList.Contains(fi.Name));
        }

    }
}
