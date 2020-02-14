// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.ComInterop {

    // Note: we only need to support the operations used by ComBinder
    internal class ComMetaObject : DynamicMetaObject {
        internal ComMetaObject(Expression expression, BindingRestrictions restrictions, object arg)
            : base(expression, restrictions, arg) {
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.Defer(args.AddFirst(WrapSelf()));
        }

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.Defer(args.AddFirst(WrapSelf()));
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.Defer(WrapSelf());
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.Defer(WrapSelf(), value);
        }

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.Defer(WrapSelf(), indexes);
        }

        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return binder.Defer(WrapSelf(), indexes.AddLast(value));
        }

        private DynamicMetaObject WrapSelf() {
            return new DynamicMetaObject(
                ComObject.RcwToComObject(Expression),
                BindingRestrictions.GetExpressionRestriction(
                    Expression.Call(
                        typeof(ComBinder).GetMethod(nameof(ComBinder.IsComObject), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public),
                        Helpers.Convert(Expression, typeof(object))
                    )
                )
            );
        }
    }
}

#endif
