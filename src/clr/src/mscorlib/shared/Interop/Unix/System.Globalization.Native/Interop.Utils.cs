// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

internal static partial class Interop
{
    /// <summary>
    /// Helper for making interop calls that return a string, but we don't know
    /// the correct size of buffer to make. So invoke the interop call with an
    /// increasing buffer until the size is big enough.
    /// </summary>
    internal static bool CallStringMethod<TArg1, TArg2, TArg3>(
        Func<TArg1, TArg2, TArg3, StringBuilder, GlobalizationInterop.ResultCode> interopCall,
        TArg1 arg1,
        TArg2 arg2,
        TArg3 arg3,
        out string result)
    {
        const int initialStringSize = 80;
        const int maxDoubleAttempts = 5;

        StringBuilder stringBuilder = StringBuilderCache.Acquire(initialStringSize);

        for (int i = 0; i < maxDoubleAttempts; i++)
        {
            GlobalizationInterop.ResultCode resultCode = interopCall(arg1, arg2, arg3, stringBuilder);

            if (resultCode == GlobalizationInterop.ResultCode.Success)
            {
                result = StringBuilderCache.GetStringAndRelease(stringBuilder);
                return true;
            }
            else if (resultCode == GlobalizationInterop.ResultCode.InsufficentBuffer)
            {
                // increase the string size and loop
                stringBuilder.EnsureCapacity(stringBuilder.Capacity * 2);
            }
            else
            {
                // if there is an unknown error, don't proceed
                break;
            }
        }

        StringBuilderCache.Release(stringBuilder);
        result = null;
        return false;
    }
}
