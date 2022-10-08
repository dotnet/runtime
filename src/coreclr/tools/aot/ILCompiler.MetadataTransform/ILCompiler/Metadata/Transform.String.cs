// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Metadata.NativeFormat.Writer;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        private Dictionary<string, ConstantStringValue> _strings = new Dictionary<string, ConstantStringValue>(StringComparer.Ordinal);

        private ConstantStringValue HandleString(string s)
        {
            if (s == null)
                return null;

            ConstantStringValue result;
            if (!_strings.TryGetValue(s, out result))
            {
                result = (ConstantStringValue)s;
                _strings.Add(s, result);
            }

            return result;
        }
    }
}
