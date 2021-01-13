// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

using System.Reflection;

namespace System.Xml.Linq
{
    internal static class XHelper
    {
        internal static bool IsInstanceOfType(object? o, Type type)
        {
            Debug.Assert(type != null);

            return o != null && type.IsAssignableFrom(o.GetType());
        }
    }
}
