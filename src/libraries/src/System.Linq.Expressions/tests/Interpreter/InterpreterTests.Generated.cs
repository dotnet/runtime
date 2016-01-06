// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if FEATURE_INTERPRET
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace Tests.Expressions
{
    partial class InterpreterTests
    {
        [Fact]
        public static void CompileInterpretCrossCheck_Add()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Add, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Add);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_AddChecked()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.AddChecked, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.AddChecked);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_And()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.And, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.And);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_AndAlso()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.AndAlso, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.AndAlso);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_ArrayLength()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.ArrayLength, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.ArrayLength);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_ArrayIndex()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.ArrayIndex, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.ArrayIndex);
            }
        }

        [Fact]
        [ActiveIssue(5067, PlatformID.Windows)]
        public static void CompileInterpretCrossCheck_Call()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Call, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Call);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Coalesce()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Coalesce, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Coalesce);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Conditional()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Conditional, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Conditional);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Constant()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Constant, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Constant);
            }
        }

        [Fact(Skip = "4019")]
        public static void CompileInterpretCrossCheck_Convert()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Convert, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Convert);
            }
        }

        [Fact(Skip = "4022")]
        public static void CompileInterpretCrossCheck_ConvertChecked()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.ConvertChecked, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.ConvertChecked);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Divide()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Divide, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Divide);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Equal()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Equal, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Equal);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_ExclusiveOr()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.ExclusiveOr, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.ExclusiveOr);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_GreaterThan()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.GreaterThan, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.GreaterThan);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_GreaterThanOrEqual()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.GreaterThanOrEqual, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.GreaterThanOrEqual);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Invoke()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Invoke, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Invoke);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Lambda()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Lambda, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Lambda);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_LeftShift()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.LeftShift, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.LeftShift);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_LessThan()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.LessThan, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.LessThan);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_LessThanOrEqual()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.LessThanOrEqual, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.LessThanOrEqual);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_ListInit()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.ListInit, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.ListInit);
            }
        }

        [Fact]
        [ActiveIssue(5067, PlatformID.Windows)]
        public static void CompileInterpretCrossCheck_MemberAccess()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.MemberAccess, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.MemberAccess);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_MemberInit()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.MemberInit, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.MemberInit);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Modulo()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Modulo, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Modulo);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Multiply()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Multiply, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Multiply);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_MultiplyChecked()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.MultiplyChecked, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.MultiplyChecked);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Negate()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Negate, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Negate);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_UnaryPlus()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.UnaryPlus, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.UnaryPlus);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_NegateChecked()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.NegateChecked, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.NegateChecked);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_New()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.New, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.New);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_NewArrayInit()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.NewArrayInit, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.NewArrayInit);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_NewArrayBounds()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.NewArrayBounds, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.NewArrayBounds);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Not()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Not, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Not);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_NotEqual()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.NotEqual, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.NotEqual);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Or()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Or, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Or);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_OrElse()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.OrElse, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.OrElse);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Parameter()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Parameter, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Parameter);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Power()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Power, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Power);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Quote()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Quote, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Quote);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_RightShift()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.RightShift, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.RightShift);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Subtract()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Subtract, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Subtract);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_SubtractChecked()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.SubtractChecked, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.SubtractChecked);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_TypeAs()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.TypeAs, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.TypeAs);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_TypeIs()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.TypeIs, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.TypeIs);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Assign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Assign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Assign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Block()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Block, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Block);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_DebugInfo()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.DebugInfo, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.DebugInfo);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Decrement()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Decrement, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Decrement);
            }
        }

        [Fact(Skip = "3995")]
        public static void CompileInterpretCrossCheck_Dynamic()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Dynamic, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Dynamic);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Default()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Default, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Default);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Extension()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Extension, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Extension);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Goto()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Goto, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Goto);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Increment()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Increment, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Increment);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Index()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Index, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Index);
            }
        }

        [Fact(Skip = "3995")]
        public static void CompileInterpretCrossCheck_Label()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Label, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Label);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_RuntimeVariables()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.RuntimeVariables, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.RuntimeVariables);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Loop()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Loop, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Loop);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Switch()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Switch, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Switch);
            }
        }

        [Fact(Skip = "3995")]
        public static void CompileInterpretCrossCheck_Throw()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Throw, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Throw);
            }
        }

        [Fact(Skip = "3995")]
        public static void CompileInterpretCrossCheck_Try()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Try, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Try);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_Unbox()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.Unbox, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.Unbox);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_AddAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.AddAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.AddAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_AndAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.AndAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.AndAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_DivideAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.DivideAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.DivideAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_ExclusiveOrAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.ExclusiveOrAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.ExclusiveOrAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_LeftShiftAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.LeftShiftAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.LeftShiftAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_ModuloAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.ModuloAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.ModuloAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_MultiplyAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.MultiplyAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.MultiplyAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_OrAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.OrAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.OrAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_PowerAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.PowerAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.PowerAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_RightShiftAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.RightShiftAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.RightShiftAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_SubtractAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.SubtractAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.SubtractAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_AddAssignChecked()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.AddAssignChecked, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.AddAssignChecked);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_MultiplyAssignChecked()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.MultiplyAssignChecked, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.MultiplyAssignChecked);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_SubtractAssignChecked()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.SubtractAssignChecked, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.SubtractAssignChecked);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_PreIncrementAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.PreIncrementAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.PreIncrementAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_PreDecrementAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.PreDecrementAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.PreDecrementAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_PostIncrementAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.PostIncrementAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.PostIncrementAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_PostDecrementAssign()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.PostDecrementAssign, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.PostDecrementAssign);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_TypeEqual()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.TypeEqual, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.TypeEqual);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_OnesComplement()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.OnesComplement, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.OnesComplement);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_IsTrue()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.IsTrue, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.IsTrue);
            }
        }

        [Fact]
        public static void CompileInterpretCrossCheck_IsFalse()
        {
            var exprs = default(IEnumerable<Expression>);
            if (ExpressionCatalog.Catalog.TryGetValue(ExpressionType.IsFalse, out exprs))
            {
                foreach (var e in exprs)
                {
                    Verify(e);
                }
            }
            else
            {
                MissingTest(ExpressionType.IsFalse);
            }
        }

        static partial void MissingTest(ExpressionType nodeType);
    }
}
#endif
