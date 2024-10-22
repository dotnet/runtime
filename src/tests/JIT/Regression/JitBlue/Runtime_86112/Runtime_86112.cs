// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_86112
{
    static int _intStatic;

    [Fact]
    public static int Problem()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Throw()
        {
            _intStatic = 1;
            throw new Exception();
        }

        _intStatic = 2;

        try
        {
            Throw();
            _intStatic = 2;
        }
        catch (Exception)
        {
            if (_intStatic == 2)
            {
                return 101;
            }
        }

        return 100;
    }
}
