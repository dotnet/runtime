// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Security;

namespace System.Threading
{
    //
    // AsyncLocal<T> represents "ambient" data that is local to a given asynchronous control flow, such as an
    // async method.  For example, say you want to associate a culture with a given async flow:
    //
    // static AsyncLocal<Culture> s_currentCulture = new AsyncLocal<Culture>();
    //
    // static async Task SomeOperationAsync(Culture culture)
    // {
    //    s_currentCulture.Value = culture;
    //
    //    await FooAsync();
    // }
    //
    // static async Task FooAsync()
    // {
    //    PrintStringWithCulture(s_currentCulture.Value);
    // }
    //
    // AsyncLocal<T> also provides optional notifications when the value associated with the current thread
    // changes, either because it was explicitly changed by setting the Value property, or implicitly changed
    // when the thread encountered an "await" or other context transition.  For example, we might want our
    // current culture to be communicated to the OS as well:
    //
    // static AsyncLocal<Culture> s_currentCulture = new AsyncLocal<Culture>(
    //   args =>
    //   {
    //      NativeMethods.SetThreadCulture(args.CurrentValue.LCID);
    //   });
    //
    public sealed class AsyncLocal<T> : IAsyncLocal
    {
        [SecurityCritical] // critical because this action will terminate the process if it throws.
        private readonly Action<AsyncLocalValueChangedArgs<T>> m_valueChangedHandler;

        //
        // Constructs an AsyncLocal<T> that does not receive change notifications.
        //
        public AsyncLocal() 
        {
        }

        //
        // Constructs an AsyncLocal<T> with a delegate that is called whenever the current value changes
        // on any thread.
        //
        [SecurityCritical]
        public AsyncLocal(Action<AsyncLocalValueChangedArgs<T>> valueChangedHandler) 
        {
            m_valueChangedHandler = valueChangedHandler;
        }

        public T Value
        {
            [SecuritySafeCritical]
            get 
            { 
                object obj = ExecutionContext.GetLocalValue(this);
                return (obj == null) ? default(T) : (T)obj;
            }
            [SecuritySafeCritical]
            set 
            {
                ExecutionContext.SetLocalValue(this, value, m_valueChangedHandler != null); 
            }
        }

        [SecurityCritical]
        void IAsyncLocal.OnValueChanged(object previousValueObj, object currentValueObj, bool contextChanged)
        {
            Contract.Assert(m_valueChangedHandler != null);
            T previousValue = previousValueObj == null ? default(T) : (T)previousValueObj;
            T currentValue = currentValueObj == null ? default(T) : (T)currentValueObj;
            m_valueChangedHandler(new AsyncLocalValueChangedArgs<T>(previousValue, currentValue, contextChanged));
        }
    }

    //
    // Interface to allow non-generic code in ExecutionContext to call into the generic AsyncLocal<T> type.
    //
    internal interface IAsyncLocal
    {
        [SecurityCritical]
        void OnValueChanged(object previousValue, object currentValue, bool contextChanged);
    }

    public struct AsyncLocalValueChangedArgs<T>
    {
        public T PreviousValue { get; private set; }
        public T CurrentValue { get; private set; }
        
        //
        // If the value changed because we changed to a different ExecutionContext, this is true.  If it changed
        // because someone set the Value property, this is false.
        //
        public bool ThreadContextChanged { get; private set; }

        internal AsyncLocalValueChangedArgs(T previousValue, T currentValue, bool contextChanged)
            : this()
        {
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            ThreadContextChanged = contextChanged;
        }
    }

    //
    // Interface used to store an IAsyncLocal => object mapping in ExecutionContext.
    // Implementations are specialized based on the number of elements in the immutable
    // map in order to minimize memory consumption and look-up times.
    //
    interface IAsyncLocalValueMap
    {
        bool TryGetValue(IAsyncLocal key, out object value);
        IAsyncLocalValueMap Set(IAsyncLocal key, object value);
    }

    //
    // Utility functions for getting/creating instances of IAsyncLocalValueMap
    //
    internal static class AsyncLocalValueMap
    {
        public static IAsyncLocalValueMap Empty { get; } = new EmptyAsyncLocalValueMap();

        public static IAsyncLocalValueMap Create(IAsyncLocal key, object value) => new OneElementAsyncLocalValueMap(key, value);

        // Instance without any key/value pairs.  Used as a singleton/
        private sealed class EmptyAsyncLocalValueMap : IAsyncLocalValueMap
        {
            public IAsyncLocalValueMap Set(IAsyncLocal key, object value) => new OneElementAsyncLocalValueMap(key, value);

            public bool TryGetValue(IAsyncLocal key, out object value)
            {
                value = null;
                return false;
            }
        }

        // Instance with one key/value pair.
        private sealed class OneElementAsyncLocalValueMap : IAsyncLocalValueMap
        {
            private readonly IAsyncLocal _key1;
            private readonly object _value1;

            public OneElementAsyncLocalValueMap(IAsyncLocal key, object value)
            {
                _key1 = key; _value1 = value;
            }

            public IAsyncLocalValueMap Set(IAsyncLocal key, object value)
            {
                return ReferenceEquals(key, _key1) ?
                    new OneElementAsyncLocalValueMap(key, value) :
                    (IAsyncLocalValueMap)new TwoElementAsyncLocalValueMap(_key1, _value1, key, value);
            }

