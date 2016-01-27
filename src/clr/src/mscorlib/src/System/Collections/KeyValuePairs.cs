// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: KeyValuePairs to display items in collection class under debugger
**
**
===========================================================*/

namespace System.Collections {
    using System.Diagnostics;
    
    [DebuggerDisplay("{value}", Name = "[{key}]", Type = "" )]
    internal class KeyValuePairs {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object key;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object value;

        public KeyValuePairs(object key, object value) {
            this.value = value;
            this.key = key;
        }

        public object Key {
            get { return key; }
        }

        public object Value {
            get { return value; }
        }
    }    
}
