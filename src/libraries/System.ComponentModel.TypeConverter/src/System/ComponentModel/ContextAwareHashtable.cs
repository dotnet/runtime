// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.ComponentModel
{
    /// <summary>
    /// Hashtable that maps MemberInfo object key to an object and
    /// uses a WeakReference for the collectible keys (if MemberInfo.IsCollectible is true).
    /// Uses a Hashtable for non-collectible keys and WeakHashtable for the collectible keys.
    /// </summary>
    internal sealed class ContextAwareHashtable
    {
        private readonly Hashtable _defaultTable = new Hashtable();
        private readonly ConditionalWeakTable<object, object?> _collectibleTable = new ConditionalWeakTable<object, object?>();

        public object? this[MemberInfo key]
        {
            get
            {
                return !key.IsCollectible ? _defaultTable[key] : (_collectibleTable.TryGetValue(key, out object? value) ? value : null);
            }

            set
            {
                if (!key.IsCollectible)
                {
                    _defaultTable[key] = value;
                }
                else
                {
                    _collectibleTable.AddOrUpdate(key, value);
                }
            }
        }
    }
}
