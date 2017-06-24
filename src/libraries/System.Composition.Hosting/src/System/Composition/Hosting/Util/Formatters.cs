// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Internal;

namespace System.Composition.Hosting.Util
{
    internal static class Formatters
    {
        public static string ReadableList(IEnumerable<string> items)
        {
            Assumes.NotNull(items);

            string reply = string.Join(SR.Formatter_ListSeparatorWithSpace, items.OrderBy(t => t));
            return !string.IsNullOrEmpty(reply) ? reply : SR.Formatter_None;
        }

        public static string Format(Type type)
        {
            Assumes.NotNull(type);

            if (type.IsConstructedGenericType)
            {
                return FormatClosedGeneric(type);
            }
            return type.Name;
        }

        private static string FormatClosedGeneric(Type closedGenericType)
        {
            Assumes.NotNull(closedGenericType);
            Assumes.IsTrue(closedGenericType.IsConstructedGenericType);

            var name = closedGenericType.Name.Substring(0, closedGenericType.Name.IndexOf("`"));
            var args = closedGenericType.GenericTypeArguments.Select(t => Format(t));
            return string.Format("{0}<{1}>", name, string.Join(", ", args));
        }
    }
}
