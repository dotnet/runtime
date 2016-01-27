// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
public class AA
{
    public static int Main() { Main1(); return 100; }
    public static void Main1()
    {
        (new float[1, 1, 1, 1])[0, 0, 0, 0] -= (new float[1, 1])[0, 0];
    }
}
