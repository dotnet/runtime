// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM
using System.Linq.Expressions;

using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Scripting.Runtime;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.ComInterop {

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
                    AstUtils.Constant(((ComTypeEnumDesc)Value).GetValue(binder.Name), typeof(object)),
                    EnumRestrictions()
                );
            }

            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetDynamicMemberNames() {
            return _desc.GetMemberNames();
        }

        private BindingRestrictions EnumRestrictions() {
            return BindingRestrictionsHelpers.GetRuntimeTypeRestriction(
                Expression, typeof(ComTypeEnumDesc)
            ).Merge(
                // ((ComTypeEnumDesc)<arg>).TypeLib.Guid == <guid>
                BindingRestrictions.GetExpressionRestriction(
                    Expression.Equal(
                        Expression.Property(
                            Expression.Property(
                                AstUtils.Convert(Expression, typeof(ComTypeEnumDesc)),
                                typeof(ComTypeDesc).GetProperty("TypeLib")),
                            typeof(ComTypeLibDesc).GetProperty("Guid")),
                        AstUtils.Constant(_desc.TypeLib.Guid)
                    )
                )
            ).Merge(
                BindingRestrictions.GetExpressionRestriction(
                    Expression.Equal(
                        Expression.Property(
                            AstUtils.Convert(Expression, typeof(ComTypeEnumDesc)),
                            typeof(ComTypeEnumDesc).GetProperty("TypeName")
                        ),
                        AstUtils.Constant(_desc.TypeName)
                    )
                )
            );
        }
    }
}

#endif
