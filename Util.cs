using System;
using System.Collections.Generic;
using System.Linq;

namespace Fingercrypt
{
    public static class Util
    {
        public static IEnumerable<IEnumerable<T>> Split<T>(this T[] array, int size)
        {
            for (var i = 0; i < (float)array.Length / size; i++)
            {
                yield return array.Skip(i * size).Take(size);
            }
        }
        
        public static IEnumerable<string> SplitByLength(this string str, int maxLength) {
            for (var index = 0; index < str.Length; index += maxLength) {
                yield return str.Substring(index, Math.Min(maxLength, str.Length - index));
            }
        }
        
        public static IEnumerable<string> WholeChunks(this string str, int chunkSize) {
            for (int i = 0; i < str.Length; i += chunkSize) 
                yield return str.Substring(i, chunkSize);
        }
        
        public static IEnumerable<string> Combinations(this IEnumerable<int> input, int length)
        {
            if (length <= 0)
                yield return "";
            else
            {
                foreach(var i in input)
                    foreach(var c in Combinations(input, length-1))
                        yield return i + c;
            }
        }
    }
}