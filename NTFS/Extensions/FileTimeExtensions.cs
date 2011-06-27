using System;

namespace NTFS.Extensions
{
    public static class FileTimeExtensions
    {
        public static DateTime ToDateTime(this Types.FileTime fileTime)
        {
            long fileTimeLong = (((long)fileTime.HighDateTime) << 32) | fileTime.LowDateTime;

            // XXX: FromFileTimeUTC returns a UTC DateTime which is less useful
            return DateTime.FromFileTime(fileTimeLong);
        }
    }
}