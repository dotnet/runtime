// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Composition.Runtime.Util
{
    internal static class Formatters
    {
        public static string Format(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (value is string)
                return "\"" + value + "\"";

            return value.ToString();
        }

        public static string Format(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (type.IsConstructedGenericType)
                return FormatClosedGeneric(type);

            return type.Name;
        }

        private static string FormatClosedGeneric(Type closedGenericType)
        {
            Debug.Assert(closedGenericType != null);
            Debug.Assert(closedGenericType.IsConstructedGenericType);
            var name = closedGenericType.Name.Substring(0, closedGenericType.Name.IndexOf('`'));
            IEnumerable<string> args = closedGenericType.GenericTypeArguments.Select(t => Format(t));
            return $"{name}<{string.Join(SR.Formatter_ListSeparatorWithSpace, args)}>";
        }
    }
}
