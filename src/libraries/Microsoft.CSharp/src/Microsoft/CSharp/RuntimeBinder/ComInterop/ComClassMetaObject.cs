﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal class ComClassMetaObject : DynamicMetaObject
    {
        internal ComClassMetaObject(Expression expression, ComTypeClassDesc cls)
            : base(expression, BindingRestrictions.Empty, cls)
        {
        }

        public override DynamicMetaObject BindCreateInstance(CreateInstanceBinder binder, DynamicMetaObject[] args)
        {
            return new DynamicMetaObject(
                Expression.Call(
                    Helpers.Convert(Expression, typeof(ComTypeClassDesc)),
                    typeof(ComTypeClassDesc).GetMethod(nameof(ComTypeClassDesc.CreateInstance))
                ),
                BindingRestrictions.Combine(args).Merge(
                    BindingRestrictions.GetTypeRestriction(Expression, typeof(ComTypeClassDesc))
                )
            );
        }
    }
}
