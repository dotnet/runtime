// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    internal sealed class ChildSections : ICollection<IConfigurationSection>, IEnumerator<IConfigurationSection>
    {
        private const int NotEnumerating = -2;

        private readonly IConfigurationRoot _root;
        private readonly string? _path;
        private readonly string[] _keys;
        private readonly int _count;
        private bool _ordered;
        private int _index = NotEnumerating;

        internal ChildSections(IConfigurationRoot root, string? path, string[] keys, int count)
        {
            _root = root;
            _path = path;
            _keys = keys;
            _count = count;
        }

        public int Count => _count;

        public bool IsReadOnly => true;

        public IConfigurationSection Current => Section(_keys[_index]);

        object IEnumerator.Current => Current;

        public IEnumerator<IConfigurationSection> GetEnumerator()
        {
            EnsureOrdered();
            if (_index == NotEnumerating)
            {
                _index = -1;
                return this;
            }

            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool MoveNext() => ++_index < _count;

        public void Reset() => _index = -1;

        public void Dispose() => _index = NotEnumerating;

        public void CopyTo(IConfigurationSection[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

            if (array.Length - arrayIndex < _count)
            {
                throw new ArgumentException(SR.Error_DestinationArrayTooSmall, nameof(array));
            }

            EnsureOrdered();
            for (int i = 0; i < _count; i++)
            {
                array[arrayIndex + i] = Section(_keys[i]);
            }
        }

        public bool Contains(IConfigurationSection item)
        {
            if (item?.Path is not string path)
            {
                return false;
            }

            int start = 0;
            if (_path is not null)
            {
                if (path.Length <= _path.Length || path[_path.Length] != ':' || !path.StartsWith(_path, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                start = _path.Length + 1;
            }

            ReadOnlySpan<char> key = path.AsSpan(start);
            for (int i = 0; i < _count; i++)
            {
                if (key.Equals(_keys[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        void ICollection<IConfigurationSection>.Add(IConfigurationSection item) =>
            throw new NotSupportedException(SR.Error_ReadOnlyChildKeys);

        void ICollection<IConfigurationSection>.Clear() =>
            throw new NotSupportedException(SR.Error_ReadOnlyChildKeys);

        bool ICollection<IConfigurationSection>.Remove(IConfigurationSection item) =>
            throw new NotSupportedException(SR.Error_ReadOnlyChildKeys);

        private IConfigurationSection Section(string key)
        {
            return _root.GetSection(_path is null ? key : _path + ConfigurationPath.KeyDelimiter + key);
        }

        private void EnsureOrdered()
        {
            if (!_ordered)
            {
                ChildKeySorter.Sort(_keys, _count);
                _ordered = true;
            }
        }

        private struct Enumerator : IEnumerator<IConfigurationSection>
        {
            private readonly ChildSections _owner;
            private int _index = -1;

            internal Enumerator(ChildSections owner) => _owner = owner;

            public IConfigurationSection Current => _owner.Section(_owner._keys[_index]);

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index < _owner._count;

            public void Reset() => _index = -1;

            public void Dispose() => Reset();
        }
    }
}
