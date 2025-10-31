// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Wraps a <see cref="MethodDesc"/> object and delegates methods to that <see cref="MethodDesc"/>.
    /// </summary>
    public abstract partial class MethodDelegator : MethodDesc
    {
       public abstract override AsyncMethodData AsyncMethodData { get; }
    }
}
