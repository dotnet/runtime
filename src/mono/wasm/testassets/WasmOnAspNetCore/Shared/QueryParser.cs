// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Specialized;

namespace Shared;

public static class QueryParser
{
    public static string GetValue(NameValueCollection parameters, string key)
    {
        var values = parameters.GetValues(key);
        if (values == null || values.Length == 0)
        {
            throw new Exception($"Parameter '{key}' is required in the query string");
        }
        if (values.Length > 1)
        {
            throw new Exception($"Parameter '{key}' should be unique in the query string");
        }
        return values[0];
    }
}
