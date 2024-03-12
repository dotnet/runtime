// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

class Program
{
    class Mine { }

    static int Main()
    {
        Mine[] s = new Mine[1] { new Mine() };
        Mine[] d = new Mine[1];
        ((ICollection<Mine>)s).CopyTo(d, 0);
        return s[0] == d[0] ? 100 : -1;
    }
}
