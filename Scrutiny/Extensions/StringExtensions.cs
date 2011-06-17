namespace Scrutiny.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Get the string slice between the two indexes.
        /// Inclusive for start index, exclusive for end index.
        /// </summary>
        public static string Slice(this string source, int start, int end)
        { 
            // Keep this for negative end support
            if (end < 0)
            {
                end = source.Length + end;
            }

            int length = end - start;
            
            return source.Substring(start, length);
        }
    }
}
