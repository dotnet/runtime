// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public struct G_VA<T>
{
    public static int S_I;
    public int I;
    public T MyT;
    public G_VA(int i, T t)
    {
        I = i;
        S_I += i;
        MyT = t;
    }
}

public class G_CA<T>
{
    public static int S_I;
    public int I;
    public T MyT;
    public G_CA(int i, T t)
    {
        I = i;
        S_I += i;
        MyT = t;
    }
}


