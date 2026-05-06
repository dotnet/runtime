// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// An anonymous QilExpression function node.
    /// </summary>
    /// <remarks>
    /// <para>Function is a block, so it may introduce assignments (scoped to the function body).
    /// Additionally, it has an argument list, which will be assigned values
    /// when the function is invoked.</para>
    /// <para>The XmlType property defines the expected return type of this function.
    /// Normally, this should be the same as its definition's types, so setting the function
    /// definition changes the function's types.  In some rare cases, a compiler may wish to
    /// override the types after setting the function's definition (for example, an XQuery
    /// might define a function's return type to be wider than its definition would imply.)</para>
    /// </remarks>
    internal sealed class QilFunction : QilReference
    {
        private QilNode _arguments, _definition, _sideEffects;

        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a node
        /// </summary>
        public QilFunction(QilNodeType nodeType, QilNode arguments, QilNode definition, QilNode sideEffects, XmlQueryType resultType)
            : base(nodeType)
        {
            _arguments = arguments;
            _definition = definition;
            _sideEffects = sideEffects;
            this.xmlType = resultType;
        }


        //-----------------------------------------------
        // IList<QilNode> methods -- override
        //-----------------------------------------------

        public override int Count
        {
            get { return 3; }
        }

        public override QilNode this[int index]
        {
            get
            {
                return index switch
                {
                    0 => _arguments,
                    1 => _definition,
                    2 => _sideEffects,
                    _ => throw new IndexOutOfRangeException(),
                };
            }
            set
            {
                switch (index)
                {
                    case 0: _arguments = value; break;
                    case 1: _definition = value; break;
                    case 2: _sideEffects = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }


        //-----------------------------------------------
        // QilFunction methods
        //-----------------------------------------------

        /// <summary>
        /// Formal arguments of this function.
        /// </summary>
        public QilList Arguments
        {
            get { return (QilList)_arguments; }
            set { _arguments = value; }
        }

        /// <summary>
        /// Body of this function.
        /// </summary>
        public QilNode Definition
        {
            get { return _definition; }
            set { _definition = value; }
        }

        /// <summary>
        /// QilNodeType.True if this function might have side-effects.
        /// </summary>
        public bool MaybeSideEffects
        {
            get { return _sideEffects.NodeType == QilNodeType.True; }
            set { _sideEffects.NodeType = value ? QilNodeType.True : QilNodeType.False; }
        }
    }
}
