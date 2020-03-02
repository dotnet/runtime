﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal class TypeEnumMetaObject : DynamicMetaObject {
        private readonly ComTypeEnumDesc _desc;

        internal TypeEnumMetaObject(ComTypeEnumDesc desc, Expression expression)
            : base(expression, BindingRestrictions.Empty, desc) {
            _desc = desc;
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder) {
            if (_desc.HasMember(binder.Name)) {
                return new DynamicMetaObject(
                    // return (.bound $arg0).GetValue("<name>")
                    Expression.Constant(((ComTypeEnumDesc)Value).GetValue(binder.Name), typeof(object)),
                    EnumRestrictions()
                );
            }

            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetDynamicMemberNames() {
            return _desc.GetMemberNames();
        }

        private BindingRestrictions EnumRestrictions() {
            return BindingRestrictions.GetTypeRestriction(
                Expression, typeof(ComTypeEnumDesc)
            ).Merge(
                // ((ComTypeEnumDesc)<arg>).TypeLib.Guid == <guid>
                BindingRestrictions.GetExpressionRestriction(
                    Expression.Equal(
                        Expression.Property(
                            Expression.Property(
                                Helpers.Convert(Expression, typeof(ComTypeEnumDesc)),
                                typeof(ComTypeDesc).GetProperty(nameof(ComTypeDesc.TypeLib))),
                            typeof(ComTypeLibDesc).GetProperty(nameof(ComTypeLibDesc.Guid))),
                        Expression.Constant(_desc.TypeLib.Guid)
                    )
                )
            ).Merge(
                BindingRestrictions.GetExpressionRestriction(
                    Expression.Equal(
                        Expression.Property(
                            Helpers.Convert(Expression, typeof(ComTypeEnumDesc)),
                            typeof(ComTypeEnumDesc).GetProperty(nameof(ComTypeEnumDesc.TypeName))
                        ),
                        Expression.Constant(_desc.TypeName)
                    )
                )
            );
        }
    }
}
