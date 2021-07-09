// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal interface IPseudoComObject
    {
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        DynamicMetaObject GetMetaObject(Expression expression);
    }
}
