// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using LinqError = System.Linq.Expressions.Error;

namespace System.Dynamic.Utils
{
    internal abstract class ListProvider<T> : IList<T>
        where T : class
    {
        protected abstract T First { get; }
        protected abstract int ElementCount { get; }
        protected abstract T GetElement(int index);

        #region IList<T> Members

        public int IndexOf(T item)
        {
            if (First == item)
            {
                return 0;
            }

            for (int i = 1, n = ElementCount; i < n; i++)
            {
                if (GetElement(i) == item)
                {
                    return i;
                }
            }

            return -1;
        }

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public void Insert(int index, T item)
        {
            throw ContractUtils.Unreachable;
        }

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public void RemoveAt(int index)
        {
            throw ContractUtils.Unreachable;
        }

        public T this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return First;
                }

                return GetElement(index);
            }
            [ExcludeFromCodeCoverage(Justification = "Unreachable")]
            set
            {
                throw ContractUtils.Unreachable;
            }
        }

        #endregion

        #region ICollection<T> Members

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public void Add(T item)
        {
            throw ContractUtils.Unreachable;
        }

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public void Clear()
        {
            throw ContractUtils.Unreachable;
        }

        public bool Contains(T item) => IndexOf(item) != -1;

        public void CopyTo(T[] array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);
            if (index < 0)
            {
                throw LinqError.ArgumentOutOfRange(nameof(index));
            }

            int n = ElementCount;
            Debug.Assert(n > 0);
            if (index + n > array.Length)
            {
                throw new ArgumentException();
            }

            array[index++] = First;
            for (int i = 1; i < n; i++)
            {
                array[index++] = GetElement(i);
            }
        }

        public int Count => ElementCount;

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public bool IsReadOnly => true;

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public bool Remove(T item)
        {
            throw ContractUtils.Unreachable;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            yield return First;

            for (int i = 1, n = ElementCount; i < n; i++)
            {
                yield return GetElement(i);
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }

    /// <summary>
    /// Provides a wrapper around an IArgumentProvider which exposes the argument providers
    /// members out as an IList of Expression.  This is used to avoid allocating an array
    /// which needs to be stored inside of a ReadOnlyCollection.  Instead this type has
    /// the same amount of overhead as an array without duplicating the storage of the
    /// elements.  This ensures that internally we can avoid creating and copying arrays
    /// while users of the Expression trees also don't pay a size penalty for this internal
    /// optimization.  See IArgumentProvider for more general information on the Expression
    /// tree optimizations being used here.
    /// </summary>
    internal sealed class ListArgumentProvider : ListProvider<Expression>
    {
        private readonly IArgumentProvider _provider;
        private readonly Expression _arg0;

        internal ListArgumentProvider(IArgumentProvider provider, Expression arg0)
        {
            _provider = provider;
            _arg0 = arg0;
        }

        protected override Expression First => _arg0;
        protected override int ElementCount => _provider.ArgumentCount;
        protected override Expression GetElement(int index) => _provider.GetArgument(index);
    }
}
