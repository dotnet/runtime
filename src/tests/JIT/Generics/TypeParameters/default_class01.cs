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


public class Gen<T>
{
    public bool DefaultTest(bool status)
    {
        bool result = true;
        T t = default(T);

        if (((object)t == null) != status)
        {
            result = false;
            Console.WriteLine("default(T) Failed for" + typeof(Gen<T>));
        }

        return result;
    }
}

public class Test_default_class01
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
        Eval(new Gen<int>().DefaultTest(false));
        Eval(new Gen<double>().DefaultTest(false));
        Eval(new Gen<string>().DefaultTest(true));
        Eval(new Gen<object>().DefaultTest(true));
        Eval(new Gen<Guid>().DefaultTest(false));

        Eval(new Gen<int[]>().DefaultTest(true));
        Eval(new Gen<double[,]>().DefaultTest(true));
        Eval(new Gen<string[][][]>().DefaultTest(true));
        Eval(new Gen<object[, , ,]>().DefaultTest(true));
        Eval(new Gen<Guid[][, , ,][]>().DefaultTest(true));

        Eval(new Gen<RefX1<int>[]>().DefaultTest(true));
        Eval(new Gen<RefX1<double>[,]>().DefaultTest(true));
        Eval(new Gen<RefX1<string>[][][]>().DefaultTest(true));
        Eval(new Gen<RefX1<object>[, , ,]>().DefaultTest(true));
        Eval(new Gen<RefX1<Guid>[][, , ,][]>().DefaultTest(true));
        Eval(new Gen<RefX2<int, int>[]>().DefaultTest(true));
        Eval(new Gen<RefX2<double, double>[,]>().DefaultTest(true));
        Eval(new Gen<RefX2<string, string>[][][]>().DefaultTest(true));
        Eval(new Gen<RefX2<object, object>[, , ,]>().DefaultTest(true));
        Eval(new Gen<RefX2<Guid, Guid>[][, , ,][]>().DefaultTest(true));

        Eval(new Gen<ValX1<int>[]>().DefaultTest(true));
        Eval(new Gen<ValX1<double>[,]>().DefaultTest(true));
        Eval(new Gen<ValX1<string>[][][]>().DefaultTest(true));
        Eval(new Gen<ValX1<object>[, , ,]>().DefaultTest(true));
        Eval(new Gen<ValX1<Guid>[][, , ,][]>().DefaultTest(true));

        Eval(new Gen<ValX2<int, int>[]>().DefaultTest(true));
        Eval(new Gen<ValX2<double, double>[,]>().DefaultTest(true));
        Eval(new Gen<ValX2<string, string>[][][]>().DefaultTest(true));
        Eval(new Gen<ValX2<object, object>[, , ,]>().DefaultTest(true));
        Eval(new Gen<ValX2<Guid, Guid>[][, , ,][]>().DefaultTest(true));

        Eval(new Gen<RefX1<int>>().DefaultTest(true));
        Eval(new Gen<RefX1<ValX1<int>>>().DefaultTest(true));
        Eval(new Gen<RefX2<int, string>>().DefaultTest(true));
        Eval(new Gen<RefX3<int, string, Guid>>().DefaultTest(true));

        Eval(new Gen<RefX1<RefX1<int>>>().DefaultTest(true));
        Eval(new Gen<RefX1<RefX1<RefX1<string>>>>().DefaultTest(true));
        Eval(new Gen<RefX1<RefX1<RefX1<RefX1<Guid>>>>>().DefaultTest(true));

        Eval(new Gen<RefX1<RefX2<int, string>>>().DefaultTest(true));
        Eval(new Gen<RefX2<RefX2<RefX1<int>, RefX3<int, string, RefX1<RefX2<int, string>>>>, RefX2<RefX1<int>, RefX3<int, string, RefX1<RefX2<int, string>>>>>>().DefaultTest(true));
        Eval(new Gen<RefX3<RefX1<int[][, , ,]>, RefX2<object[, , ,][][], Guid[][][]>, RefX3<double[, , , , , , , , , ,], Guid[][][][, , , ,][, , , ,][][][], string[][][][][][][][][][][]>>>().DefaultTest(true));

        Eval(new Gen<ValX1<int>>().DefaultTest(false));
        Eval(new Gen<ValX1<RefX1<int>>>().DefaultTest(false));
        Eval(new Gen<ValX2<int, string>>().DefaultTest(false));
        Eval(new Gen<ValX3<int, string, Guid>>().DefaultTest(false));

        Eval(new Gen<ValX1<ValX1<int>>>().DefaultTest(false));
        Eval(new Gen<ValX1<ValX1<ValX1<string>>>>().DefaultTest(false));
        Eval(new Gen<ValX1<ValX1<ValX1<ValX1<Guid>>>>>().DefaultTest(false));

        Eval(new Gen<ValX1<ValX2<int, string>>>().DefaultTest(false));
        Eval(new Gen<ValX2<ValX2<ValX1<int>, ValX3<int, string, ValX1<ValX2<int, string>>>>, ValX2<ValX1<int>, ValX3<int, string, ValX1<ValX2<int, string>>>>>>().DefaultTest(false));
        Eval(new Gen<ValX3<ValX1<int[][, , ,]>, ValX2<object[, , ,][][], Guid[][][]>, ValX3<double[, , , , , , , , , ,], Guid[][][][, , , ,][, , , ,][][][], string[][][][][][][][][][][]>>>().DefaultTest(false));



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
