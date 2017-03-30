// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.AccountManagement
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using System.DirectoryServices;

    internal class ExtensionCacheValue
    {
        internal ExtensionCacheValue(object[] value)
        {
            _value = value;
            _filterOnly = false;
        }

        internal ExtensionCacheValue(object[] value, Type type, MatchType matchType)
        {
            _value = value;
            _type = type;
            _matchType = matchType;
            _filterOnly = true;
        }

        internal object[] Value
        {
            get { return _value; }
        }
        internal bool Filter
        {
            get { return _filterOnly; }
        }
        internal Type Type
        {
            get { return _type; }
        }
        internal MatchType MatchType
        {
            get { return _matchType; }
        }

        private object[] _value;
        private bool _filterOnly;
        private Type _type;
        private MatchType _matchType;
    }

    internal class ExtensionCache
    {
        private Dictionary<string, ExtensionCacheValue> _cache = new Dictionary<string, ExtensionCacheValue>();

        internal ExtensionCache() { }

        internal bool TryGetValue(string attr, out ExtensionCacheValue o)
        {
            return (_cache.TryGetValue(attr, out o));
        }

        internal Dictionary<string, ExtensionCacheValue> properties
        {
            get
            {
                return _cache;
            }
        }
    }
}
