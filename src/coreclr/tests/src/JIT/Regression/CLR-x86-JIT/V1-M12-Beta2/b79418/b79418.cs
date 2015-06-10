// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class PInvokeTest
{

    static String foo = "foo";
    static String bar = "bar";

    public static int Main(String[] args)
    {
        if (foo == bar)
            foo = "foo";
        return 100;
    }
}
