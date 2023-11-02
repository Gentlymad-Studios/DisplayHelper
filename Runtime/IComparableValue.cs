using System;
using System.Collections.Generic;

namespace DisplayHelper {
    /// <summary>
    /// Special comparable value facility to sort new entries into the list fast
    /// </summary>
    /// <typeparam name="T">The type of the comparable value</typeparam>
    public interface IComparableValue<T> where T : IComparable {
        T GetValue();

        /// <summary>
        /// Search for a specific item by its comparable value and return its index.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="list">The list to search</param>
        /// <param name="value">The value to find</param>
        /// <returns></returns>
        public static int BinarySearch<U>(List<U> list, T value) where U : IComparableValue<T> {
            int left = 0;
            int right = list.Count - 1;

            while (left <= right) {
                int mid = left + (right - left) / 2;
                int comparison = list[mid].GetValue().CompareTo(value);

                if (comparison == 0) {
                    return mid; // Item found
                } else if (comparison < 0) {
                    left = mid + 1;
                } else {
                    right = mid - 1;
                }
            }

            return ~left;
        }

        /// <summary>
        /// Insert a specific item sorted by its comparable value and return its index.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="list">The list to search</param>
        /// <param name="value">The value to insert</param>
        /// <returns></returns>
        public static int InsertSorted<U>(List<U> list, T value, Func<U> callback, out bool existed) where U : IComparableValue<T> {
            int position = BinarySearch(list, value);

            if (position >= 0) {
                existed = true;
                return position;
            }

            U newItem = callback();
            list.Insert(~position, newItem);

            existed = false;
            return ~position;
        }
    }
}
