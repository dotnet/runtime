// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is the C# file used to generate GitHub_89337.il

class GitHub_89337
{
    class Generic<T>
    {
    }

    class Derived : System.Xml.NameTable { }
    static Generic<Derived>? Passthru(Generic<Derived>? param)
    {
        return param;
    }

    static int Main()
    {
        return 100;
    }
}
