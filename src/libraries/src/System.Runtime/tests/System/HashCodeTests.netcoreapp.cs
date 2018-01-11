// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

public static class HashCodeTests
{
    [Fact]
    public static void HashCode_Add()
    {
        // The version of xUnit used by corefx does not support params theories.
        void Theory(uint expected, params uint[] vector)
        {
            var hc = new HashCode();
            for (int i = 0; i < vector.Length; i++)
                hc.Add(vector[i]);

#if SYSTEM_HASHCODE_TESTVECTORS
            // HashCode is not deterministic across AppDomains by design. This means
            // that these tests can not be executed against the version that exists
            // within CoreCLR. Copy HashCode and set m_seed to 0 in order to execute
            // these tests.
        
            Assert.Equal(expected, (uint)hc.ToHashCode());
#else
            // Validate that the HashCode.m_seed is randomized. This has a 1 in 4
            // billion chance of resulting in a false negative, as HashCode.m_seed
            // can be 0.

            Assert.NotEqual(expected, (uint)hc.ToHashCode());
#endif
        }

        // These test vectors were created using https://asecuritysite.com/encryption/xxHash
        // 1. Find the hash for "".
        // 2. Find the hash for "abcd". ASCII "abcd" and bit convert to uint.
        // 3. Find the hash for "abcd1234". ASCII [ "abcd", "1234"] and bit convert to 2 uints.
        // n. Continue until "abcd0123efgh4567ijkl8901mnop2345qrst6789uvwx0123yzab".

        Theory(0x02cc5d05U);
        Theory(0xa3643705U, 0x64636261U );
        Theory(0x4603e94cU, 0x64636261U, 0x33323130U );
        Theory(0xd8a1e80fU, 0x64636261U, 0x33323130U, 0x68676665U );
        Theory(0x4b62a7cfU, 0x64636261U, 0x33323130U, 0x68676665U, 0x37363534U );
        Theory(0xc33a7641U, 0x64636261U, 0x33323130U, 0x68676665U, 0x37363534U, 0x6c6b6a69U );
        Theory(0x1a794705U, 0x64636261U, 0x33323130U, 0x68676665U, 0x37363534U, 0x6c6b6a69U, 0x31303938U );
        Theory(0x4d79177dU, 0x64636261U, 0x33323130U, 0x68676665U, 0x37363534U, 0x6c6b6a69U, 0x31303938U, 0x706f6e6dU );
        Theory(0x59d79205U, 0x64636261U, 0x33323130U, 0x68676665U, 0x37363534U, 0x6c6b6a69U, 0x31303938U, 0x706f6e6dU, 0x35343332U );
        Theory(0x49585aaeU, 0x64636261U, 0x33323130U, 0x68676665U, 0x37363534U, 0x6c6b6a69U, 0x31303938U, 0x706f6e6dU, 0x35343332U, 0x74737271U );
        Theory(0x2f005ff1U, 0x64636261U, 0x33323130U, 0x68676665U, 0x37363534U, 0x6c6b6a69U, 0x31303938U, 0x706f6e6dU, 0x35343332U, 0x74737271U, 0x39383736U );
        Theory(0x0ce339bdU, 0x64636261U, 0x33323130U, 0x68676665U, 0x37363534U, 0x6c6b6a69U, 0x31303938U, 0x706f6e6dU, 0x35343332U, 0x74737271U, 0x39383736U, 0x78777675U );
        Theory(0xb31bd2ffU, 0x64636261U, 0x33323130U, 0x68676665U, 0x37363534U, 0x6c6b6a69U, 0x31303938U, 0x706f6e6dU, 0x35343332U, 0x74737271U, 0x39383736U, 0x78777675U, 0x33323130U );
        Theory(0xa821efa3U, 0x64636261U, 0x33323130U, 0x68676665U, 0x37363534U, 0x6c6b6a69U, 0x31303938U, 0x706f6e6dU, 0x35343332U, 0x74737271U, 0x39383736U, 0x78777675U, 0x33323130U, 0x62617a79U );
    }

    [Fact]
    public static void HashCode_Add_HashCode()
    {        
        var hc1 = new HashCode();
        hc1.Add("Hello");

        var hc2 = new HashCode();
        hc2.Add("Hello".GetHashCode());

        Assert.Equal(hc1.ToHashCode(), hc2.ToHashCode());
    }

    [Fact]
    public static void HashCode_Add_Generic()
    {
        var hc = new HashCode();
        hc.Add(1);
        hc.Add(new ConstHashCodeType());

        var expected = new HashCode();
        expected.Add(1);
        expected.Add(ConstComparer.ConstantValue);

        Assert.Equal(expected.ToHashCode(), hc.ToHashCode());
    }

    [Fact]
    public static void HashCode_Add_Null()
    {
        var hc = new HashCode();
        hc.Add<string>(null);

        var expected = new HashCode();
        expected.Add(EqualityComparer<string>.Default.GetHashCode(null));

        Assert.Equal(expected.ToHashCode(), hc.ToHashCode());
    }

    [Fact]
    public static void HashCode_Add_GenericEqualityComparer()
    {
        var hc = new HashCode();
        hc.Add(1);
        hc.Add("Hello", new ConstComparer());

        var expected = new HashCode();
        expected.Add(1);
        expected.Add(ConstComparer.ConstantValue);

        Assert.Equal(expected.ToHashCode(), hc.ToHashCode());
    }

