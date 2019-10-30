// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public interface I<W> 
{ 
    string Method(); 
}
public class MyBase<U, V> : I<U> 
{ 
    string I<U>.Method()
    { return "MyBase.Method()"; } 
}
public class MyClass<T> : MyBase<string, T>, I<T> 
{ 
    string I<T>.Method()
    { return "MyClass.Method()"; }
}

class Test
{
    public static int Main() 
    {
        MyClass<string> s1 = new MyClass<string>();
        MyClass<object> s2 = new MyClass<object>();

        string res1 = ((I<string>)s1).Method();
        string res2 = ((I<string>)s2).Method();
        string res3 = ((I<object>)s2).Method();
        Console.WriteLine(res1);
        Console.WriteLine(res2);
        Console.WriteLine(res3);

        if (res1 == "MyClass.Method()" && res2 == "MyBase.Method()" && res3 == "MyClass.Method()")
        {
            Console.WriteLine("Pass");
            return 100;
        }
        Console.WriteLine("FAIL");
        return -1;
    }
}
