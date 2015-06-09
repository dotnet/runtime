// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
class AA
{
    static Array Static1(ref Array[, ,] param1, ref int param2)
    {
        return param1[param2, param2,
            ((byte)(33 / param2)) | ((byte)((float)((byte)(33 / param2))))];
    }
    static int Main()
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
