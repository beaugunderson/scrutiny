using System;

namespace NTFS.Extensions
{
    public static class DateTimeExtensions
    {
        public static Types.FileTime ToFiletime(this DateTime dateTime)
        {
            Types.FileTime dateTimeToFileTime;

            long fileTimeLong = dateTime.ToFileTimeUtc();
            
            dateTimeToFileTime.LowDateTime = (uint)(fileTimeLong & 0xFFFFFFFF);
            dateTimeToFileTime.HighDateTime = (uint)(fileTimeLong >> 32);
            
            return dateTimeToFileTime;
        }
    }
}