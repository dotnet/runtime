// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM
using System.Linq.Expressions;

using System;
using System.Diagnostics;

namespace Microsoft.Scripting.ComInterop {

    /// <summary>
    /// ArgBuilder which always produces null.  
    /// </summary>
    internal sealed class NullArgBuilder : ArgBuilder {
        internal NullArgBuilder() { }

        internal override Expression Marshal(Expression parameter) {
            return Expression.Constant(null);
        }
    }
}

#endif
