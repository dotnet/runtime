// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Emits or clears a sequence point for debug information.
    ///
    /// This allows the debugger to highlight the correct source code when
    /// debugging.
    /// </summary>
    [DebuggerTypeProxy(typeof(DebugInfoExpressionProxy))]
    public class DebugInfoExpression : Expression
    {
        internal DebugInfoExpression(SymbolDocumentInfo document)
        {
            Document = document;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type => typeof(void);

        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.DebugInfo;

        /// <summary>
        /// Gets the start line of this <see cref="DebugInfoExpression"/>.
        /// </summary>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual int StartLine
        {
            get { throw ContractUtils.Unreachable; }
        }

        /// <summary>
        /// Gets the start column of this <see cref="DebugInfoExpression"/>.
        /// </summary>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual int StartColumn
        {
            get { throw ContractUtils.Unreachable; }
        }

        /// <summary>
        /// Gets the end line of this <see cref="DebugInfoExpression"/>.
        /// </summary>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual int EndLine
        {
            get { throw ContractUtils.Unreachable; }
        }

        /// <summary>
        /// Gets the end column of this <see cref="DebugInfoExpression"/>.
        /// </summary>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual int EndColumn
        {
            get { throw ContractUtils.Unreachable; }
        }

        /// <summary>
        /// Gets the <see cref="SymbolDocumentInfo"/> that represents the source file.
        /// </summary>
        public SymbolDocumentInfo Document { get; }

        /// <summary>
        /// Gets the value to indicate if the <see cref="DebugInfoExpression"/> is for clearing a sequence point.
        /// </summary>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual bool IsClear
        {
            get { throw ContractUtils.Unreachable; }
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
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

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="DebugInfoExpression"/> with the specified span.
        /// </summary>
        /// <param name="document">The <see cref="SymbolDocumentInfo"/> that represents the source file.</param>
        /// <param name="startLine">The start line of this <see cref="DebugInfoExpression"/>. Must be greater than 0.</param>
        /// <param name="startColumn">The start column of this <see cref="DebugInfoExpression"/>. Must be greater than 0.</param>
        /// <param name="endLine">The end line of this <see cref="DebugInfoExpression"/>. Must be greater or equal than the start line.</param>
        /// <param name="endColumn">The end column of this <see cref="DebugInfoExpression"/>. If the end line is the same as the start line, it must be greater or equal than the start column. In any case, must be greater than 0.</param>
        /// <returns>An instance of <see cref="DebugInfoExpression"/>.</returns>
        public static DebugInfoExpression DebugInfo(SymbolDocumentInfo document, int startLine, int startColumn, int endLine, int endColumn)
        {
            ArgumentNullException.ThrowIfNull(document);
            if (startLine == 0xfeefee && startColumn == 0 && endLine == 0xfeefee && endColumn == 0)
            {
                return new ClearDebugInfoExpression(document);
            }

            ValidateSpan(startLine, startColumn, endLine, endColumn);
            return new SpanDebugInfoExpression(document, startLine, startColumn, endLine, endColumn);
        }

        /// <summary>
        /// Creates a <see cref="DebugInfoExpression"/> for clearing a sequence point.
        /// </summary>
        /// <param name="document">The <see cref="SymbolDocumentInfo"/> that represents the source file.</param>
        /// <returns>An instance of <see cref="DebugInfoExpression"/> for clearing a sequence point.</returns>
        public static DebugInfoExpression ClearDebugInfo(SymbolDocumentInfo document)
        {
            ArgumentNullException.ThrowIfNull(document);

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
