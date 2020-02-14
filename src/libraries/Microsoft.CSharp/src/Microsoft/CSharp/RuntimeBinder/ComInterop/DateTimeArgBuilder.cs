// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


#if FEATURE_COM
using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.ComInterop {
    internal sealed class DateTimeArgBuilder : SimpleArgBuilder {
        internal DateTimeArgBuilder(Type parameterType)
            : base(parameterType) {
            Debug.Assert(parameterType == typeof(DateTime));
        }

        internal override Expression MarshalToRef(Expression parameter) {
            // parameter.ToOADate()
            return Expression.Call(
                Marshal(parameter),
                typeof(DateTime).GetMethod("ToOADate")
            );
        }

        internal override Expression UnmarshalFromRef(Expression value) {
            // DateTime.FromOADate(value)
            return base.UnmarshalFromRef(
                Expression.Call(
                    typeof(DateTime).GetMethod("FromOADate"),
                    value
                )
            );
        }
    }
}

#endif