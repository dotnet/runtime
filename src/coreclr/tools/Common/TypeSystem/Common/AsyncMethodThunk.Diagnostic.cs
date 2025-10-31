// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents the async-callable (CORINFO_CALLCONV_ASYNCCALL) variant of a Task/ValueTask returning method.
    /// </summary>
    public sealed partial class AsyncMethodThunk : MethodDelegator
    {
        public override string DiagnosticName
        {
            get
            {
                return "Async thunk: " + _wrappedMethod.DiagnosticName;
            }
        }
    }
}
