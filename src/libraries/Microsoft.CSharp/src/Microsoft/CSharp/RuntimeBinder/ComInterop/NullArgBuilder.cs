// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    /// <summary>
    /// ArgBuilder which always produces null.
    /// </summary>
    internal sealed class NullArgBuilder : ArgBuilder
    {
        internal NullArgBuilder() { }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal override Expression Marshal(Expression parameter)
        {
            return Expression.Constant(null);
        }
    }
}
