// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests GC.GetGeneration for boxed-parameters
// should box parameter into an Object

using System;

public struct StructType {
}

public enum EnumType {
}

public class Test {

    public static int Main() {
        // literals
        int gen = GC.GetGeneration(-1);
        Console.WriteLine(gen);

        gen = GC.GetGeneration("hello");
        Console.WriteLine(gen);

        // integral types
        gen = GC.GetGeneration(new int());
        Console.WriteLine(gen);

        gen = GC.GetGeneration(new byte());
        Console.WriteLine(gen);

        gen = GC.GetGeneration(new sbyte());
        Console.WriteLine(gen);

        gen = GC.GetGeneration(new short());
        Console.WriteLine(gen);

        gen = GC.GetGeneration(new ushort());
        Console.WriteLine(gen);

        gen = GC.GetGeneration(new uint());
        Console.WriteLine(gen);

        gen = GC.GetGeneration(new long());
        Console.WriteLine(gen);

        gen = GC.GetGeneration(new ulong());
        Console.WriteLine(gen);

        gen = GC.GetGeneration(new char());
        Console.WriteLine(gen);

        //floating point types
        gen = GC.GetGeneration(new float());
        Console.WriteLine(gen);

        gen = GC.GetGeneration(new double());
        Console.WriteLine(gen);

        // boolean types
        gen = GC.GetGeneration(new bool());
        Console.WriteLine(gen);

        // other value types

        gen = GC.GetGeneration(new StructType());
        Console.WriteLine(gen);

        gen = GC.GetGeneration(new EnumType());
        Console.WriteLine(gen);

        gen = GC.GetGeneration(new decimal());
        Console.WriteLine(gen);

        Console.WriteLine("Test passed");
        return 100;
    }
}
