// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    /// <summary>
    /// Acts as the base class for all values that represent a reference to another value. These should only be held in a ref type or on the stack as a result of a 'load address' instruction (e.g. ldloca).
    /// </summary>
    public abstract record ReferenceValue : SingleValue { }
}
