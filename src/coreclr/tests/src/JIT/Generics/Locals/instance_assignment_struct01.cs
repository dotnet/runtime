// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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


public struct Gen<T>
{
    public T Assign(T t)
    {
        T Fld1 = t;
        return Fld1;
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

        int _int = 1;
        Eval(new Gen<int>().Assign(_int).Equals(_int));

        double _double = 1;
        Eval(new Gen<double>().Assign(_double).Equals(_double));

        string _string = "string";
        Eval(new Gen<string>().Assign(_string).Equals(_string));

        object _object = new object();
        Eval(new Gen<object>().Assign(_object).Equals(_object));

        Guid _Guid = new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        Eval(new Gen<Guid>().Assign(_Guid).Equals(_Guid));

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
