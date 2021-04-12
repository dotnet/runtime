// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    // Note: we only need to support the operations used by ComBinder
    internal sealed class ComMetaObject : DynamicMetaObject
    {
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal ComMetaObject(Expression expression, BindingRestrictions restrictions, object arg)
            : base(expression, restrictions, arg)
        {
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            Requires.NotNull(binder, nameof(binder));
            return binder.Defer(args.AddFirst(WrapSelf()));
        }

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            Requires.NotNull(binder, nameof(binder));
            return binder.Defer(args.AddFirst(WrapSelf()));
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            Requires.NotNull(binder, nameof(binder));
            return binder.Defer(WrapSelf());
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            Requires.NotNull(binder, nameof(binder));
            return binder.Defer(WrapSelf(), value);
        }

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            Requires.NotNull(binder, nameof(binder));
            return binder.Defer(WrapSelf(), indexes);
        }

        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            Requires.NotNull(binder, nameof(binder));
            return binder.Defer(WrapSelf(), indexes.AddLast(value));
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        private DynamicMetaObject WrapSelf()
        {
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
