// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


#if FEATURE_COM
#pragma warning disable 612, 618
using System.Linq.Expressions;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.ComInterop {
    internal sealed class CurrencyArgBuilder : SimpleArgBuilder {
        internal CurrencyArgBuilder(Type parameterType)
            : base(parameterType) {
            Debug.Assert(parameterType == typeof(CurrencyWrapper));
        }

        internal override Expression Marshal(Expression parameter) {
            // parameter.WrappedObject
            return Expression.Property(
                Helpers.Convert(base.Marshal(parameter), typeof(CurrencyWrapper)),
                "WrappedObject"
            );
        }

        internal override Expression MarshalToRef(Expression parameter) {
            // Decimal.ToOACurrency(parameter.WrappedObject)
            return Expression.Call(
                typeof(Decimal).GetMethod("ToOACurrency"),
                Marshal(parameter)
            );
        }

        internal override Expression UnmarshalFromRef(Expression value) {
            // Decimal.FromOACurrency(value)
            return base.UnmarshalFromRef(
                Expression.New(
                    typeof(CurrencyWrapper).GetConstructor(new Type[] { typeof(Decimal) }),
                    Expression.Call(
                        typeof(Decimal).GetMethod("FromOACurrency"),
                        value
                    )
                )
            );
        }
    }
}

#endif
