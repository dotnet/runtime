// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* DESCRIPTION: regression test for VSWhidbey 271010
 *              Should throw OOM
 */

using System;
using System.Runtime.CompilerServices;

public class Test {

    public static int Main() {

        int[][] otherarray;

        try
        {
            otherarray = new int[16384][];
            for(int i=0;i<16384;i++)
            {
                otherarray[i] = new int[1024*500];
            }
        }
        catch (System.OutOfMemoryException)
        {
            otherarray = null;
            return 100;
        }
        return 1;

    }
}
