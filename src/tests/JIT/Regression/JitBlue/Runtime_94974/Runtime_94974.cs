// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public static class Runtime_94974
{
    [Fact]
    public static int Problem()
    {
        int result = 100;
        try
        {
            try
            {
                throw new Exception();
            }
            finally
            {
                result = 100;
            }
        }
        catch when (Bar(result = 101))
        {
            return result;
        }
        
        return result;
    }
    
    public static bool Bar(int x) => true;
}
