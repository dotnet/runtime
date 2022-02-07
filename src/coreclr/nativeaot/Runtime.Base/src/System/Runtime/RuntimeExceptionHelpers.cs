// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;

namespace System
{
    public class RuntimeExceptionHelpers
    {
        public static void FailFast(String message)
        {
            InternalCalls.RhpFallbackFailFast();
        }
    }
}
