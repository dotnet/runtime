// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Runtime.Caching
{
    // CoreClr 2.0 supports GC sized refs but does not expose System.SizedReference
#if NETCOREAPP21
    internal class SRef {
        private static Type s_type = Type.GetType("System.SizedReference", true, false);
        private Object _sizedRef;

        internal SRef(Object target) {
            _sizedRef = Activator.CreateInstance(s_type,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.CreateInstance,
                    null,
                    new object[] { target },
                    null);
        }

        internal long ApproximateSize {
            get {
                object o = s_type.InvokeMember("ApproximateSize",
                                               BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty,
                                               null, // binder
                                               _sizedRef, // target
                                               null, // args
                                               CultureInfo.InvariantCulture);
                return (long)o;
            }
        }

        internal void Dispose() {
            s_type.InvokeMember("Dispose",
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod,
                                null, // binder
                                _sizedRef, // target
                                null, // args
                                CultureInfo.InvariantCulture);
        }
    }

    internal class SRefMultiple {
        private SRef[] _srefs;
        private long[] _sizes;  // Getting SRef size in the debugger is extremely tedious so we keep the last read value here

        internal SRefMultiple(object[] targets) {
            _srefs = new SRef[targets.Length];
            _sizes = new long[targets.Length];
            for (int i = 0; i < targets.Length; i++) {
                _srefs[i] = new SRef(targets[i]);
            }
        }

        internal long ApproximateSize {
            get {
                long size = 0;
                for (int i = 0; i < _srefs.Length; i++) {
                    size += (_sizes[i] = _srefs[i].ApproximateSize);
                }
                return size;
            }
        }

        internal void Dispose() {
            foreach (SRef s in _srefs) {
                s.Dispose();
            }
        }
    }

#else
    // until then we provide a stub
    internal class SRefMultiple
    {
        internal SRefMultiple(object[] targets)
        {
        }
        internal long ApproximateSize => 0;
        internal void Dispose()
        {
        }
    }
#endif

    internal class GCHandleRef<T> : IDisposable
    where T : class, IDisposable
    {
        private GCHandle _handle;
        private T _t;

        public GCHandleRef(T t)
        {
            _handle = GCHandle.Alloc(t);
        }

        public T Target
        {
            get
            {
                try
                {
                    T t = (T)_handle.Target;
                    if (t != null)
                    {
                        return t;
                    }
                }
                catch (InvalidOperationException)
                {
                    // use the normal reference instead of throwing an exception when _handle is already freed
                }
                return _t;
            }
        }

        public void Dispose()
        {
            Target.Dispose();
            // Safe to call Dispose more than once but not thread-safe
            if (_handle.IsAllocated)
            {
                // We must free the GC handle to avoid leaks.
                // However after _handle is freed we no longer have access to its Target
                // which will cause AVs and various concurrency issues under stress.
                // We revert to using normal references after disposing the GC handle
                _t = (T)_handle.Target;
                _handle.Free();
            }
        }
    }
}
