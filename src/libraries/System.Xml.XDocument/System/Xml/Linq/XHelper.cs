// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CultureInfo = System.Globalization.CultureInfo;
using Debug = System.Diagnostics.Debug;
using IEnumerable = System.Collections.IEnumerable;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
using Enumerable = System.Linq.Enumerable;
using IComparer = System.Collections.IComparer;
using IEqualityComparer = System.Collections.IEqualityComparer;
using StringBuilder = System.Text.StringBuilder;
using Encoding = System.Text.Encoding;
using Interlocked = System.Threading.Interlocked;
using System.Reflection;

namespace System.Xml.Linq
{
    static class XHelper
    {
        internal static string ToLower_InvariantCulture(string str)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToLower(str);
        }

        internal static bool IsInstanceOfType(object o, Type type)
        {
            Debug.Assert(type != null);

            if (o == null)
                return false;

            return type.GetTypeInfo().IsAssignableFrom(o.GetType().GetTypeInfo());
        }
    }
}
