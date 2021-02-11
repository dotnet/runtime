// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.ExceptionServices
{
    internal class ExceptionDispatchInfo
    {
        public static ExceptionDispatchInfo Capture(Exception source) => null;
        public static void Throw(Exception source) => throw source;
        public void Throw() => throw null;
    }
}
