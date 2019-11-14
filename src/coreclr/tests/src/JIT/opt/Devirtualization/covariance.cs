// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

interface I<in T>
{
    int A(T t);
}

class X<T> : I<T>
{
    int c = 0;
    int I<T>.A(T t)
    {
        return ++c;
    }
}

class T
{
    static int F(I<string> i) 
    {
        return i.A("A");
    }

    public static int Main()
    {
        // Jit should inline F and then devirtualize
        // and inline the call to A.
        int j = F(new X<object>());
        return j + 99;
    }
}
