// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// The CQR implementation of QilExpression.
    /// </summary>
    /// <remarks>
    ///    <p>QilExpression is the XML Query Intermediate Language invented by Michael Brundage and Chris Suver.
    ///    QilExpression is an intermediate representation (IR) for all XML query and view languages.  QilExpression is
    ///    designed for optimization, composition with virtual XML views, translation into other forms,
    ///    and direct execution.  See also <a href="http://dynamo/qil/qil.xml">the QIL specification</a>.</p>
    /// </remarks>
    internal sealed class QilExpression : QilNode
    {
        private QilFactory _factory;
        private QilNode _isDebug;
        private QilNode _defWSet;
        private QilNode _wsRules;
        private QilNode _gloVars;
        private QilNode _gloParams;
        private QilNode _earlBnd;
        private QilNode _funList;
        private QilNode _rootNod;

        /// <summary>
        /// Construct QIL from a rooted graph of QilNodes with a specific factory.
        /// </summary>
        public QilExpression(QilNodeType nodeType, QilNode root, QilFactory factory) : base(nodeType)
        {
            _factory = factory;
            _isDebug = factory.False();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.ConformanceLevel = ConformanceLevel.Auto;
            _defWSet = factory.LiteralObject(settings);

            _wsRules = factory.LiteralObject(new List<WhitespaceRule>());
            _gloVars = factory.GlobalVariableList();
            _gloParams = factory.GlobalParameterList();
            _earlBnd = factory.LiteralObject(new List<EarlyBoundInfo>());
            _funList = factory.FunctionList();
            _rootNod = root;
        }


        //-----------------------------------------------
        // IList<QilNode> methods -- override
        //-----------------------------------------------

        public override int Count
        {
            get { return 8; }
        }

        public override QilNode this[int index]
        {
            get
            {
                return index switch
                {
                    0 => _isDebug,
                    1 => _defWSet,
                    2 => _wsRules,
                    3 => _gloParams,
                    4 => _gloVars,
                    5 => _earlBnd,
                    6 => _funList,
                    7 => _rootNod,
                    _ => throw new IndexOutOfRangeException(),
                };
            }
            set
            {
                switch (index)
                {
                    case 0: _isDebug = value; break;
                    case 1: _defWSet = value; break;
                    case 2: _wsRules = value; break;
                    case 3: _gloParams = value; break;
                    case 4: _gloVars = value; break;
                    case 5: _earlBnd = value; break;
                    case 6: _funList = value; break;
                    case 7: _rootNod = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }


        //-----------------------------------------------
        // QilExpression methods
        //-----------------------------------------------

        /// <summary>
        /// QilFactory to be used in constructing nodes in this graph.
        /// </summary>
        public QilFactory Factory
        {
            get { return _factory; }
            set { _factory = value; }
        }

        /// <summary>
        /// True if this expression contains debugging information.
        /// </summary>
        public bool IsDebug
        {
            get { return _isDebug.NodeType == QilNodeType.True; }
            set { _isDebug = value ? _factory.True() : _factory.False(); }
        }

        /// <summary>
        /// Default serialization options that will be used if the user does not supply a writer at execution time.
        /// </summary>
        public XmlWriterSettings DefaultWriterSettings
        {
            get { return (XmlWriterSettings)((QilLiteral)_defWSet).Value!; }
            set
            {
                value.ReadOnly = true;
                ((QilLiteral)_defWSet).Value = value;
            }
        }

        /// <summary>
        /// Xslt whitespace strip/preserve rules.
        /// </summary>
        public IList<WhitespaceRule> WhitespaceRules
        {
            get { return (IList<WhitespaceRule>)((QilLiteral)_wsRules).Value!; }
            set { ((QilLiteral)_wsRules).Value = value; }
        }

        /// <summary>
        /// External parameters.
        /// </summary>
        public QilList GlobalParameterList
        {
            get { return (QilList)_gloParams; }
            set { _gloParams = value; }
        }

        /// <summary>
        /// Global variables.
        /// </summary>
        public QilList GlobalVariableList
        {
            get { return (QilList)_gloVars; }
            set { _gloVars = value; }
        }

        /// <summary>
        /// Early bound function objects.
        /// </summary>
        public IList<EarlyBoundInfo> EarlyBoundTypes
        {
            get { return (IList<EarlyBoundInfo>)((QilLiteral)_earlBnd).Value!; }
            set { ((QilLiteral)_earlBnd).Value = value; }
        }

        /// <summary>
        /// Function definitions.
        /// </summary>
        public QilList FunctionList
        {
            get { return (QilList)_funList; }
            set { _funList = value; }
        }

        /// <summary>
        /// The root node of the QilExpression graph
        /// </summary>
        public QilNode Root
        {
            get { return _rootNod; }
            set { _rootNod = value; }
        }
    }
}
