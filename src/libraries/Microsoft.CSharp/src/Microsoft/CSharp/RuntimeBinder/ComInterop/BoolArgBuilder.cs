// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal sealed class BoolArgBuilder : SimpleArgBuilder
    {
        internal BoolArgBuilder(Type parameterType)
            : base(parameterType)
        {
            Debug.Assert(parameterType == typeof(bool));
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal override Expression MarshalToRef(Expression parameter)
        {
            // parameter  ? -1 : 0
            return Expression.Condition(
                Marshal(parameter),
                Expression.Constant((short)(-1)),
                Expression.Constant((short)0)
            );
        }

        internal override Expression UnmarshalFromRef(Expression value)
        {
            //parameter = temp != 0
            return base.UnmarshalFromRef(
                Expression.NotEqual(
                     value,
                     Expression.Constant((short)0)
                )
            );
        }
    }
}
