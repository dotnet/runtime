// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Emits or clears a sequence point for debug information. This allows the debugger to highlight the correct source code when debugging.</summary>
    [DebuggerTypeProxy(typeof(DebugInfoExpressionProxy))]
    public class DebugInfoExpression : Expression
    {
        internal DebugInfoExpression(SymbolDocumentInfo document)
        {
            Document = document;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.DebugInfoExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type => typeof(void);

        /// <summary>Returns the node type of this <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> that represents this expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.DebugInfo;

        /// <summary>Gets the start line of this <see cref="System.Linq.Expressions.DebugInfoExpression" />.</summary>
        /// <value>The number of the start line of the code that was used to generate the wrapped expression.</value>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual int StartLine
        {
            get { throw ContractUtils.Unreachable; }
        }

        /// <summary>Gets the start column of this <see cref="System.Linq.Expressions.DebugInfoExpression" />.</summary>
        /// <value>The number of the start column of the code that was used to generate the wrapped expression.</value>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual int StartColumn
        {
            get { throw ContractUtils.Unreachable; }
        }

        /// <summary>Gets the end line of this <see cref="System.Linq.Expressions.DebugInfoExpression" />.</summary>
        /// <value>The number of the end line of the code that was used to generate the wrapped expression.</value>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual int EndLine
        {
            get { throw ContractUtils.Unreachable; }
        }

        /// <summary>Gets the end column of this <see cref="System.Linq.Expressions.DebugInfoExpression" />.</summary>
        /// <value>The number of the end column of the code that was used to generate the wrapped expression.</value>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual int EndColumn
        {
            get { throw ContractUtils.Unreachable; }
        }

        /// <summary>Gets the <see cref="System.Linq.Expressions.SymbolDocumentInfo" /> that represents the source file.</summary>
        /// <value>The <see cref="System.Linq.Expressions.SymbolDocumentInfo" /> that represents the source file.</value>
        public SymbolDocumentInfo Document { get; }

        /// <summary>Gets the value to indicate if the <see cref="System.Linq.Expressions.DebugInfoExpression" /> is for clearing a sequence point.</summary>
        /// <value><see langword="true" /> if the <see cref="System.Linq.Expressions.DebugInfoExpression" /> is for clearing a sequence point; otherwise, <see langword="false" />.</value>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual bool IsClear
        {
            get { throw ContractUtils.Unreachable; }
        }

        /// <summary>Dispatches to the specific visit method for this node type. For example, <see cref="System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        /// <remarks>This default implementation for <see cref="System.Linq.Expressions.ExpressionType.Extension" /> nodes calls <see cref="System.Linq.Expressions.ExpressionVisitor.VisitExtension" />. Override this method to call into a more specific method on a derived visitor class of the <see cref="System.Linq.Expressions.ExpressionVisitor" /> class. However, it should still support unknown visitors by calling <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />.</remarks>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitDebugInfo(this);
        }
    }

    #region Specialized subclasses

    internal sealed class SpanDebugInfoExpression : DebugInfoExpression
    {
        private readonly int _startLine, _startColumn, _endLine, _endColumn;

        internal SpanDebugInfoExpression(SymbolDocumentInfo document, int startLine, int startColumn, int endLine, int endColumn)
            : base(document)
        {
            _startLine = startLine;
            _startColumn = startColumn;
            _endLine = endLine;
            _endColumn = endColumn;
        }

        public override int StartLine => _startLine;

        public override int StartColumn => _startColumn;

        public override int EndLine => _endLine;

        public override int EndColumn => _endColumn;

        public override bool IsClear => false;

        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitDebugInfo(this);
        }
    }

    internal sealed class ClearDebugInfoExpression : DebugInfoExpression
    {
        internal ClearDebugInfoExpression(SymbolDocumentInfo document)
            : base(document)
        {
        }

        public override bool IsClear => true;

        public override int StartLine => 0xfeefee;

        public override int StartColumn => 0;

        public override int EndLine => 0xfeefee;

        public override int EndColumn => 0;
    }
    #endregion
    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.DebugInfoExpression" /> with the specified span.</summary>
        /// <param name="document">The <see cref="System.Linq.Expressions.SymbolDocumentInfo" /> that represents the source file.</param>
        /// <param name="startLine">The start line of this <see cref="System.Linq.Expressions.DebugInfoExpression" />. Must be greater than 0.</param>
        /// <param name="startColumn">The start column of this <see cref="System.Linq.Expressions.DebugInfoExpression" />. Must be greater than 0.</param>
        /// <param name="endLine">The end line of this <see cref="System.Linq.Expressions.DebugInfoExpression" />. Must be greater or equal than the start line.</param>
        /// <param name="endColumn">The end column of this <see cref="System.Linq.Expressions.DebugInfoExpression" />. If the end line is the same as the start line, it must be greater or equal than the start column. In any case, must be greater than 0.</param>
        /// <returns>An instance of <see cref="System.Linq.Expressions.DebugInfoExpression" />.</returns>
        public static DebugInfoExpression DebugInfo(SymbolDocumentInfo document, int startLine, int startColumn, int endLine, int endColumn)
        {
            ContractUtils.RequiresNotNull(document, nameof(document));
            if (startLine == 0xfeefee && startColumn == 0 && endLine == 0xfeefee && endColumn == 0)
            {
                return new ClearDebugInfoExpression(document);
            }

            ValidateSpan(startLine, startColumn, endLine, endColumn);
            return new SpanDebugInfoExpression(document, startLine, startColumn, endLine, endColumn);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DebugInfoExpression" /> for clearing a sequence point.</summary>
        /// <param name="document">The <see cref="System.Linq.Expressions.SymbolDocumentInfo" /> that represents the source file.</param>
        /// <returns>An instance of <see cref="System.Linq.Expressions.DebugInfoExpression" /> for clearing a sequence point.</returns>
        public static DebugInfoExpression ClearDebugInfo(SymbolDocumentInfo document)
        {
            ContractUtils.RequiresNotNull(document, nameof(document));

            return new ClearDebugInfoExpression(document);
        }

        private static void ValidateSpan(int startLine, int startColumn, int endLine, int endColumn)
        {
            if (startLine < 1)
            {
                throw Error.OutOfRange(nameof(startLine), 1);
            }
            if (startColumn < 1)
            {
                throw Error.OutOfRange(nameof(startColumn), 1);
            }
            if (endLine < 1)
            {
                throw Error.OutOfRange(nameof(endLine), 1);
            }
            if (endColumn < 1)
            {
                throw Error.OutOfRange(nameof(endColumn), 1);
            }
            if (startLine > endLine)
            {
                throw Error.StartEndMustBeOrdered();
            }
            if (startLine == endLine && startColumn > endColumn)
            {
                throw Error.StartEndMustBeOrdered();
            }
        }
    }
}
