// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.ComponentModel
{
    /// <summary>
    /// Hashtable that maps a <see cref="MemberInfo"/> object key to an associated value.
    /// <para>
    /// For keys where <see cref="MemberInfo.IsCollectible"/> is <c>false</c>, a standard <see cref="Hashtable"/> is used.
    /// For keys where <see cref="MemberInfo.IsCollectible"/> is <c>true</c>, a <see cref="ConditionalWeakTable{TKey, TValue}"/> is used.
    /// This ensures that collectible <see cref="MemberInfo"/> instances (such as those from collectible assemblies) do not prevent their assemblies from being unloaded.
    /// </para>
    /// </summary>
    internal sealed class CollectibleKeyHashtable
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
