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


public class Gen<T>
{
    public object Box(T t)
    {
        return (object)t;
    }

    public T Unbox(object obj)
    {
        return (T)obj;
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
        Eval(new Gen<int>().Unbox(new Gen<int>().Box(1)).Equals(1));
        Eval(new Gen<double>().Unbox(new Gen<double>().Box(1.111)).Equals(1.111));
        Eval(new Gen<string>().Unbox(new Gen<string>().Box("boxme")).Equals("boxme"));
        Eval(new Gen<Guid>().Unbox(new Gen<Guid>().Box(new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11))).Equals(new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)));

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
