// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

public struct ValX0 { }
public struct ValY0 { }
public struct ValX1<T> { }
public struct ValY1<T> { }
public struct ValX2<T, U> { }
public struct ValY2<T, U> { }
public struct ValX3<T, U, V> { }
public struct ValY3<T, U, V> { }
public class RefX0 { }
public class RefY0 { }
public class RefX1<T> { }
public class RefY1<T> { }
public class RefX2<T, U> { }
public class RefY2<T, U> { }
public class RefX3<T, U, V> { }
public class RefY3<T, U, V> { }

public class GenBase<T, U, V>
{
    public T Fld1;
    public U Fld2;

    public V Fld3;

    public GenBase(T fld1, U fld2, V fld3)
    {
        Fld1 = fld1;
        Fld2 = fld2;
        Fld3 = fld3;

    }

    public bool InstVerify(System.Type t1, System.Type t2, System.Type t3)
    {
        bool result = true;

        if (!(Fld1.GetType().Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(GenBase<T, U, V>));
        }

        if (!(Fld2.GetType().Equals(t2)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld2 in: " + typeof(GenBase<T, U, V>));
        }

        if (!(Fld3.GetType().Equals(t3)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld3 in: " + typeof(GenBase<T, U, V>));
        }

        return result;
    }
}

public class Gen<T, U, V> : GenBase<T, U, V>
{
    public Gen(T fld1, U fld2, V fld3) : base(fld1, fld2, fld3) { }
    new public bool InstVerify(System.Type t1, System.Type t2, System.Type t3)
    {
        return base.InstVerify(t1, t2, t3);
    }
}
public class Test
{
    public static int counter = 0;
    public static bool result = true;
    public static void Eval(bool exp)
    {
        counter++;
        if (!exp)
        {
            result = exp;
            Console.WriteLine("Test Failed at location: " + counter);
        }

    }

    public static int Main()
    {
        Eval((new Gen<int, double, Guid>(new int(), new double(), new Guid())).InstVerify(typeof(int), typeof(double), typeof(Guid)));
        Eval((new Gen<double, Guid, string>(new double(), new Guid(), "string")).InstVerify(typeof(double), typeof(Guid), typeof(string)));
        Eval((new Gen<Guid, string, object>(new Guid(), "string", new object())).InstVerify(typeof(Guid), typeof(string), typeof(object)));
        Eval((new Gen<string, object, int[]>("string", new object(), new int[1])).InstVerify(typeof(string), typeof(object), typeof(int[])));
        Eval((new Gen<object, int[], RefX1<ValX1<int>>>(new object(), new int[1], new RefX1<ValX1<int>>())).InstVerify(typeof(object), typeof(int[]), typeof(RefX1<ValX1<int>>)));
        Eval((new Gen<int[], RefX1<ValX1<int>>, ValX1<RefX2<int, double>>>(new int[1], new RefX1<ValX1<int>>(), new ValX1<RefX2<int, double>>())).InstVerify(typeof(int[]), typeof(RefX1<ValX1<int>>), typeof(ValX1<RefX2<int, double>>)));

        if (result)
        {
            Console.WriteLine("Test Passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Test Failed");
            return 1;
        }
    }

}
