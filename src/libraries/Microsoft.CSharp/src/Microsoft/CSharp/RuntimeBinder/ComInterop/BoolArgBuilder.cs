// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM
using System.Linq.Expressions;

using System.Collections.Generic;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.ComInterop {
    internal sealed class BoolArgBuilder : SimpleArgBuilder {
        internal BoolArgBuilder(Type parameterType)
            : base(parameterType) {
            Debug.Assert(parameterType == typeof(bool));
        }

        internal override Expression MarshalToRef(Expression parameter) {
            // parameter  ? -1 : 0
            return Expression.Condition(
                Marshal(parameter),
                Expression.Constant((Int16)(-1)),
                Expression.Constant((Int16)0)
            );
        }

        internal override Expression UnmarshalFromRef(Expression value) {
            //parameter = temp != 0
            return base.UnmarshalFromRef(
                Expression.NotEqual(
                     value,
                     Expression.Constant((Int16)0)
                )
            );
        }
    }
}

#endif