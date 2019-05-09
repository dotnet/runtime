// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // Provides access to a System.Collections.Generic.KeyValuePair<K, V> via the IKeyValuePair<K, V> WinRT interface.
    internal sealed class CLRIKeyValuePairImpl<K, V> : IKeyValuePair<K, V>,
                                                       IGetProxyTarget
    {
        private readonly KeyValuePair<K, V> _pair;

        public CLRIKeyValuePairImpl([In] ref KeyValuePair<K, V> pair)
        {
            _pair = pair;
        }

        // IKeyValuePair<K, V> implementation
        public K Key
        {
            get { return _pair.Key; }
        }

        public V Value
        {
            get { return _pair.Value; }
        }

        // Called from the VM to wrap a boxed KeyValuePair with a CLRIKeyValuePairImpl.
        internal static object BoxHelper(object pair)
        {
            Debug.Assert(pair != null);

            KeyValuePair<K, V> unboxedPair = (KeyValuePair<K, V>)pair;
            return new CLRIKeyValuePairImpl<K, V>(ref unboxedPair);
        }

        // Called from the VM to get a boxed KeyValuePair out of a CLRIKeyValuePairImpl.
        internal static object UnboxHelper(object wrapper)
        {
            Debug.Assert(wrapper != null);

            CLRIKeyValuePairImpl<K, V> reference = (CLRIKeyValuePairImpl<K, V>)wrapper;
            return reference._pair;
        }

        public override string ToString()
        {
            return _pair.ToString();
        }

        object IGetProxyTarget.GetTarget()
        {
            return _pair;
        }
    }
}
