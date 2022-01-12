// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

using static TestData;

internal unsafe class Test
{
    internal record DataContext<T, U>(T Invalid, T Unmappable, T Valid, Func<T, U> GetValueToCompare);

    internal readonly struct Functions<T>
    {
        public Functions(
            delegate*<T, bool> inByValue,
            delegate*<ref T, bool> inByRef,
            delegate*<ref T, bool> inOutByRef)
        {
            In = inByValue;
            InByRef = inByRef;
            InOutByRef = inOutByRef;
        }

        public readonly delegate*<T, bool> In;
        public readonly delegate*<ref T, bool> InByRef;
        public readonly delegate*<ref T, bool> InOutByRef;
    }

    public static void Validate<T, U>(bool bestFitMapping, bool throwOnUnmappableChar, Functions<T> funcs, DataContext<T, U> data)
    {
        Console.WriteLine($" ---- Marshal {typeof(T).Name}");

        bool shouldThrowOnInvalid = !bestFitMapping && throwOnUnmappableChar;

        T invalid = data.Invalid;
        if (shouldThrowOnInvalid)
        {
            Assert.Throws<ArgumentException>(() => funcs.In(invalid));

            invalid = data.Invalid;
            Assert.Throws<ArgumentException>(() => funcs.InByRef(ref invalid));

            invalid = data.Invalid;
            Assert.Throws<ArgumentException>(() => funcs.InOutByRef(ref invalid));
        }
        else
        {
            Assert.True(funcs.In(invalid));

            invalid = data.Invalid;
            Assert.True(funcs.InByRef(ref invalid));
            Assert.Equal(data.GetValueToCompare(data.Invalid), data.GetValueToCompare(invalid));

            invalid = data.Invalid;
            Assert.True(funcs.InOutByRef(ref invalid));
            Assert.NotEqual(data.GetValueToCompare(data.Invalid), data.GetValueToCompare(invalid));
        }

        T unmappable = data.Unmappable;
        if (throwOnUnmappableChar)
        {
            Assert.Throws<ArgumentException>(() => funcs.In(unmappable));

            unmappable = data.Unmappable;
            Assert.Throws<ArgumentException>(() => funcs.InByRef(ref unmappable));

            unmappable = data.Unmappable;
            Assert.Throws<ArgumentException>(() => funcs.InOutByRef(ref unmappable));
        }
        else
        {
            Assert.True(funcs.In(unmappable));

            unmappable = data.Unmappable;
            Assert.True(funcs.InByRef(ref unmappable));
            Assert.Equal(data.GetValueToCompare(data.Unmappable), data.GetValueToCompare(unmappable));

            unmappable = data.Unmappable;
            Assert.True(funcs.InOutByRef(ref unmappable));
            Assert.NotEqual(data.GetValueToCompare(data.Unmappable), data.GetValueToCompare(unmappable));
        }

        T valid = data.Valid;
        Assert.True(funcs.In(valid));

        valid = data.Valid;
        Assert.True(funcs.InByRef(ref valid));
        Assert.Equal(data.GetValueToCompare(data.Valid), data.GetValueToCompare(valid));

        valid = data.Valid;
        Assert.True(funcs.InOutByRef(ref valid));
        Assert.Equal(data.GetValueToCompare(data.Valid), data.GetValueToCompare(valid));
    }

    public static void ValidateChar(bool bestFitMapping, bool throwOnUnmappableChar, Functions<char> funcs)
    {
        var context = new DataContext<char, char>(InvalidChar, UnmappableChar, ValidChar, (char c) => c);
        Validate(bestFitMapping, throwOnUnmappableChar, funcs, context);
    }

    public static void ValidateString(bool bestFitMapping, bool throwOnUnmappableChar, Functions<string> funcs)
    {
        var context = new DataContext<string, string>(InvalidString, UnmappableString, ValidString, (string s) => s);
        Validate(bestFitMapping, throwOnUnmappableChar, funcs, context);
    }

    public static void ValidateStringBuilder(bool bestFitMapping, bool throwOnUnmappableChar, Functions<StringBuilder> funcs)
    {
        var context = new DataContext<StringBuilder, string>(InvalidStringBuilder, UnmappableStringBuilder, ValidStringBuilder, (StringBuilder s) => s.ToString());
        Validate(bestFitMapping, throwOnUnmappableChar, funcs, context);
    }

    public static void ValidateStringArray(bool bestFitMapping, bool throwOnUnmappableChar, Functions<string[]> funcs)
    {
        var context = new DataContext<string[], string>(InvalidStringArray, UnmappableStringArray, ValidStringArray, (string[] s) => s[0]);
        Validate(bestFitMapping, throwOnUnmappableChar, funcs, context);
    }
}
