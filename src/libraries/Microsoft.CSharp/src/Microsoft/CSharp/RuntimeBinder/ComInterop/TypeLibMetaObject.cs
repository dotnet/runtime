﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal class TypeLibMetaObject : DynamicMetaObject {
        private readonly ComTypeLibDesc _lib;

        internal TypeLibMetaObject(Expression expression, ComTypeLibDesc lib)
            : base(expression, BindingRestrictions.Empty, lib) {
            _lib = lib;
        }

        private DynamicMetaObject TryBindGetMember(string name) {
            if (_lib.HasMember(name)) {
                BindingRestrictions restrictions =
                    BindingRestrictions.GetTypeRestriction(
                        Expression, typeof(ComTypeLibDesc)
                    ).Merge(
                        BindingRestrictions.GetExpressionRestriction(
                            Expression.Equal(
                                Expression.Property(
                                    Helpers.Convert(
                                        Expression, typeof(ComTypeLibDesc)
                                    ),
                                    typeof(ComTypeLibDesc).GetProperty(nameof(ComTypeLibDesc.Guid))
                                ),
                                Expression.Constant(_lib.Guid)
                            )
                        )
                    );

                return new DynamicMetaObject(
                    Expression.Constant(
                        ((ComTypeLibDesc)Value).GetTypeLibObjectDesc(name)
                    ),
                    restrictions
                );
            }

            return null;
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder) {
            return TryBindGetMember(binder.Name) ?? base.BindGetMember(binder);
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args) {
            DynamicMetaObject result = TryBindGetMember(binder.Name);
            if (result != null) {
                return binder.FallbackInvoke(result, args, null);
            }

            return base.BindInvokeMember(binder, args);
        }

        public override IEnumerable<string> GetDynamicMemberNames() {
            return _lib.GetMemberNames();
        }
    }
}
