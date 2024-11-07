#nullable enable
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// An unsafe class that provides a set of methods to access the underlying data representations of collections.
    /// </summary>
    public static class CollectionsMarshal
    {
        private class ListView<T>
        {
            public T[] _items;
            public int _size;
            public int _version;
            private static readonly T[] s_emptyArray = new T[0];

            public void EnsureCapacity(int min)
            {
                if (this._items.Length >= min)
                    return;
                int num = this._items.Length == 0 ? 4 : this._items.Length * 2;
                if ((uint)num > 2146435071U)
                    num = 2146435071;
                if (num < min)
                    num = min;
                this.Capacity = num;
            }

            public int Capacity
            {
                get => this._items.Length;
                set
                {
                    if (value < this._size)
                        throw new ArgumentOutOfRangeException(nameof(value), "Capacity is set too small");
                    if (value == this._items.Length)
                        return;
                    if (value > 0)
                    {
                        T[] destinationArray = new T[value];
                        if (this._size > 0)
                            Array.Copy((Array)this._items, 0, (Array)destinationArray, 0, this._size);
                        this._items = destinationArray;
                    }
                    else
                        this._items = s_emptyArray;
                }
            }
        }

        /// <summary>
        /// Get a <see cref="Span{T}"/> view over a <see cref="List{T}"/>'s data.
        /// Items should not be added or removed from the <see cref="List{T}"/> while the <see cref="Span{T}"/> is in use.
        /// </summary>
        /// <param name="list">The list to get the data view over.</param>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        public static Span<T> AsSpan<T>(this List<T>? list)
        {
            if (list is null)
                return default;
            var listView = Unsafe.As<ListView<T>>(list);
            return listView._items.AsSpan();
        }

        public static void SetCount<T>(this List<T> list, int count)
        {
            ForceSetCount(list, count);
        }

        /// <summary>
        /// Sets the count of the <see cref="List{T}"/> to the specified value.
        /// </summary>
        /// <param name="list">The list to set the count of.</param>
        /// <param name="count">The value to set the list's count to.</param>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <exception cref="NullReferenceException">
        /// <paramref name="list"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is negative.
        /// </exception>
        /// <remarks>
        /// When increasing the count, uninitialized data is being exposed.
        /// </remarks>
        public static void ForceSetCount<T>(this List<T> list, int count)
        {
            if (list is null)
                throw new NullReferenceException("list is null.");
            var listView = Unsafe.As<ListView<T>>(list);
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"{nameof(count)} must be more than 0!");
            }

            listView._version++;

            if (count > list.Capacity)
            {
                listView.EnsureCapacity(count);
            }
            else if (count < listView._size && RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(listView._items, count, listView._size - count);
            }

            listView._size = count;
        }
    }
}