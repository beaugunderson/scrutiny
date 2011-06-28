using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace NTFS.Extensions
{
    public static class ParallelExtensions
    {
        private const int Threshold = 150;

        private static void Swap<T>(IList<T> array, int i, int j)
        {
            Contract.Requires(array != null);

            var temp = array[i];

            array[i] = array[j];

            array[j] = temp;
        }

        private static int Partition<T>(IList<T> array, int from, int to, int pivot) where T : IComparable<T>
        {
            Contract.Requires(array != null);

            var arrayPivot = array[pivot];

            Swap(array, pivot, to - 1);

            var newPivot = from;

            for (int i = from; i < to - 1; i++)
            {
                if (array[i].CompareTo(arrayPivot) <= 0)
                {
                    Swap(array, newPivot, i);

                    newPivot++;
                }
            }

            Swap(array, newPivot, to - 1);

            return newPivot;
        }

        private static void InsertionSort<T>(this IList<T> array, int from, int to) where T : IComparable<T>
        {
            Contract.Requires(array != null);

            for (int i = from + 1; i < to; i++)
            {
                var a = array[i];

                int j = i - 1;

                while (j >= from && array[j].CompareTo(a) > 0)
                {
                    array[j + 1] = array[j];

                    j--;
                }

                array[j + 1] = a;
            }
        }

        public static void SequentialQuickSort<T>(this T[] array) where T : IComparable<T>
        {
            Contract.Requires(array != null);

            SequentialQuickSort(array, 0, array.Length);
        }

        private static void SequentialQuickSort<T>(this IList<T> array, int from, int to) where T : IComparable<T>
        {
            Contract.Requires(array != null);

            if (to - from <= Threshold)
            {
                InsertionSort(array, from, to);
            }
            else
            {
                int pivot = from + (to - from) / 2;

                pivot = Partition(array, from, to, pivot);

                SequentialQuickSort(array, from, pivot);
                SequentialQuickSort(array, pivot + 1, to);
            }
        }

        public static void ParallelQuickSort<T>(this T[] array) where T : IComparable<T>
        {
            Contract.Requires(array != null);

            ParallelQuickSort(array, 0, array.Length,
                (int)Math.Log(Environment.ProcessorCount, 2) + 4);
        }

        public static void ParallelQuickSort<T>(this T[] array, int from, int to, int depthRemaining) where T : IComparable<T>
        {
            Contract.Requires(array != null);

            if (to - from <= Threshold)
            {
                InsertionSort(array, from, to);
            }
            else
            {
                int pivot = from + (to - from) / 2;

                pivot = Partition(array, from, to, pivot);

                if (depthRemaining > 0)
                {
                    Parallel.Invoke(
                        () => ParallelQuickSort(array, from, pivot, depthRemaining - 1),
                        () => ParallelQuickSort(array, pivot + 1, to, depthRemaining - 1));
                }
                else
                {
                    ParallelQuickSort(array, from, pivot, 0);
                    ParallelQuickSort(array, pivot + 1, to, 0);
                }
            }
        }
    }
}