            public bool TryGetValue(IAsyncLocal key, out object value)
            {
                if (ReferenceEquals(key, _key1))
                {
                    value = _value1;
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
        }

        // Instance with two key/value pairs.
        private sealed class TwoElementAsyncLocalValueMap : IAsyncLocalValueMap
        {
            private readonly IAsyncLocal _key1, _key2;
            private readonly object _value1, _value2;

            public TwoElementAsyncLocalValueMap(IAsyncLocal key1, object value1, IAsyncLocal key2, object value2)
            {
                _key1 = key1; _value1 = value1;
                _key2 = key2; _value2 = value2;
            }

            public IAsyncLocalValueMap Set(IAsyncLocal key, object value)
            {
                return
                    ReferenceEquals(key, _key1) ? new TwoElementAsyncLocalValueMap(key, value, _key2, _value2) :
                    ReferenceEquals(key, _key2) ? new TwoElementAsyncLocalValueMap(_key1, _value1, key, value) :
                    (IAsyncLocalValueMap)new ThreeElementAsyncLocalValueMap(_key1, _value1, _key2, _value2, key, value);
            }

            public bool TryGetValue(IAsyncLocal key, out object value)
            {
                if (ReferenceEquals(key, _key1))
                {
                    value = _value1;
                    return true;
                }
                else if (ReferenceEquals(key, _key2))
                {
                    value = _value2;
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
        }

        // Instance with three key/value pairs.
        private sealed class ThreeElementAsyncLocalValueMap : IAsyncLocalValueMap
        {
            private readonly IAsyncLocal _key1, _key2, _key3;
            private readonly object _value1, _value2, _value3;

            public ThreeElementAsyncLocalValueMap(IAsyncLocal key1, object value1, IAsyncLocal key2, object value2, IAsyncLocal key3, object value3)
            {
                _key1 = key1; _value1 = value1;
                _key2 = key2; _value2 = value2;
                _key3 = key3; _value3 = value3;
            }

            public IAsyncLocalValueMap Set(IAsyncLocal key, object value)
            {
                if (ReferenceEquals(key, _key1)) return new ThreeElementAsyncLocalValueMap(key, value, _key2, _value2, _key3, _value3);
                if (ReferenceEquals(key, _key2)) return new ThreeElementAsyncLocalValueMap(_key1, _value1, key, value, _key3, _value3);
                if (ReferenceEquals(key, _key3)) return new ThreeElementAsyncLocalValueMap(_key1, _value1, _key2, _value2, key, value);

                var multi = new MultiElementAsyncLocalValueMap(4);
                multi.UnsafeStore(0, _key1, _value1);
                multi.UnsafeStore(1, _key2, _value2);
                multi.UnsafeStore(2, _key3, _value3);
                multi.UnsafeStore(3, key, value);
                return multi;
            }

            public bool TryGetValue(IAsyncLocal key, out object value)
            {
                if (ReferenceEquals(key, _key1))
                {
                    value = _value1;
                    return true;
                }
                else if (ReferenceEquals(key, _key2))
                {
                    value = _value2;
                    return true;
                }
                else if (ReferenceEquals(key, _key3))
                {
                    value = _value3;
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
        }

        // Instance with up to 16 key/value pairs.
        private sealed class MultiElementAsyncLocalValueMap : IAsyncLocalValueMap
        {
            private const int MaxMultiElements = 16;
            private readonly KeyValuePair<IAsyncLocal, object>[] _keyValues;

            internal MultiElementAsyncLocalValueMap(int count)
            {
                Debug.Assert(count <= MaxMultiElements);
                _keyValues = new KeyValuePair<IAsyncLocal, object>[count];
            }

            internal void UnsafeStore(int index, IAsyncLocal key, object value)
            {
                Debug.Assert(index < _keyValues.Length);
                _keyValues[index] = new KeyValuePair<IAsyncLocal, object>(key, value);
            }

            public IAsyncLocalValueMap Set(IAsyncLocal key, object value)
            {
                for (int i = 0; i < _keyValues.Length; i++)
                {
                    if (ReferenceEquals(key, _keyValues[i].Key))
                    {
                        var multi = new MultiElementAsyncLocalValueMap(_keyValues.Length);
                        Array.Copy(_keyValues, 0, multi._keyValues, 0, _keyValues.Length);
                        multi._keyValues[i] = new KeyValuePair<IAsyncLocal, object>(key, value);
                        return multi;
                    }
                }

                if (_keyValues.Length < MaxMultiElements)
                {
                    var multi = new MultiElementAsyncLocalValueMap(_keyValues.Length + 1);
                    Array.Copy(_keyValues, 0, multi._keyValues, 0, _keyValues.Length);
                    multi._keyValues[_keyValues.Length] = new KeyValuePair<IAsyncLocal, object>(key, value);
                    return multi;
                }

                var many = new ManyElementAsyncLocalValueMap(MaxMultiElements + 1);
                foreach (KeyValuePair<IAsyncLocal, object> pair in _keyValues)
                {
                    many[pair.Key] = pair.Value;
                }
                many[key] = value;
                return many;
            }

            public bool TryGetValue(IAsyncLocal key, out object value)
            {
                foreach (KeyValuePair<IAsyncLocal, object> pair in _keyValues)
                {
                    if (ReferenceEquals(key, pair.Key))
                    {
                        value = pair.Value;
                        return true;
                    }
                }
                value = null;
                return false;
            }
        }

        // Instance with any number of key/value pairs.
        private sealed class ManyElementAsyncLocalValueMap : Dictionary<IAsyncLocal, object>, IAsyncLocalValueMap
        {
            public ManyElementAsyncLocalValueMap(int capacity) : base(capacity) { }

            public IAsyncLocalValueMap Set(IAsyncLocal key, object value)
            {
                var map = new ManyElementAsyncLocalValueMap(Count);
                foreach (KeyValuePair<IAsyncLocal,object> pair in this)
                {
                    map[pair.Key] = pair.Value;
                }
                map[key] = value;
                return map;
            }
        }
    }
}
