// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents the Task-returning variant of an async call convention method.
    /// </summary>
    public sealed partial class TaskReturningAsyncThunk : MethodDelegator
    {
        public override string DiagnosticName => "TaskReturningVariant: " + _wrappedMethod.DiagnosticName;
    }
}
