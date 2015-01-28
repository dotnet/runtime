// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Class:  KeyValuePairs
**
** Purpose: Defines key/value pairs for displaying items
**          in a collection class under the debugger.
**
===========================================================*/

using System.Diagnostics;

namespace System.Collections
{
    [DebuggerDisplay("{_value}", Name = "[{_key}]", Type = "")]
    internal class KeyValuePairs
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object _key;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object _value;

        public KeyValuePairs(object key, object value)
        {
            _value = value;
            _key = key;
        }

        public object Key
        {
            get { return _key; }
        }

        public object Value
        {
            get { return _value; }
        }
    }
}
