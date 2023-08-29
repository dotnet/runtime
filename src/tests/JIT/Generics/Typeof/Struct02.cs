// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

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


public struct Gen<T, U>
{
    public T Fld1;
    public U Fld2;

    public Gen(T fld1, U fld2)
    {
        Fld1 = fld1;
        Fld2 = fld2;
    }

    public bool InstVerify(System.Type t1, System.Type t2)
    {
        bool result = true;

        if (!(typeof(T).Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(Gen<T, U>));
        }

        if (!(typeof(U).Equals(t2)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld2 in: " + typeof(Gen<T, U>));
        }

        return result;
    }
}

public class Test_Struct02
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

    [Fact]
    public static int TestEntryPoint()
    {
        Eval((new Gen<int, int>(new int(), new int())).InstVerify(typeof(int), typeof(int)));
        Eval((new Gen<int, double>(new int(), new double())).InstVerify(typeof(int), typeof(double)));
        Eval((new Gen<int, string>(new int(), "string")).InstVerify(typeof(int), typeof(string)));
        Eval((new Gen<int, object>(new int(), new object())).InstVerify(typeof(int), typeof(object)));
        Eval((new Gen<int, Guid>(new int(), new Guid())).InstVerify(typeof(int), typeof(Guid)));
        Eval((new Gen<int, RefX1<int>>(new int(), new RefX1<int>())).InstVerify(typeof(int), typeof(RefX1<int>)));
        Eval((new Gen<int, RefX1<string>>(new int(), new RefX1<string>())).InstVerify(typeof(int), typeof(RefX1<string>)));
        Eval((new Gen<int, RefX1<int[][, , ,][]>>(new int(), new RefX1<int[][, , ,][]>())).InstVerify(typeof(int), typeof(RefX1<int[][, , ,][]>)));
        Eval((new Gen<int, ValX1<int>>(new int(), new ValX1<int>())).InstVerify(typeof(int), typeof(ValX1<int>)));
        Eval((new Gen<int, ValX1<string>>(new int(), new ValX1<string>())).InstVerify(typeof(int), typeof(ValX1<string>)));
        Eval((new Gen<int, ValX1<int[][, , ,][]>>(new int(), new ValX1<int[][, , ,][]>())).InstVerify(typeof(int), typeof(ValX1<int[][, , ,][]>)));

        Eval((new Gen<double, int>(new double(), new int())).InstVerify(typeof(double), typeof(int)));
        Eval((new Gen<double, double>(new double(), new double())).InstVerify(typeof(double), typeof(double)));
        Eval((new Gen<double, string>(new double(), "string")).InstVerify(typeof(double), typeof(string)));
        Eval((new Gen<double, object>(new double(), new object())).InstVerify(typeof(double), typeof(object)));
        Eval((new Gen<double, Guid>(new double(), new Guid())).InstVerify(typeof(double), typeof(Guid)));
        Eval((new Gen<double, RefX1<double>>(new double(), new RefX1<double>())).InstVerify(typeof(double), typeof(RefX1<double>)));
        Eval((new Gen<double, RefX1<string>>(new double(), new RefX1<string>())).InstVerify(typeof(double), typeof(RefX1<string>)));
        Eval((new Gen<double, RefX1<double[][, , ,][]>>(new double(), new RefX1<double[][, , ,][]>())).InstVerify(typeof(double), typeof(RefX1<double[][, , ,][]>)));
        Eval((new Gen<double, ValX1<double>>(new double(), new ValX1<double>())).InstVerify(typeof(double), typeof(ValX1<double>)));
        Eval((new Gen<double, ValX1<string>>(new double(), new ValX1<string>())).InstVerify(typeof(double), typeof(ValX1<string>)));
        Eval((new Gen<double, ValX1<double[][, , ,][]>>(new double(), new ValX1<double[][, , ,][]>())).InstVerify(typeof(double), typeof(ValX1<double[][, , ,][]>)));

        Eval((new Gen<string, int>("string", new int())).InstVerify(typeof(string), typeof(int)));
        Eval((new Gen<string, double>("string", new double())).InstVerify(typeof(string), typeof(double)));
        Eval((new Gen<string, string>("string", "string")).InstVerify(typeof(string), typeof(string)));
        Eval((new Gen<string, object>("string", new object())).InstVerify(typeof(string), typeof(object)));
        Eval((new Gen<string, Guid>("string", new Guid())).InstVerify(typeof(string), typeof(Guid)));
        Eval((new Gen<string, RefX1<string>>("string", new RefX1<string>())).InstVerify(typeof(string), typeof(RefX1<string>)));
        Eval((new Gen<string, RefX1<string>>("string", new RefX1<string>())).InstVerify(typeof(string), typeof(RefX1<string>)));
        Eval((new Gen<string, RefX1<string[][, , ,][]>>("string", new RefX1<string[][, , ,][]>())).InstVerify(typeof(string), typeof(RefX1<string[][, , ,][]>)));
        Eval((new Gen<string, ValX1<string>>("string", new ValX1<string>())).InstVerify(typeof(string), typeof(ValX1<string>)));
        Eval((new Gen<string, ValX1<string>>("string", new ValX1<string>())).InstVerify(typeof(string), typeof(ValX1<string>)));
        Eval((new Gen<string, ValX1<string[][, , ,][]>>("string", new ValX1<string[][, , ,][]>())).InstVerify(typeof(string), typeof(ValX1<string[][, , ,][]>)));

        Eval((new Gen<object, int>(new object(), new int())).InstVerify(typeof(object), typeof(int)));
        Eval((new Gen<object, double>(new object(), new double())).InstVerify(typeof(object), typeof(double)));
        Eval((new Gen<object, string>(new object(), "string")).InstVerify(typeof(object), typeof(string)));
        Eval((new Gen<object, object>(new object(), new object())).InstVerify(typeof(object), typeof(object)));
        Eval((new Gen<object, Guid>(new object(), new Guid())).InstVerify(typeof(object), typeof(Guid)));
        Eval((new Gen<object, RefX1<object>>(new object(), new RefX1<object>())).InstVerify(typeof(object), typeof(RefX1<object>)));
        Eval((new Gen<object, RefX1<string>>(new object(), new RefX1<string>())).InstVerify(typeof(object), typeof(RefX1<string>)));
        Eval((new Gen<object, RefX1<object[][, , ,][]>>(new object(), new RefX1<object[][, , ,][]>())).InstVerify(typeof(object), typeof(RefX1<object[][, , ,][]>)));
        Eval((new Gen<object, ValX1<object>>(new object(), new ValX1<object>())).InstVerify(typeof(object), typeof(ValX1<object>)));
        Eval((new Gen<object, ValX1<string>>(new object(), new ValX1<string>())).InstVerify(typeof(object), typeof(ValX1<string>)));
        Eval((new Gen<object, ValX1<object[][, , ,][]>>(new object(), new ValX1<object[][, , ,][]>())).InstVerify(typeof(object), typeof(ValX1<object[][, , ,][]>)));

        Eval((new Gen<Guid, int>(new Guid(), new int())).InstVerify(typeof(Guid), typeof(int)));
        Eval((new Gen<Guid, double>(new Guid(), new double())).InstVerify(typeof(Guid), typeof(double)));
        Eval((new Gen<Guid, string>(new Guid(), "string")).InstVerify(typeof(Guid), typeof(string)));
        Eval((new Gen<Guid, object>(new Guid(), new object())).InstVerify(typeof(Guid), typeof(object)));
        Eval((new Gen<Guid, Guid>(new Guid(), new Guid())).InstVerify(typeof(Guid), typeof(Guid)));
        Eval((new Gen<Guid, RefX1<Guid>>(new Guid(), new RefX1<Guid>())).InstVerify(typeof(Guid), typeof(RefX1<Guid>)));
        Eval((new Gen<Guid, RefX1<string>>(new Guid(), new RefX1<string>())).InstVerify(typeof(Guid), typeof(RefX1<string>)));
        Eval((new Gen<Guid, RefX1<Guid[][, , ,][]>>(new Guid(), new RefX1<Guid[][, , ,][]>())).InstVerify(typeof(Guid), typeof(RefX1<Guid[][, , ,][]>)));
        Eval((new Gen<Guid, ValX1<Guid>>(new Guid(), new ValX1<Guid>())).InstVerify(typeof(Guid), typeof(ValX1<Guid>)));
        Eval((new Gen<Guid, ValX1<string>>(new Guid(), new ValX1<string>())).InstVerify(typeof(Guid), typeof(ValX1<string>)));
        Eval((new Gen<Guid, ValX1<Guid[][, , ,][]>>(new Guid(), new ValX1<Guid[][, , ,][]>())).InstVerify(typeof(Guid), typeof(ValX1<Guid[][, , ,][]>)));

        Eval((new Gen<RefX1<int>, int>(new RefX1<int>(), new int())).InstVerify(typeof(RefX1<int>), typeof(int)));
        Eval((new Gen<RefX1<long>, double>(new RefX1<long>(), new double())).InstVerify(typeof(RefX1<long>), typeof(double)));
        Eval((new Gen<RefX1<long>, string>(new RefX1<long>(), "string")).InstVerify(typeof(RefX1<long>), typeof(string)));
        Eval((new Gen<RefX1<long>, object>(new RefX1<long>(), new object())).InstVerify(typeof(RefX1<long>), typeof(object)));
        Eval((new Gen<RefX1<long>, Guid>(new RefX1<long>(), new Guid())).InstVerify(typeof(RefX1<long>), typeof(Guid)));
        Eval((new Gen<RefX1<long>, RefX1<RefX1<long>>>(new RefX1<long>(), new RefX1<RefX1<long>>())).InstVerify(typeof(RefX1<long>), typeof(RefX1<RefX1<long>>)));
        Eval((new Gen<RefX1<long>, RefX1<string>>(new RefX1<long>(), new RefX1<string>())).InstVerify(typeof(RefX1<long>), typeof(RefX1<string>)));
        Eval((new Gen<RefX1<long>, RefX1<RefX1<long[][, , ,][]>>>(new RefX1<long>(), new RefX1<RefX1<long[][, , ,][]>>())).InstVerify(typeof(RefX1<long>), typeof(RefX1<RefX1<long[][, , ,][]>>)));
        Eval((new Gen<RefX1<long>, ValX1<RefX1<long>>>(new RefX1<long>(), new ValX1<RefX1<long>>())).InstVerify(typeof(RefX1<long>), typeof(ValX1<RefX1<long>>)));
        Eval((new Gen<RefX1<long>, ValX1<string>>(new RefX1<long>(), new ValX1<string>())).InstVerify(typeof(RefX1<long>), typeof(ValX1<string>)));
        Eval((new Gen<RefX1<long>, ValX1<RefX1<long>[][, , ,][]>>(new RefX1<long>(), new ValX1<RefX1<long>[][, , ,][]>())).InstVerify(typeof(RefX1<long>), typeof(ValX1<RefX1<long>[][, , ,][]>)));

        Eval((new Gen<ValX1<string>, int>(new ValX1<string>(), new int())).InstVerify(typeof(ValX1<string>), typeof(int)));
        Eval((new Gen<ValX1<string>, double>(new ValX1<string>(), new double())).InstVerify(typeof(ValX1<string>), typeof(double)));
        Eval((new Gen<ValX1<string>, string>(new ValX1<string>(), "string")).InstVerify(typeof(ValX1<string>), typeof(string)));
        Eval((new Gen<ValX1<string>, object>(new ValX1<string>(), new object())).InstVerify(typeof(ValX1<string>), typeof(object)));
        Eval((new Gen<ValX1<string>, Guid>(new ValX1<string>(), new Guid())).InstVerify(typeof(ValX1<string>), typeof(Guid)));
        Eval((new Gen<ValX1<string>, RefX1<ValX1<string>>>(new ValX1<string>(), new RefX1<ValX1<string>>())).InstVerify(typeof(ValX1<string>), typeof(RefX1<ValX1<string>>)));
        Eval((new Gen<ValX1<string>, RefX1<string>>(new ValX1<string>(), new RefX1<string>())).InstVerify(typeof(ValX1<string>), typeof(RefX1<string>)));
        Eval((new Gen<ValX1<string>, RefX1<ValX1<string>[][, , ,][]>>(new ValX1<string>(), new RefX1<ValX1<string>[][, , ,][]>())).InstVerify(typeof(ValX1<string>), typeof(RefX1<ValX1<string>[][, , ,][]>)));
        Eval((new Gen<ValX1<string>, ValX1<ValX1<string>>>(new ValX1<string>(), new ValX1<ValX1<string>>())).InstVerify(typeof(ValX1<string>), typeof(ValX1<ValX1<string>>)));
        Eval((new Gen<ValX1<string>, ValX1<string>>(new ValX1<string>(), new ValX1<string>())).InstVerify(typeof(ValX1<string>), typeof(ValX1<string>)));
        Eval((new Gen<ValX1<string>, ValX1<ValX1<string>[][, , ,][]>>(new ValX1<string>(), new ValX1<ValX1<string>[][, , ,][]>())).InstVerify(typeof(ValX1<string>), typeof(ValX1<ValX1<string>[][, , ,][]>)));

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
