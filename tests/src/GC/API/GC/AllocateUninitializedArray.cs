// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Tests GC.Collect()

using System;

public class Test {


    public static int Main() {
        // allocate a bunch of SOH byte arrays and touch them.
        var r = new Random(1234);
        for (int i = 0; i < 10000; i++)
        {
            int size = r.Next(10000);
            var arr = AllocUninitialized<byte>.Call(size);

            if (size > 1)
            {
                arr[0] = 5;
                arr[size - 1] = 17;
                if (arr[0] != 5 || arr[size - 1] != 17)
                {
                    Console.WriteLine("Scenario 1 for GC.AllocUninitialized() failed!");
                    return 1;
                }
            }
        }

        // allocate a bunch of LOH int arrays and touch them.
        for (int i = 0; i < 1000; i++)
        {
            int size = r.Next(100000, 1000000);
            var arr = AllocUninitialized<int>.Call(size);

            arr[0] = 5;
            arr[size - 1] = 17;
            if (arr[0] != 5 || arr[size - 1] != 17)
            {
                Console.WriteLine("Scenario 2 for GC.AllocUninitialized() failed!");
                return 1;
            }
        }

        // allocate a string array
        {
            int i = 100;
            var arr = AllocUninitialized<string>.Call(i);

            arr[0] = "5";
            arr[i - 1] = "17";
            if (arr[0] != "5" || arr[i - 1] != "17")
            {
                Console.WriteLine("Scenario 3 for GC.AllocUninitialized() failed!");
                return 1;
            }
        }

        // allocate max size byte array
        {
            if (IntPtr.Size == 8)
            {
                int i = 0x7FFFFFC7;
                var arr = AllocUninitialized<byte>.Call(i);

                arr[0] = 5;
                arr[i - 1] = 17;
                if (arr[0] != 5 || arr[i - 1] != 17)
                {
                    Console.WriteLine("Scenario 4 for GC.AllocUninitialized() failed!");
                    return 1;
                }
            }
        }

        // negative size
        {
            try
            {
                var arr = AllocUninitialized<byte>.Call(-1);

                Console.WriteLine("Scenario 5 Expected exception!");
                return 1;
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        // too large
        {
            try
            {
                var arr = AllocUninitialized<double>.Call(int.MaxValue);

                Console.WriteLine("Scenario 6 Expected exception!");
                return 1;
            }
            catch (OutOfMemoryException)
            {
            }
        }


        Console.WriteLine("Test for GC.Collect() passed!");
        return 100;
    }

    //TODO: This should be removed once the API is public.
    static class AllocUninitialized<T>
    {
        public static Func<int, T[]> Call = (i) =>
        {
            // replace the stub with actual impl.
            Call = (Func<int, T[]>)typeof(System.GC).
            GetMethod("AllocateUninitializedArray",
                bindingAttr: System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                binder: null,
                new Type[] { typeof(int) },
                modifiers: new System.Reflection.ParameterModifier[0]).
            MakeGenericMethod(new Type[] { typeof(T) }).
            CreateDelegate(typeof(Func<int, T[]>));

            // call the impl.
            return Call(i);
        };
    }
}
