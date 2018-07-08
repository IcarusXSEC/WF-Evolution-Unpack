using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EvolutionUnpack
{
    public class FileEntry
    {
        public long Offset { get; set; }
        public DateTime Date { get; set; }
        public int CompressedLength { get; set; }
        public int Length { get; set; }
        public int ScopeIndex { get; set; }
        public int ParentDirectoryIndex { get; set; }
        public string FileName { get; set; }

        public bool IsDirectory
        {
            get
            {
                return Offset == -1;
            }
        }
    }
}
