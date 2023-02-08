// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class AA
{
    static Array Static1(ref Array[, ,] param1, ref int param2)
    {
        return param1[param2, param2,
            ((byte)(33 / param2)) | ((byte)((float)((byte)(33 / param2))))];
    }
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Array[, ,] a = null;
            int b = 0;
            Static1(ref a, ref b);
            return 101;
        }
        catch (DivideByZeroException)
        {
            return 100;
        }
    }
}
