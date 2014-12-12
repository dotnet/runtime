// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Debug = System.Diagnostics.Debug;

using System.Reflection;

namespace System.Xml.Linq
{
    static class XHelper
    {
        internal static bool IsInstanceOfType(object o, Type type)
        {
            Debug.Assert(type != null);

            if (o == null)
                return false;

            return type.GetTypeInfo().IsAssignableFrom(o.GetType().GetTypeInfo());
        }
    }
}
