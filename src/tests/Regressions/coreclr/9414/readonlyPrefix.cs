// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public class Program
{
    interface IFrobber
    {
        void Frob();
    }

    class Frobber : IFrobber
    {
        public void Frob()
        {
        }
    }

    class Foo<T> where T : IFrobber
    {
        public static void FrobAll(T[,] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                // 'readonly' prefix on call to array's Address method must be respected, and the type check bypassed
                arr[0, i].Frob();
            }
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Foo<IFrobber>.FrobAll(new Frobber[,] { { new Frobber() } });
    }
}
