﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal sealed class DateTimeArgBuilder : SimpleArgBuilder
    {
        internal DateTimeArgBuilder(Type parameterType)
            : base(parameterType)
        {
            Debug.Assert(parameterType == typeof(DateTime));
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            // parameter.ToOADate()
            return Expression.Call(
                Marshal(parameter),
                typeof(DateTime).GetMethod(nameof(DateTime.ToOADate))
            );
        }

        internal override Expression UnmarshalFromRef(Expression value)
        {
            // DateTime.FromOADate(value)
            return base.UnmarshalFromRef(
                Expression.Call(
                    typeof(DateTime).GetMethod(nameof(DateTime.FromOADate)),
                    value
                )
            );
        }
    }
}
