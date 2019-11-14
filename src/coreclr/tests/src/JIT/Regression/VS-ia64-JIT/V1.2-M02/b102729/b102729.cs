// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

struct Foo
{
#pragma warning disable 0414
    public int a;
    public int b;
    public int c;
#pragma warning restore 0414
}

class Bar
{
    static Foo[] _myArray;

    public static void Bork(ref Foo arg)
    {
        arg = _myArray[3];
    }

    public static int Main()
    {
        _myArray = new Foo[10];

        Foo duh = new Foo();

        duh.a = 1;
        duh.b = 2;
        duh.c = 3;

        Bork(ref duh);

        return 100;
    }
}
