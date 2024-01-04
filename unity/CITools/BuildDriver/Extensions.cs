// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace BuildDriver;

public static class Extensions
{
    public static string AggregateWithSpace(this IEnumerable<string> elements)
    {
        return elements.AggregateWithSpace(new StringBuilder());
    }

    public static string AggregateWithSpace(this IEnumerable<string> elements, StringBuilder builder)
    {
        return elements.AggregateWith(" ", builder);
    }

    public static string AggregateWith(this IEnumerable<string> elements, string separator)
        => elements.AggregateWith(separator, new StringBuilder());

    public static string AggregateWith(this IEnumerable<string> elements, string separator, StringBuilder builder)
    {
        bool addSep = false;
        foreach (var item in elements)
        {
            if (addSep)
                builder.Append(separator);
            builder.Append(item);
            addSep = true;
        }

        return builder.ToString();
    }
}
