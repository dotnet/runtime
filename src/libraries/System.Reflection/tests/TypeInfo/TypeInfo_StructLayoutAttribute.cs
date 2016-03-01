// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Reflection.Tests
{
    public class TypeInfo_StructLayoutAttribute
    {
        [Fact]
        public static void StructLayoutAttribute()
        {
            ValidateAttribute(GetStructLayoutAttribute(typeof(StructWithoutExplicitStructLayout)), LayoutKind.Sequential, CharSet.Ansi, 8);
            ValidateAttribute(GetStructLayoutAttribute(typeof(StructWithExplicitStructLayout)), LayoutKind.Explicit, CharSet.Ansi, 1);
            ValidateAttribute(GetStructLayoutAttribute(typeof(ClassWithoutExplicitStructLayout)), LayoutKind.Auto, CharSet.Ansi, 8);
            ValidateAttribute(GetStructLayoutAttribute(typeof(ClassWithExplicitStructLayout)), LayoutKind.Explicit, CharSet.Unicode, 2);
        }

        public static StructLayoutAttribute GetStructLayoutAttribute(Type t)
        {
            return t.GetTypeInfo().StructLayoutAttribute;
        }

        public static void ValidateAttribute(StructLayoutAttribute atrib, LayoutKind kind, CharSet charset, int pack)
        {
            Assert.Equal(atrib.Value, kind);
            Assert.Equal(atrib.CharSet, charset);
            Assert.Equal(atrib.Pack, pack);
        }
    }

    public struct StructWithoutExplicitStructLayout
    {
        public int x;
        public string y;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct StructWithExplicitStructLayout
    {
        [FieldOffset(0)]
        public byte x;

        [FieldOffset(1)]
        public short y;

        [FieldOffset(3)]
        public byte z;
    }

    public class ClassWithoutExplicitStructLayout
    {
        public int x;
        public string y;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 2, CharSet = CharSet.Unicode)]
    public class ClassWithExplicitStructLayout
    {
        [FieldOffset(0)]
        public byte x;

        [FieldOffset(1)]
        public short y;
    }
}