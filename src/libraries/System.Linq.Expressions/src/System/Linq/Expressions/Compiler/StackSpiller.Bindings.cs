// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler
{
    internal partial class StackSpiller
    {
        private abstract class BindingRewriter
        {
            protected readonly MemberBinding _binding;
            protected readonly StackSpiller _spiller;
            protected RewriteAction _action;

            internal BindingRewriter(MemberBinding binding, StackSpiller spiller)
            {
                _binding = binding;
                _spiller = spiller;
            }

            internal RewriteAction Action => _action;

            internal abstract MemberBinding AsBinding();
            internal abstract Expression AsExpression(Expression target);

            internal static BindingRewriter Create(MemberBinding binding, StackSpiller spiller, Stack stack)
            {
                switch (binding.BindingType)
                {
                    case MemberBindingType.Assignment:
                        MemberAssignment assign = (MemberAssignment)binding;
                        return new MemberAssignmentRewriter(assign, spiller, stack);
                    case MemberBindingType.ListBinding:
                        MemberListBinding list = (MemberListBinding)binding;
                        return new ListBindingRewriter(list, spiller, stack);
                    case MemberBindingType.MemberBinding:
                        MemberMemberBinding member = (MemberMemberBinding)binding;
                        return new MemberMemberBindingRewriter(member, spiller, stack);
                }
                throw Error.UnhandledBinding();
            }

            protected void RequireNoValueProperty()
            {
                var property = _binding.Member as PropertyInfo;

                if (property != null && property.PropertyType.GetTypeInfo().IsValueType)
                {
                    throw Error.CannotAutoInitializeValueTypeMemberThroughProperty(property);
                }
            }
        }

        private class MemberMemberBindingRewriter : BindingRewriter
        {
            private ReadOnlyCollection<MemberBinding> _bindings;
            private BindingRewriter[] _bindingRewriters;

            internal MemberMemberBindingRewriter(MemberMemberBinding binding, StackSpiller spiller, Stack stack) :
                base(binding, spiller)
            {
                _bindings = binding.Bindings;
                _bindingRewriters = new BindingRewriter[_bindings.Count];
                for (int i = 0; i < _bindings.Count; i++)
                {
                    BindingRewriter br = BindingRewriter.Create(_bindings[i], spiller, stack);
                    _action |= br.Action;
                    _bindingRewriters[i] = br;
                }
            }

            internal override MemberBinding AsBinding()
            {
                switch (_action)
                {
                    case RewriteAction.None:
                        return _binding;
                    case RewriteAction.Copy:
                        MemberBinding[] newBindings = new MemberBinding[_bindings.Count];
                        for (int i = 0; i < _bindings.Count; i++)
                        {
                            newBindings[i] = _bindingRewriters[i].AsBinding();
                        }
                        return new MemberMemberBinding(_binding.Member, new TrueReadOnlyCollection<MemberBinding>(newBindings));
                }
                throw ContractUtils.Unreachable;
            }

            internal override Expression AsExpression(Expression target)
            {
                RequireNoValueProperty();
                RequireNotRefInstance(target);

                Expression member = MemberExpression.Make(target, _binding.Member);
                Expression memberTemp = _spiller.MakeTemp(member.Type);

                Expression[] block = new Expression[_bindings.Count + 2];
                block[0] = new AssignBinaryExpression(memberTemp, member);

                for (int i = 0; i < _bindings.Count; i++)
                {
                    BindingRewriter br = _bindingRewriters[i];
                    block[i + 1] = br.AsExpression(memberTemp);
                }

                // We need to copy back value types
                if (memberTemp.Type.GetTypeInfo().IsValueType)
                {
                    block[_bindings.Count + 1] = Expression.Block(
                        typeof(void),
                        new AssignBinaryExpression(MemberExpression.Make(target, _binding.Member), memberTemp)
                    );
                }
                else
                {
                    block[_bindings.Count + 1] = Utils.Empty();
                }
                return MakeBlock(block);
            }
        }

        private class ListBindingRewriter : BindingRewriter
        {
            private ReadOnlyCollection<ElementInit> _inits;
            private ChildRewriter[] _childRewriters;

            internal ListBindingRewriter(MemberListBinding binding, StackSpiller spiller, Stack stack) :
                base(binding, spiller)
            {
                _inits = binding.Initializers;

                _childRewriters = new ChildRewriter[_inits.Count];
                for (int i = 0; i < _inits.Count; i++)
                {
                    ElementInit init = _inits[i];

                    ChildRewriter cr = new ChildRewriter(spiller, stack, init.Arguments.Count);
                    cr.Add(init.Arguments);

                    _action |= cr.Action;
                    _childRewriters[i] = cr;
                }
            }

            internal override MemberBinding AsBinding()
            {
                switch (_action)
                {
                    case RewriteAction.None:
                        return _binding;
                    case RewriteAction.Copy:
                        ElementInit[] newInits = new ElementInit[_inits.Count];
                        for (int i = 0; i < _inits.Count; i++)
                        {
                            ChildRewriter cr = _childRewriters[i];
                            if (cr.Action == RewriteAction.None)
                            {
                                newInits[i] = _inits[i];
                            }
                            else
                            {
                                newInits[i] = new ElementInit(_inits[i].AddMethod, new TrueReadOnlyCollection<Expression>(cr[0, -1]));
                            }
                        }
                        return new MemberListBinding(_binding.Member, new TrueReadOnlyCollection<ElementInit>(newInits));
                }
                throw ContractUtils.Unreachable;
            }

            internal override Expression AsExpression(Expression target)
            {
                RequireNoValueProperty();
                RequireNotRefInstance(target);

                Expression member = MemberExpression.Make(target, _binding.Member);
                Expression memberTemp = _spiller.MakeTemp(member.Type);

                Expression[] block = new Expression[_inits.Count + 2];
                block[0] = new AssignBinaryExpression(memberTemp, member);

                for (int i = 0; i < _inits.Count; i++)
                {
                    ChildRewriter cr = _childRewriters[i];
                    Result add = cr.Finish(new InstanceMethodCallExpressionN(_inits[i].AddMethod, memberTemp, cr[0, -1]));
                    block[i + 1] = add.Node;
                }

                // We need to copy back value types
                if (memberTemp.Type.GetTypeInfo().IsValueType)
                {
                    block[_inits.Count + 1] = Expression.Block(
                        typeof(void),
                        new AssignBinaryExpression(MemberExpression.Make(target, _binding.Member), memberTemp)
                    );
                }
                else
                {
                    block[_inits.Count + 1] = Utils.Empty();
                }
                return MakeBlock(block);
            }
        }

        private class MemberAssignmentRewriter : BindingRewriter
        {
            private Expression _rhs;

            internal MemberAssignmentRewriter(MemberAssignment binding, StackSpiller spiller, Stack stack) :
                base(binding, spiller)
            {
                Result result = spiller.RewriteExpression(binding.Expression, stack);
                _action = result.Action;
                _rhs = result.Node;
            }

            internal override MemberBinding AsBinding()
            {
                switch (_action)
                {
                    case RewriteAction.None:
                        return _binding;
                    case RewriteAction.Copy:
                        return new MemberAssignment(_binding.Member, _rhs);
                }
                throw ContractUtils.Unreachable;
            }

            internal override Expression AsExpression(Expression target)
            {
                RequireNotRefInstance(target);

                Expression member = MemberExpression.Make(target, _binding.Member);
                Expression memberTemp = _spiller.MakeTemp(member.Type);

                return MakeBlock(
                    new AssignBinaryExpression(memberTemp, _rhs),
                    new AssignBinaryExpression(member, memberTemp),
                    Utils.Empty()
                );
            }
        }
    }
}
