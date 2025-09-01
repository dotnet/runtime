// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

public partial class BrowserProfilerTest
{
    public class ClassWithVeryVeryLongName012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
    {
        bool _even;
        public ClassWithVeryVeryLongName012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789(int i)
        {
            _even = i % 2 == 0;
        }
    }

    [JSExport]
    public static int TestMeaning()
    {
        for(int i=0; i<100; i++){
            var r = new int[1000];
            var bla = new ClassWithVeryVeryLongName012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789(i);
        }
 
        return 42;
    }
}
