// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Class:  KeyValuePairs
**
** Purpose: Defines key/value pairs for displaying items
**          in a collection class under the debugger.
**
===========================================================*/

#nullable enable
using System.Diagnostics;

namespace System.Collections
{
    [DebuggerDisplay("{_value}", Name = "[{_key}]")]
    internal class KeyValuePairs
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object _key;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object? _value;

        public KeyValuePairs(object key, object? value)
        {
            _value = value;
            _key = key;
        }
    }
}
