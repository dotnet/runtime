// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    public partial class Expression
    {
        internal class BinaryExpressionProxy
        {
            private readonly BinaryExpression _node;

            public BinaryExpressionProxy(BinaryExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public LambdaExpression Conversion => _node.Conversion;
            public string DebugView => _node.DebugView;
            public bool IsLifted => _node.IsLifted;
            public bool IsLiftedToNull => _node.IsLiftedToNull;
            public Expression Left => _node.Left;
            public MethodInfo Method => _node.Method;
            public ExpressionType NodeType => _node.NodeType;
            public Expression Right => _node.Right;
            public Type Type => _node.Type;
        }

        internal class BlockExpressionProxy
        {
            private readonly BlockExpression _node;

            public BlockExpressionProxy(BlockExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public ReadOnlyCollection<Expression> Expressions => _node.Expressions;
            public ExpressionType NodeType => _node.NodeType;
            public Expression Result => _node.Result;
            public Type Type => _node.Type;
            public ReadOnlyCollection<ParameterExpression> Variables => _node.Variables;
        }

        internal class CatchBlockProxy
        {
            private readonly CatchBlock _node;

            public CatchBlockProxy(CatchBlock node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public Expression Body => _node.Body;
            public Expression Filter => _node.Filter;
            public Type Test => _node.Test;
            public ParameterExpression Variable => _node.Variable;
        }

        internal class ConditionalExpressionProxy
        {
            private readonly ConditionalExpression _node;

            public ConditionalExpressionProxy(ConditionalExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public Expression IfFalse => _node.IfFalse;
            public Expression IfTrue => _node.IfTrue;
            public ExpressionType NodeType => _node.NodeType;
            public Expression Test => _node.Test;
            public Type Type => _node.Type;
        }

        internal class ConstantExpressionProxy
        {
            private readonly ConstantExpression _node;

            public ConstantExpressionProxy(ConstantExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
            public object Value => _node.Value;
        }

        internal class DebugInfoExpressionProxy
        {
            private readonly DebugInfoExpression _node;

            public DebugInfoExpressionProxy(DebugInfoExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public SymbolDocumentInfo Document => _node.Document;
            public int EndColumn => _node.EndColumn;
            public int EndLine => _node.EndLine;
            public bool IsClear => _node.IsClear;
            public ExpressionType NodeType => _node.NodeType;
            public int StartColumn => _node.StartColumn;
            public int StartLine => _node.StartLine;
            public Type Type => _node.Type;
        }

        internal class DefaultExpressionProxy
        {
            private readonly DefaultExpression _node;

            public DefaultExpressionProxy(DefaultExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
        }

        internal class GotoExpressionProxy
        {
            private readonly GotoExpression _node;

            public GotoExpressionProxy(GotoExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public GotoExpressionKind Kind => _node.Kind;
            public ExpressionType NodeType => _node.NodeType;
            public LabelTarget Target => _node.Target;
            public Type Type => _node.Type;
            public Expression Value => _node.Value;
        }

        internal class IndexExpressionProxy
        {
            private readonly IndexExpression _node;

            public IndexExpressionProxy(IndexExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public ReadOnlyCollection<Expression> Arguments => _node.Arguments;
            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public PropertyInfo Indexer => _node.Indexer;
            public ExpressionType NodeType => _node.NodeType;
            public Expression Object => _node.Object;
            public Type Type => _node.Type;
        }

        internal class InvocationExpressionProxy
        {
            private readonly InvocationExpression _node;

            public InvocationExpressionProxy(InvocationExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public ReadOnlyCollection<Expression> Arguments => _node.Arguments;
            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public Expression Expression => _node.Expression;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
        }

        internal class LabelExpressionProxy
        {
            private readonly LabelExpression _node;

            public LabelExpressionProxy(LabelExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public Expression DefaultValue => _node.DefaultValue;
            public ExpressionType NodeType => _node.NodeType;
            public LabelTarget Target => _node.Target;
            public Type Type => _node.Type;
        }

        internal class LambdaExpressionProxy
        {
            private readonly LambdaExpression _node;

            public LambdaExpressionProxy(LambdaExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public Expression Body => _node.Body;
            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public string Name => _node.Name;
            public ExpressionType NodeType => _node.NodeType;
            public ReadOnlyCollection<ParameterExpression> Parameters => _node.Parameters;
            public Type ReturnType => _node.ReturnType;
            public bool TailCall => _node.TailCall;
            public Type Type => _node.Type;
        }

        internal class ListInitExpressionProxy
        {
            private readonly ListInitExpression _node;

            public ListInitExpressionProxy(ListInitExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public ReadOnlyCollection<ElementInit> Initializers => _node.Initializers;
            public NewExpression NewExpression => _node.NewExpression;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
        }

        internal class LoopExpressionProxy
        {
            private readonly LoopExpression _node;

            public LoopExpressionProxy(LoopExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public Expression Body => _node.Body;
            public LabelTarget BreakLabel => _node.BreakLabel;
            public bool CanReduce => _node.CanReduce;
            public LabelTarget ContinueLabel => _node.ContinueLabel;
            public string DebugView => _node.DebugView;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
        }

        internal class MemberExpressionProxy
        {
            private readonly MemberExpression _node;

            public MemberExpressionProxy(MemberExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public Expression Expression => _node.Expression;
            public MemberInfo Member => _node.Member;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
        }

        internal class MemberInitExpressionProxy
        {
            private readonly MemberInitExpression _node;

            public MemberInitExpressionProxy(MemberInitExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public ReadOnlyCollection<MemberBinding> Bindings => _node.Bindings;
            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public NewExpression NewExpression => _node.NewExpression;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
        }

        internal class MethodCallExpressionProxy
        {
            private readonly MethodCallExpression _node;

            public MethodCallExpressionProxy(MethodCallExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public ReadOnlyCollection<Expression> Arguments => _node.Arguments;
            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public MethodInfo Method => _node.Method;
            public ExpressionType NodeType => _node.NodeType;
            public Expression Object => _node.Object;
            public Type Type => _node.Type;
        }

        internal class NewArrayExpressionProxy
        {
            private readonly NewArrayExpression _node;

            public NewArrayExpressionProxy(NewArrayExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public ReadOnlyCollection<Expression> Expressions => _node.Expressions;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
        }

        internal class NewExpressionProxy
        {
            private readonly NewExpression _node;

            public NewExpressionProxy(NewExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public ReadOnlyCollection<Expression> Arguments => _node.Arguments;
            public bool CanReduce => _node.CanReduce;
            public ConstructorInfo Constructor => _node.Constructor;
            public string DebugView => _node.DebugView;
            public ReadOnlyCollection<MemberInfo> Members => _node.Members;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
        }

        internal class ParameterExpressionProxy
        {
            private readonly ParameterExpression _node;

            public ParameterExpressionProxy(ParameterExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public bool IsByRef => _node.IsByRef;
            public string Name => _node.Name;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
        }

        internal class RuntimeVariablesExpressionProxy
        {
            private readonly RuntimeVariablesExpression _node;

            public RuntimeVariablesExpressionProxy(RuntimeVariablesExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
            public ReadOnlyCollection<ParameterExpression> Variables => _node.Variables;
        }

        internal class SwitchCaseProxy
        {
            private readonly SwitchCase _node;

            public SwitchCaseProxy(SwitchCase node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public Expression Body => _node.Body;
            public ReadOnlyCollection<Expression> TestValues => _node.TestValues;
        }

        internal class SwitchExpressionProxy
        {
            private readonly SwitchExpression _node;

            public SwitchExpressionProxy(SwitchExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public ReadOnlyCollection<SwitchCase> Cases => _node.Cases;
            public MethodInfo Comparison => _node.Comparison;
            public string DebugView => _node.DebugView;
            public Expression DefaultBody => _node.DefaultBody;
            public ExpressionType NodeType => _node.NodeType;
            public Expression SwitchValue => _node.SwitchValue;
            public Type Type => _node.Type;
        }

        internal class TryExpressionProxy
        {
            private readonly TryExpression _node;

            public TryExpressionProxy(TryExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public Expression Body => _node.Body;
            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public Expression Fault => _node.Fault;
            public Expression Finally => _node.Finally;
            public ReadOnlyCollection<CatchBlock> Handlers => _node.Handlers;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
        }

        internal class TypeBinaryExpressionProxy
        {
            private readonly TypeBinaryExpression _node;

            public TypeBinaryExpressionProxy(TypeBinaryExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public Expression Expression => _node.Expression;
            public ExpressionType NodeType => _node.NodeType;
            public Type Type => _node.Type;
            public Type TypeOperand => _node.TypeOperand;
        }

        internal class UnaryExpressionProxy
        {
            private readonly UnaryExpression _node;

            public UnaryExpressionProxy(UnaryExpression node)
            {
                ContractUtils.RequiresNotNull(node, nameof(node));
                _node = node;
            }

            public bool CanReduce => _node.CanReduce;
            public string DebugView => _node.DebugView;
            public bool IsLifted => _node.IsLifted;
            public bool IsLiftedToNull => _node.IsLiftedToNull;
            public MethodInfo Method => _node.Method;
            public ExpressionType NodeType => _node.NodeType;
            public Expression Operand => _node.Operand;
            public Type Type => _node.Type;
        }
    }
}
