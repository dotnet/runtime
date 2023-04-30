// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;


public interface IGetContents<T> {
    (string, int, T) GetContents();
}
public struct MyStruct<T> : IGetContents<T> {
    public string s;
    public int a;
    public T t;

    public (string, int, T) GetContents()
    {
        return (s, a, t);
    }
}

public class Program {

    public delegate (string, int, T) MyDelegate<T>(IGetContents<T> arg);

    public static int Main(string[] args)
    {
        int retVal = 100;

        try {
            MyStruct<string> myStruct = new MyStruct<string>();
            myStruct.s = "test1";
            myStruct.a = 42;
            myStruct.t = "test2";

            MethodInfo mi = typeof(IGetContents<string>).GetMethod("GetContents");
            MyDelegate<string> func = (MyDelegate<string>)mi.CreateDelegate(typeof(MyDelegate<string>));

            (string c1, int c2, string c3) = func(myStruct);
            if (c1 != "test1")
                retVal = 1;
            if (c2 != 42)
                retVal = 2;
            if (c3 != "test2")
                retVal = 3;
        } catch (Exception e) {
            Console.WriteLine(e);
            retVal = 1;
        }

        return retVal;
    }
}
