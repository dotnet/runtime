// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Composition.Runtime.Util
{
    internal static class Formatters
    {
        public static string Format(object value) =>
            value is null ? throw new ArgumentNullException(nameof(value)) :
            value is string ? $"\"{value}\"" :
            value.ToString();

        public static string Format(Type type) =>
            type is null ? throw new ArgumentNullException(nameof(type)) :
            type.IsConstructedGenericType ? FormatClosedGeneric(type) :
            type.Name;

        private static string FormatClosedGeneric(Type closedGenericType)
        {
            Debug.Assert(closedGenericType != null);
            Debug.Assert(closedGenericType.IsConstructedGenericType);

            var name = closedGenericType.Name.Substring(0, closedGenericType.Name.IndexOf('`'));
            IEnumerable<string> args = closedGenericType.GenericTypeArguments.Select(Format);
            return $"{name}<{string.Join(SR.Formatter_ListSeparatorWithSpace, args)}>";
        }
    }
}
