using System;
using System.IO;

namespace Scrutiny.Utilities
{
    public static class Paths
    {
        public static string CombineBaseDirectory(string path)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }
    }
}