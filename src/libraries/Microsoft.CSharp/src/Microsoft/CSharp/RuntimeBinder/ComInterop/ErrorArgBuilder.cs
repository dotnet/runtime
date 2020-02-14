// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM
#pragma warning disable 612, 618

using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Microsoft.Scripting.ComInterop {
    internal class ErrorArgBuilder : SimpleArgBuilder {
        internal ErrorArgBuilder(Type parameterType)
            : base(parameterType) {

            Debug.Assert(parameterType == typeof(ErrorWrapper));
        }

        internal override Expression Marshal(Expression parameter) {
            // parameter.ErrorCode
            return Expression.Property(
                Helpers.Convert(base.Marshal(parameter), typeof(ErrorWrapper)),
                "ErrorCode"
            );
        }

        internal override Expression UnmarshalFromRef(Expression value) {
            // new ErrorWrapper(value)
            return base.UnmarshalFromRef(
                Expression.New(
                    typeof(ErrorWrapper).GetConstructor(new Type[] { typeof(int) }),
                    value
                )
            );
        }
    }
}

#endif
