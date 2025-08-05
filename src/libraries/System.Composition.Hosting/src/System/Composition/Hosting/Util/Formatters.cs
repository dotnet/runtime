// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Composition.Hosting.Util
{
    internal static class Formatters
    {
        public static string ReadableList(IEnumerable<string> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            string reply = string.Join(SR.Formatter_ListSeparatorWithSpace, items.OrderBy(t => t));
            return !string.IsNullOrEmpty(reply) ? reply : SR.Formatter_None;
        }

        public static string Format(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (type.IsConstructedGenericType)
            {
                return FormatClosedGeneric(type);
            }
            return type.Name;
        }

        private static string FormatClosedGeneric(Type closedGenericType)
        {
            ArgumentNullException.ThrowIfNull(closedGenericType);

            if (!closedGenericType.IsConstructedGenericType)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }

            var name = closedGenericType.Name.Substring(0, closedGenericType.Name.IndexOf('`'));
            var args = closedGenericType.GenericTypeArguments.Select(Format);
            return $"{name}<{string.Join(", ", args)}>";
        }
    }
}
