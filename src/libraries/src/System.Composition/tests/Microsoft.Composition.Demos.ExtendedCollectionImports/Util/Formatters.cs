// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Composition.Demos.ExtendedCollectionImports.Util
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

        public static string ReadableQuotedList(IEnumerable<string> items)
        {
            return ReadableList(items.Select(i => "'" + i + "'"));
        }

        public static string ReadableList(IEnumerable<string> items)
        {
            var itemArray = items.ToArray();
            if (itemArray.Length == 0)
                return "<none>";

            if (itemArray.Length == 1)
                return itemArray[0];

            var ordered = itemArray.OrderByDescending(t => t).ToArray();
            var commaSeparated = ordered.Skip(1).Reverse();
            var last = ordered.First();
            return string.Join(", ", commaSeparated) + " and " + last;
        }
    }
}
