// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 618 // CurrencyWrapper is obsolete

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal sealed class CurrencyArgBuilder : SimpleArgBuilder
    {
        internal CurrencyArgBuilder(Type parameterType)
            : base(parameterType)
        {
            Debug.Assert(parameterType == typeof(CurrencyWrapper));
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal override Expression Marshal(Expression parameter)
        {
            // parameter.WrappedObject
            return Expression.Property(
                Helpers.Convert(base.Marshal(parameter), typeof(CurrencyWrapper)),
                nameof(CurrencyWrapper.WrappedObject)
            );
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal override Expression MarshalToRef(Expression parameter)
        {
            // Decimal.ToOACurrency(parameter.WrappedObject)
            return Expression.Call(
                typeof(decimal).GetMethod(nameof(decimal.ToOACurrency)),
                Marshal(parameter)
            );
        }

        internal override Expression UnmarshalFromRef(Expression value)
        {
            // Decimal.FromOACurrency(value)
            return base.UnmarshalFromRef(
                Expression.New(
                    typeof(CurrencyWrapper).GetConstructor(new Type[] { typeof(decimal) }),
                    Expression.Call(
                        typeof(decimal).GetMethod(nameof(decimal.FromOACurrency)),
                        value
                    )
                )
            );
        }
    }
}
