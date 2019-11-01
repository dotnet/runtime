// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.Caching
{
    internal enum EntryState : byte
    {
        NotInCache = 0x00,  // Created but not in hashtable
        AddingToCache = 0x01,  // In hashtable only
        AddedToCache = 0x02,  // In hashtable + expires + usage
        RemovingFromCache = 0x04,  // Removed from hashtable only
        RemovedFromCache = 0x08,  // Removed from hashtable & expires & usage
        Closed = 0x10,
    }
}
