// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
** Purpose: An implementation of a ArrayList.
**
**
=============================================================================*/

using System.Collections.Generic;
using System.Diagnostics;


namespace System.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    internal class ArrayList : List<object>
    {
        public ArrayList() { }
        public ArrayList(int capacity) : base(capacity) { }

        public virtual bool IsFixedSize
        {
            get { return false; }
        }

        // Is this ArrayList read-only?
        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        // Is this ArrayList synchronized (thread-safe)?
        public virtual bool IsSynchronized
        {
            get { return false; }
        }

        public Array ToArray(Type type)
        {
            return ArrayList.ToArray(this, type);
        }

        public static Array ToArray(IList list, Type type)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            if (type == null)
                throw new ArgumentNullException("type");
            Array array = Array.CreateInstance(type, list.Count);
            list.CopyTo(array, 0);
            return array;
        }
    }
}
