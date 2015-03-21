// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    public partial class Expression
    {
        internal class BinaryExpressionProxy
        {
            private readonly BinaryExpression _node;

            public BinaryExpressionProxy(BinaryExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public LambdaExpression Conversion { get { return _node.Conversion; } }
            public String DebugView { get { return _node.DebugView; } }
            public Boolean IsLifted { get { return _node.IsLifted; } }
            public Boolean IsLiftedToNull { get { return _node.IsLiftedToNull; } }
            public Expression Left { get { return _node.Left; } }
            public MethodInfo Method { get { return _node.Method; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Expression Right { get { return _node.Right; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class BlockExpressionProxy
        {
            private readonly BlockExpression _node;

            public BlockExpressionProxy(BlockExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public ReadOnlyCollection<Expression> Expressions { get { return _node.Expressions; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Expression Result { get { return _node.Result; } }
            public Type Type { get { return _node.Type; } }
            public ReadOnlyCollection<ParameterExpression> Variables { get { return _node.Variables; } }
        }

        internal class CatchBlockProxy
        {
            private readonly CatchBlock _node;

            public CatchBlockProxy(CatchBlock node)
            {
                _node = node;
            }

            public Expression Body { get { return _node.Body; } }
            public Expression Filter { get { return _node.Filter; } }
            public Type Test { get { return _node.Test; } }
            public ParameterExpression Variable { get { return _node.Variable; } }
        }

        internal class ConditionalExpressionProxy
        {
            private readonly ConditionalExpression _node;

            public ConditionalExpressionProxy(ConditionalExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public Expression IfFalse { get { return _node.IfFalse; } }
            public Expression IfTrue { get { return _node.IfTrue; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Expression Test { get { return _node.Test; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class ConstantExpressionProxy
        {
            private readonly ConstantExpression _node;

            public ConstantExpressionProxy(ConstantExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
            public Object Value { get { return _node.Value; } }
        }

        internal class DebugInfoExpressionProxy
        {
            private readonly DebugInfoExpression _node;

            public DebugInfoExpressionProxy(DebugInfoExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public SymbolDocumentInfo Document { get { return _node.Document; } }
            public Int32 EndColumn { get { return _node.EndColumn; } }
            public Int32 EndLine { get { return _node.EndLine; } }
            public Boolean IsClear { get { return _node.IsClear; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Int32 StartColumn { get { return _node.StartColumn; } }
            public Int32 StartLine { get { return _node.StartLine; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class DefaultExpressionProxy
        {
            private readonly DefaultExpression _node;

            public DefaultExpressionProxy(DefaultExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class GotoExpressionProxy
        {
            private readonly GotoExpression _node;

            public GotoExpressionProxy(GotoExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public GotoExpressionKind Kind { get { return _node.Kind; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public LabelTarget Target { get { return _node.Target; } }
            public Type Type { get { return _node.Type; } }
            public Expression Value { get { return _node.Value; } }
        }

        internal class IndexExpressionProxy
        {
            private readonly IndexExpression _node;

            public IndexExpressionProxy(IndexExpression node)
            {
                _node = node;
            }

            public ReadOnlyCollection<Expression> Arguments { get { return _node.Arguments; } }
            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public PropertyInfo Indexer { get { return _node.Indexer; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Expression Object { get { return _node.Object; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class InvocationExpressionProxy
        {
            private readonly InvocationExpression _node;

            public InvocationExpressionProxy(InvocationExpression node)
            {
                _node = node;
            }

            public ReadOnlyCollection<Expression> Arguments { get { return _node.Arguments; } }
            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public Expression Expression { get { return _node.Expression; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class LabelExpressionProxy
        {
            private readonly LabelExpression _node;

            public LabelExpressionProxy(LabelExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public Expression DefaultValue { get { return _node.DefaultValue; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public LabelTarget Target { get { return _node.Target; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class LambdaExpressionProxy
        {
            private readonly LambdaExpression _node;

            public LambdaExpressionProxy(LambdaExpression node)
            {
                _node = node;
            }

            public Expression Body { get { return _node.Body; } }
            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public String Name { get { return _node.Name; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public ReadOnlyCollection<ParameterExpression> Parameters { get { return _node.Parameters; } }
            public Type ReturnType { get { return _node.ReturnType; } }
            public Boolean TailCall { get { return _node.TailCall; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class ListInitExpressionProxy
        {
            private readonly ListInitExpression _node;

            public ListInitExpressionProxy(ListInitExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public ReadOnlyCollection<ElementInit> Initializers { get { return _node.Initializers; } }
            public NewExpression NewExpression { get { return _node.NewExpression; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class LoopExpressionProxy
        {
            private readonly LoopExpression _node;

            public LoopExpressionProxy(LoopExpression node)
            {
                _node = node;
            }

            public Expression Body { get { return _node.Body; } }
            public LabelTarget BreakLabel { get { return _node.BreakLabel; } }
            public Boolean CanReduce { get { return _node.CanReduce; } }
            public LabelTarget ContinueLabel { get { return _node.ContinueLabel; } }
            public String DebugView { get { return _node.DebugView; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class MemberExpressionProxy
        {
            private readonly MemberExpression _node;

            public MemberExpressionProxy(MemberExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public Expression Expression { get { return _node.Expression; } }
            public MemberInfo Member { get { return _node.Member; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class MemberInitExpressionProxy
        {
            private readonly MemberInitExpression _node;

            public MemberInitExpressionProxy(MemberInitExpression node)
            {
                _node = node;
            }

            public ReadOnlyCollection<MemberBinding> Bindings { get { return _node.Bindings; } }
            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public NewExpression NewExpression { get { return _node.NewExpression; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class MethodCallExpressionProxy
        {
            private readonly MethodCallExpression _node;

            public MethodCallExpressionProxy(MethodCallExpression node)
            {
                _node = node;
            }

            public ReadOnlyCollection<Expression> Arguments { get { return _node.Arguments; } }
            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public MethodInfo Method { get { return _node.Method; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Expression Object { get { return _node.Object; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class NewArrayExpressionProxy
        {
            private readonly NewArrayExpression _node;

            public NewArrayExpressionProxy(NewArrayExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public ReadOnlyCollection<Expression> Expressions { get { return _node.Expressions; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class NewExpressionProxy
        {
            private readonly NewExpression _node;

            public NewExpressionProxy(NewExpression node)
            {
                _node = node;
            }

            public ReadOnlyCollection<Expression> Arguments { get { return _node.Arguments; } }
            public Boolean CanReduce { get { return _node.CanReduce; } }
            public ConstructorInfo Constructor { get { return _node.Constructor; } }
            public String DebugView { get { return _node.DebugView; } }
            public ReadOnlyCollection<MemberInfo> Members { get { return _node.Members; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class ParameterExpressionProxy
        {
            private readonly ParameterExpression _node;

            public ParameterExpressionProxy(ParameterExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public Boolean IsByRef { get { return _node.IsByRef; } }
            public String Name { get { return _node.Name; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class RuntimeVariablesExpressionProxy
        {
            private readonly RuntimeVariablesExpression _node;

            public RuntimeVariablesExpressionProxy(RuntimeVariablesExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
            public ReadOnlyCollection<ParameterExpression> Variables { get { return _node.Variables; } }
        }

        internal class SwitchCaseProxy
        {
            private readonly SwitchCase _node;

            public SwitchCaseProxy(SwitchCase node)
            {
                _node = node;
            }

            public Expression Body { get { return _node.Body; } }
            public ReadOnlyCollection<Expression> TestValues { get { return _node.TestValues; } }
        }

        internal class SwitchExpressionProxy
        {
            private readonly SwitchExpression _node;

            public SwitchExpressionProxy(SwitchExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public ReadOnlyCollection<SwitchCase> Cases { get { return _node.Cases; } }
            public MethodInfo Comparison { get { return _node.Comparison; } }
            public String DebugView { get { return _node.DebugView; } }
            public Expression DefaultBody { get { return _node.DefaultBody; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Expression SwitchValue { get { return _node.SwitchValue; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class TryExpressionProxy
        {
            private readonly TryExpression _node;

            public TryExpressionProxy(TryExpression node)
            {
                _node = node;
            }

            public Expression Body { get { return _node.Body; } }
            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public Expression Fault { get { return _node.Fault; } }
            public Expression Finally { get { return _node.Finally; } }
            public ReadOnlyCollection<CatchBlock> Handlers { get { return _node.Handlers; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
        }

        internal class TypeBinaryExpressionProxy
        {
            private readonly TypeBinaryExpression _node;

            public TypeBinaryExpressionProxy(TypeBinaryExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public Expression Expression { get { return _node.Expression; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Type Type { get { return _node.Type; } }
            public Type TypeOperand { get { return _node.TypeOperand; } }
        }

        internal class UnaryExpressionProxy
        {
            private readonly UnaryExpression _node;

            public UnaryExpressionProxy(UnaryExpression node)
            {
                _node = node;
            }

            public Boolean CanReduce { get { return _node.CanReduce; } }
            public String DebugView { get { return _node.DebugView; } }
            public Boolean IsLifted { get { return _node.IsLifted; } }
            public Boolean IsLiftedToNull { get { return _node.IsLiftedToNull; } }
            public MethodInfo Method { get { return _node.Method; } }
            public ExpressionType NodeType { get { return _node.NodeType; } }
            public Expression Operand { get { return _node.Operand; } }
            public Type Type { get { return _node.Type; } }
        }
    }
}
