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
    public static bool EqualNull(T t)
    {
        return ((object)t == null);
    }
}

public class Test_static_equalnull_class01
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

        int _int = 0;
        Eval(false == Gen<int>.EqualNull(_int));

        double _double = 0;
        Eval(false == Gen<double>.EqualNull(_double));

        Guid _Guid = new Guid();
        Eval(false == Gen<Guid>.EqualNull(_Guid));

        string _string = "string";
        Eval(false == Gen<string>.EqualNull(_string));
        Eval(true == Gen<string>.EqualNull(null));

        object _object = new object();
        Eval(false == Gen<object>.EqualNull(_string));
        Eval(true == Gen<object>.EqualNull(null));

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
