using System.IO;

namespace BoxLaunch
{
    internal class UpdateItem
    {
        public FileInfo Source { get; set; }
        public FileInfo Target { get; set; }
        public decimal FileSize { get; set; }
    }
}