/*
 * The Alphanum Algorithm is an improved sorting algorithm for strings
 * containing numbers.  Instead of sorting numbers in ASCII order like
 * a standard sort, this algorithm sorts numbers in numeric order.
 *
 * The Alphanum Algorithm is discussed at http://www.DaveKoelle.com
 *
 * Based on the Java implementation of Dave Koelle's Alphanum algorithm.
 * Contributed by Jonathan Ruckwood <jonathan.ruckwood@gmail.com>
 * 
 * Adapted by Dominik Hurnaus <dominik.hurnaus@gmail.com> to 
 *   - correctly sort words where one word starts with another word
 *   - have slightly better performance
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 *
 */

using System.Collections.Generic;
using System.Text;

/* 
 * Please compare against the latest Java version at http://www.DaveKoelle.com
 * to see the most recent modifications 
 */
namespace Scrutiny.Utilities
{
    public class AlphanumComparator<T> : IComparer<T>
    {
        // Length of string is passed in for improved efficiency (only need to calculate it once)
        private static string GetChunk(string s, int marker)
        {
            var chunk = new StringBuilder();

            var character = s[marker];
            
            chunk.Append(character);
            
            marker++;

            if (char.IsDigit(character))
            {
                while (marker < s.Length)
                {
                    character = s[marker];

                    if (!char.IsDigit(character))
                        break;

                    chunk.Append(character);

                    marker++;
                }
            }
            else
            {
                while (marker < s.Length)
                {
                    character = s[marker];

                    if (char.IsDigit(character))
                        break;

                    chunk.Append(character);

                    marker++;
                }
            }

            return chunk.ToString();
        }

        public int Compare(T x, T y)
        {
            // ToString() raise exception for null
            // Convert.ToString returns string.Empty
            // (string) allows null object
            string s1 = x.ToString();
            string s2 = y.ToString();

            int thisMarker = 0;
            int thatMarker = 0;

            while (thisMarker < s1.Length && thatMarker < s2.Length)
            {
                string thisChunk = GetChunk(s1, thisMarker);
                string thatChunk = GetChunk(s2, thatMarker);
                
                thisMarker += thisChunk.Length;
                thatMarker += thatChunk.Length;

                int result;

                // If both chunks contain numeric characters, sort them numerically
                if (char.IsDigit(thisChunk[0]) && char.IsDigit(thatChunk[0]))
                {
                    // Simple chunk comparison by length.
                    result = thisChunk.Length - thatChunk.Length;

                    // If equal, the first different number counts
                    if (result == 0)
                    {
                        for (int i = 0; i < thisChunk.Length; i++)
                        {
                            result = thisChunk[i] - thatChunk[i];
                    
                            if (result != 0)
                            {
                                return result;
                            }
                        }
                    }
                }
                else
                {
                    result = thisChunk.CompareTo(thatChunk);
                }

                if (result != 0)
                    return result;
            }

            return s1.Length - s2.Length;
        }
    }
}