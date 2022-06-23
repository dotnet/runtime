// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.IlGen
{
    /// <summary>
    /// Until run-time, the exact xml state cannot always be determined.  However, the construction analyzer
    /// keeps track of the set of possible xml states at each node in order to reduce run-time state management.
    /// </summary>
    internal enum PossibleXmlStates
    {
        None = 0,
        WithinSequence,
        EnumAttrs,
        WithinContent,
        WithinAttr,
        WithinComment,
        WithinPI,
        Any,
    };


    /// <summary>
    /// 1. Some expressions are lazily materialized by creating an iterator over the results (ex. LiteralString, Content).
    /// 2. Some expressions are incrementally constructed by a Writer (ex. ElementCtor, XsltCopy).
    /// 3. Some expressions can be iterated or written (ex. List).
    /// </summary>
    internal enum XmlILConstructMethod
    {
        Iterator,               // Construct iterator over expression's results
        Writer,                 // Construct expression through calls to Writer
        WriterThenIterator,     // Construct expression through calls to caching Writer; then construct iterator over cached results
        IteratorThenWriter,     // Iterate over expression's results and send each item to Writer
    };


    /// <summary>
    /// Every node is annotated with information about how it will be constructed by ILGen.
    /// </summary>
    internal sealed class XmlILConstructInfo : IQilAnnotation
    {
        private readonly QilNodeType _nodeType;
        private PossibleXmlStates _xstatesInitial, _xstatesFinal, _xstatesBeginLoop, _xstatesEndLoop;
        private bool _isNmspInScope, _mightHaveNmsp, _mightHaveAttrs, _mightHaveDupAttrs, _mightHaveNmspAfterAttrs;
        private XmlILConstructMethod _constrMeth;
        private XmlILConstructInfo? _parentInfo;
        private ArrayList? _callersInfo;
        private bool _isReadOnly;

        private static volatile XmlILConstructInfo? s_default;

        /// <summary>
        /// Get ConstructInfo annotation for the specified node.  Lazily create if necessary.
        /// </summary>
        public static XmlILConstructInfo Read(QilNode nd)
        {
            XmlILAnnotation? ann = nd.Annotation as XmlILAnnotation;
            XmlILConstructInfo? constrInfo = (ann != null) ? ann.ConstructInfo : null;

            if (constrInfo == null)
            {
                if (s_default == null)
                {
                    constrInfo = new XmlILConstructInfo(QilNodeType.Unknown);
                    constrInfo._isReadOnly = true;

                    s_default = constrInfo;
                }
                else
                {
                    constrInfo = s_default;
                }
            }

            return constrInfo;
        }

        /// <summary>
        /// Create and initialize XmlILConstructInfo annotation for the specified node.
        /// </summary>
        public static XmlILConstructInfo Write(QilNode nd)
        {
            XmlILAnnotation ann = XmlILAnnotation.Write(nd);
            XmlILConstructInfo? constrInfo = ann.ConstructInfo;

            if (constrInfo == null || constrInfo._isReadOnly)
            {
                constrInfo = new XmlILConstructInfo(nd.NodeType);
                ann.ConstructInfo = constrInfo;
            }

            return constrInfo;
        }

        /// <summary>
        /// Default to worst possible construction information.
        /// </summary>
        private XmlILConstructInfo(QilNodeType nodeType)
        {
            _nodeType = nodeType;
            _xstatesInitial = _xstatesFinal = PossibleXmlStates.Any;
            _xstatesBeginLoop = _xstatesEndLoop = PossibleXmlStates.None;
            _isNmspInScope = false;
            _mightHaveNmsp = true;
            _mightHaveAttrs = true;
            _mightHaveDupAttrs = true;
            _mightHaveNmspAfterAttrs = true;
            _constrMeth = XmlILConstructMethod.Iterator;
            _parentInfo = null;
        }

        /// <summary>
        /// Xml states that are possible as construction of the annotated expression begins.
        /// </summary>
        public PossibleXmlStates InitialStates
        {
            get { return _xstatesInitial; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                _xstatesInitial = value;
            }
        }

        /// <summary>
        /// Xml states that are possible as construction of the annotated expression ends.
        /// </summary>
        public PossibleXmlStates FinalStates
        {
            get { return _xstatesFinal; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                _xstatesFinal = value;
            }
        }

        /// <summary>
        /// Xml states that are possible as looping begins.  This is None if the annotated expression does not loop.
        /// </summary>
        public PossibleXmlStates BeginLoopStates
        {
            //get { return this.xstatesBeginLoop; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                _xstatesBeginLoop = value;
            }
        }

        /// <summary>
        /// Xml states that are possible as looping ends.  This is None if the annotated expression does not loop.
        /// </summary>
        public PossibleXmlStates EndLoopStates
        {
            //get { return this.xstatesEndLoop; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                _xstatesEndLoop = value;
            }
        }

        /// <summary>
        /// Return the method that will be used to construct the annotated node.
        /// </summary>
        public XmlILConstructMethod ConstructMethod
        {
            get { return _constrMeth; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                _constrMeth = value;
            }
        }

        /// <summary>
        /// Returns true if construction method is Writer or WriterThenIterator.
        /// </summary>
        public bool PushToWriterFirst
        {
            get { return _constrMeth == XmlILConstructMethod.Writer || _constrMeth == XmlILConstructMethod.WriterThenIterator; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                Debug.Assert(value);

                switch (_constrMeth)
                {
                    case XmlILConstructMethod.Iterator:
                        _constrMeth = XmlILConstructMethod.WriterThenIterator;
                        break;

                    case XmlILConstructMethod.IteratorThenWriter:
                        _constrMeth = XmlILConstructMethod.Writer;
                        break;
                }
            }
        }

        /// <summary>
        /// Returns true if construction method is Writer or IteratorThenWriter.
        /// </summary>
        public bool PushToWriterLast
        {
            get { return _constrMeth == XmlILConstructMethod.Writer || _constrMeth == XmlILConstructMethod.IteratorThenWriter; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                Debug.Assert(value);

                switch (_constrMeth)
                {
                    case XmlILConstructMethod.Iterator:
                        _constrMeth = XmlILConstructMethod.IteratorThenWriter;
                        break;

                    case XmlILConstructMethod.WriterThenIterator:
                        _constrMeth = XmlILConstructMethod.Writer;
                        break;
                }
            }
        }

        /// <summary>
        /// Returns true if construction method is IteratorThenWriter or Iterator.
        /// </summary>
        public bool PullFromIteratorFirst
        {
            get { return _constrMeth == XmlILConstructMethod.IteratorThenWriter || _constrMeth == XmlILConstructMethod.Iterator; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                Debug.Assert(value);

                switch (_constrMeth)
                {
                    case XmlILConstructMethod.Writer:
                        _constrMeth = XmlILConstructMethod.IteratorThenWriter;
                        break;

                    case XmlILConstructMethod.WriterThenIterator:
                        _constrMeth = XmlILConstructMethod.Iterator;
                        break;
                }
            }
        }

        /// <summary>
        /// If the annotated expression will be constructed as the content of another constructor, and this can be
        /// guaranteed at compile-time, then this property will be the non-null XmlILConstructInfo of that constructor.
        /// </summary>
        public XmlILConstructInfo? ParentInfo
        {
            //get { return this.parentInfo; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                _parentInfo = value;
            }
        }

        /// <summary>
        /// If the annotated expression will be constructed as the content of an ElementCtor, and this can be
        /// guaranteed at compile-time, then this property will be the non-null XmlILConstructInfo of that constructor.
        /// </summary>
        public XmlILConstructInfo? ParentElementInfo
        {
            get
            {
                if (_parentInfo != null && _parentInfo._nodeType == QilNodeType.ElementCtor)
                    return _parentInfo;

                return null;
            }
        }

        /// <summary>
        /// This annotation is only applicable to NamespaceDecl nodes and to ElementCtor and AttributeCtor nodes with
        /// literal names.  If the namespace is already guaranteed to be constructed, then this property will be true.
        /// </summary>
        public bool IsNamespaceInScope
        {
            get { return _isNmspInScope; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                _isNmspInScope = value;
            }
        }

        /// <summary>
        /// This annotation is only applicable to ElementCtor nodes.  If the element might have local namespaces
        /// added to it at runtime, then this property will be true.
        /// </summary>
        public bool MightHaveNamespaces
        {
            get { return _mightHaveNmsp; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                _mightHaveNmsp = value;
            }
        }

        /// <summary>
        /// This annotation is only applicable to ElementCtor nodes.  If the element might have namespaces added to it after
        /// attributes have already been added, then this property will be true.
        /// </summary>
        public bool MightHaveNamespacesAfterAttributes
        {
            get { return _mightHaveNmspAfterAttrs; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                _mightHaveNmspAfterAttrs = value;
            }
        }

        /// <summary>
        /// This annotation is only applicable to ElementCtor nodes.  If the element might have attributes added to it at
        /// runtime, then this property will be true.
        /// </summary>
        public bool MightHaveAttributes
        {
            get { return _mightHaveAttrs; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                _mightHaveAttrs = value;
            }
        }

        /// <summary>
        /// This annotation is only applicable to ElementCtor nodes.  If the element might have multiple attributes added to
        /// it with the same name, then this property will be true.
        /// </summary>
        public bool MightHaveDuplicateAttributes
        {
            get { return _mightHaveDupAttrs; }
            set
            {
                Debug.Assert(!_isReadOnly, "This XmlILConstructInfo instance is read-only.");
                _mightHaveDupAttrs = value;
            }
        }

        /// <summary>
        /// This annotation is only applicable to Function nodes.  It contains a list of XmlILConstructInfo annotations
        /// for all QilInvoke nodes which call the annotated function.
        /// </summary>
        public ArrayList CallersInfo
        {
            get
            {
                if (_callersInfo == null)
                    _callersInfo = new ArrayList();

                return _callersInfo;
            }
        }

        /// <summary>
        /// Return name of this annotation.
        /// </summary>
        public string Name
        {
            get { return "ConstructInfo"; }
        }

        /// <summary>
        /// Return string representation of this annotation.
        /// </summary>
        public override string ToString()
        {
            string s = "";

            if (_constrMeth != XmlILConstructMethod.Iterator)
            {
                s += _constrMeth.ToString();

                s += $", {_xstatesInitial}";

                if (_xstatesBeginLoop != PossibleXmlStates.None)
                {
                    s += $" => {_xstatesBeginLoop} => {_xstatesEndLoop}";
                }

                s += $" => {_xstatesFinal}";

                if (!MightHaveAttributes)
                    s += ", NoAttrs";

                if (!MightHaveDuplicateAttributes)
                    s += ", NoDupAttrs";

                if (!MightHaveNamespaces)
                    s += ", NoNmsp";

                if (!MightHaveNamespacesAfterAttributes)
                    s += ", NoNmspAfterAttrs";
            }

            return s;
        }
    }


    /// <summary>
    /// Scans the content of an constructor and tries to minimize the number of well-formed checks that will have
    /// to be made at runtime when constructing content.
    /// </summary>
    internal class XmlILStateAnalyzer
    {
        protected XmlILConstructInfo? parentInfo;
        protected QilFactory fac;
        protected PossibleXmlStates xstates;
        protected bool withinElem;

        /// <summary>
        /// Constructor.
        /// </summary>
        public XmlILStateAnalyzer(QilFactory fac)
        {
            this.fac = fac;
        }

        /// <summary>
        /// Perform analysis on the specified constructor and its content.  Return the ndContent that was passed in,
        /// or a replacement.
        /// </summary>
        public virtual QilNode? Analyze(QilNode? ndConstr, QilNode? ndContent)
        {
            if (ndConstr == null)
            {
                // Root expression is analyzed
                this.parentInfo = null;
                this.xstates = PossibleXmlStates.WithinSequence;
                this.withinElem = false;

                Debug.Assert(ndContent != null);
                ndContent = AnalyzeContent(ndContent);
            }
            else
            {
                this.parentInfo = XmlILConstructInfo.Write(ndConstr);

                if (ndConstr.NodeType == QilNodeType.Function)
                {
                    // Results of function should be pushed to writer
                    this.parentInfo.ConstructMethod = XmlILConstructMethod.Writer;

                    // Start with PossibleXmlStates.None and then add additional possible starting states
                    PossibleXmlStates xstates = PossibleXmlStates.None;
                    foreach (XmlILConstructInfo infoCaller in this.parentInfo.CallersInfo)
                    {
                        if (xstates == PossibleXmlStates.None)
                        {
                            xstates = infoCaller.InitialStates;
                        }
                        else if (xstates != infoCaller.InitialStates)
                        {
                            xstates = PossibleXmlStates.Any;
                        }

                        // Function's results are pushed to Writer, so make sure that Invoke nodes' construct methods match
                        infoCaller.PushToWriterFirst = true;
                    }
                    this.parentInfo.InitialStates = xstates;
                }
                else
                {
                    // Build a standalone tree, with this constructor as its root
                    if (ndConstr.NodeType != QilNodeType.Choice)
                        this.parentInfo.InitialStates = this.parentInfo.FinalStates = PossibleXmlStates.WithinSequence;

                    // Don't stream Rtf; fully cache the Rtf and copy it into any containing tree in order to simplify XmlILVisitor.VisitRtfCtor
                    if (ndConstr.NodeType != QilNodeType.RtfCtor)
                        this.parentInfo.ConstructMethod = XmlILConstructMethod.WriterThenIterator;
                }

                // Set withinElem = true if analyzing element content
                this.withinElem = (ndConstr.NodeType == QilNodeType.ElementCtor);

                switch (ndConstr.NodeType)
                {
                    case QilNodeType.DocumentCtor: this.xstates = PossibleXmlStates.WithinContent; break;
                    case QilNodeType.ElementCtor: this.xstates = PossibleXmlStates.EnumAttrs; break;
                    case QilNodeType.AttributeCtor: this.xstates = PossibleXmlStates.WithinAttr; break;
                    case QilNodeType.NamespaceDecl: Debug.Assert(ndContent == null); break;
                    case QilNodeType.TextCtor: Debug.Assert(ndContent == null); break;
                    case QilNodeType.RawTextCtor: Debug.Assert(ndContent == null); break;
                    case QilNodeType.CommentCtor: this.xstates = PossibleXmlStates.WithinComment; break;
                    case QilNodeType.PICtor: this.xstates = PossibleXmlStates.WithinPI; break;
                    case QilNodeType.XsltCopy: this.xstates = PossibleXmlStates.Any; break;
                    case QilNodeType.XsltCopyOf: Debug.Assert(ndContent == null); break;
                    case QilNodeType.Function: this.xstates = this.parentInfo.InitialStates; break;
                    case QilNodeType.RtfCtor: this.xstates = PossibleXmlStates.WithinContent; break;
                    case QilNodeType.Choice: this.xstates = PossibleXmlStates.Any; break;
                    default: Debug.Fail($"{ndConstr.NodeType} is not handled by XmlILStateAnalyzer."); break;
                }

                if (ndContent != null)
                    ndContent = AnalyzeContent(ndContent);

                if (ndConstr.NodeType == QilNodeType.Choice)
                    AnalyzeChoice((ndConstr as QilChoice)!, this.parentInfo);

                // Since Function will never be another node's content, set its final states here
                if (ndConstr.NodeType == QilNodeType.Function)
                    this.parentInfo.FinalStates = this.xstates;
            }

            return ndContent;
        }

        /// <summary>
        /// Recursively analyze content.  Return "nd" or a replacement for it.
        /// </summary>
        protected virtual QilNode AnalyzeContent(QilNode nd)
        {
            XmlILConstructInfo info;
            QilNode ndChild;

            // Handle special node-types that are replaced
            switch (nd.NodeType)
            {
                case QilNodeType.For:
                case QilNodeType.Let:
                case QilNodeType.Parameter:
                    // Iterator references are shared and cannot be annotated directly with ConstructInfo,
                    // so wrap them with Nop node.
                    nd = this.fac.Nop(nd);
                    break;
            }

            // Get node's ConstructInfo annotation
            info = XmlILConstructInfo.Write(nd);

            // Set node's guaranteed parent constructor
            info.ParentInfo = this.parentInfo;

            // Construct all content using the Writer
            info.PushToWriterLast = true;

            // Set states that are possible before expression is constructed
            info.InitialStates = this.xstates;

            switch (nd.NodeType)
            {
                case QilNodeType.Loop: AnalyzeLoop((nd as QilLoop)!, info); break;
                case QilNodeType.Sequence: AnalyzeSequence((nd as QilList)!, info); break;
                case QilNodeType.Conditional: AnalyzeConditional((nd as QilTernary)!, info); break;
                case QilNodeType.Choice: AnalyzeChoice((nd as QilChoice)!, info); break;

                case QilNodeType.Error:
                case QilNodeType.Warning:
                    // Ensure that construct method is Writer
                    info.ConstructMethod = XmlILConstructMethod.Writer;
                    break;

                case QilNodeType.Nop:
                    ndChild = (nd as QilUnary)!.Child;
                    switch (ndChild.NodeType)
                    {
                        case QilNodeType.For:
                        case QilNodeType.Let:
                        case QilNodeType.Parameter:
                            // Copy iterator items as content
                            AnalyzeCopy(nd, info);
                            break;

                        default:
                            // Ensure that construct method is Writer and recursively analyze content
                            info.ConstructMethod = XmlILConstructMethod.Writer;
                            AnalyzeContent(ndChild);
                            break;
                    }
                    break;

                default:
                    AnalyzeCopy(nd, info);
                    break;
            }

            // Set states that are possible after expression is constructed
            info.FinalStates = this.xstates;

            return nd;
        }

        /// <summary>
        /// Analyze loop.
        /// </summary>
        protected virtual void AnalyzeLoop(QilLoop ndLoop, XmlILConstructInfo info)
        {
            XmlQueryType typ = ndLoop.XmlType!;

            // Ensure that construct method is Writer
            info.ConstructMethod = XmlILConstructMethod.Writer;

            if (!typ.IsSingleton)
                StartLoop(typ, info);

            // Body constructs content
            ndLoop.Body = AnalyzeContent(ndLoop.Body);

            if (!typ.IsSingleton)
                EndLoop(typ, info);
        }

        /// <summary>
        /// Analyze list.
        /// </summary>
        protected virtual void AnalyzeSequence(QilList ndSeq, XmlILConstructInfo info)
        {
            // Ensure that construct method is Writer
            info.ConstructMethod = XmlILConstructMethod.Writer;

            // Analyze each item in the list
            for (int idx = 0; idx < ndSeq.Count; idx++)
                ndSeq[idx] = AnalyzeContent(ndSeq[idx]);
        }

        /// <summary>
        /// Analyze conditional.
        /// </summary>
        protected virtual void AnalyzeConditional(QilTernary ndCond, XmlILConstructInfo info)
        {
            PossibleXmlStates xstatesTrue;

            // Ensure that construct method is Writer
            info.ConstructMethod = XmlILConstructMethod.Writer;

            // Visit true branch; save resulting states
            ndCond.Center = AnalyzeContent(ndCond.Center);
            xstatesTrue = this.xstates;

            // Restore starting states and visit false branch
            this.xstates = info.InitialStates;
            ndCond.Right = AnalyzeContent(ndCond.Right);

            // Conditional ending states consist of combination of true and false branch states
            if (xstatesTrue != this.xstates)
                this.xstates = PossibleXmlStates.Any;
        }

        /// <summary>
        /// Analyze choice.
        /// </summary>
        protected virtual void AnalyzeChoice(QilChoice ndChoice, XmlILConstructInfo info)
        {
            PossibleXmlStates xstatesChoice;
            int idx;

            // Visit default branch; save resulting states
            idx = ndChoice.Branches.Count - 1;
            ndChoice.Branches[idx] = AnalyzeContent(ndChoice.Branches[idx]);
            xstatesChoice = this.xstates;

            // Visit all other branches
            while (--idx >= 0)
            {
                // Restore starting states and visit the next branch
                this.xstates = info.InitialStates;
                ndChoice.Branches[idx] = AnalyzeContent(ndChoice.Branches[idx]);

                // Choice ending states consist of combination of all branch states
                if (xstatesChoice != this.xstates)
                    xstatesChoice = PossibleXmlStates.Any;
            }

            this.xstates = xstatesChoice;
        }

        /// <summary>
        /// Analyze copying items.
        /// </summary>
        protected virtual void AnalyzeCopy(QilNode ndCopy, XmlILConstructInfo info)
        {
            XmlQueryType typ = ndCopy.XmlType!;

            // Copying item(s) to output involves looping if there is not exactly one item in the sequence
            if (!typ.IsSingleton)
                StartLoop(typ, info);

            // Determine state transitions that may take place
            if (MaybeContent(typ))
            {
                if (MaybeAttrNmsp(typ))
                {
                    // Node might be Attr/Nmsp or non-Attr/Nmsp, so transition from EnumAttrs to WithinContent *may* occur
                    if (this.xstates == PossibleXmlStates.EnumAttrs)
                        this.xstates = PossibleXmlStates.Any;
                }
                else
                {
                    // Node is guaranteed not to be Attr/Nmsp, so transition to WithinContent will occur if starting
                    // state is EnumAttrs or if constructing within an element (guaranteed to be in EnumAttrs or WithinContent state)
                    if (this.xstates == PossibleXmlStates.EnumAttrs || this.withinElem)
                        this.xstates = PossibleXmlStates.WithinContent;
                }
            }

            if (!typ.IsSingleton)
                EndLoop(typ, info);
        }

        /// <summary>
        /// Calculate starting xml states that will result when iterating over and constructing an expression of the specified type.
        /// </summary>
        private void StartLoop(XmlQueryType typ, XmlILConstructInfo info)
        {
            Debug.Assert(!typ.IsSingleton);

            // This is tricky, because the looping introduces a feedback loop:
            //   1. Because loops may be executed many times, the beginning set of states must include the ending set of states.
            //   2. Because loops may be executed 0 times, the final set of states after all looping is complete must include
            //      the initial set of states.
            //
            // +-- states-initial
            // |         |
            // | states-begin-loop <--+
            // |         |            |
            // |  +--------------+    |
            // |  | Construction |    |
            // |  +--------------+    |
            // |         |            |
            // |  states-end-loop ----+
            // |         |
            // +--> states-final

            // Save starting loop states
            info.BeginLoopStates = this.xstates;

            if (typ.MaybeMany)
            {
                // If transition might occur from EnumAttrs to WithinContent, then states-end might be WithinContent, which
                // means states-begin needs to also include WithinContent.
                if (this.xstates == PossibleXmlStates.EnumAttrs && MaybeContent(typ))
                    info.BeginLoopStates = this.xstates = PossibleXmlStates.Any;
            }
        }

        /// <summary>
        /// Calculate ending xml states that will result when iterating over and constructing an expression of the specified type.
        /// </summary>
        private void EndLoop(XmlQueryType typ, XmlILConstructInfo info)
        {
            Debug.Assert(!typ.IsSingleton);

            // Save ending loop states
            info.EndLoopStates = this.xstates;

            // If it's possible to loop zero times, then states-final needs to include states-initial
            if (typ.MaybeEmpty && info.InitialStates != this.xstates)
                this.xstates = PossibleXmlStates.Any;
        }

        /// <summary>
        /// Return true if an instance of the specified type might be an attribute or a namespace node.
        /// </summary>
        private static bool MaybeAttrNmsp(XmlQueryType typ)
        {
            return (typ.NodeKinds & (XmlNodeKindFlags.Attribute | XmlNodeKindFlags.Namespace)) != XmlNodeKindFlags.None;
        }

        /// <summary>
        /// Return true if an instance of the specified type might be a non-empty content type (attr/nsmp don't count).
        /// </summary>
        private static bool MaybeContent(XmlQueryType typ)
        {
            return !typ.IsNode || (typ.NodeKinds & ~(XmlNodeKindFlags.Attribute | XmlNodeKindFlags.Namespace)) != XmlNodeKindFlags.None;
        }
    }


    /// <summary>
    /// Scans the content of an ElementCtor and tries to minimize the number of well-formed checks that will have
    /// to be made at runtime when constructing content.
    /// </summary>
    internal sealed class XmlILElementAnalyzer : XmlILStateAnalyzer
    {
        private readonly NameTable _attrNames = new NameTable();
        private readonly ArrayList _dupAttrs = new ArrayList();

        /// <summary>
        /// Constructor.
        /// </summary>
        public XmlILElementAnalyzer(QilFactory fac) : base(fac)
        {
        }

        /// <summary>
        /// Analyze the content argument of the ElementCtor.  Try to eliminate as many runtime checks as possible,
        /// both for the ElementCtor and for content constructors.
        /// </summary>
        public override QilNode? Analyze(QilNode? ndElem, QilNode? ndContent)
        {
            Debug.Assert(ndElem!.NodeType == QilNodeType.ElementCtor);
            this.parentInfo = XmlILConstructInfo.Write(ndElem);

            // Start by assuming that these properties are false (they default to true, but analyzer might be able to
            // prove they are really false).
            this.parentInfo.MightHaveNamespacesAfterAttributes = false;
            this.parentInfo.MightHaveAttributes = false;
            this.parentInfo.MightHaveDuplicateAttributes = false;

            // The element's namespace might need to be declared
            this.parentInfo.MightHaveNamespaces = !this.parentInfo.IsNamespaceInScope;

            // Clear list of duplicate attributes
            _dupAttrs.Clear();

            return base.Analyze(ndElem, ndContent);
        }

        /// <summary>
        /// Analyze loop.
        /// </summary>
        protected override void AnalyzeLoop(QilLoop ndLoop, XmlILConstructInfo info)
        {
            // Constructing attributes/namespaces in a loop can cause duplicates, namespaces after attributes, etc.
            if (ndLoop.XmlType!.MaybeMany)
                CheckAttributeNamespaceConstruct(ndLoop.XmlType);

            base.AnalyzeLoop(ndLoop, info);
        }

        /// <summary>
        /// Analyze copying items.
        /// </summary>
        protected override void AnalyzeCopy(QilNode ndCopy, XmlILConstructInfo info)
        {
            if (ndCopy.NodeType == QilNodeType.AttributeCtor)
            {
                QilBinary? binaryNode = ndCopy as QilBinary;
                Debug.Assert(binaryNode != null);
                AnalyzeAttributeCtor(binaryNode, info);
            }
            else
            {
                CheckAttributeNamespaceConstruct(ndCopy.XmlType!);
            }

            base.AnalyzeCopy(ndCopy, info);
        }

        /// <summary>
        /// Analyze attribute constructor.
        /// </summary>
        private void AnalyzeAttributeCtor(QilBinary ndAttr, XmlILConstructInfo info)
        {
            if (ndAttr.Left.NodeType == QilNodeType.LiteralQName)
            {
                QilName? ndName = ndAttr.Left as QilName;
                Debug.Assert(ndName != null);
                XmlQualifiedName qname;
                int idx;

                // This attribute might be constructed on the parent element
                this.parentInfo!.MightHaveAttributes = true;

                // Check to see whether this attribute is a duplicate of a previous attribute
                if (!this.parentInfo.MightHaveDuplicateAttributes)
                {
                    qname = new XmlQualifiedName(_attrNames.Add(ndName.LocalName), _attrNames.Add(ndName.NamespaceUri));

                    for (idx = 0; idx < _dupAttrs.Count; idx++)
                    {
                        XmlQualifiedName qnameDup = (XmlQualifiedName)_dupAttrs[idx]!;

                        if ((object)qnameDup.Name == (object)qname.Name && (object)qnameDup.Namespace == (object)qname.Namespace)
                        {
                            // A duplicate attribute has been encountered
                            this.parentInfo.MightHaveDuplicateAttributes = true;
                        }
                    }

                    if (idx >= _dupAttrs.Count)
                    {
                        // This is not a duplicate attribute, so add it to the set
                        _dupAttrs.Add(qname);
                    }
                }

                // The attribute's namespace might need to be declared
                if (!info.IsNamespaceInScope)
                    this.parentInfo.MightHaveNamespaces = true;
            }
            else
            {
                // Attribute prefix and namespace are not known at compile-time
                CheckAttributeNamespaceConstruct(ndAttr.XmlType!);
            }
        }

        /// <summary>
        /// If type might contain attributes or namespaces, set appropriate parent element flags.
        /// </summary>
        private void CheckAttributeNamespaceConstruct(XmlQueryType typ)
        {
            // If content might contain attributes,
            if ((typ.NodeKinds & XmlNodeKindFlags.Attribute) != XmlNodeKindFlags.None)
            {
                // Mark element as possibly having attributes and duplicate attributes (since we don't know the names)
                this.parentInfo!.MightHaveAttributes = true;
                this.parentInfo.MightHaveDuplicateAttributes = true;

                // Attribute namespaces might be declared
                this.parentInfo.MightHaveNamespaces = true;
            }

            // If content might contain namespaces,
            if ((typ.NodeKinds & XmlNodeKindFlags.Namespace) != XmlNodeKindFlags.None)
            {
                // Then element might have namespaces,
                this.parentInfo!.MightHaveNamespaces = true;

                // If attributes might already have been constructed,
                if (this.parentInfo.MightHaveAttributes)
                {
                    // Then attributes might precede namespace declarations
                    this.parentInfo.MightHaveNamespacesAfterAttributes = true;
                }
            }
        }
    }


    /// <summary>
    /// Scans constructed content, looking for redundant namespace declarations.  If any are found, then they are marked
    /// and removed later.
    /// </summary>
    internal sealed class XmlILNamespaceAnalyzer
    {
        private readonly XmlNamespaceManager _nsmgr = new XmlNamespaceManager(new NameTable());
        private bool _addInScopeNmsp;
        private int _cntNmsp;

        /// <summary>
        /// Perform scan.
        /// </summary>
        public void Analyze(QilNode nd, bool defaultNmspInScope)
        {
            _addInScopeNmsp = false;
            _cntNmsp = 0;

            // If xmlns="" is in-scope, push it onto the namespace stack
            if (defaultNmspInScope)
            {
                _nsmgr.PushScope();
                _nsmgr.AddNamespace(string.Empty, string.Empty);
                _cntNmsp++;
            }

            AnalyzeContent(nd);

            if (defaultNmspInScope)
                _nsmgr.PopScope();
        }

        /// <summary>
        /// Recursively analyze content.  Return "nd" or a replacement for it.
        /// </summary>
        private void AnalyzeContent(QilNode nd)
        {
            int cntNmspSave;

            switch (nd.NodeType)
            {
                case QilNodeType.Loop:
                    _addInScopeNmsp = false;
                    AnalyzeContent((nd as QilLoop)!.Body);
                    break;

                case QilNodeType.Sequence:
                    foreach (QilNode ndContent in nd)
                        AnalyzeContent(ndContent);
                    break;

                case QilNodeType.Conditional:
                    _addInScopeNmsp = false;
                    AnalyzeContent((nd as QilTernary)!.Center);
                    AnalyzeContent((nd as QilTernary)!.Right);
                    break;

                case QilNodeType.Choice:
                    _addInScopeNmsp = false;
                    QilList ndBranches = (nd as QilChoice)!.Branches;
                    for (int idx = 0; idx < ndBranches.Count; idx++)
                        AnalyzeContent(ndBranches[idx]);

                    break;

                case QilNodeType.ElementCtor:
                    // Start a new namespace scope
                    _addInScopeNmsp = true;
                    _nsmgr.PushScope();
                    cntNmspSave = _cntNmsp;

                    if (CheckNamespaceInScope((nd as QilBinary)!))
                        AnalyzeContent((nd as QilBinary)!.Right);

                    _nsmgr.PopScope();
                    _addInScopeNmsp = false;
                    _cntNmsp = cntNmspSave;
                    break;

                case QilNodeType.AttributeCtor:
                    _addInScopeNmsp = false;
                    CheckNamespaceInScope((nd as QilBinary)!);
                    break;

                case QilNodeType.NamespaceDecl:
                    CheckNamespaceInScope((nd as QilBinary)!);
                    break;

                case QilNodeType.Nop:
                    AnalyzeContent((nd as QilUnary)!.Child);
                    break;

                default:
                    _addInScopeNmsp = false;
                    break;
            }
        }

        /// <summary>
        /// Determine whether an ElementCtor, AttributeCtor, or NamespaceDecl's namespace is already declared.  If it is,
        /// set the IsNamespaceInScope property to True.  Otherwise, add the namespace to the set of in-scope namespaces if
        /// addInScopeNmsp is True.  Return false if the name is computed or is invalid.
        /// </summary>
        private bool CheckNamespaceInScope(QilBinary nd)
        {
            QilName? ndName;
            string prefix, ns;
            string? prefixExisting, nsExisting;
            XPathNodeType nodeType;

            switch (nd.NodeType)
            {
                case QilNodeType.ElementCtor:
                case QilNodeType.AttributeCtor:
                    ndName = nd.Left as QilName;
                    if (ndName != null)
                    {
                        prefix = ndName.Prefix;
                        ns = ndName.NamespaceUri;
                        nodeType = (nd.NodeType == QilNodeType.ElementCtor) ? XPathNodeType.Element : XPathNodeType.Attribute;
                        break;
                    }

                    // Not a literal name, so return false
                    return false;

                default:
                    Debug.Assert(nd.NodeType == QilNodeType.NamespaceDecl);
                    prefix = (string)(QilLiteral)nd.Left;
                    ns = (string)(QilLiteral)nd.Right;
                    nodeType = XPathNodeType.Namespace;
                    break;
            }

            // Attribute with null namespace and xmlns:xml are always in-scope
            if (nd.NodeType == QilNodeType.AttributeCtor && ns.Length == 0 ||
                prefix == "xml" && ns == XmlReservedNs.NsXml)
            {
                XmlILConstructInfo.Write(nd).IsNamespaceInScope = true;
                return true;
            }

            // Don't process names that are invalid
            if (!ValidateNames.ValidateName(prefix, string.Empty, ns, nodeType, ValidateNames.Flags.CheckPrefixMapping))
                return false;

            // Atomize names
            prefix = _nsmgr.NameTable!.Add(prefix);
            ns = _nsmgr.NameTable.Add(ns);

            // Determine whether namespace is already in-scope
            for (int iNmsp = 0; iNmsp < _cntNmsp; iNmsp++)
            {
                _nsmgr.GetNamespaceDeclaration(iNmsp, out prefixExisting, out nsExisting);

                // If prefix is already declared,
                if ((object)prefix == (object?)prefixExisting)
                {
                    // Then if the namespace is the same, this namespace is redundant
                    if ((object)ns == (object?)nsExisting)
                        XmlILConstructInfo.Write(nd).IsNamespaceInScope = true;

                    // Else quit searching, because any further matching prefixes will be hidden (not in-scope)
                    Debug.Assert(nd.NodeType != QilNodeType.NamespaceDecl || !_nsmgr.HasNamespace(prefix) || _nsmgr.LookupNamespace(prefix) == ns,
                        "Compilers must ensure that namespace declarations do not conflict with the namespace used by the element constructor.");
                    break;
                }
            }

            // If not in-scope, then add if it's allowed
            if (_addInScopeNmsp)
            {
                _nsmgr.AddNamespace(prefix, ns);
                _cntNmsp++;
            }

            return true;
        }
    }
}
