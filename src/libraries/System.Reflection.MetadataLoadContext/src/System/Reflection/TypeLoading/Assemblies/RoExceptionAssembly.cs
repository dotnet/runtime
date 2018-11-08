// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// This "assembly" holds an exception resulting from a failure to bind an assembly name. It can be stored in bind caches and assembly ref
    /// memoization tables.
    /// </summary>
    internal sealed class RoExceptionAssembly : RoStubAssembly
    {
        internal RoExceptionAssembly(Exception exception) 
            : base()
        {
            Exception = exception;
        }

        internal Exception Exception { get; }
    }
}
