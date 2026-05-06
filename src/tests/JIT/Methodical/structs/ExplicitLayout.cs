// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class ExplicitLayout
{
#pragma warning disable 618
    [StructLayout(LayoutKind.Explicit, Size = SIZE)]
    internal unsafe struct TestStruct
    {
        public const int SIZE = 32;

        [FieldOffset(0)]
        private fixed byte _data[SIZE];

        [FieldOffset(0), MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
        public Guid Guid1;

        [FieldOffset(16), MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
        public Guid Guid2;
    }
#pragma warning restore 618

    [StructLayout(LayoutKind.Explicit)]
    public class ExplicitBase
    {
        [FieldOffset(8)] public object? m_objectField;
        [FieldOffset(0)] public double m_doubleField;

        public double DoubleValue
        {
            get => m_doubleField;
            set => m_doubleField = value;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public class EmptyExplicitClassDerivingFromExplicitClass : ExplicitBase
    {
    }

    public class AutoDerivingFromEmptyExplicitClass : EmptyExplicitClassDerivingFromExplicitClass
    {
        string MyStringField;

        public AutoDerivingFromEmptyExplicitClass(string fieldValue = "Default Value")
        {
            MyStringField = fieldValue;
        }

        public string GetMyStringField()
        {
            return MyStringField;
        }
    }

    public class Program
    {
        [Fact]
        [OuterLoop]
        public static void ExplicitLayoutStruct()
        {
            TestStruct t = new TestStruct();
            t.Guid1 = Guid.NewGuid();
            t.Guid2 = t.Guid1;

            Assert.Equal(t.Guid1, t.Guid2);

            TestStruct t2 = new TestStruct();
            Guid newGuid = Guid.NewGuid();
            t2.Guid1 = newGuid;
            t2.Guid2 = newGuid;

            Assert.Equal(t2.Guid1, t2.Guid2);
        }

        [Fact]
        public static void EmptyExplicitClass()
        {
            AutoDerivingFromEmptyExplicitClass emptyDirectBase = new("AutoDerivingFromEmptyExplicitClass");

            emptyDirectBase.DoubleValue = 17.0;

            Assert.Equal("AutoDerivingFromEmptyExplicitClass", emptyDirectBase.GetMyStringField());
        }
    }
}
