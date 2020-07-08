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


public struct Gen<T>
{
    public static T Fld1;
    public static T Fld2;

    public T PassAsIn(T t)
    {
        return t;
    }

    public T PassAsRef(ref T t)
    {
        T temp = t;
        t = Fld2;
        return temp;
    }

    public void PassAsOut(out T t)
    {
        t = Fld2;
    }
    public void PassAsParameter(T t1, T t2)
    {
        Fld1 = t1;
        Fld2 = t2;

        T temp = t1;

        Test.Eval(Fld1.Equals(PassAsIn(temp)));
        Test.Eval(Fld1.Equals(PassAsRef(ref temp)));
        Test.Eval(Fld2.Equals(temp));
        temp = t1;
        PassAsOut(out temp);
        Test.Eval(Fld2.Equals(temp));
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

        int _int1 = 1;
        int _int2 = -1;
        new Gen<int>().PassAsParameter(_int1, _int2);

        double _double1 = 1;
        double _double2 = -1;
        new Gen<double>().PassAsParameter(_double1, _double2);

        string _string1 = "string1";
        string _string2 = "string2";
        new Gen<string>().PassAsParameter(_string1, _string2);

        object _object1 = (object)_string1;
        object _object2 = (object)_string2;
        new Gen<object>().PassAsParameter(_object1, _object2);

        Guid _Guid1 = new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        Guid _Guid2 = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
        new Gen<Guid>().PassAsParameter(_Guid1, _Guid2);

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
