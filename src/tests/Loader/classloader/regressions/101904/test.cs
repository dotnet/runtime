// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test should be run with verification on: e.g. caspol -m -cg 1.1 Everything
// In this test, we have MyType implementing IFoo<T> twice, first indirectly through MyBaseType
// as IFoo<string>, and second directly as IFoo<int>.
// In the end, a MyType<string,int> should be assignable to an IFoo<string> or an IFoo<int>.
using System;

public interface IFoo<T>{
}

public class MyBaseType<T> : IFoo<T>{
}

public class MyType<S,T> : MyBaseType<S>, IFoo<T>{
}

public class CMain{
    public static int Main(){
        MyType<string,int> mt = new MyType<string,int>();
        IFoo<int> f = mt;
        Console.WriteLine("PASS"); // if we make this far, we passed.
        return 100;
    }
}
