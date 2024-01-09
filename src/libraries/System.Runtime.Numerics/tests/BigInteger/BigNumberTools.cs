// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Reflection;

namespace BigNumberTools
{
    public class Utils
    {
#if DEBUG
        public static void RunWithFakeThreshold(ref int field, int value, Action action)
        {
            int lastValue = field;
            try
            {
                field = value;
                action();
            }
            finally
            {
                field = lastValue;
            }
        }
#endif
    }
}
