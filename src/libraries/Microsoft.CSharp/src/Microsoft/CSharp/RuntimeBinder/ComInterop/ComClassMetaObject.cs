// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM

using System.Linq.Expressions;

using System.Dynamic;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop {

    internal class ComClassMetaObject : DynamicMetaObject {
        internal ComClassMetaObject(Expression expression, ComTypeClassDesc cls)
            : base(expression, BindingRestrictions.Empty, cls) {
        }

        public override DynamicMetaObject BindCreateInstance(CreateInstanceBinder binder, DynamicMetaObject[] args) {
            return new DynamicMetaObject(
                Expression.Call(
                    Helpers.Convert(Expression, typeof(ComTypeClassDesc)),
                    typeof(ComTypeClassDesc).GetMethod("CreateInstance")
                ),
                BindingRestrictions.Combine(args).Merge(
                    BindingRestrictions.GetTypeRestriction(Expression, typeof(ComTypeClassDesc))
                )
            );
        }
    }
}

#endif
