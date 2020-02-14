// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM
using System.Linq.Expressions;

using System.Collections.Generic;
using System.Dynamic;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.ComInterop {

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
                                    AstUtils.Convert(
                                        Expression, typeof(ComTypeLibDesc)
                                    ),
                                    typeof(ComTypeLibDesc).GetProperty("Guid")
                                ),
                                AstUtils.Constant(_lib.Guid)
                            )
                        )
                    );

                return new DynamicMetaObject(
                    AstUtils.Constant(
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
            var result = TryBindGetMember(binder.Name);
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

#endif
