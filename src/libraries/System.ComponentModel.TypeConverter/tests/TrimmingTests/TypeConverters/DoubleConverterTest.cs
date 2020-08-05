// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

/// <summary>
/// Tests that the relevant constructor on DoubleConverter is preserved when needed in a trimmed application.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        Type targetType = typeof(double);
        Type expectedConverterType = typeof(DoubleConverter);

        TypeConverter converter = TypeDescriptor.GetConverter(targetType);
        if (converter.GetType() != expectedConverterType)
        {
            return -1;
        }

        return converter.CanConvertTo(typeof(string)) ? 100 : -1;
    }
}
