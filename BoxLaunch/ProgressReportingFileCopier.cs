using System;
using System.IO;

namespace BoxLaunch
{
    public delegate void ProgressChangeDelegate(double percentage, ref bool cancel);
    public delegate void Completedelegate();

    public class ProgressReportingFileCopier : IDisposable 
    {
        public string SourceFilePath { get; set; }
        public string DestFilePath { get; set; }

        public event ProgressChangeDelegate OnProgressChanged;
        public event Completedelegate OnComplete;

        public ProgressReportingFileCopier(string source, string dest)
        {
            SourceFilePath = source;
            DestFilePath = dest;

            OnProgressChanged += delegate { };
            OnComplete += delegate { };
        }

        public void Copy()
        {
            var buffer = new byte[1024 * 1024]; // 1MB buffer                

            using (var source = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read))
            {
                var fileLength = source.Length;                
                using (var dest = new FileStream(DestFilePath, FileMode.CreateNew, FileAccess.Write))
                {
                    long totalBytes = 0;
                    int currentBlockSize;

                    while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        totalBytes += currentBlockSize;
                        var progressPercentage = totalBytes * 100.0 / fileLength;

                        dest.Write(buffer, 0, currentBlockSize);

                        var cancelFlag = false;
                        OnProgressChanged(progressPercentage, ref cancelFlag);

                        if (cancelFlag)
                        {
                            // Delete dest file here
                            break;
                        }
                    }
                }
            }
            OnComplete();
        }

        public void Dispose()
        {
            OnComplete = null;
            OnProgressChanged = null;
        }
    }
}

