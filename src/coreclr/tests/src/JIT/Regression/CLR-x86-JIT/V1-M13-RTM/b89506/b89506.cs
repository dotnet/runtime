// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
