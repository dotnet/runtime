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
    public void AssignRef(T tin, ref T tref)
    {
        tref = tin;
    }

    public void AssignOut(T tin, out T tout)
    {
        tout = tin;
    }
}

public class Test_instance_assignment_class01
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

        int _int1 = 1;
        int _int2 = 2;
        new Gen<int>().AssignRef(_int1, ref _int2);
        Eval(_int1.Equals(_int2));
        _int2 = 2;
        new Gen<int>().AssignOut(_int1, out _int2);
        Eval(_int1.Equals(_int2));

        double _double1 = 1;
        double _double2 = 2;
        new Gen<double>().AssignRef(_double1, ref _double2);
        Eval(_double1.Equals(_double2));
        _double2 = 2;
        new Gen<double>().AssignOut(_double1, out _double2);
        Eval(_double1.Equals(_double2));

        string _string1 = "string1";
        string _string2 = "string2";
        new Gen<string>().AssignRef(_string1, ref _string2);
        Eval(_string1.Equals(_string2));
        _string2 = "string2";
        new Gen<string>().AssignOut(_string1, out _string2);
        Eval(_string1.Equals(_string2));

        object _object1 = (object)_int1;
        object _object2 = (object)_string2;
        new Gen<object>().AssignRef(_object1, ref _object2);
        Eval(_object1.Equals(_object2));
        _object2 = (object)_string2;
        new Gen<object>().AssignOut(_object1, out _object2);
        Eval(_object1.Equals(_object2));

        Guid _Guid1 = new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        Guid _Guid2 = new Guid(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
        new Gen<Guid>().AssignRef(_Guid1, ref _Guid2);
        Eval(_Guid1.Equals(_Guid2));
        _Guid2 = new Guid(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
        new Gen<Guid>().AssignOut(_Guid1, out _Guid2);
        Eval(_Guid1.Equals(_Guid2));

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
