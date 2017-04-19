// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Data.Common
{
    internal partial class DbConnectionOptions
    {
        protected DbConnectionOptions(string connectionString, Dictionary<string, string> synonyms)
            : this (connectionString, new Hashtable(synonyms), false)
        {
        }

        internal bool TryGetParsetableValue(string key, out string value)
        {
            if (_parsetable.ContainsKey(key))
            {
                value = (string)_parsetable[key];
                return true;
            }
            value = null;
            return false;
        }
	}
}