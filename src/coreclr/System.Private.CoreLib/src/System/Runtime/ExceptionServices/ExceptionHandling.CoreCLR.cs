// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.ExceptionServices
{
    public static partial class ExceptionHandling
    {
        public static partial Exception? GetCurrentException()
        {
            unsafe
            {
                return EH.GetCurrentException(InternalCalls.RhpGetCurrentExInfo()) as Exception;
            }
        }
    }
}
