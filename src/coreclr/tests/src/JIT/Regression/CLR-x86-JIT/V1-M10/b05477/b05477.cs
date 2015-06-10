// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public class TestClass
{
    public int IntI = 0;
}

public class mem035
{

    public static TestClass getTC
    {
        get
        {
            return null;
        }
    }

    public static int Main()
    {
        int RetInt = 1;

        try
        {
            int TempInt = getTC.IntI;
        }
        catch (NullReferenceException)
        {
            RetInt = 100;
        }
        return RetInt;
    }
}
