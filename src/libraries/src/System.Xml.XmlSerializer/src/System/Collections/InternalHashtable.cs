// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
** Purpose: An implementation of a Hashtable.
**
**
=============================================================================*/

using System.Collections.Generic;
using System.Diagnostics;


namespace System.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    internal class InternalHashtable : Dictionary<object, object>
    {
        public InternalHashtable()
        {
        }

        public InternalHashtable(IDictionary<object, object> dictionary)
            : base(dictionary)
        {
        }

        public new object this[object key]
        {
            get
            {
                object value;
                return base.TryGetValue(key, out value) ? value : null;
            }
            set
            {
                base[key] = value;
            }
        }

        public InternalHashtable Clone()
        {
            return new InternalHashtable(this);
        }
    }
}
