// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    /// <summary>
    /// ArgBuilder which always produces null.
    /// </summary>
    internal sealed class NullArgBuilder : ArgBuilder
    {
        internal NullArgBuilder() { }

        internal override Expression Marshal(Expression parameter)
        {
            return Expression.Constant(null);
        }
    }
}
