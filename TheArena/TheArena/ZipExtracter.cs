using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace TheArena
{
    public static class ZipExtracter
    {
        public static void ExtractZip(string source_path, string output_dir)
        {
            ZipFile.ExtractToDirectory(source_path, output_dir);
        }
    }
}