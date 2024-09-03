// plist-cil - An open source library to parse and generate property lists for .NET
// Copyright (C) 2016 Quamotion
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections;
using System.Collections.Generic;

namespace Claunia.PropertyList
{
    partial class NSArray : IList<NSObject>
    {
        /// <inheritdoc />
        public NSObject this[int index]
        {
            get => array[index];

            set => array[index] = value;
        }

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public void Add(NSObject item) => array.Add(item);

        /// <inheritdoc />
        public void Clear() => array.Clear();

        /// <inheritdoc />
        public bool Contains(NSObject item) => array.Contains(item);

        /// <inheritdoc />
        public void CopyTo(NSObject[] array, int arrayIndex) => this.array.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<NSObject> GetEnumerator() => array.GetEnumerator();

        /// <inheritdoc />
        public int IndexOf(NSObject item) => array.IndexOf(item);

        /// <inheritdoc />
        public void Insert(int index, NSObject item) => array.Insert(index, item);

        /// <inheritdoc />
        public bool Remove(NSObject item) => array.Remove(item);

        /// <inheritdoc />
        public void RemoveAt(int index) => array.RemoveAt(index);

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => array.GetEnumerator();

        public void Add(object item) => Add(Wrap(item));

        public bool Contains(object item) => Contains(Wrap(item));

        public int IndexOf(object item) => array.IndexOf(Wrap(item));

        public void Insert(int index, object item) => Insert(index, Wrap(item));

        public bool Remove(object item) => Remove(Wrap(item));
    }
}