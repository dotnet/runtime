// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// The Map object holds key-value pairs and remembers the original insertion order of the keys.
    /// Any value (both objects and primitive values) may be used as either a key or a value.
    /// </summary>
    public class Map : CoreObject, IDictionary
    {
        /// <summary>
        /// Initializes a new instance of the Map class.
        /// </summary>
        public Map() : base(Runtime.New<Map>())
        { }

        /// <summary>
        /// Initializes a new instance of the Map class.
        /// </summary>
        /// <param name="jsHandle">Js handle.</param>
        /// <param name="ownsHandle">Whether or not the handle is owned by the clr or not.</param>
        internal Map(IntPtr jsHandle, bool ownsHandle) : base(jsHandle, ownsHandle)
        { }

        /// <summary>
        /// Gets a value indicating whether the Map object has a fixed size.
        /// </summary>
        public bool IsFixedSize => false;

        /// <summary>
        /// Gets a value indicating whether the Map object is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets an System.Collections.ICollection object containing the keys of the Map object.
        /// </summary>
        public ICollection Keys => new MapItemCollection(this, "keys");

        /// <summary>
        /// Gets an System.Collections.ICollection object containing the values of the Map object.
        /// </summary>
        public ICollection Values => new MapItemCollection(this, "values");

        public int Count => (int)GetObjectProperty("size");

        public bool IsSynchronized => false;

        public object SyncRoot => false;

        public void Add(object key, object? value) => Invoke("set", key, value);

        public void Clear() => Invoke("clear");

        public bool Contains(object key) => (bool)Invoke("has", key);

        public IDictionaryEnumerator GetEnumerator() => new MapEnumerator(this);

        public void Remove(object key) => Invoke("delete", key);

        public void CopyTo(System.Array array, int index) => throw new NotImplementedException();

        // Construct and return an enumerator.
        IEnumerator IEnumerable.GetEnumerator() => new MapEnumerator(this);

        /// <summary>
        /// Gets or sets the Map with the key specified by <paramref name="key" />.
        /// </summary>
        /// <param name="key">The key.</param>
        public object? this[object key]
        {
            get
            {
                return Invoke("get", key);
            }
            set
            {
                Invoke("set", key, value);
            }
        }

        private sealed class MapEnumerator : IDictionaryEnumerator, IDisposable
        {
            private JSObject? _mapIterator;
            private readonly Map _map;
            public MapEnumerator(Map map)
            {
                _map = map;
            }

            // Return the current item.
            public object Current => new DictionaryEntry(Key, Value);

            // Return the current dictionary entry.
            public DictionaryEntry Entry => (DictionaryEntry)Current;

            // Return the key of the current item.
            public object Key { get; private set; } = new object();

            // Return the value of the current item.
            public object? Value { get; private set; }

            // Advance to the next item.
            public bool MoveNext()
            {
                if (_mapIterator == null)
                    _mapIterator = (JSObject)_map.Invoke("entries");

                using (var result = (JSObject)_mapIterator.Invoke("next"))
                {
                    using (var resultValue = (Array)result.GetObjectProperty("value"))
                    {
                        if (resultValue != null)
                        {
                            Key = resultValue[0];
                            Value = resultValue[1];
                        }
                        else
                        {
                            Value = null;
                        }
                    }
                    return !(bool)result.GetObjectProperty("done");
                }
            }

            // Reset the index to restart the enumeration.
            public void Reset()
            {
                _mapIterator?.Dispose();
                _mapIterator = null;
            }

            #region IDisposable Support
            private bool _disposedValue; // To detect redundant calls

            private void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    _mapIterator?.Dispose();
                    _mapIterator = null;
                    _disposedValue = true;
                }
            }

            ~MapEnumerator()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion
        }

        /// <summary>
        /// Class that implements an ICollection over the "keys" or "values"
        /// </summary>
        private sealed class MapItemCollection : ICollection
        {
            private readonly Map _map;
            private readonly string _iterator;  // "keys" or "values"

            public MapItemCollection(Map map, string iterator)
            {
                _map = map;
                _iterator = iterator;

            }
            public int Count => _map.Count;

            public bool IsSynchronized => false;

            public object SyncRoot => this;

            public void CopyTo(System.Array array, int index) => throw new NotImplementedException();

            // Construct and return an enumerator.
            public IEnumerator GetEnumerator() => new MapItemEnumerator(this);

            /// <summary>
            /// The custom enumerator used by MapItemCollection
            /// </summary>
            private sealed class MapItemEnumerator : IEnumerator
            {

                private readonly MapItemCollection _mapItemCollection;
                private JSObject? _mapItemIterator;

                public object? Current { get; private set; }

                public MapItemEnumerator(MapItemCollection mapCollection)
                {
                    _mapItemCollection = mapCollection;
                }

                public bool MoveNext()
                {
                    if (_mapItemIterator == null)
                        _mapItemIterator = (JSObject)_mapItemCollection._map.Invoke(_mapItemCollection._iterator);

                    var done = false;
                    using (var result = (JSObject)_mapItemIterator.Invoke("next"))
                    {
                        done = (bool)result.GetObjectProperty("done");
                        if (!done)
                            Current = result.GetObjectProperty("value");
                        return !done;
                    }
                }

                public void Reset()
                {
                    _mapItemIterator?.Dispose();
                    _mapItemIterator = null;
                }

                #region IDisposable Support
                private bool _disposedValue; // To detect redundant calls

                private void Dispose(bool disposing)
                {
                    if (!_disposedValue)
                    {

                        _mapItemIterator?.Dispose();
                        _mapItemIterator = null;
                        _disposedValue = true;
                    }
                }

                ~MapItemEnumerator()
                {
                    // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                    Dispose(false);
                }

                // This code added to correctly implement the disposable pattern.
                public void Dispose()
                {
                    // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                    Dispose(true);
                    // TODO: uncomment the following line if the finalizer is overridden above.
                    GC.SuppressFinalize(this);
                }
                #endregion
            }
        }
    }
}
