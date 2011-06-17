using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Scrutiny.Utilities
{
    public static class ObservableCollectionExtensions
    {
        //public static void Sort<T>(this ObservableCollection<T> collection, IComparable<T> comparable)
        //{
        //    collection.Items

        //    Array.Sort<T>(collection.Items as List<T>, comparable);
        //}

        //public static void Sort<TSource, TKey>(this Collection<TSource> collection, Func<TSource, TKey> keySelector)
        //{
        //    List<TSource> sortedList = collection.OrderBy(keySelector).ToList();

        //    collection.Clear();

        //    foreach (var sortedItem in sortedList)
        //        collection.Add(sortedItem);
        //}

        //public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        //{
        //    var comparer = new Comparer<T>(comparison);

        //    List<T> sorted = collection.OrderBy(x => x, comparer).ToList();

        //    for (int i = 0; i < sorted.Count(); i++)
        //        collection.Move(collection.IndexOf(sorted[i]), i);
        //}

        //private class Comparer<T> : IComparer<T>
        //{
        //    private readonly Comparison<T> comparison;

        //    public Comparer(Comparison<T> comparison)
        //    {
        //        this.comparison = comparison;
        //    }

        //    public int Compare(T x, T y)
        //    {
        //        return comparison.Invoke(x, y);
        //    }
        //}


    }
}