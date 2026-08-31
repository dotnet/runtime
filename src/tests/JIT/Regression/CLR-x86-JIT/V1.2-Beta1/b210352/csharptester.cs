// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Xunit;

public class M
{
    internal static void GenericClassStaticMethod()
    {
        GenericClass<int>.classParameterType = typeof(int);
        GenericClass<string>.classParameterType = typeof(string);
        GenericClass<int[]>.classParameterType = typeof(int[]);
        GenericClass<string[]>.classParameterType = typeof(string[]);

        if (GenericClass<int>.StaticGenericMethod<int>(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticGenericMethod<int>(1, typeof(int)) to be 1, but found '" + GenericClass<int>.StaticGenericMethod<int>(1, typeof(int)) + "'");
        }

        if (GenericClass<int>.StaticGenericMethod<string>("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticGenericMethod<string>(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<int>.StaticGenericMethod<string>("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<int>.StaticGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticGenericMethod<int[]>(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<int>.StaticGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<int>.StaticGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<int>.StaticGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<string>.StaticGenericMethod<int>(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticGenericMethod<int>(1, typeof(int)) to be 1, but found '" + GenericClass<string>.StaticGenericMethod<int>(1, typeof(int)) + "'");
        }

        if (GenericClass<string>.StaticGenericMethod<string>("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticGenericMethod<string>(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<string>.StaticGenericMethod<string>("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<string>.StaticGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticGenericMethod<int[]>(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<string>.StaticGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<string>.StaticGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<string>.StaticGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<int[]>.StaticGenericMethod<int>(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticGenericMethod<int>(1, typeof(int)) to be 1, but found '" + GenericClass<int[]>.StaticGenericMethod<int>(1, typeof(int)) + "'");
        }

        if (GenericClass<int[]>.StaticGenericMethod<string>("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticGenericMethod<string>(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<int[]>.StaticGenericMethod<string>("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<int[]>.StaticGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticGenericMethod<int[]>(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<int[]>.StaticGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<int[]>.StaticGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<int[]>.StaticGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<string[]>.StaticGenericMethod<int>(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticGenericMethod<int>(1, typeof(int)) to be 1, but found '" + GenericClass<string[]>.StaticGenericMethod<int>(1, typeof(int)) + "'");
        }

        if (GenericClass<string[]>.StaticGenericMethod<string>("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticGenericMethod<string>(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<string[]>.StaticGenericMethod<string>("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<string[]>.StaticGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticGenericMethod<int[]>(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<string[]>.StaticGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<string[]>.StaticGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<string[]>.StaticGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<int>.StaticNonGenericMethodInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticNonGenericMethodInt(1, typeof(int)) to be 1, but found '" + GenericClass<int>.StaticNonGenericMethodInt(1, typeof(int)) + "'");
        }

        if (GenericClass<int>.StaticNonGenericMethodString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticNonGenericMethodString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<int>.StaticNonGenericMethodString("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<int>.StaticNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<int>.StaticNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<int>.StaticNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<int>.StaticNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<string>.StaticNonGenericMethodInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticNonGenericMethodInt(1, typeof(int)) to be 1, but found '" + GenericClass<string>.StaticNonGenericMethodInt(1, typeof(int)) + "'");
        }

        if (GenericClass<string>.StaticNonGenericMethodString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticNonGenericMethodString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<string>.StaticNonGenericMethodString("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<string>.StaticNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<string>.StaticNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<string>.StaticNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<string>.StaticNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<int[]>.StaticNonGenericMethodInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticNonGenericMethodInt(1, typeof(int)) to be 1, but found '" + GenericClass<int[]>.StaticNonGenericMethodInt(1, typeof(int)) + "'");
        }

        if (GenericClass<int[]>.StaticNonGenericMethodString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticNonGenericMethodString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<int[]>.StaticNonGenericMethodString("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<int[]>.StaticNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<int[]>.StaticNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<int[]>.StaticNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<int[]>.StaticNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<string[]>.StaticNonGenericMethodInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticNonGenericMethodInt(1, typeof(int)) to be 1, but found '" + GenericClass<string[]>.StaticNonGenericMethodInt(1, typeof(int)) + "'");
        }

        if (GenericClass<string[]>.StaticNonGenericMethodString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticNonGenericMethodString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<string[]>.StaticNonGenericMethodString("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<string[]>.StaticNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<string[]>.StaticNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<string[]>.StaticNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<string[]>.StaticNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<int>.StaticGenericMethodUsesClassTypeParam<int>(27, 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticGenericMethodUsesClassTypeParam<int>(27, 1, typeof(int)) to be 1, but found '" + GenericClass<int>.StaticGenericMethodUsesClassTypeParam<int>(27, 1, typeof(int)) + "'");
        }

        if (GenericClass<int>.StaticGenericMethodUsesClassTypeParam<string>(27, "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticGenericMethodUsesClassTypeParam<string>(27, \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<int>.StaticGenericMethodUsesClassTypeParam<string>(27, "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<int>.StaticGenericMethodUsesClassTypeParam<int[]>(27, new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticGenericMethodUsesClassTypeParam<int[]>(27, new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<int>.StaticGenericMethodUsesClassTypeParam<int[]>(27, new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<int>.StaticGenericMethodUsesClassTypeParam<string[]>(27, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticGenericMethodUsesClassTypeParam<string[]>(27, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<int>.StaticGenericMethodUsesClassTypeParam<string[]>(27, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<string>.StaticGenericMethodUsesClassTypeParam<int>("", 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticGenericMethodUsesClassTypeParam<int>(\"\", 1, typeof(int)) to be 1, but found '" + GenericClass<string>.StaticGenericMethodUsesClassTypeParam<int>("", 1, typeof(int)) + "'");
        }

        if (GenericClass<string>.StaticGenericMethodUsesClassTypeParam<string>("", "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticGenericMethodUsesClassTypeParam<string>(\"\", \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<string>.StaticGenericMethodUsesClassTypeParam<string>("", "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<string>.StaticGenericMethodUsesClassTypeParam<int[]>("", new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticGenericMethodUsesClassTypeParam<int[]>(\"\", new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<string>.StaticGenericMethodUsesClassTypeParam<int[]>("", new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<string>.StaticGenericMethodUsesClassTypeParam<string[]>("", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticGenericMethodUsesClassTypeParam<string[]>(\"\", new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<string>.StaticGenericMethodUsesClassTypeParam<string[]>("", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<int[]>.StaticGenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticGenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int)) to be 1, but found '" + GenericClass<int[]>.StaticGenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int)) + "'");
        }

        if (GenericClass<int[]>.StaticGenericMethodUsesClassTypeParam<string>(new int[0], "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticGenericMethodUsesClassTypeParam<string>(new int[0], \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<int[]>.StaticGenericMethodUsesClassTypeParam<string>(new int[0], "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<int[]>.StaticGenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticGenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<int[]>.StaticGenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<int[]>.StaticGenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticGenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<int[]>.StaticGenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<string[]>.StaticGenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticGenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int)) to be 1, but found '" + GenericClass<string[]>.StaticGenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int)) + "'");
        }

        if (GenericClass<string[]>.StaticGenericMethodUsesClassTypeParam<string>(new string[0], "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticGenericMethodUsesClassTypeParam<string>(new string[0], \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<string[]>.StaticGenericMethodUsesClassTypeParam<string>(new string[0], "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<string[]>.StaticGenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticGenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<string[]>.StaticGenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<string[]>.StaticGenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticGenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<string[]>.StaticGenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<int>.StaticNonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticNonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int)) to be 1, but found '" + GenericClass<int>.StaticNonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int)) + "'");
        }

        if (GenericClass<int>.StaticNonGenericMethodStringUsesClassTypeParam(Int32.MaxValue, "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticNonGenericMethodString(Int32.MaxValue, \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<int>.StaticNonGenericMethodStringUsesClassTypeParam(Int32.MaxValue, "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<int>.StaticNonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticNonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<int>.StaticNonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<int>.StaticNonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<int>.StaticNonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<int>.StaticNonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<string>.StaticNonGenericMethodIntUsesClassTypeParam("wxyzabcdefgh", 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticNonGenericMethodIntUsesClassTypeParam(\"wxyzabcdefgh\", 1, typeof(int)) to be 1, but found '" + GenericClass<string>.StaticNonGenericMethodIntUsesClassTypeParam("wxyzabcdefgh", 1, typeof(int)) + "'");
        }

        if (GenericClass<string>.StaticNonGenericMethodStringUsesClassTypeParam("wxyzabcdefgh", "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticNonGenericMethodString(\"wxyzabcdefgh\", \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<string>.StaticNonGenericMethodStringUsesClassTypeParam("wxyzabcdefgh", "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<string>.StaticNonGenericMethodIntArrayUsesClassTypeParam("wxyzabcdefgh", new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticNonGenericMethodIntArrayUsesClassTypeParam(\"wxyzabcdefgh\", new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<string>.StaticNonGenericMethodIntArrayUsesClassTypeParam("wxyzabcdefgh", new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<string>.StaticNonGenericMethodStringArrayUsesClassTypeParam("wxyzabcdefgh", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<string>.StaticNonGenericMethodStringArrayUsesClassTypeParam(\"wxyzabcdefgh\", new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<string>.StaticNonGenericMethodStringArrayUsesClassTypeParam("wxyzabcdefgh", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<int[]>.StaticNonGenericMethodIntUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticNonGenericMethodIntUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, 1, typeof(int)) to be 1, but found '" + GenericClass<int[]>.StaticNonGenericMethodIntUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, 1, typeof(int)) + "'");
        }

        if (GenericClass<int[]>.StaticNonGenericMethodStringUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticNonGenericMethodString(new int[] {Int32.MaxValue, Int32.MinValue}, \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<int[]>.StaticNonGenericMethodStringUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<int[]>.StaticNonGenericMethodIntArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticNonGenericMethodIntArrayUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<int[]>.StaticNonGenericMethodIntArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<int[]>.StaticNonGenericMethodStringArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<int[]>.StaticNonGenericMethodStringArrayUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<int[]>.StaticNonGenericMethodStringArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClass<string[]>.StaticNonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticNonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int)) to be 1, but found '" + GenericClass<string[]>.StaticNonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int)) + "'");
        }

        if (GenericClass<string[]>.StaticNonGenericMethodStringUsesClassTypeParam(new string[1000], "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticNonGenericMethodString(new string[1000], \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClass<string[]>.StaticNonGenericMethodStringUsesClassTypeParam(new string[1000], "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClass<string[]>.StaticNonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticNonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClass<string[]>.StaticNonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClass<string[]>.StaticNonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClass<string[]>.StaticNonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClass<string[]>.StaticNonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }
    }

    internal static void GenericClassInstanceMethod()
    {
        GenericClass<int>.classParameterType = typeof(int);
        GenericClass<string>.classParameterType = typeof(string);
        GenericClass<int[]>.classParameterType = typeof(int[]);
        GenericClass<string[]>.classParameterType = typeof(string[]);

        GenericClass<int> GenericClassInt = new GenericClass<int>();
        GenericClass<string> GenericClassString = new GenericClass<string>();
        GenericClass<int[]> GenericClassIntArray = new GenericClass<int[]>();
        GenericClass<string[]> GenericClassStringArray = new GenericClass<string[]>();

        if (GenericClassInt.GenericMethod<int>(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethod<int>(1, typeof(int)) to be 1, but found '" + GenericClassInt.GenericMethod<int>(1, typeof(int)) + "'");
        }

        if (GenericClassInt.GenericMethod<string>("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethod<string>(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassInt.GenericMethod<string>("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethod<int[]>(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInt.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInt.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassString.GenericMethod<int>(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethod<int>(1, typeof(int)) to be 1, but found '" + GenericClassString.GenericMethod<int>(1, typeof(int)) + "'");
        }

        if (GenericClassString.GenericMethod<string>("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethod<string>(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassString.GenericMethod<string>("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethod<int[]>(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassString.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassString.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassIntArray.GenericMethod<int>(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethod<int>(1, typeof(int)) to be 1, but found '" + GenericClassIntArray.GenericMethod<int>(1, typeof(int)) + "'");
        }

        if (GenericClassIntArray.GenericMethod<string>("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethod<string>(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassIntArray.GenericMethod<string>("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethod<int[]>(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassStringArray.GenericMethod<int>(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethod<int>(1, typeof(int)) to be 1, but found '" + GenericClassStringArray.GenericMethod<int>(1, typeof(int)) + "'");
        }

        if (GenericClassStringArray.GenericMethod<string>("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethod<string>(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassStringArray.GenericMethod<string>("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethod<int[]>(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassInt.NonGenericMethodInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodInt(1, typeof(int)) to be 1, but found '" + GenericClassInt.NonGenericMethodInt(1, typeof(int)) + "'");
        }

        if (GenericClassInt.NonGenericMethodString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassInt.NonGenericMethodString("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodIntArray(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInt.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInt.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassString.NonGenericMethodInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodInt(1, typeof(int)) to be 1, but found '" + GenericClassString.NonGenericMethodInt(1, typeof(int)) + "'");
        }

        if (GenericClassString.NonGenericMethodString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassString.NonGenericMethodString("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodIntArray(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassString.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassString.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassIntArray.NonGenericMethodInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodInt(1, typeof(int)) to be 1, but found '" + GenericClassIntArray.NonGenericMethodInt(1, typeof(int)) + "'");
        }

        if (GenericClassIntArray.NonGenericMethodString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassIntArray.NonGenericMethodString("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodIntArray(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassStringArray.NonGenericMethodInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodInt(1, typeof(int)) to be 1, but found '" + GenericClassStringArray.NonGenericMethodInt(1, typeof(int)) + "'");
        }

        if (GenericClassStringArray.NonGenericMethodString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassStringArray.NonGenericMethodString("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodIntArray(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassInt.GenericMethodUsesClassTypeParam<int>(27, 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethodUsesClassTypeParam<int>(27, 1, typeof(int)) to be 1, but found '" + GenericClassInt.GenericMethodUsesClassTypeParam<int>(27, 1, typeof(int)) + "'");
        }

        if (GenericClassInt.GenericMethodUsesClassTypeParam<string>(27, "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethodUsesClassTypeParam<string>(27, \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassInt.GenericMethodUsesClassTypeParam<string>(27, "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.GenericMethodUsesClassTypeParam<int[]>(27, new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethodUsesClassTypeParam<int[]>(27, new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInt.GenericMethodUsesClassTypeParam<int[]>(27, new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.GenericMethodUsesClassTypeParam<string[]>(27, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethodUsesClassTypeParam<string[]>(27, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInt.GenericMethodUsesClassTypeParam<string[]>(27, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassString.GenericMethodUsesClassTypeParam<int>("", 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethodUsesClassTypeParam<int>(\"\", 1, typeof(int)) to be 1, but found '" + GenericClassString.GenericMethodUsesClassTypeParam<int>("", 1, typeof(int)) + "'");
        }

        if (GenericClassString.GenericMethodUsesClassTypeParam<string>("", "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethodUsesClassTypeParam<string>(\"\", \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassString.GenericMethodUsesClassTypeParam<string>("", "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.GenericMethodUsesClassTypeParam<int[]>("", new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethodUsesClassTypeParam<int[]>(\"\", new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassString.GenericMethodUsesClassTypeParam<int[]>("", new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.GenericMethodUsesClassTypeParam<string[]>("", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethodUsesClassTypeParam<string[]>(\"\", new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassString.GenericMethodUsesClassTypeParam<string[]>("", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassIntArray.GenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int)) to be 1, but found '" + GenericClassIntArray.GenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int)) + "'");
        }

        if (GenericClassIntArray.GenericMethodUsesClassTypeParam<string>(new int[0], "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethodUsesClassTypeParam<string>(new int[0], \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassIntArray.GenericMethodUsesClassTypeParam<string>(new int[0], "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.GenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.GenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.GenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.GenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassStringArray.GenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int)) to be 1, but found '" + GenericClassStringArray.GenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int)) + "'");
        }

        if (GenericClassStringArray.GenericMethodUsesClassTypeParam<string>(new string[0], "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethodUsesClassTypeParam<string>(new string[0], \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassStringArray.GenericMethodUsesClassTypeParam<string>(new string[0], "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.GenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.GenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.GenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.GenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassInt.NonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int)) to be 1, but found '" + GenericClassInt.NonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int)) + "'");
        }

        if (GenericClassInt.NonGenericMethodStringUsesClassTypeParam(Int32.MaxValue, "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodString(Int32.MaxValue, \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassInt.NonGenericMethodStringUsesClassTypeParam(Int32.MaxValue, "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.NonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInt.NonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.NonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInt.NonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassString.NonGenericMethodIntUsesClassTypeParam("wxyzabcdefgh", 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodIntUsesClassTypeParam(\"wxyzabcdefgh\", 1, typeof(int)) to be 1, but found '" + GenericClassString.NonGenericMethodIntUsesClassTypeParam("wxyzabcdefgh", 1, typeof(int)) + "'");
        }

        if (GenericClassString.NonGenericMethodStringUsesClassTypeParam("wxyzabcdefgh", "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodString(\"wxyzabcdefgh\", \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassString.NonGenericMethodStringUsesClassTypeParam("wxyzabcdefgh", "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.NonGenericMethodIntArrayUsesClassTypeParam("wxyzabcdefgh", new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodIntArrayUsesClassTypeParam(\"wxyzabcdefgh\", new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassString.NonGenericMethodIntArrayUsesClassTypeParam("wxyzabcdefgh", new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.NonGenericMethodStringArrayUsesClassTypeParam("wxyzabcdefgh", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodStringArrayUsesClassTypeParam(\"wxyzabcdefgh\", new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassString.NonGenericMethodStringArrayUsesClassTypeParam("wxyzabcdefgh", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassIntArray.NonGenericMethodIntUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodIntUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, 1, typeof(int)) to be 1, but found '" + GenericClassIntArray.NonGenericMethodIntUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, 1, typeof(int)) + "'");
        }

        if (GenericClassIntArray.NonGenericMethodStringUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodString(new int[] {Int32.MaxValue, Int32.MinValue}, \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassIntArray.NonGenericMethodStringUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.NonGenericMethodIntArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodIntArrayUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.NonGenericMethodIntArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.NonGenericMethodStringArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodStringArrayUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.NonGenericMethodStringArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassStringArray.NonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int)) to be 1, but found '" + GenericClassStringArray.NonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int)) + "'");
        }

        if (GenericClassStringArray.NonGenericMethodStringUsesClassTypeParam(new string[1000], "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodString(new string[1000], \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassStringArray.NonGenericMethodStringUsesClassTypeParam(new string[1000], "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.NonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.NonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.NonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.NonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }
    }

    internal static void GenericClassVirtualMethod()
    {
        GenericClass<int>.classParameterType = typeof(int);
        GenericClass<string>.classParameterType = typeof(string);
        GenericClass<int[]>.classParameterType = typeof(int[]);
        GenericClass<string[]>.classParameterType = typeof(string[]);

        GenericClass<int> GenericClassInt = new GenericClass<int>();
        GenericClass<string> GenericClassString = new GenericClass<string>();
        GenericClass<int[]> GenericClassIntArray = new GenericClass<int[]>();
        GenericClass<string[]> GenericClassStringArray = new GenericClass<string[]>();

        GenericClassInheritsFromGenericClass<int> GenericClassInheritsFromGenericClassInt = new GenericClassInheritsFromGenericClass<int>();
        GenericClassInheritsFromGenericClass<string> GenericClassInheritsFromGenericClassString = new GenericClassInheritsFromGenericClass<string>();
        GenericClassInheritsFromGenericClass<int[]> GenericClassInheritsFromGenericClassIntArray = new GenericClassInheritsFromGenericClass<int[]>();
        GenericClassInheritsFromGenericClass<string[]> GenericClassInheritsFromGenericClassStringArray = new GenericClassInheritsFromGenericClass<string[]>();

        GenericClass<int> GenericClassInheritsFromGenericClassCastAsGenericClassInt = new GenericClassInheritsFromGenericClass<int>();
        GenericClass<string> GenericClassInheritsFromGenericClassCastAsGenericClassString = new GenericClassInheritsFromGenericClass<string>();
        GenericClass<int[]> GenericClassInheritsFromGenericClassCastAsGenericClassIntArray = new GenericClassInheritsFromGenericClass<int[]>();
        GenericClass<string[]> GenericClassInheritsFromGenericClassCastAsGenericClassStringArray = new GenericClassInheritsFromGenericClass<string[]>();

        if (GenericClassInt.VirtualGenericMethod<int>(1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualGenericMethod<int>(1, typeof(int, true)) to be 1, but found '" + GenericClassInt.VirtualGenericMethod<int>(1, typeof(int), true) + "'");
        }

        if (GenericClassInt.VirtualGenericMethod<string>("aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualGenericMethod<string>(\"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassInt.VirtualGenericMethod<string>("aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualGenericMethod<int[]>(new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInt.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInt.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassString.VirtualGenericMethod<int>(1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualGenericMethod<int>(1, typeof(int, true)) to be 1, but found '" + GenericClassString.VirtualGenericMethod<int>(1, typeof(int), true) + "'");
        }

        if (GenericClassString.VirtualGenericMethod<string>("aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualGenericMethod<string>(\"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassString.VirtualGenericMethod<string>("aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualGenericMethod<int[]>(new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassString.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassString.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassIntArray.VirtualGenericMethod<int>(1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualGenericMethod<int>(1, typeof(int, true)) to be 1, but found '" + GenericClassIntArray.VirtualGenericMethod<int>(1, typeof(int), true) + "'");
        }

        if (GenericClassIntArray.VirtualGenericMethod<string>("aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualGenericMethod<string>(\"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassIntArray.VirtualGenericMethod<string>("aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualGenericMethod<int[]>(new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassStringArray.VirtualGenericMethod<int>(1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualGenericMethod<int>(1, typeof(int, true)) to be 1, but found '" + GenericClassStringArray.VirtualGenericMethod<int>(1, typeof(int), true) + "'");
        }

        if (GenericClassStringArray.VirtualGenericMethod<string>("aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualGenericMethod<string>(\"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassStringArray.VirtualGenericMethod<string>("aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualGenericMethod<int[]>(new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassInt.VirtualNonGenericMethodInt(1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualNonGenericMethodInt(1, typeof(int, true)) to be 1, but found '" + GenericClassInt.VirtualNonGenericMethodInt(1, typeof(int), true) + "'");
        }

        if (GenericClassInt.VirtualNonGenericMethodString("aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualNonGenericMethodString(\"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassInt.VirtualNonGenericMethodString("aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInt.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInt.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassString.VirtualNonGenericMethodInt(1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualNonGenericMethodInt(1, typeof(int, true)) to be 1, but found '" + GenericClassString.VirtualNonGenericMethodInt(1, typeof(int), true) + "'");
        }

        if (GenericClassString.VirtualNonGenericMethodString("aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualNonGenericMethodString(\"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassString.VirtualNonGenericMethodString("aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassString.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassString.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassIntArray.VirtualNonGenericMethodInt(1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualNonGenericMethodInt(1, typeof(int, true)) to be 1, but found '" + GenericClassIntArray.VirtualNonGenericMethodInt(1, typeof(int), true) + "'");
        }

        if (GenericClassIntArray.VirtualNonGenericMethodString("aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualNonGenericMethodString(\"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassIntArray.VirtualNonGenericMethodString("aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassStringArray.VirtualNonGenericMethodInt(1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualNonGenericMethodInt(1, typeof(int, true)) to be 1, but found '" + GenericClassStringArray.VirtualNonGenericMethodInt(1, typeof(int), true) + "'");
        }

        if (GenericClassStringArray.VirtualNonGenericMethodString("aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualNonGenericMethodString(\"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassStringArray.VirtualNonGenericMethodString("aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassInt.VirtualGenericMethodUsesClassTypeParam<int>(27, 1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualGenericMethodUsesClassTypeParam<int>(27, 1, typeof(int, true)) to be 1, but found '" + GenericClassInt.VirtualGenericMethodUsesClassTypeParam<int>(27, 1, typeof(int), true) + "'");
        }

        if (GenericClassInt.VirtualGenericMethodUsesClassTypeParam<string>(27, "aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualGenericMethodUsesClassTypeParam<string>(27, \"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassInt.VirtualGenericMethodUsesClassTypeParam<string>(27, "aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.VirtualGenericMethodUsesClassTypeParam<int[]>(27, new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualGenericMethodUsesClassTypeParam<int[]>(27, new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInt.VirtualGenericMethodUsesClassTypeParam<int[]>(27, new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.VirtualGenericMethodUsesClassTypeParam<string[]>(27, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualGenericMethodUsesClassTypeParam<string[]>(27, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInt.VirtualGenericMethodUsesClassTypeParam<string[]>(27, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassString.VirtualGenericMethodUsesClassTypeParam<int>("", 1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualGenericMethodUsesClassTypeParam<int>(\"\", 1, typeof(int, true)) to be 1, but found '" + GenericClassString.VirtualGenericMethodUsesClassTypeParam<int>("", 1, typeof(int), true) + "'");
        }

        if (GenericClassString.VirtualGenericMethodUsesClassTypeParam<string>("", "aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualGenericMethodUsesClassTypeParam<string>(\"\", \"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassString.VirtualGenericMethodUsesClassTypeParam<string>("", "aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.VirtualGenericMethodUsesClassTypeParam<int[]>("", new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualGenericMethodUsesClassTypeParam<int[]>(\"\", new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassString.VirtualGenericMethodUsesClassTypeParam<int[]>("", new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.VirtualGenericMethodUsesClassTypeParam<string[]>("", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualGenericMethodUsesClassTypeParam<string[]>(\"\", new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassString.VirtualGenericMethodUsesClassTypeParam<string[]>("", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int, true)) to be 1, but found '" + GenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int), true) + "'");
        }

        if (GenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string>(new int[0], "aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string>(new int[0], \"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string>(new int[0], "aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int, true)) to be 1, but found '" + GenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int), true) + "'");
        }

        if (GenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string>(new string[0], "aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string>(new string[0], \"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string>(new string[0], "aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassInt.VirtualNonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualNonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int, true)) to be 1, but found '" + GenericClassInt.VirtualNonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int), true) + "'");
        }

        if (GenericClassInt.VirtualNonGenericMethodStringUsesClassTypeParam(Int32.MaxValue, "aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualNonGenericMethodString(Int32.MaxValue, \"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassInt.VirtualNonGenericMethodStringUsesClassTypeParam(Int32.MaxValue, "aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.VirtualNonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualNonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInt.VirtualNonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.VirtualNonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.VirtualNonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInt.VirtualNonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassString.VirtualNonGenericMethodIntUsesClassTypeParam("wxyzabcdefgh", 1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualNonGenericMethodIntUsesClassTypeParam(\"wxyzabcdefgh\", 1, typeof(int, true)) to be 1, but found '" + GenericClassString.VirtualNonGenericMethodIntUsesClassTypeParam("wxyzabcdefgh", 1, typeof(int), true) + "'");
        }

        if (GenericClassString.VirtualNonGenericMethodStringUsesClassTypeParam("wxyzabcdefgh", "aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualNonGenericMethodString(\"wxyzabcdefgh\", \"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassString.VirtualNonGenericMethodStringUsesClassTypeParam("wxyzabcdefgh", "aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.VirtualNonGenericMethodIntArrayUsesClassTypeParam("wxyzabcdefgh", new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualNonGenericMethodIntArrayUsesClassTypeParam(\"wxyzabcdefgh\", new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassString.VirtualNonGenericMethodIntArrayUsesClassTypeParam("wxyzabcdefgh", new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.VirtualNonGenericMethodStringArrayUsesClassTypeParam("wxyzabcdefgh", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassString.VirtualNonGenericMethodStringArrayUsesClassTypeParam(\"wxyzabcdefgh\", new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassString.VirtualNonGenericMethodStringArrayUsesClassTypeParam("wxyzabcdefgh", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassIntArray.VirtualNonGenericMethodIntUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, 1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualNonGenericMethodIntUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, 1, typeof(int, true)) to be 1, but found '" + GenericClassIntArray.VirtualNonGenericMethodIntUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, 1, typeof(int), true) + "'");
        }

        if (GenericClassIntArray.VirtualNonGenericMethodStringUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, "aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualNonGenericMethodString(new int[] {Int32.MaxValue, Int32.MinValue}, \"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassIntArray.VirtualNonGenericMethodStringUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, "aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassStringArray.VirtualNonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int), true) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualNonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int, true)) to be 1, but found '" + GenericClassStringArray.VirtualNonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int), true) + "'");
        }

        if (GenericClassStringArray.VirtualNonGenericMethodStringUsesClassTypeParam(new string[1000], "aaaa", typeof(string), true) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualNonGenericMethodString(new string[1000], \"aaaa\", typeof(string, true)) to be \"aaaa\", but found '" + GenericClassStringArray.VirtualNonGenericMethodStringUsesClassTypeParam(new string[1000], "aaaa", typeof(string), true) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] { 1, 2, 3 }, typeof(int[]), true), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] {1,2,3} typeof(int[]), true) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] { 1, 2, 3 }, typeof(int[]), true)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), true) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), true)) + "'");
        }

        if (GenericClassInheritsFromGenericClassInt.VirtualGenericMethod<int>(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualGenericMethod<int>(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassInt.VirtualGenericMethod<int>(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassInt.VirtualGenericMethod<string>("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualGenericMethod<string>(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassInt.VirtualGenericMethod<string>("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassInt.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualGenericMethod<int[]>(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassInt.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassInt.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassInt.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassString.VirtualGenericMethod<int>(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualGenericMethod<int>(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassString.VirtualGenericMethod<int>(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassString.VirtualGenericMethod<string>("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualGenericMethod<string>(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassString.VirtualGenericMethod<string>("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassString.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualGenericMethod<int[]>(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassString.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassString.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassString.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethod<int>(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethod<int>(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethod<int>(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethod<string>("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethod<string>(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethod<string>("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethod<int[]>(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethod<int>(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethod<int>(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethod<int>(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethod<string>("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethod<string>(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethod<string>("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethod<int[]>(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodInt(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodInt(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodInt(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodString("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodString(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodString("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodInt(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodInt(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodInt(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodString("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodString(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodString("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodInt(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodInt(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodInt(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodString("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodString(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodString("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodInt(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodInt(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodInt(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodString("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodString(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodString("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassInt.VirtualGenericMethodUsesClassTypeParam<int>(27, 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualGenericMethodUsesClassTypeParam<int>(27, 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassInt.VirtualGenericMethodUsesClassTypeParam<int>(27, 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassInt.VirtualGenericMethodUsesClassTypeParam<string>(27, "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualGenericMethodUsesClassTypeParam<string>(27, \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassInt.VirtualGenericMethodUsesClassTypeParam<string>(27, "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassInt.VirtualGenericMethodUsesClassTypeParam<int[]>(27, new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualGenericMethodUsesClassTypeParam<int[]>(27, new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassInt.VirtualGenericMethodUsesClassTypeParam<int[]>(27, new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassInt.VirtualGenericMethodUsesClassTypeParam<string[]>(27, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualGenericMethodUsesClassTypeParam<string[]>(27, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassInt.VirtualGenericMethodUsesClassTypeParam<string[]>(27, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassString.VirtualGenericMethodUsesClassTypeParam<int>("", 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualGenericMethodUsesClassTypeParam<int>(\"\", 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassString.VirtualGenericMethodUsesClassTypeParam<int>("", 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassString.VirtualGenericMethodUsesClassTypeParam<string>("", "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualGenericMethodUsesClassTypeParam<string>(\"\", \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassString.VirtualGenericMethodUsesClassTypeParam<string>("", "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassString.VirtualGenericMethodUsesClassTypeParam<int[]>("", new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualGenericMethodUsesClassTypeParam<int[]>(\"\", new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassString.VirtualGenericMethodUsesClassTypeParam<int[]>("", new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassString.VirtualGenericMethodUsesClassTypeParam<string[]>("", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualGenericMethodUsesClassTypeParam<string[]>(\"\", new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassString.VirtualGenericMethodUsesClassTypeParam<string[]>("", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string>(new int[0], "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string>(new int[0], \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string>(new int[0], "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string>(new string[0], "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string>(new string[0], \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string>(new string[0], "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodStringUsesClassTypeParam(Int32.MaxValue, "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodString(Int32.MaxValue, \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodStringUsesClassTypeParam(Int32.MaxValue, "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassInt.VirtualNonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodIntUsesClassTypeParam("wxyzabcdefgh", 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodIntUsesClassTypeParam(\"wxyzabcdefgh\", 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodIntUsesClassTypeParam("wxyzabcdefgh", 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodStringUsesClassTypeParam("wxyzabcdefgh", "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodString(\"wxyzabcdefgh\", \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodStringUsesClassTypeParam("wxyzabcdefgh", "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodIntArrayUsesClassTypeParam("wxyzabcdefgh", new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodIntArrayUsesClassTypeParam(\"wxyzabcdefgh\", new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodIntArrayUsesClassTypeParam("wxyzabcdefgh", new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodStringArrayUsesClassTypeParam("wxyzabcdefgh", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodStringArrayUsesClassTypeParam(\"wxyzabcdefgh\", new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassString.VirtualNonGenericMethodStringArrayUsesClassTypeParam("wxyzabcdefgh", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodIntUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodIntUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodIntUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodStringUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodString(new int[] {Int32.MaxValue, Int32.MinValue}, \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodStringUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassIntArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodStringUsesClassTypeParam(new string[1000], "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodString(new string[1000], \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodStringUsesClassTypeParam(new string[1000], "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassStringArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethod<int>(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethod<int>(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethod<int>(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethod<string>("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethod<string>(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethod<string>("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethod<int[]>(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethod<int>(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethod<int>(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethod<int>(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethod<string>("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethod<string>(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethod<string>("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethod<int[]>(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethod<int>(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethod<int>(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethod<int>(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethod<string>("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethod<string>(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethod<string>("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethod<int[]>(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethod<int>(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethod<int>(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethod<int>(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethod<string>("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethod<string>(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethod<string>("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethod<int[]>(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodInt(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodInt(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodInt(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodString("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodString(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodString("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodInt(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodInt(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodInt(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodString("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodString(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodString("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodInt(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodInt(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodInt(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodString("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodString(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodString("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodInt(1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodInt(1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodInt(1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodString("aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodString(\"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodString("aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodIntArray(new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethodUsesClassTypeParam<int>(27, 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethodUsesClassTypeParam<int>(27, 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethodUsesClassTypeParam<int>(27, 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethodUsesClassTypeParam<string>(27, "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethodUsesClassTypeParam<string>(27, \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethodUsesClassTypeParam<string>(27, "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethodUsesClassTypeParam<int[]>(27, new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethodUsesClassTypeParam<int[]>(27, new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethodUsesClassTypeParam<int[]>(27, new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethodUsesClassTypeParam<string[]>(27, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethodUsesClassTypeParam<string[]>(27, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualGenericMethodUsesClassTypeParam<string[]>(27, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethodUsesClassTypeParam<int>("", 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethodUsesClassTypeParam<int>(\"\", 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethodUsesClassTypeParam<int>("", 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethodUsesClassTypeParam<string>("", "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethodUsesClassTypeParam<string>(\"\", \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethodUsesClassTypeParam<string>("", "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethodUsesClassTypeParam<int[]>("", new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethodUsesClassTypeParam<int[]>(\"\", new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethodUsesClassTypeParam<int[]>("", new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethodUsesClassTypeParam<string[]>("", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethodUsesClassTypeParam<string[]>(\"\", new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualGenericMethodUsesClassTypeParam<string[]>("", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string>(new int[0], "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string>(new int[0], \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string>(new int[0], "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string>(new string[0], "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string>(new string[0], \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string>(new string[0], "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualGenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodStringUsesClassTypeParam(Int32.MaxValue, "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodString(Int32.MaxValue, \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodStringUsesClassTypeParam(Int32.MaxValue, "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.VirtualNonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodIntUsesClassTypeParam("wxyzabcdefgh", 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodIntUsesClassTypeParam(\"wxyzabcdefgh\", 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodIntUsesClassTypeParam("wxyzabcdefgh", 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodStringUsesClassTypeParam("wxyzabcdefgh", "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodString(\"wxyzabcdefgh\", \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodStringUsesClassTypeParam("wxyzabcdefgh", "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodIntArrayUsesClassTypeParam("wxyzabcdefgh", new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodIntArrayUsesClassTypeParam(\"wxyzabcdefgh\", new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodIntArrayUsesClassTypeParam("wxyzabcdefgh", new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodStringArrayUsesClassTypeParam("wxyzabcdefgh", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodStringArrayUsesClassTypeParam(\"wxyzabcdefgh\", new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassString.VirtualNonGenericMethodStringArrayUsesClassTypeParam("wxyzabcdefgh", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodIntUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodIntUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodIntUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodStringUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodString(new int[] {Int32.MaxValue, Int32.MinValue}, \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodStringUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int), false) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int, false)) to be 1, but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int), false) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodStringUsesClassTypeParam(new string[1000], "aaaa", typeof(string), false) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodString(new string[1000], \"aaaa\", typeof(string, false)) to be \"aaaa\", but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodStringUsesClassTypeParam(new string[1000], "aaaa", typeof(string), false) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] { 1, 2, 3 }, typeof(int[]), false), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] {1,2,3} typeof(int[]), false) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] { 1, 2, 3 }, typeof(int[]), false)) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[]), false) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.VirtualNonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]), false)) + "'");
        }
    }

    internal static void GenericClassDelegate()
    {
        GenericClass<int>.classParameterType = typeof(int);
        GenericClass<string>.classParameterType = typeof(string);
        GenericClass<int[]>.classParameterType = typeof(int[]);
        GenericClass<string[]>.classParameterType = typeof(string[]);

        GenericClass<int> GenericClassInt = new GenericClass<int>();
        GenericClass<string> GenericClassString = new GenericClass<string>();
        GenericClass<int[]> GenericClassIntArray = new GenericClass<int[]>();
        GenericClass<string[]> GenericClassStringArray = new GenericClass<string[]>();
        GenericClass<int>.genericDelegate<int> GenericClassIntGenericDelegateInt = new GenericClass<int>.genericDelegate<int>(GenericClassInt.GenericMethod<int>);
        if (GenericClassIntGenericDelegateInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntGenericDelegateInt(1, typeof(int)) to be 1, but found '" + GenericClassIntGenericDelegateInt(1, typeof(int)) + "'");
        }

        GenericClass<int>.genericDelegate<string> GenericClassIntGenericDelegateString = new GenericClass<int>.genericDelegate<string>(GenericClassInt.GenericMethod<string>);
        if (GenericClassIntGenericDelegateString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntGenericDelegateString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassIntGenericDelegateString("aaaa", typeof(string)) + "'");
        }
        if (Utils.CompareArray<int>(GenericClassInt.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethod<int[]>(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInt.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInt.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassString.GenericMethod<int>(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethod<int>(1, typeof(int)) to be 1, but found '" + GenericClassString.GenericMethod<int>(1, typeof(int)) + "'");
        }

        if (GenericClassString.GenericMethod<string>("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethod<string>(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassString.GenericMethod<string>("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethod<int[]>(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassString.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassString.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassIntArray.GenericMethod<int>(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethod<int>(1, typeof(int)) to be 1, but found '" + GenericClassIntArray.GenericMethod<int>(1, typeof(int)) + "'");
        }

        if (GenericClassIntArray.GenericMethod<string>("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethod<string>(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassIntArray.GenericMethod<string>("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethod<int[]>(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassStringArray.GenericMethod<int>(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethod<int>(1, typeof(int)) to be 1, but found '" + GenericClassStringArray.GenericMethod<int>(1, typeof(int)) + "'");
        }

        if (GenericClassStringArray.GenericMethod<string>("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethod<string>(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassStringArray.GenericMethod<string>("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethod<int[]>(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.GenericMethod<int[]>(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethod<string[]>(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.GenericMethod<string[]>(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassInt.NonGenericMethodInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodInt(1, typeof(int)) to be 1, but found '" + GenericClassInt.NonGenericMethodInt(1, typeof(int)) + "'");
        }

        if (GenericClassInt.NonGenericMethodString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassInt.NonGenericMethodString("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodIntArray(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInt.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInt.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassString.NonGenericMethodInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodInt(1, typeof(int)) to be 1, but found '" + GenericClassString.NonGenericMethodInt(1, typeof(int)) + "'");
        }

        if (GenericClassString.NonGenericMethodString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassString.NonGenericMethodString("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodIntArray(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassString.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassString.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassIntArray.NonGenericMethodInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodInt(1, typeof(int)) to be 1, but found '" + GenericClassIntArray.NonGenericMethodInt(1, typeof(int)) + "'");
        }

        if (GenericClassIntArray.NonGenericMethodString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassIntArray.NonGenericMethodString("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodIntArray(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassStringArray.NonGenericMethodInt(1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodInt(1, typeof(int)) to be 1, but found '" + GenericClassStringArray.NonGenericMethodInt(1, typeof(int)) + "'");
        }

        if (GenericClassStringArray.NonGenericMethodString("aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodString(\"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassStringArray.NonGenericMethodString("aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodIntArray(new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.NonGenericMethodIntArray(new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodStringArray(new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.NonGenericMethodStringArray(new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassInt.GenericMethodUsesClassTypeParam<int>(27, 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethodUsesClassTypeParam<int>(27, 1, typeof(int)) to be 1, but found '" + GenericClassInt.GenericMethodUsesClassTypeParam<int>(27, 1, typeof(int)) + "'");
        }

        if (GenericClassInt.GenericMethodUsesClassTypeParam<string>(27, "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethodUsesClassTypeParam<string>(27, \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassInt.GenericMethodUsesClassTypeParam<string>(27, "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.GenericMethodUsesClassTypeParam<int[]>(27, new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethodUsesClassTypeParam<int[]>(27, new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInt.GenericMethodUsesClassTypeParam<int[]>(27, new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.GenericMethodUsesClassTypeParam<string[]>(27, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.GenericMethodUsesClassTypeParam<string[]>(27, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInt.GenericMethodUsesClassTypeParam<string[]>(27, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassString.GenericMethodUsesClassTypeParam<int>("", 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethodUsesClassTypeParam<int>(\"\", 1, typeof(int)) to be 1, but found '" + GenericClassString.GenericMethodUsesClassTypeParam<int>("", 1, typeof(int)) + "'");
        }

        if (GenericClassString.GenericMethodUsesClassTypeParam<string>("", "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethodUsesClassTypeParam<string>(\"\", \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassString.GenericMethodUsesClassTypeParam<string>("", "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.GenericMethodUsesClassTypeParam<int[]>("", new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethodUsesClassTypeParam<int[]>(\"\", new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassString.GenericMethodUsesClassTypeParam<int[]>("", new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.GenericMethodUsesClassTypeParam<string[]>("", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassString.GenericMethodUsesClassTypeParam<string[]>(\"\", new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassString.GenericMethodUsesClassTypeParam<string[]>("", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassIntArray.GenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int)) to be 1, but found '" + GenericClassIntArray.GenericMethodUsesClassTypeParam<int>(new int[0], 1, typeof(int)) + "'");
        }

        if (GenericClassIntArray.GenericMethodUsesClassTypeParam<string>(new int[0], "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethodUsesClassTypeParam<string>(new int[0], \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassIntArray.GenericMethodUsesClassTypeParam<string>(new int[0], "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.GenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.GenericMethodUsesClassTypeParam<int[]>(new int[0], new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.GenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.GenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.GenericMethodUsesClassTypeParam<string[]>(new int[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassStringArray.GenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int)) to be 1, but found '" + GenericClassStringArray.GenericMethodUsesClassTypeParam<int>(new string[0], 1, typeof(int)) + "'");
        }

        if (GenericClassStringArray.GenericMethodUsesClassTypeParam<string>(new string[0], "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethodUsesClassTypeParam<string>(new string[0], \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassStringArray.GenericMethodUsesClassTypeParam<string>(new string[0], "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.GenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.GenericMethodUsesClassTypeParam<int[]>(new string[0], new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.GenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.GenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.GenericMethodUsesClassTypeParam<string[]>(new string[0], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassInt.NonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int)) to be 1, but found '" + GenericClassInt.NonGenericMethodIntUsesClassTypeParam(Int32.MaxValue, 1, typeof(int)) + "'");
        }

        if (GenericClassInt.NonGenericMethodStringUsesClassTypeParam(Int32.MaxValue, "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodString(Int32.MaxValue, \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassInt.NonGenericMethodStringUsesClassTypeParam(Int32.MaxValue, "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.NonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassInt.NonGenericMethodIntArrayUsesClassTypeParam(Int32.MaxValue, new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.NonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.NonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassInt.NonGenericMethodStringArrayUsesClassTypeParam(Int32.MaxValue, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassString.NonGenericMethodIntUsesClassTypeParam("wxyzabcdefgh", 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodIntUsesClassTypeParam(\"wxyzabcdefgh\", 1, typeof(int)) to be 1, but found '" + GenericClassString.NonGenericMethodIntUsesClassTypeParam("wxyzabcdefgh", 1, typeof(int)) + "'");
        }

        if (GenericClassString.NonGenericMethodStringUsesClassTypeParam("wxyzabcdefgh", "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodString(\"wxyzabcdefgh\", \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassString.NonGenericMethodStringUsesClassTypeParam("wxyzabcdefgh", "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.NonGenericMethodIntArrayUsesClassTypeParam("wxyzabcdefgh", new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodIntArrayUsesClassTypeParam(\"wxyzabcdefgh\", new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassString.NonGenericMethodIntArrayUsesClassTypeParam("wxyzabcdefgh", new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.NonGenericMethodStringArrayUsesClassTypeParam("wxyzabcdefgh", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassString.NonGenericMethodStringArrayUsesClassTypeParam(\"wxyzabcdefgh\", new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassString.NonGenericMethodStringArrayUsesClassTypeParam("wxyzabcdefgh", new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassIntArray.NonGenericMethodIntUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodIntUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, 1, typeof(int)) to be 1, but found '" + GenericClassIntArray.NonGenericMethodIntUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, 1, typeof(int)) + "'");
        }

        if (GenericClassIntArray.NonGenericMethodStringUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodString(new int[] {Int32.MaxValue, Int32.MinValue}, \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassIntArray.NonGenericMethodStringUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.NonGenericMethodIntArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodIntArrayUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.NonGenericMethodIntArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.NonGenericMethodStringArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.NonGenericMethodStringArrayUsesClassTypeParam(new int[] {Int32.MaxValue, Int32.MinValue}, new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.NonGenericMethodStringArrayUsesClassTypeParam(new int[] { Int32.MaxValue, Int32.MinValue }, new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }

        if (GenericClassStringArray.NonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int)) != 1)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int)) to be 1, but found '" + GenericClassStringArray.NonGenericMethodIntUsesClassTypeParam(new string[1000], 1, typeof(int)) + "'");
        }

        if (GenericClassStringArray.NonGenericMethodStringUsesClassTypeParam(new string[1000], "aaaa", typeof(string)) != "aaaa")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodString(new string[1000], \"aaaa\", typeof(string)) to be \"aaaa\", but found '" + GenericClassStringArray.NonGenericMethodStringUsesClassTypeParam(new string[1000], "aaaa", typeof(string)) + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.NonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] { 1, 2, 3 }, typeof(int[])), new int[] { 1, 2, 3 }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] {1,2,3} typeof(int[])) to be int[] {1,2,3}, but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.NonGenericMethodIntArrayUsesClassTypeParam(new string[1000], new int[] { 1, 2, 3 }, typeof(int[]))) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.NonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[])), new string[] { "abc", "def", "ghi", "jkl" }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.NonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] {\"abc\",\"def\",\"ghi\",\"jkl\"} typeof(string[])) to be string[] {\"abc\",\"def\",\"ghi\",\"jkl\"}, but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.NonGenericMethodStringArrayUsesClassTypeParam(new string[1000], new string[] { "abc", "def", "ghi", "jkl" }, typeof(string[]))) + "'");
        }
    }

    internal static void GenericClassField()
    {
        GenericClass<int>.classParameterType = typeof(int);
        GenericClass<string>.classParameterType = typeof(string);
        GenericClass<int[]>.classParameterType = typeof(int[]);
        GenericClass<string[]>.classParameterType = typeof(string[]);

        GenericClass<int> GenericClassInt = new GenericClass<int>();
        GenericClass<string> GenericClassString = new GenericClass<string>();
        GenericClass<int[]> GenericClassIntArray = new GenericClass<int[]>();
        GenericClass<string[]> GenericClassStringArray = new GenericClass<string[]>();

        GenericClassInt.genericField = Int32.MaxValue;
        GenericClassInt.nongenericIntField = Int32.MinValue;
        GenericClassInt.nongenericStringField = "";
        GenericClassInt.nongenericIntArrayField = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassInt.nongenericStringArrayField = new string[] { "", "", "", " " };

        if (GenericClassInt.genericField != Int32.MaxValue)
        {
            Utils.Fail("Expected returned value of GenericClassInt.genericField to be '" + Int32.MaxValue + "', but found '" + GenericClassInt.genericField + "'");
        }

        if (GenericClassInt.nongenericIntField != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassInt.nongenericIntField to be '" + Int32.MinValue + "', but found '" + GenericClassInt.nongenericIntField + "'");
        }

        if (GenericClassInt.nongenericStringField != "")
        {
            Utils.Fail("Expected returned value of GenericClassInt.nongenericStringField to be '" + "" + "', but found '" + GenericClassInt.nongenericStringField + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.nongenericIntArrayField, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.nongenericIntArrayField to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassInt.nongenericIntArrayField) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.nongenericStringArrayField, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.nongenericStringArrayField to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInt.nongenericStringArrayField) + "'");
        }

        GenericClassString.nongenericIntField = Int32.MinValue;
        GenericClassString.nongenericStringField = "";
        GenericClassString.nongenericIntArrayField = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassString.nongenericStringArrayField = new string[] { "", "", "", " " };

        if (GenericClassString.genericField != null)
        {
            Utils.Fail("Expected returned value of GenericClassString.genericField to be 'null', but found '" + GenericClassString.genericField + "'");
        }

        if (GenericClassString.nongenericIntField != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassString.nongenericIntField to be '" + Int32.MinValue + "', but found '" + GenericClassString.nongenericIntField + "'");
        }

        if (GenericClassString.nongenericStringField != "")
        {
            Utils.Fail("Expected returned value of GenericClassString.nongenericStringField to be '" + "" + "', but found '" + GenericClassString.nongenericStringField + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.nongenericIntArrayField, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassString.nongenericIntArrayField to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassString.nongenericIntArrayField) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.nongenericStringArrayField, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassString.nongenericStringArrayField to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassString.nongenericStringArrayField) + "'");
        }

        GenericClassIntArray.genericField = new int[] { 6, 4, 2, 1, 0 };
        GenericClassIntArray.nongenericIntField = Int32.MinValue;
        GenericClassIntArray.nongenericStringField = "";
        GenericClassIntArray.nongenericIntArrayField = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassIntArray.nongenericStringArrayField = new string[] { "", "", "", " " };

        if (Utils.CompareArray<int>(GenericClassIntArray.genericField, new int[] { 6, 4, 2, 1, 0 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.genericField to be 'new int[]{6,4,2,1,0}', but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.genericField) + "'");
        }

        if (GenericClassIntArray.nongenericIntField != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.nongenericIntField to be '" + Int32.MinValue + "', but found '" + GenericClassIntArray.nongenericIntField + "'");
        }

        if (GenericClassIntArray.nongenericStringField != "")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.nongenericStringField to be '" + "" + "', but found '" + GenericClassIntArray.nongenericStringField + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.nongenericIntArrayField, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.nongenericIntArrayField to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.nongenericIntArrayField) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.nongenericStringArrayField, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.nongenericStringArrayField to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.nongenericStringArrayField) + "'");
        }

        GenericClassStringArray.genericField = new string[] { " ", "", "", " " };
        GenericClassStringArray.nongenericIntField = Int32.MinValue;
        GenericClassStringArray.nongenericStringField = "";
        GenericClassStringArray.nongenericIntArrayField = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassStringArray.nongenericStringArrayField = new string[] { "", "", "", " " };

        if (Utils.CompareArray<string>(GenericClassStringArray.genericField, new string[] { " ", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.genericField to be 'new string[]{\" \",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.genericField) + "'");
        }

        if (GenericClassStringArray.nongenericIntField != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.nongenericIntField to be '" + Int32.MinValue + "', but found '" + GenericClassStringArray.nongenericIntField + "'");
        }

        if (GenericClassStringArray.nongenericStringField != "")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.nongenericStringField to be '" + "" + "', but found '" + GenericClassStringArray.nongenericStringField + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.nongenericIntArrayField, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.nongenericIntArrayField to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.nongenericIntArrayField) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.nongenericStringArrayField, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.nongenericStringArrayField to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.nongenericStringArrayField) + "'");
        }
    }

    internal static void GenericClassProperty()
    {
        GenericClass<int>.classParameterType = typeof(int);
        GenericClass<string>.classParameterType = typeof(string);
        GenericClass<int[]>.classParameterType = typeof(int[]);
        GenericClass<string[]>.classParameterType = typeof(string[]);

        GenericClass<int> GenericClassInt = new GenericClass<int>();
        GenericClass<string> GenericClassString = new GenericClass<string>();
        GenericClass<int[]> GenericClassIntArray = new GenericClass<int[]>();
        GenericClass<string[]> GenericClassStringArray = new GenericClass<string[]>();

        GenericClassInt.genericProperty = Int32.MaxValue;
        GenericClassInt.nongenericIntProperty = Int32.MinValue;
        GenericClassInt.nongenericStringProperty = "";
        GenericClassInt.nongenericIntArrayProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassInt.nongenericStringArrayProperty = new string[] { "", "", "", " " };

        if (GenericClassInt.genericProperty != Int32.MaxValue)
        {
            Utils.Fail("Expected returned value of GenericClassInt.genericProperty to be '" + Int32.MaxValue + "', but found '" + GenericClassInt.genericProperty + "'");
        }

        if (GenericClassInt.nongenericIntProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassInt.nongenericIntProperty to be '" + Int32.MinValue + "', but found '" + GenericClassInt.nongenericIntProperty + "'");
        }

        if (GenericClassInt.nongenericStringProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassInt.nongenericStringProperty to be '" + "" + "', but found '" + GenericClassInt.nongenericStringProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.nongenericIntArrayProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.nongenericIntArrayProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassInt.nongenericIntArrayProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.nongenericStringArrayProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.nongenericStringArrayProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInt.nongenericStringArrayProperty) + "'");
        }

        GenericClassString.genericProperty = string.Empty;
        GenericClassString.nongenericIntProperty = Int32.MinValue;
        GenericClassString.nongenericStringProperty = "";
        GenericClassString.nongenericIntArrayProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassString.nongenericStringArrayProperty = new string[] { "", "", "", " " };

        if (GenericClassString.genericProperty != string.Empty)
        {
            Utils.Fail("Expected returned value of GenericClassString.genericProperty to be '" + string.Empty + "', but found '" + GenericClassString.genericProperty + "'");
        }

        if (GenericClassString.nongenericIntProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassString.nongenericIntProperty to be '" + Int32.MinValue + "', but found '" + GenericClassString.nongenericIntProperty + "'");
        }

        if (GenericClassString.nongenericStringProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassString.nongenericStringProperty to be '" + "" + "', but found '" + GenericClassString.nongenericStringProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.nongenericIntArrayProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassString.nongenericIntArrayProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassString.nongenericIntArrayProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.nongenericStringArrayProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassString.nongenericStringArrayProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassString.nongenericStringArrayProperty) + "'");
        }

        GenericClassIntArray.genericProperty = new int[] { 6, 4, 2, 1, 0 };
        GenericClassIntArray.nongenericIntProperty = Int32.MinValue;
        GenericClassIntArray.nongenericStringProperty = "";
        GenericClassIntArray.nongenericIntArrayProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassIntArray.nongenericStringArrayProperty = new string[] { "", "", "", " " };

        if (Utils.CompareArray<int>(GenericClassIntArray.genericProperty, new int[] { 6, 4, 2, 1, 0 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.genericProperty to be 'new int[]{6,4,2,1,0}', but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.genericProperty) + "'");
        }

        if (GenericClassIntArray.nongenericIntProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.nongenericIntProperty to be '" + Int32.MinValue + "', but found '" + GenericClassIntArray.nongenericIntProperty + "'");
        }

        if (GenericClassIntArray.nongenericStringProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.nongenericStringProperty to be '" + "" + "', but found '" + GenericClassIntArray.nongenericStringProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.nongenericIntArrayProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.nongenericIntArrayProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.nongenericIntArrayProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.nongenericStringArrayProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.nongenericStringArrayProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.nongenericStringArrayProperty) + "'");
        }

        GenericClassStringArray.genericProperty = new string[] { " ", "", "", " " };
        GenericClassStringArray.nongenericIntProperty = Int32.MinValue;
        GenericClassStringArray.nongenericStringProperty = "";
        GenericClassStringArray.nongenericIntArrayProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassStringArray.nongenericStringArrayProperty = new string[] { "", "", "", " " };

        if (Utils.CompareArray<string>(GenericClassStringArray.genericProperty, new string[] { " ", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.genericProperty to be 'new string[]{\" \",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.genericProperty) + "'");
        }

        if (GenericClassStringArray.nongenericIntProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.nongenericIntProperty to be '" + Int32.MinValue + "', but found '" + GenericClassStringArray.nongenericIntProperty + "'");
        }

        if (GenericClassStringArray.nongenericStringProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.nongenericStringProperty to be '" + "" + "', but found '" + GenericClassStringArray.nongenericStringProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.nongenericIntArrayProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.nongenericIntArrayProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.nongenericIntArrayProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.nongenericStringArrayProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.nongenericStringArrayProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.nongenericStringArrayProperty) + "'");
        }
    }

    internal static void GenericClassVirtualProperty()
    {
        GenericClass<int>.classParameterType = typeof(int);
        GenericClass<string>.classParameterType = typeof(string);
        GenericClass<int[]>.classParameterType = typeof(int[]);
        GenericClass<string[]>.classParameterType = typeof(string[]);

        GenericClass<int> GenericClassInt = new GenericClass<int>();
        GenericClass<string> GenericClassString = new GenericClass<string>();
        GenericClass<int[]> GenericClassIntArray = new GenericClass<int[]>();
        GenericClass<string[]> GenericClassStringArray = new GenericClass<string[]>();
        GenericClassInt.usingBaseVirtualProperty = true;
        GenericClassString.usingBaseVirtualProperty = true;
        GenericClassIntArray.usingBaseVirtualProperty = true;
        GenericClassStringArray.usingBaseVirtualProperty = true;

        GenericClassInheritsFromGenericClass<int> GenericClassInheritsFromGenericClassInt = new GenericClassInheritsFromGenericClass<int>();
        GenericClassInheritsFromGenericClass<string> GenericClassInheritsFromGenericClassString = new GenericClassInheritsFromGenericClass<string>();
        GenericClassInheritsFromGenericClass<int[]> GenericClassInheritsFromGenericClassIntArray = new GenericClassInheritsFromGenericClass<int[]>();
        GenericClassInheritsFromGenericClass<string[]> GenericClassInheritsFromGenericClassStringArray = new GenericClassInheritsFromGenericClass<string[]>();
        GenericClassInheritsFromGenericClassInt.usingBaseVirtualProperty = false;
        GenericClassInheritsFromGenericClassString.usingBaseVirtualProperty = false;
        GenericClassInheritsFromGenericClassIntArray.usingBaseVirtualProperty = false;
        GenericClassInheritsFromGenericClassStringArray.usingBaseVirtualProperty = false;

        GenericClass<int> GenericClassInheritsFromGenericClassCastAsGenericClassInt = GenericClassInheritsFromGenericClassInt;
        GenericClass<string> GenericClassInheritsFromGenericClassCastAsGenericClassString = GenericClassInheritsFromGenericClassString;
        GenericClass<int[]> GenericClassInheritsFromGenericClassCastAsGenericClassIntArray = GenericClassInheritsFromGenericClassIntArray;
        GenericClass<string[]> GenericClassInheritsFromGenericClassCastAsGenericClassStringArray = GenericClassInheritsFromGenericClassStringArray;

        GenericClassInt.genericVirtualProperty = Int32.MaxValue;
        GenericClassInt.nongenericIntVirtualProperty = Int32.MinValue;
        GenericClassInt.nongenericStringVirtualProperty = "";
        GenericClassInt.nongenericIntArrayVirtualProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassInt.nongenericStringArrayVirtualProperty = new string[] { "", "", "", " " };

        if (GenericClassInt.genericVirtualProperty != Int32.MaxValue)
        {
            Utils.Fail("Expected returned value of GenericClassInt.genericVirtualProperty to be '" + Int32.MaxValue + "', but found '" + GenericClassInt.genericVirtualProperty + "'");
        }

        if (GenericClassInt.nongenericIntVirtualProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassInt.nongenericIntVirtualProperty to be '" + Int32.MinValue + "', but found '" + GenericClassInt.nongenericIntVirtualProperty + "'");
        }

        if (GenericClassInt.nongenericStringVirtualProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassInt.nongenericStringVirtualProperty to be '" + "" + "', but found '" + GenericClassInt.nongenericStringVirtualProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInt.nongenericIntArrayVirtualProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.nongenericIntArrayVirtualProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassInt.nongenericIntArrayVirtualProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInt.nongenericStringArrayVirtualProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInt.nongenericStringArrayVirtualProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInt.nongenericStringArrayVirtualProperty) + "'");
        }

        GenericClassString.genericVirtualProperty = string.Empty;
        GenericClassString.nongenericIntVirtualProperty = Int32.MinValue;
        GenericClassString.nongenericStringVirtualProperty = "";
        GenericClassString.nongenericIntArrayVirtualProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassString.nongenericStringArrayVirtualProperty = new string[] { "", "", "", " " };

        if (GenericClassString.genericVirtualProperty != string.Empty)
        {
            Utils.Fail("Expected returned value of GenericClassString.genericVirtualProperty to be '" + string.Empty + "', but found '" + GenericClassString.genericVirtualProperty + "'");
        }

        if (GenericClassString.nongenericIntVirtualProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassString.nongenericIntVirtualProperty to be '" + Int32.MinValue + "', but found '" + GenericClassString.nongenericIntVirtualProperty + "'");
        }

        if (GenericClassString.nongenericStringVirtualProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassString.nongenericStringVirtualProperty to be '" + "" + "', but found '" + GenericClassString.nongenericStringVirtualProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassString.nongenericIntArrayVirtualProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassString.nongenericIntArrayVirtualProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassString.nongenericIntArrayVirtualProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassString.nongenericStringArrayVirtualProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassString.nongenericStringArrayVirtualProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassString.nongenericStringArrayVirtualProperty) + "'");
        }

        GenericClassIntArray.genericVirtualProperty = new int[] { 6, 4, 2, 1, 0 };
        GenericClassIntArray.nongenericIntVirtualProperty = Int32.MinValue;
        GenericClassIntArray.nongenericStringVirtualProperty = "";
        GenericClassIntArray.nongenericIntArrayVirtualProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassIntArray.nongenericStringArrayVirtualProperty = new string[] { "", "", "", " " };

        if (Utils.CompareArray<int>(GenericClassIntArray.genericVirtualProperty, new int[] { 6, 4, 2, 1, 0 }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.genericVirtualProperty to be 'new int[]{6,4,2,1,0}', but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.genericVirtualProperty) + "'");
        }

        if (GenericClassIntArray.nongenericIntVirtualProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.nongenericIntVirtualProperty to be '" + Int32.MinValue + "', but found '" + GenericClassIntArray.nongenericIntVirtualProperty + "'");
        }

        if (GenericClassIntArray.nongenericStringVirtualProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.nongenericStringVirtualProperty to be '" + "" + "', but found '" + GenericClassIntArray.nongenericStringVirtualProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassIntArray.nongenericIntArrayVirtualProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.nongenericIntArrayVirtualProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassIntArray.nongenericIntArrayVirtualProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassIntArray.nongenericStringArrayVirtualProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassIntArray.nongenericStringArrayVirtualProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassIntArray.nongenericStringArrayVirtualProperty) + "'");
        }

        GenericClassStringArray.genericVirtualProperty = new string[] { " ", "", "", " " };
        GenericClassStringArray.nongenericIntVirtualProperty = Int32.MinValue;
        GenericClassStringArray.nongenericStringVirtualProperty = "";
        GenericClassStringArray.nongenericIntArrayVirtualProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassStringArray.nongenericStringArrayVirtualProperty = new string[] { "", "", "", " " };

        if (Utils.CompareArray<string>(GenericClassStringArray.genericVirtualProperty, new string[] { " ", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.genericVirtualProperty to be 'new string[]{\" \",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.genericVirtualProperty) + "'");
        }

        if (GenericClassStringArray.nongenericIntVirtualProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.nongenericIntVirtualProperty to be '" + Int32.MinValue + "', but found '" + GenericClassStringArray.nongenericIntVirtualProperty + "'");
        }

        if (GenericClassStringArray.nongenericStringVirtualProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.nongenericStringVirtualProperty to be '" + "" + "', but found '" + GenericClassStringArray.nongenericStringVirtualProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassStringArray.nongenericIntArrayVirtualProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.nongenericIntArrayVirtualProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassStringArray.nongenericIntArrayVirtualProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassStringArray.nongenericStringArrayVirtualProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassStringArray.nongenericStringArrayVirtualProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassStringArray.nongenericStringArrayVirtualProperty) + "'");
        }

        GenericClassInheritsFromGenericClassInt.genericVirtualProperty = Int32.MaxValue;
        GenericClassInheritsFromGenericClassInt.nongenericIntVirtualProperty = Int32.MinValue;
        GenericClassInheritsFromGenericClassInt.nongenericStringVirtualProperty = "";
        GenericClassInheritsFromGenericClassInt.nongenericIntArrayVirtualProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassInheritsFromGenericClassInt.nongenericStringArrayVirtualProperty = new string[] { "", "", "", " " };

        if (GenericClassInheritsFromGenericClassInt.genericVirtualProperty != Int32.MaxValue)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.genericVirtualProperty to be '" + Int32.MaxValue + "', but found '" + GenericClassInheritsFromGenericClassInt.genericVirtualProperty + "'");
        }

        if (GenericClassInheritsFromGenericClassInt.nongenericIntVirtualProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.nongenericIntVirtualProperty to be '" + Int32.MinValue + "', but found '" + GenericClassInheritsFromGenericClassInt.nongenericIntVirtualProperty + "'");
        }

        if (GenericClassInheritsFromGenericClassInt.nongenericStringVirtualProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.nongenericStringVirtualProperty to be '" + "" + "', but found '" + GenericClassInheritsFromGenericClassInt.nongenericStringVirtualProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassInt.nongenericIntArrayVirtualProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.nongenericIntArrayVirtualProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassInt.nongenericIntArrayVirtualProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassInt.nongenericStringArrayVirtualProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassInt.nongenericStringArrayVirtualProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassInt.nongenericStringArrayVirtualProperty) + "'");
        }

        GenericClassInheritsFromGenericClassString.genericVirtualProperty = string.Empty;
        GenericClassInheritsFromGenericClassString.nongenericIntVirtualProperty = Int32.MinValue;
        GenericClassInheritsFromGenericClassString.nongenericStringVirtualProperty = "";
        GenericClassInheritsFromGenericClassString.nongenericIntArrayVirtualProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassInheritsFromGenericClassString.nongenericStringArrayVirtualProperty = new string[] { "", "", "", " " };

        if (GenericClassInheritsFromGenericClassString.genericVirtualProperty != string.Empty)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.genericVirtualProperty to be '" + string.Empty + "', but found '" + GenericClassInheritsFromGenericClassString.genericVirtualProperty + "'");
        }

        if (GenericClassInheritsFromGenericClassString.nongenericIntVirtualProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.nongenericIntVirtualProperty to be '" + Int32.MinValue + "', but found '" + GenericClassInheritsFromGenericClassString.nongenericIntVirtualProperty + "'");
        }

        if (GenericClassInheritsFromGenericClassString.nongenericStringVirtualProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.nongenericStringVirtualProperty to be '" + "" + "', but found '" + GenericClassInheritsFromGenericClassString.nongenericStringVirtualProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassString.nongenericIntArrayVirtualProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.nongenericIntArrayVirtualProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassString.nongenericIntArrayVirtualProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassString.nongenericStringArrayVirtualProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassString.nongenericStringArrayVirtualProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassString.nongenericStringArrayVirtualProperty) + "'");
        }

        GenericClassInheritsFromGenericClassIntArray.genericVirtualProperty = new int[] { 6, 4, 2, 1, 0 };
        GenericClassInheritsFromGenericClassIntArray.nongenericIntVirtualProperty = Int32.MinValue;
        GenericClassInheritsFromGenericClassIntArray.nongenericStringVirtualProperty = "";
        GenericClassInheritsFromGenericClassIntArray.nongenericIntArrayVirtualProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassInheritsFromGenericClassIntArray.nongenericStringArrayVirtualProperty = new string[] { "", "", "", " " };

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassIntArray.genericVirtualProperty, new int[] { 6, 4, 2, 1, 0 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.genericVirtualProperty to be 'new int[]{6,4,2,1,0}', but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassIntArray.genericVirtualProperty) + "'");
        }

        if (GenericClassInheritsFromGenericClassIntArray.nongenericIntVirtualProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.nongenericIntVirtualProperty to be '" + Int32.MinValue + "', but found '" + GenericClassInheritsFromGenericClassIntArray.nongenericIntVirtualProperty + "'");
        }

        if (GenericClassInheritsFromGenericClassIntArray.nongenericStringVirtualProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.nongenericStringVirtualProperty to be '" + "" + "', but found '" + GenericClassInheritsFromGenericClassIntArray.nongenericStringVirtualProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassIntArray.nongenericIntArrayVirtualProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.nongenericIntArrayVirtualProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassIntArray.nongenericIntArrayVirtualProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassIntArray.nongenericStringArrayVirtualProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassIntArray.nongenericStringArrayVirtualProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassIntArray.nongenericStringArrayVirtualProperty) + "'");
        }

        GenericClassInheritsFromGenericClassStringArray.genericVirtualProperty = new string[] { " ", "", "", " " };
        GenericClassInheritsFromGenericClassStringArray.nongenericIntVirtualProperty = Int32.MinValue;
        GenericClassInheritsFromGenericClassStringArray.nongenericStringVirtualProperty = "";
        GenericClassInheritsFromGenericClassStringArray.nongenericIntArrayVirtualProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassInheritsFromGenericClassStringArray.nongenericStringArrayVirtualProperty = new string[] { "", "", "", " " };

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassStringArray.genericVirtualProperty, new string[] { " ", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.genericVirtualProperty to be 'new string[]{\" \",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassStringArray.genericVirtualProperty) + "'");
        }

        if (GenericClassInheritsFromGenericClassStringArray.nongenericIntVirtualProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.nongenericIntVirtualProperty to be '" + Int32.MinValue + "', but found '" + GenericClassInheritsFromGenericClassStringArray.nongenericIntVirtualProperty + "'");
        }

        if (GenericClassInheritsFromGenericClassStringArray.nongenericStringVirtualProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.nongenericStringVirtualProperty to be '" + "" + "', but found '" + GenericClassInheritsFromGenericClassStringArray.nongenericStringVirtualProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassStringArray.nongenericIntArrayVirtualProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.nongenericIntArrayVirtualProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassStringArray.nongenericIntArrayVirtualProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassStringArray.nongenericStringArrayVirtualProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassStringArray.nongenericStringArrayVirtualProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassStringArray.nongenericStringArrayVirtualProperty) + "'");
        }


        GenericClassInheritsFromGenericClassCastAsGenericClassInt.genericVirtualProperty = Int32.MaxValue;
        GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericIntVirtualProperty = Int32.MinValue;
        GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericStringVirtualProperty = "";
        GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericIntArrayVirtualProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericStringArrayVirtualProperty = new string[] { "", "", "", " " };

        if (GenericClassInheritsFromGenericClassCastAsGenericClassInt.genericVirtualProperty != Int32.MaxValue)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.genericVirtualProperty to be '" + Int32.MaxValue + "', but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassInt.genericVirtualProperty + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericIntVirtualProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericIntVirtualProperty to be '" + Int32.MinValue + "', but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericIntVirtualProperty + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericStringVirtualProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericStringVirtualProperty to be '" + "" + "', but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericStringVirtualProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericIntArrayVirtualProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericIntArrayVirtualProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericIntArrayVirtualProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericStringArrayVirtualProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericStringArrayVirtualProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassInt.nongenericStringArrayVirtualProperty) + "'");
        }

        GenericClassInheritsFromGenericClassCastAsGenericClassString.genericVirtualProperty = string.Empty;
        GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericIntVirtualProperty = Int32.MinValue;
        GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericStringVirtualProperty = "";
        GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericIntArrayVirtualProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericStringArrayVirtualProperty = new string[] { "", "", "", " " };

        if (GenericClassInheritsFromGenericClassCastAsGenericClassString.genericVirtualProperty != string.Empty)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.genericVirtualProperty to be '" + string.Empty + "', but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassString.genericVirtualProperty + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericIntVirtualProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericIntVirtualProperty to be '" + Int32.MinValue + "', but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericIntVirtualProperty + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericStringVirtualProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericStringVirtualProperty to be '" + "" + "', but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericStringVirtualProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericIntArrayVirtualProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericIntArrayVirtualProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericIntArrayVirtualProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericStringArrayVirtualProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericStringArrayVirtualProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassString.nongenericStringArrayVirtualProperty) + "'");
        }

        GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.genericVirtualProperty = new int[] { 6, 4, 2, 1, 0 };
        GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericIntVirtualProperty = Int32.MinValue;
        GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericStringVirtualProperty = "";
        GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericIntArrayVirtualProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericStringArrayVirtualProperty = new string[] { "", "", "", " " };

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.genericVirtualProperty, new int[] { 6, 4, 2, 1, 0 }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.genericVirtualProperty to be 'new int[]{6,4,2,1,0}', but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.genericVirtualProperty) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericIntVirtualProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericIntVirtualProperty to be '" + Int32.MinValue + "', but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericIntVirtualProperty + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericStringVirtualProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericStringVirtualProperty to be '" + "" + "', but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericStringVirtualProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericIntArrayVirtualProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericIntArrayVirtualProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericIntArrayVirtualProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericStringArrayVirtualProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericStringArrayVirtualProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassIntArray.nongenericStringArrayVirtualProperty) + "'");
        }

        GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.genericVirtualProperty = new string[] { " ", "", "", " " };
        GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericIntVirtualProperty = Int32.MinValue;
        GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericStringVirtualProperty = "";
        GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericIntArrayVirtualProperty = new int[] { 0, Int32.MaxValue, Int32.MinValue };
        GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericStringArrayVirtualProperty = new string[] { "", "", "", " " };

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.genericVirtualProperty, new string[] { " ", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.genericVirtualProperty to be 'new string[]{\" \",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.genericVirtualProperty) + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericIntVirtualProperty != Int32.MinValue)
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericIntVirtualProperty to be '" + Int32.MinValue + "', but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericIntVirtualProperty + "'");
        }

        if (GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericStringVirtualProperty != "")
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericStringVirtualProperty to be '" + "" + "', but found '" + GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericStringVirtualProperty + "'");
        }

        if (Utils.CompareArray<int>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericIntArrayVirtualProperty, new int[] { 0, Int32.MaxValue, Int32.MinValue }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericIntArrayVirtualProperty to be 'new int[]{0, " + Int32.MaxValue + ", " + Int32.MinValue + "}', but found '" + Utils.BuildArrayString<int>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericIntArrayVirtualProperty) + "'");
        }

        if (Utils.CompareArray<string>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericStringArrayVirtualProperty, new string[] { "", "", "", " " }))
        {
            Utils.Fail("Expected returned value of GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericStringArrayVirtualProperty to be 'new string[]{\"\",\"\",\"\",\" \"}', but found '" + Utils.BuildArrayString<string>(GenericClassInheritsFromGenericClassCastAsGenericClassStringArray.nongenericStringArrayVirtualProperty) + "'");
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            GenericClassStaticMethod();
        }
        catch (Exception E)
        {
            Utils.Fail("GenericClassStaticMethod failed due to unknown exception: " + E);
        }

        try
        {
            GenericClassInstanceMethod();
        }
        catch (Exception E)
        {
            Utils.Fail("GenericClassInstanceMethod failed due to unknown exception: " + E);
        }

        try
        {
            GenericClassVirtualMethod();
        }
        catch (Exception E)
        {
            Utils.Fail("GenericClassVirtualMethod failed due to unknown exception: " + E);
        }

        try
        {
            GenericClassField();
        }
        catch (Exception E)
        {
            Utils.Fail("GenericClassField failed due to unknown exception: " + E);
        }

        try
        {
            GenericClassProperty();
        }
        catch (Exception E)
        {
            Utils.Fail("GenericClassProperty failed due to unknown exception: " + E);
        }

        try
        {
            GenericClassVirtualProperty();
        }
        catch (Exception E)
        {
            Utils.Fail("GenericClassVirtualProperty failed due to unknown exception: " + E);
        }

        try
        {
            GenericClassDelegate();
        }
        catch (Exception E)
        {
            Utils.Fail("GenericClassDelegate failed due to unknown exception: " + E);
        }

        if (Utils.failures == 0)
        {
            Console.WriteLine("Test Passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Test Failed!");
            return 99;
        }
    }
}

