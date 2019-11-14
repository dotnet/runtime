// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.freach.freach001.freach001
{
    // <Title> Dynamic in Foreach </Title>
    // <Description>
    //  Conversions between elements in foreach expressions to the type of the identifier in the foreach loop
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        // Implicit and Explicit Numeric Conversions. Test will throw a runtime exception if it fails.
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int i = 0;
            sbyte[] x1 = new sbyte[]
            {
            1, 2, 3
            }

            ;
            byte[] x2 = new byte[]
            {
            1, 2, 3
            }

            ;
            short[] x3 = new short[]
            {
            1, 2, 3
            }

            ;
            ushort[] x4 = new ushort[]
            {
            1, 2, 3
            }

            ;
            int[] x5 = new int[]
            {
            1, 2, 3
            }

            ;
            uint[] x6 = new uint[]
            {
            1, 2, 3
            }

            ;
            long[] x7 = new long[]
            {
            1, 2, 3
            }

            ;
            ulong[] x8 = new ulong[]
            {
            1, 2, 3
            }

            ;
            char[] x9 = new char[]
            {
            '1', '2', '3'
            }

            ;
            float[] x10 = new float[]
            {
            1.1f, 2.2f, 3.3f
            }

            ;
            double[] x11 = new double[]
            {
            1.1, 2.2, 3.35
            }

            ;
            decimal[] x12 = new decimal[]
            {
            1.1m, 22.2m, 33.3m
            }

            ;
            // IMPLICIT NUMERIC CONVERSIONS
            // sybte to short, int, long, float, double, decimal
            foreach (short y in (dynamic)x1)
                i++;
            foreach (int y in (dynamic)x1)
                i++;
            foreach (long y in (dynamic)x1)
                i++;
            foreach (float y in (dynamic)x1)
                i++;
            foreach (double y in (dynamic)x1)
                i++;
            foreach (decimal y in (dynamic)x1)
                i++;
            // byte to short, ushort, int, uint, long, ulong, float, double, decimal
            foreach (short y in (dynamic)x2)
                i++;
            foreach (ushort y in (dynamic)x2)
                i++;
            foreach (int y in (dynamic)x2)
                i++;
            foreach (uint y in (dynamic)x2)
                i++;
            foreach (long y in (dynamic)x2)
                i++;
            foreach (ulong y in (dynamic)x2)
                i++;
            foreach (float y in (dynamic)x2)
                i++;
            foreach (double y in (dynamic)x2)
                i++;
            foreach (decimal y in (dynamic)x2)
                i++;
            // short to int, long, float, double, decimal
            foreach (int y in (dynamic)x3)
                i++;
            foreach (long y in (dynamic)x3)
                i++;
            foreach (float y in (dynamic)x3)
                i++;
            foreach (double y in (dynamic)x3)
                i++;
            foreach (decimal y in (dynamic)x3)
                i++;
            // ushort to int, uint, long, ulong, float, double, decimal
            foreach (int y in (dynamic)x4)
                i++;
            foreach (uint y in (dynamic)x4)
                i++;
            foreach (long y in (dynamic)x4)
                i++;
            foreach (ulong y in (dynamic)x4)
                i++;
            foreach (float y in (dynamic)x4)
                i++;
            foreach (double y in (dynamic)x4)
                i++;
            foreach (decimal y in (dynamic)x4)
                i++;
            // int to long, float, double, decimal
            foreach (long y in (dynamic)x5)
                i++;
            foreach (float y in (dynamic)x5)
                i++;
            foreach (double y in (dynamic)x5)
                i++;
            foreach (decimal y in (dynamic)x5)
                i++;
            // uint to long, ulong, float, double, decimal
            foreach (long y in (dynamic)x6)
                i++;
            foreach (ulong y in (dynamic)x6)
                i++;
            foreach (float y in (dynamic)x6)
                i++;
            foreach (double y in (dynamic)x6)
                i++;
            foreach (decimal y in (dynamic)x6)
                i++;
            // long to float, double, decimal
            foreach (float y in (dynamic)x7)
                i++;
            foreach (double y in (dynamic)x7)
                i++;
            foreach (decimal y in (dynamic)x7)
                i++;
            // ulong to float, double, decimal
            foreach (float y in (dynamic)x8)
                i++;
            foreach (double y in (dynamic)x8)
                i++;
            foreach (decimal y in (dynamic)x8)
                i++;
            // char to ushort, int, uint, long, ulong, float, double, decimal
            foreach (ushort y in (dynamic)x9)
                i++;
            foreach (int y in (dynamic)x9)
                i++;
            foreach (uint y in (dynamic)x9)
                i++;
            foreach (long y in (dynamic)x9)
                i++;
            foreach (ulong y in (dynamic)x9)
                i++;
            foreach (float y in (dynamic)x9)
                i++;
            foreach (double y in (dynamic)x9)
                i++;
            foreach (decimal y in (dynamic)x9)
                i++;
            // float to double
            foreach (double y in (dynamic)x10)
                i++;
            // EXPLICIT NUMERIC CONVERSIONS
            // sbyte to byte, ushort, uint, ulong char
            foreach (byte y in (dynamic)x1)
                i++;
            foreach (ushort y in (dynamic)x1)
                i++;
            foreach (uint y in (dynamic)x1)
                i++;
            foreach (ulong y in (dynamic)x1)
                i++;
            foreach (char y in (dynamic)x1)
                i++;
            // byte to sbyte, char
            foreach (sbyte y in (dynamic)x2)
                i++;
            foreach (char y in (dynamic)x2)
                i++;
            // short to sbyte, byte, ushort, uint, ulong, char
            foreach (sbyte y in (dynamic)x3)
                i++;
            foreach (byte y in (dynamic)x3)
                i++;
            foreach (ushort y in (dynamic)x3)
                i++;
            foreach (uint y in (dynamic)x3)
                i++;
            foreach (ulong y in (dynamic)x3)
                i++;
            foreach (char y in (dynamic)x3)
                i++;
            // ushort to sbyte, byte, short, char
            foreach (sbyte y in (dynamic)x4)
                i++;
            foreach (byte y in (dynamic)x4)
                i++;
            foreach (short y in (dynamic)x4)
                i++;
            foreach (char y in (dynamic)x4)
                i++;
            // int to sbyte, byte, short, ushort, uint, ulong, char
            foreach (sbyte y in (dynamic)x5)
                i++;
            foreach (byte y in (dynamic)x5)
                i++;
            foreach (short y in (dynamic)x5)
                i++;
            foreach (ushort y in (dynamic)x5)
                i++;
            foreach (uint y in (dynamic)x5)
                i++;
            foreach (ulong y in (dynamic)x5)
                i++;
            foreach (char y in (dynamic)x5)
                i++;
            // uint to sbyte, byte, short, ushort, int, char
            foreach (sbyte y in (dynamic)x6)
                i++;
            foreach (byte y in (dynamic)x6)
                i++;
            foreach (short y in (dynamic)x6)
                i++;
            foreach (ushort y in (dynamic)x6)
                i++;
            foreach (int y in (dynamic)x6)
                i++;
            foreach (char y in (dynamic)x6)
                i++;
            // long to sbyte, byte, short, ushort, int, uint, ulong, char
            foreach (sbyte y in (dynamic)x7)
                i++;
            foreach (byte y in (dynamic)x7)
                i++;
            foreach (short y in (dynamic)x7)
                i++;
            foreach (ushort y in (dynamic)x7)
                i++;
            foreach (int y in (dynamic)x7)
                i++;
            foreach (uint y in (dynamic)x7)
                i++;
            foreach (ulong y in (dynamic)x7)
                i++;
            foreach (char y in (dynamic)x7)
                i++;
            // ulong to sbyte, byte, short, ushort, int, uint, ulong, char
            foreach (sbyte y in (dynamic)x8)
                i++;
            foreach (byte y in (dynamic)x8)
                i++;
            foreach (short y in (dynamic)x8)
                i++;
            foreach (ushort y in (dynamic)x8)
                i++;
            foreach (int y in (dynamic)x8)
                i++;
            foreach (uint y in (dynamic)x8)
                i++;
            foreach (ulong y in (dynamic)x8)
                i++;
            foreach (char y in (dynamic)x8)
                i++;
            // char to sbyte, byte, short
            foreach (sbyte y in (dynamic)x9)
                i++;
            foreach (byte y in (dynamic)x9)
                i++;
            foreach (short y in (dynamic)x9)
                i++;
            // float to sybte, byte, short, ushort, int, uint, long, ulong, char, decimal
            foreach (sbyte y in (dynamic)x10)
                i++;
            foreach (byte y in (dynamic)x10)
                i++;
            foreach (short y in (dynamic)x10)
                i++;
            foreach (ushort y in (dynamic)x10)
                i++;
            foreach (int y in (dynamic)x10)
                i++;
            foreach (uint y in (dynamic)x10)
                i++;
            foreach (long y in (dynamic)x10)
                i++;
            foreach (ulong y in (dynamic)x10)
                i++;
            foreach (char y in (dynamic)x10)
                i++;
            foreach (decimal y in (dynamic)x10)
                i++;
            // double to sbyte, short, ushort, int, uint, long, ulong, char, float, decimal
            foreach (sbyte y in (dynamic)x11)
                i++;
            foreach (short y in (dynamic)x11)
                i++;
            foreach (ushort y in (dynamic)x11)
                i++;
            foreach (int y in (dynamic)x11)
                i++;
            foreach (uint y in (dynamic)x11)
                i++;
            foreach (long y in (dynamic)x11)
                i++;
            foreach (ulong y in (dynamic)x11)
                i++;
            foreach (char y in (dynamic)x11)
                i++;
            foreach (float y in (dynamic)x11)
                i++;
            foreach (decimal y in (dynamic)x11)
                i++;
            // decimal to sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double
            foreach (sbyte y in (dynamic)x12)
                i++;
            foreach (byte y in (dynamic)x12)
                i++;
            foreach (short y in (dynamic)x12)
                i++;
            foreach (ushort y in (dynamic)x12)
                i++;
            foreach (int y in (dynamic)x12)
                i++;
            foreach (uint y in (dynamic)x12)
                i++;
            foreach (long y in (dynamic)x12)
                i++;
            foreach (ulong y in (dynamic)x12)
                i++;
            foreach (char y in (dynamic)x12)
                i++;
            foreach (float y in (dynamic)x12)
                i++;
            foreach (double y in (dynamic)x12)
                i++;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.freach.freach002.freach002
{
    // <Title> Dynamic in Foreach </Title>
    // <Description>
    //  Conversions between elements in foreach expressions to the type of the identifier in the foreach loop
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class A
    {
    }

    public class B : A
    {
    }

    public interface I1
    {
    }

    public interface I2 : I1
    {
    }

    public class CI1 : I1
    {
    }

    public class CI2 : I2
    {
    }

    public class Test
    {
        // Implicit and Explicit Reference Conversions. Test will throw a runtime exception if it fails.
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int i = 0;
            // IMPLICIT REFERENCE CONVERSIONS
            // Reference type to object
            Test[] x1 = new Test[]
            {
            new Test(), new Test()}

            ;
            foreach (object y in (dynamic)x1)
                i++;
            // Class-Type S to Class-Type T, S is derived from T
            B[] x2 = new B[]
            {
            new B(), new B()}

            ;
            foreach (A y in (dynamic)x2)
                i++;
            // Class-Type S to Interface-Type T, S implements T
            CI1[] x3 = new CI1[]
            {
            new CI1(), new CI1()}

            ;
            foreach (I1 y in (dynamic)x3)
                i++;
            // Interface-Type S to Interface-Type T, S is derived from T
            I2[] x4 = new I2[]
            {
            new CI2(), new CI2()}

            ;
            foreach (I1 y in (dynamic)x4)
                i++;
            // From array-type to System.Array
            int[][] x5 = new int[][]
            {
            new int[]
            {
            1, 2, 3
            }

            , new int[]
            {
            4, 5, 6
            }
            }

            ;
            foreach (System.Array y in (dynamic)x5)
                i++;
            // EXPLICIT REFERENCE CONVERSIONS
            // object to reference-type
            object[] xr1 = new object[]
            {
            new Test(), new Test()}

            ;
            foreach (Test y in (dynamic)xr1)
                i++;
            // Class-Type S to Class-Type T, S is base class from T
            A[] xr2 = new A[]
            {
            new B(), new B()}

            ;
            foreach (B y in (dynamic)xr2)
                i++;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.freach.freach003.freach003
{
    // <Title> Dynamic in Foreach </Title>
    // <Description>
    //  Conversions between elements in foreach expressions to the type of the identifier in the foreach loop
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public struct S
    {
    }

    public class Test
    {
        // Boxing and Unboxing Conversions. Test will throw a runtime exception if it fails.
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int i = 0;
            // Boxing Conversions
            int[] x1 = new int[]
            {
            1, 2, 3
            }

            ;
            S[] x2 = new S[]
            {
            new S(), new S()}

            ;
            decimal[] x3 = new decimal[]
            {
            1m, 2m, 3m
            }

            ;
            int?[] x4 = new int?[]
            {
            1, 2, 3
            }

            ;
            uint?[] x5 = new uint?[]
            {
            1, 2, 3
            }

            ;
            decimal?[] x6 = new decimal?[]
            {
            1m, 2m, 3m
            }

            ;
            // Boxing to object
            foreach (object y in (dynamic)x1)
                i++;
            foreach (object y in (dynamic)x2)
                i++;
            foreach (object y in (dynamic)x3)
                i++;
            foreach (object y in (dynamic)x4)
                i++;
            foreach (object y in (dynamic)x5)
                i++;
            foreach (object y in (dynamic)x6)
                i++;
            // Boxing to System.ValueType
            foreach (System.ValueType y in (dynamic)x1)
                i++;
            foreach (System.ValueType y in (dynamic)x2)
                i++;
            foreach (System.ValueType y in (dynamic)x3)
                i++;
            foreach (System.ValueType y in (dynamic)x4)
                i++;
            foreach (System.ValueType y in (dynamic)x5)
                i++;
            foreach (System.ValueType y in (dynamic)x6)
                i++;
            // Unboxing Conversions
            object[] xo1 = new object[]
            {
            1, 2, 3
            }

            ;
            object[] xo2 = new object[]
            {
            new S(), new S()}

            ;
            object[] xo3 = new object[]
            {
            1m, 2m, 3m
            }

            ;
            object[] xo4 = new object[]
            {
            (int ? )1, (int ? )2, (int ? )3
            }

            ;
            object[] xo5 = new object[]
            {
            (uint ? )1, (uint ? )2, (uint ? )3
            }

            ;
            object[] xo6 = new object[]
            {
            (decimal ? )1m, (decimal ? )2m, (decimal ? )3m
            }

            ;
            // Unboxing from object
            foreach (object y in (dynamic)xo1)
                i++;
            foreach (object y in (dynamic)xo2)
                i++;
            foreach (object y in (dynamic)xo3)
                i++;
            foreach (object y in (dynamic)xo4)
                i++;
            foreach (object y in (dynamic)xo5)
                i++;
            foreach (object y in (dynamic)xo6)
                i++;
            // Unboxing Conversions
            System.ValueType[] xv1 = new System.ValueType[]
            {
            1, 2, 3
            }

            ;
            System.ValueType[] xv2 = new System.ValueType[]
            {
            new S(), new S()}

            ;
            System.ValueType[] xv3 = new System.ValueType[]
            {
            1m, 2m, 3m
            }

            ;
            System.ValueType[] xv4 = new System.ValueType[]
            {
            (int ? )1, (int ? )2, (int ? )3
            }

            ;
            System.ValueType[] xv5 = new System.ValueType[]
            {
            (uint ? )1, (uint ? )2, (uint ? )3
            }

            ;
            System.ValueType[] xv6 = new System.ValueType[]
            {
            (decimal ? )1m, (decimal ? )2m, (decimal ? )3m
            }

            ;
            // Unboxing from System.ValueType
            foreach (System.ValueType y in (dynamic)xv1)
                i++;
            foreach (System.ValueType y in (dynamic)xv2)
                i++;
            foreach (System.ValueType y in (dynamic)xv3)
                i++;
            foreach (System.ValueType y in (dynamic)xv4)
                i++;
            foreach (System.ValueType y in (dynamic)xv5)
                i++;
            foreach (System.ValueType y in (dynamic)xv6)
                i++;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.freach.freach004.freach004
{
    // <Title> Dynamic in Foreach </Title>
    // <Description>
    //  Conversions between elements in foreach expressions to the type of the identifier in the foreach loop
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public enum color
    {
        Red,
        Blue,
        Green
    }

    public enum cars
    {
        Toyota,
        Lexus,
        BMW
    }

    public class Test
    {
        // Explicit Enum Conversions. Test will throw a runtime exception if it fails.
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int i = 0;
            sbyte[] x1 = new sbyte[]
            {
            1, 2, 3
            }

            ;
            byte[] x2 = new byte[]
            {
            1, 2, 3
            }

            ;
            short[] x3 = new short[]
            {
            1, 2, 3
            }

            ;
            ushort[] x4 = new ushort[]
            {
            1, 2, 3
            }

            ;
            int[] x5 = new int[]
            {
            1, 2, 3
            }

            ;
            uint[] x6 = new uint[]
            {
            1, 2, 3
            }

            ;
            long[] x7 = new long[]
            {
            1, 2, 3
            }

            ;
            ulong[] x8 = new ulong[]
            {
            1, 2, 3
            }

            ;
            char[] x9 = new char[]
            {
            '1', '2', '3'
            }

            ;
            float[] x10 = new float[]
            {
            1.1f, 2.2f, 3.3f
            }

            ;
            double[] x11 = new double[]
            {
            1.1, 2.2, 3.35
            }

            ;
            decimal[] x12 = new decimal[]
            {
            1.1m, 22.2m, 33.3m
            }

            ;
            color[] x13 = new color[]
            {
            color.Red, color.Green
            }

            ;
            cars[] x14 = new cars[]
            {
            cars.Toyota, cars.BMW
            }

            ;
            // From sybte, byte, short, ushort, int, uint, long, ulong, char, float, double, decimal to enum-type
            foreach (color y in (dynamic)x1)
                i++;
            foreach (color y in (dynamic)x2)
                i++;
            foreach (color y in (dynamic)x3)
                i++;
            foreach (color y in (dynamic)x4)
                i++;
            foreach (color y in (dynamic)x5)
                i++;
            foreach (color y in (dynamic)x6)
                i++;
            foreach (color y in (dynamic)x7)
                i++;
            foreach (color y in (dynamic)x8)
                i++;
            foreach (color y in (dynamic)x9)
                i++;
            foreach (color y in (dynamic)x10)
                i++;
            foreach (color y in (dynamic)x11)
                i++;
            foreach (color y in (dynamic)x12)
                i++;
            // From enum type to sybte, byte, short, ushort, int, uint, long, ulong, char, float, double, decimal
            foreach (sbyte y in (dynamic)x13)
                i++;
            foreach (byte y in (dynamic)x13)
                i++;
            foreach (short y in (dynamic)x13)
                i++;
            foreach (ushort y in (dynamic)x13)
                i++;
            foreach (int y in (dynamic)x13)
                i++;
            foreach (uint y in (dynamic)x13)
                i++;
            foreach (long y in (dynamic)x13)
                i++;
            foreach (ulong y in (dynamic)x13)
                i++;
            foreach (char y in (dynamic)x13)
                i++;
            foreach (float y in (dynamic)x13)
                i++;
            foreach (double y in (dynamic)x13)
                i++;
            foreach (decimal y in (dynamic)x13)
                i++;
            // From one enum type to another enum type
            foreach (color y in (dynamic)x14)
                i++;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.freach.freach005.freach005
{
    // <Title> Dynamic in Foreach </Title>
    // <Description>
    //  Conversions between elements in foreach expressions to the type of the identifier in the foreach loop
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static implicit operator int (Test t)
        {
            return 5;
        }

        public static explicit operator decimal (Test t)
        {
            return 10m;
        }

        // Explicit/Implicit User-defined Conversions. Test will throw a runtime exception if it fails.
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int i = 0;
            Test[] x1 = new Test[]
            {
            new Test(), new Test()}

            ;
            // User-defined Implicit conversions
            foreach (int y in (dynamic)x1)
                i++;
            foreach (long y in (dynamic)x1)
                i++;
            // User-defined Explicit conversions
            foreach (decimal y in (dynamic)x1)
                i++;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.freach.freach006.freach006
{
    // <Title> Dynamic in Foreach </Title>
    // <Description>
    //  Conversions between elements in foreach expressions to the type of the identifier in the foreach loop
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        // Nested foreach statements. Test will throw runtime exception if it fails.
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int i = 0;
            int[][] v1 = new int[][]
            {
            new int[]
            {
            4, 5
            }

            , new int[]
            {
            1, 2, 3
            }
            }

            ;
            // Nested foreach statements
            foreach (dynamic y in (dynamic)v1)
            {
                i++;
                foreach (long z in y)
                    i++;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.freach.freach007.freach007
{
    // <Title> Dynamic in Foreach </Title>
    // <Description>
    //  Conversions between elements in foreach expressions to the type of the identifier in the foreach loop
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        // Implicit and Explicit Nullable Numeric Conversions. Test will throw a runtime exception if it fails.
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int i = 0;
            sbyte?[] x1 = new sbyte?[]
            {
            1, 2, 3
            }

            ;
            byte?[] x2 = new byte?[]
            {
            1, 2, 3
            }

            ;
            short?[] x3 = new short?[]
            {
            1, 2, 3
            }

            ;
            ushort?[] x4 = new ushort?[]
            {
            1, 2, 3
            }

            ;
            int?[] x5 = new int?[]
            {
            1, 2, 3
            }

            ;
            uint?[] x6 = new uint?[]
            {
            1, 2, 3
            }

            ;
            long?[] x7 = new long?[]
            {
            1, 2, 3
            }

            ;
            ulong?[] x8 = new ulong?[]
            {
            1, 2, 3
            }

            ;
            char?[] x9 = new char?[]
            {
            '1', '2', '3'
            }

            ;
            float?[] x10 = new float?[]
            {
            1.1f, 2.2f, 3.3f
            }

            ;
            double?[] x11 = new double?[]
            {
            1.1, 2.2, 3.35
            }

            ;
            decimal?[] x12 = new decimal?[]
            {
            1.1m, 22.2m, 33.3m
            }

            ;
            // IMPLICIT NUMERIC CONVERSIONS
            // sybte to short, int, long, float, double, decimal
            foreach (short? y in (dynamic)x1)
                i++;
            foreach (int? y in (dynamic)x1)
                i++;
            foreach (long? y in (dynamic)x1)
                i++;
            foreach (float? y in (dynamic)x1)
                i++;
            foreach (double? y in (dynamic)x1)
                i++;
            foreach (decimal? y in (dynamic)x1)
                i++;
            // byte to short, ushort, int, uint, long, ulong, float, double, decimal
            foreach (short? y in (dynamic)x2)
                i++;
            foreach (ushort? y in (dynamic)x2)
                i++;
            foreach (int? y in (dynamic)x2)
                i++;
            foreach (uint? y in (dynamic)x2)
                i++;
            foreach (long? y in (dynamic)x2)
                i++;
            foreach (ulong? y in (dynamic)x2)
                i++;
            foreach (float? y in (dynamic)x2)
                i++;
            foreach (double? y in (dynamic)x2)
                i++;
            foreach (decimal? y in (dynamic)x2)
                i++;
            // short to int, long, float, double, decimal
            foreach (int? y in (dynamic)x3)
                i++;
            foreach (long? y in (dynamic)x3)
                i++;
            foreach (float? y in (dynamic)x3)
                i++;
            foreach (double? y in (dynamic)x3)
                i++;
            foreach (decimal? y in (dynamic)x3)
                i++;
            // ushort to int, uint, long, ulong, float, double, decimal
            foreach (int? y in (dynamic)x4)
                i++;
            foreach (uint? y in (dynamic)x4)
                i++;
            foreach (long? y in (dynamic)x4)
                i++;
            foreach (ulong? y in (dynamic)x4)
                i++;
            foreach (float? y in (dynamic)x4)
                i++;
            foreach (double? y in (dynamic)x4)
                i++;
            foreach (decimal? y in (dynamic)x4)
                i++;
            // int to long, float, double, decimal
            foreach (long? y in (dynamic)x5)
                i++;
            foreach (float? y in (dynamic)x5)
                i++;
            foreach (double? y in (dynamic)x5)
                i++;
            foreach (decimal? y in (dynamic)x5)
                i++;
            // uint to long, ulong, float, double, decimal
            foreach (long? y in (dynamic)x6)
                i++;
            foreach (ulong? y in (dynamic)x6)
                i++;
            foreach (float? y in (dynamic)x6)
                i++;
            foreach (double? y in (dynamic)x6)
                i++;
            foreach (decimal? y in (dynamic)x6)
                i++;
            // long to float, double, decimal
            foreach (float? y in (dynamic)x7)
                i++;
            foreach (double? y in (dynamic)x7)
                i++;
            foreach (decimal? y in (dynamic)x7)
                i++;
            // ulong to float, double, decimal
            foreach (float? y in (dynamic)x8)
                i++;
            foreach (double? y in (dynamic)x8)
                i++;
            foreach (decimal? y in (dynamic)x8)
                i++;
            // char to ushort, int, uint, long, ulong, float, double, decimal
            foreach (ushort? y in (dynamic)x9)
                i++;
            foreach (int? y in (dynamic)x9)
                i++;
            foreach (uint? y in (dynamic)x9)
                i++;
            foreach (long? y in (dynamic)x9)
                i++;
            foreach (ulong? y in (dynamic)x9)
                i++;
            foreach (float? y in (dynamic)x9)
                i++;
            foreach (double? y in (dynamic)x9)
                i++;
            foreach (decimal? y in (dynamic)x9)
                i++;
            // float to double
            foreach (double y in (dynamic)x10)
                i++;
            // EXPLICIT NUMERIC CONVERSIONS
            // sbyte to byte, ushort, uint, ulong char
            foreach (byte? y in (dynamic)x1)
                i++;
            foreach (ushort? y in (dynamic)x1)
                i++;
            foreach (uint? y in (dynamic)x1)
                i++;
            foreach (ulong? y in (dynamic)x1)
                i++;
            foreach (char? y in (dynamic)x1)
                i++;
            // byte to sbyte, char
            foreach (sbyte? y in (dynamic)x2)
                i++;
            foreach (char? y in (dynamic)x2)
                i++;
            // short to sbyte, byte, ushort, uint, ulong, char
            foreach (sbyte? y in (dynamic)x3)
                i++;
            foreach (byte? y in (dynamic)x3)
                i++;
            foreach (ushort? y in (dynamic)x3)
                i++;
            foreach (uint? y in (dynamic)x3)
                i++;
            foreach (ulong? y in (dynamic)x3)
                i++;
            foreach (char? y in (dynamic)x3)
                i++;
            // ushort to sbyte, byte, short, char
            foreach (sbyte? y in (dynamic)x4)
                i++;
            foreach (byte? y in (dynamic)x4)
                i++;
            foreach (short? y in (dynamic)x4)
                i++;
            foreach (char? y in (dynamic)x4)
                i++;
            // int to sbyte, byte, short, ushort, uint, ulong, char
            foreach (sbyte? y in (dynamic)x5)
                i++;
            foreach (byte? y in (dynamic)x5)
                i++;
            foreach (short? y in (dynamic)x5)
                i++;
            foreach (ushort? y in (dynamic)x5)
                i++;
            foreach (uint? y in (dynamic)x5)
                i++;
            foreach (ulong? y in (dynamic)x5)
                i++;
            foreach (char? y in (dynamic)x5)
                i++;
            // uint to sbyte, byte, short, ushort, int, char
            foreach (sbyte? y in (dynamic)x6)
                i++;
            foreach (byte? y in (dynamic)x6)
                i++;
            foreach (short? y in (dynamic)x6)
                i++;
            foreach (ushort? y in (dynamic)x6)
                i++;
            foreach (int? y in (dynamic)x6)
                i++;
            foreach (char? y in (dynamic)x6)
                i++;
            // long to sbyte, byte, short, ushort, int, uint, ulong, char
            foreach (sbyte? y in (dynamic)x7)
                i++;
            foreach (byte? y in (dynamic)x7)
                i++;
            foreach (short? y in (dynamic)x7)
                i++;
            foreach (ushort? y in (dynamic)x7)
                i++;
            foreach (int? y in (dynamic)x7)
                i++;
            foreach (uint? y in (dynamic)x7)
                i++;
            foreach (ulong? y in (dynamic)x7)
                i++;
            foreach (char? y in (dynamic)x7)
                i++;
            // ulong to sbyte, byte, short, ushort, int, uint, ulong, char
            foreach (sbyte? y in (dynamic)x8)
                i++;
            foreach (byte? y in (dynamic)x8)
                i++;
            foreach (short? y in (dynamic)x8)
                i++;
            foreach (ushort? y in (dynamic)x8)
                i++;
            foreach (int? y in (dynamic)x8)
                i++;
            foreach (uint? y in (dynamic)x8)
                i++;
            foreach (ulong? y in (dynamic)x8)
                i++;
            foreach (char? y in (dynamic)x8)
                i++;
            // char to sbyte, byte, short
            foreach (sbyte? y in (dynamic)x9)
                i++;
            foreach (byte? y in (dynamic)x9)
                i++;
            foreach (short? y in (dynamic)x9)
                i++;
            // float to sybte, byte, short, ushort, int, uint, long, ulong, char, decimal
            foreach (sbyte? y in (dynamic)x10)
                i++;
            foreach (byte? y in (dynamic)x10)
                i++;
            foreach (short? y in (dynamic)x10)
                i++;
            foreach (ushort? y in (dynamic)x10)
                i++;
            foreach (int? y in (dynamic)x10)
                i++;
            foreach (uint? y in (dynamic)x10)
                i++;
            foreach (long? y in (dynamic)x10)
                i++;
            foreach (ulong? y in (dynamic)x10)
                i++;
            foreach (char? y in (dynamic)x10)
                i++;
            foreach (decimal? y in (dynamic)x10)
                i++;
            // double to sbyte, short, ushort, int, uint, long, ulong, char, float, decimal
            foreach (sbyte? y in (dynamic)x11)
                i++;
            foreach (short? y in (dynamic)x11)
                i++;
            foreach (ushort? y in (dynamic)x11)
                i++;
            foreach (int? y in (dynamic)x11)
                i++;
            foreach (uint? y in (dynamic)x11)
                i++;
            foreach (long? y in (dynamic)x11)
                i++;
            foreach (ulong? y in (dynamic)x11)
                i++;
            foreach (char? y in (dynamic)x11)
                i++;
            foreach (float? y in (dynamic)x11)
                i++;
            foreach (decimal? y in (dynamic)x11)
                i++;
            // decimal to sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double
            foreach (sbyte? y in (dynamic)x12)
                i++;
            foreach (byte? y in (dynamic)x12)
                i++;
            foreach (short? y in (dynamic)x12)
                i++;
            foreach (ushort? y in (dynamic)x12)
                i++;
            foreach (int? y in (dynamic)x12)
                i++;
            foreach (uint? y in (dynamic)x12)
                i++;
            foreach (long? y in (dynamic)x12)
                i++;
            foreach (ulong? y in (dynamic)x12)
                i++;
            foreach (char? y in (dynamic)x12)
                i++;
            foreach (float? y in (dynamic)x12)
                i++;
            foreach (double? y in (dynamic)x12)
                i++;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.freach.freach008.freach008
{
    // <Title> Dynamic in Foreach </Title>
    // <Description>
    //  Conversions between elements in foreach expressions to the type of the identifier in the foreach loop
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections;

    public class MyCollection : IEnumerable
    {
        private int[] _items;
        public MyCollection()
        {
            _items = new int[5]
            {
            1, 4, 3, 2, 5
            }

            ;
        }

        public IEnumerator GetEnumerator()
        {
            return new MyEnumerator(this);
        }

        public class MyEnumerator : IEnumerator
        {
            private int _nIndex;
            private MyCollection _collection;
            public MyEnumerator(MyCollection coll)
            {
                _collection = coll;
                _nIndex = -1;
            }

            public bool MoveNext()
            {
                _nIndex++;
                return (_nIndex < _collection._items.GetLength(0));
            }

            public void Reset()
            {
                _nIndex = -1;
            }

            public dynamic Current
            {
                get
                {
                    return (_collection._items[_nIndex]);
                }
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyCollection col = new MyCollection();
            int[] expected = new int[] { 1, 4, 3, 2, 5 };
            int index = 0;

            foreach (int i in col)
            {
                if (i != expected[index])
                {
                    return 1;
                }

                index++;
            }

            return index - expected.Length;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.freach.freach009.freach009
{
    // <Title> Dynamic in Foreach </Title>
    // <Description>
    //  Conversions between elements in foreach expressions to the type of the identifier in the foreach loop
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections;

    public class MyCollection : IEnumerable
    {
        private int[] _items;
        public MyCollection()
        {
            _items = new int[5]
            {
            1, 4, 3, 2, 5
            }

            ;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new MyEnumerator(this);
        }

        public class MyEnumerator : IEnumerator
        {
            private int _nIndex;
            private MyCollection _collection;
            public MyEnumerator(MyCollection coll)
            {
                _collection = coll;
                _nIndex = -1;
            }

            bool IEnumerator.MoveNext()
            {
                _nIndex++;
                return (_nIndex < _collection._items.GetLength(0));
            }

            void IEnumerator.Reset()
            {
                _nIndex = -1;
            }

            dynamic IEnumerator.Current
            {
                get
                {
                    return (_collection._items[_nIndex]);
                }
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyCollection col = new MyCollection();
            int[] expected = new int[] { 1, 4, 3, 2, 5 };
            int index = 0;

            foreach (int i in col)
            {
                if (i != expected[index])
                {
                    return 1;
                }

                index++;
            }

            return index - expected.Length;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.freach.freach010.freach010
{
    // <Title> Dynamic in Foreach </Title>
    // <Description>
    //  Conversions between elements in foreach expressions to the type of the identifier in the foreach loop
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections;

    public class MyCollection : IEnumerable
    {
        private int[] _items;
        public MyCollection()
        {
            _items = new int[5]
            {
            1, 4, 3, 2, 6
            }

            ;
        }

        public MyEnumerator GetEnumerator()
        {
            return new MyEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public class MyEnumerator : IEnumerator
        {
            private int _index;
            private MyCollection _collection;
            public MyEnumerator(MyCollection coll)
            {
                _collection = coll;
                _index = -1;
            }

            public bool MoveNext()
            {
                _index++;
                return (_index < _collection._items.GetLength(0));
            }

            public void Reset()
            {
                _index = -1;
            }

            public dynamic Current
            {
                get
                {
                    return (_collection._items[_index]);
                }
            }

            dynamic IEnumerator.Current
            {
                get
                {
                    return (Current);
                }
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MyCollection col = new MyCollection();
            byte[] expected = new byte[] { 1, 4, 3, 2, 6 };
            int index = 0;
            foreach (byte i in col)
            {
                if (i != expected[index])
                {
                    return 1;
                }

                index++;
            }

            return index - expected.Length;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.freach.freach011.freach011
{
    // <Title> Dynamic in Foreach </Title>
    // <Description>
    //  Conversions between elements in foreach expressions to the type of the identifier in the foreach loop
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class GenC<T>
    {
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            int flag = 1;
            flag = 1;
            dynamic darr = new string[2]
            {
            "aa", "bb"
            }

            ;
            try
            {
                foreach (int v in darr)
                {
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoExplicitConv, ex.Message, "string", "int"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            dynamic darr2 = new string[2];
            try
            {
                foreach (int v in darr2)
                {
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.ValueCantBeNull, ex.Message, "int"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 0;
            dynamic darr3 = new GenC<string>[2];
            foreach (GenC<int> v in darr3)
            {
                if (v != null)
                    flag++;
            }

            result += flag;
            return result;
        }
    }
    // </Code>
}