    [Fact]
    public static void HashCode_Add_NullEqualityComparer()
    {
        var hc = new HashCode();
        hc.Add(1);
        hc.Add("Hello", null);

        var expected = new HashCode();
        expected.Add(1);
        expected.Add("Hello");

        Assert.Equal(expected.ToHashCode(), hc.ToHashCode());
    }

    [Fact]
    public static void HashCode_Combine()
    {
        var hcs = new int[]
        {
            HashCode.Combine(1),
            HashCode.Combine(1, 2),
            HashCode.Combine(1, 2, 3),
            HashCode.Combine(1, 2, 3, 4),
            HashCode.Combine(1, 2, 3, 4, 5),
            HashCode.Combine(1, 2, 3, 4, 5, 6),
            HashCode.Combine(1, 2, 3, 4, 5, 6, 7),
            HashCode.Combine(1, 2, 3, 4, 5, 6, 7, 8),

            HashCode.Combine(2),
            HashCode.Combine(2, 3),
            HashCode.Combine(2, 3, 4),
            HashCode.Combine(2, 3, 4, 5),
            HashCode.Combine(2, 3, 4, 5, 6),
            HashCode.Combine(2, 3, 4, 5, 6, 7),
            HashCode.Combine(2, 3, 4, 5, 6, 7, 8),
            HashCode.Combine(2, 3, 4, 5, 6, 7, 8, 9),
        };

        for (int i = 0; i < hcs.Length; i++)
            for (int j = 0; j < hcs.Length; j++)
            {
                if (i == j) continue;
                Assert.NotEqual(hcs[i], hcs[j]);
            }
    }

    [Fact]
    public static void HashCode_Combine_Add_1()
    {
        var hc = new HashCode();
        hc.Add(1);
        Assert.Equal(hc.ToHashCode(), HashCode.Combine(1));
    }

    [Fact]
    public static void HashCode_Combine_Add_2()
    {
        var hc = new HashCode();
        hc.Add(1);
        hc.Add(2);
        Assert.Equal(hc.ToHashCode(), HashCode.Combine(1, 2));
    }

    [Fact]
    public static void HashCode_Combine_Add_3()
    {
        var hc = new HashCode();
        hc.Add(1);
        hc.Add(2);
        hc.Add(3);
        Assert.Equal(hc.ToHashCode(), HashCode.Combine(1, 2, 3));
    }

    [Fact]
    public static void HashCode_Combine_Add_4()
    {
        var hc = new HashCode();
        hc.Add(1);
        hc.Add(2);
        hc.Add(3);
        hc.Add(4);
        Assert.Equal(hc.ToHashCode(), HashCode.Combine(1, 2, 3, 4));
    }

    [Fact]
    public static void HashCode_Combine_Add_5()
    {
        var hc = new HashCode();
        hc.Add(1);
        hc.Add(2);
        hc.Add(3);
        hc.Add(4);
        hc.Add(5);
        Assert.Equal(hc.ToHashCode(), HashCode.Combine(1, 2, 3, 4, 5));
    }

    [Fact]
    public static void HashCode_Combine_Add_6()
    {
        var hc = new HashCode();
        hc.Add(1);
        hc.Add(2);
        hc.Add(3);
        hc.Add(4);
        hc.Add(5);
        hc.Add(6);
        Assert.Equal(hc.ToHashCode(), HashCode.Combine(1, 2, 3, 4, 5, 6));
    }

    [Fact]
    public static void HashCode_Combine_Add_7()
    {
        var hc = new HashCode();
        hc.Add(1);
        hc.Add(2);
        hc.Add(3);
        hc.Add(4);
        hc.Add(5);
        hc.Add(6);
        hc.Add(7);
        Assert.Equal(hc.ToHashCode(), HashCode.Combine(1, 2, 3, 4, 5, 6, 7));
    }

    [Fact]
    public static void HashCode_Combine_Add_8()
    {
        var hc = new HashCode();
        hc.Add(1);
        hc.Add(2);
        hc.Add(3);
        hc.Add(4);
        hc.Add(5);
        hc.Add(6);
        hc.Add(7);
        hc.Add(8);
        Assert.Equal(hc.ToHashCode(), HashCode.Combine(1, 2, 3, 4, 5, 6, 7, 8));
    }

    [Fact]
    public static void HashCode_GetHashCode()
    {
        var hc = new HashCode();

        Assert.Throws<NotSupportedException>(() => hc.GetHashCode());
    }

    [Fact]
    public static void HashCode_Equals()
    {
        var hc = new HashCode();

        Assert.Throws<NotSupportedException>(() => hc.Equals(hc));
    }

    [Fact]
    public static void HashCode_GetHashCode_Boxed()
    {
        var hc = new HashCode();
        var obj = (object)hc;

        Assert.Throws<NotSupportedException>(() => obj.GetHashCode());
    }

    [Fact]
    public static void HashCode_Equals_Boxed()
    {
        var hc = new HashCode();
        var obj = (object)hc;

        Assert.Throws<NotSupportedException>(() => obj.Equals(obj));
    }

    public class ConstComparer : System.Collections.Generic.IEqualityComparer<string>
    {
        public const int ConstantValue = 1234;

        public bool Equals(string x, string y) => false;
        public int GetHashCode(string obj) => ConstantValue;
    }

    public class ConstHashCodeType
    {
        public override int GetHashCode() => ConstComparer.ConstantValue;
    }
}
