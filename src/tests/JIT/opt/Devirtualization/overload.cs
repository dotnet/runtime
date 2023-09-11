// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public interface Io<T,U> where T:class where U:class
{
    T FromU(U u);
    T FromS(string s);
}

public class Z : Io<string, string>
{
    string Io<string, string>.FromU(string s) { return "U"; }
    string Io<string, string>.FromS(string s) { return "S"; }

    [Fact]
    public static int TestEntryPoint()
    {
        string fromU = ((Io<string, string>) new Z()).FromU("u");
        string fromS = ((Io<string, string>) new Z()).FromS("s");

        return fromU[0] + fromS[0] - 68;
    }
}


