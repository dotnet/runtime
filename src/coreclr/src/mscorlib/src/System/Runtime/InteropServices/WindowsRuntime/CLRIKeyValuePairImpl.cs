// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

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
        [Pure]
        public K Key
        {
            get { return _pair.Key; }
        }

        [Pure]
        public V Value
        {
            get { return _pair.Value; }
        }

        // Called from the VM to wrap a boxed KeyValuePair with a CLRIKeyValuePairImpl.
        internal static object BoxHelper(object pair)
        {
            Contract.Requires(pair != null);

            KeyValuePair<K, V> unboxedPair = (KeyValuePair<K, V>)pair;
            return new CLRIKeyValuePairImpl<K, V>(ref unboxedPair);
        }

        // Called from the VM to get a boxed KeyValuePair out of a CLRIKeyValuePairImpl.
        internal static object UnboxHelper(object wrapper)
        {
            Contract.Requires(wrapper != null);
            
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
