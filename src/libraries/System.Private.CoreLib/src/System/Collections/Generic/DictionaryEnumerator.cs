using System;
using System.Collections.Generic;
using System.Text;

namespace System.Collections.Generic
{
    internal interface IKeyValue
    {
        public object? Key { get; }
        public object? Value { get; }
    }

    internal class DictionaryEnumerator : IDictionaryEnumerator
    {
        private readonly IEnumerator _enumerator;
        private IKeyValue? _current;

        public DictionaryEnumerator(IEnumerable enumerable)
        {
            _enumerator = enumerable.GetEnumerator();
            _current = null;
        }

        public object Key => _current?.Key!;

        public object? Value => _current?.Value;

        public DictionaryEntry Entry => new DictionaryEntry(_current?.Key!, _current?.Value);

        public object? Current => _current;

        public bool MoveNext()
        {
            bool result = _enumerator.MoveNext();
            _current = (IKeyValue)_enumerator.Current!;
            return result;
        }

        public void Reset() => _enumerator.Reset();
    }
}
