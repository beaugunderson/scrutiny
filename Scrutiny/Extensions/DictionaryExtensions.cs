using System;
using System.Collections.Generic;

namespace Scrutiny.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>
            (this IDictionary<TKey, TValue> dictionary,
                TKey key)
        {
            lock (dictionary)
            {
                bool exists = dictionary.ContainsKey(key);

                if (!exists)
                {
                    TValue value;

                    if (typeof (TValue).IsValueType)
                    {
                        value = default(TValue);
                    }
                    else
                    {
                        value = Activator.CreateInstance<TValue>();
                    }

                    dictionary[key] = value;
                }

                return dictionary[key];
            }
        }
    }
}