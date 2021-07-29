// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Xsl;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl.IlGen
{
    using TypeFactory = System.Xml.Xsl.XmlQueryTypeFactory;

    /// <summary>
    /// Creates Msil code for an entire QilExpression graph.  Code is generated in one of two modes: push or
    /// pull.  In push mode, code is generated to push the values in an iterator to the XmlWriter
    /// interface.  In pull mode, the values in an iterator are stored in a physical location such as
    /// the stack or a local variable by an iterator.  The iterator is passive, and will just wait for
    /// a caller to pull the data and/or instruct the iterator to enumerate the next value.
    /// </summary>
    internal sealed class XmlILVisitor : QilVisitor
    {
        private QilExpression _qil = null!;
        private GenerateHelper _helper = null!;
        private IteratorDescriptor _iterCurr = null!;
        private IteratorDescriptor? _iterNested;
        private int _indexId;

        [RequiresUnreferencedCode("Method VisitXsltInvokeEarlyBound will require code that cannot be statically analyzed.")]
        public XmlILVisitor()
        { }

        //-----------------------------------------------
        // Entry
        //-----------------------------------------------

        /// <summary>
        /// Visits the specified QilExpression graph and generates MSIL code.
        /// </summary>
        public void Visit(QilExpression qil, GenerateHelper helper, MethodInfo methRoot)
        {
            _qil = qil;
            _helper = helper;
            _iterNested = null;
            _indexId = 0;

            // Prepare each global parameter and global variable to be visited
            PrepareGlobalValues(qil.GlobalParameterList);
            PrepareGlobalValues(qil.GlobalVariableList);

            // Visit each global parameter and global variable
            VisitGlobalValues(qil.GlobalParameterList);
            VisitGlobalValues(qil.GlobalVariableList);

            // Build each function
            foreach (QilFunction ndFunc in qil.FunctionList)
            {
                // Visit each parameter and the function body
                Function(ndFunc);
            }

            // Build the root expression
            _helper.MethodBegin(methRoot, null, true);
            StartNestedIterator(qil.Root);
            Visit(qil.Root);
            Debug.Assert(_iterCurr.Storage.Location == ItemLocation.None, "Root expression should have been pushed to the writer.");
            EndNestedIterator(qil.Root);
            _helper.MethodEnd();
        }

        /// <summary>
        /// Create IteratorDescriptor for each global value.  This pre-visit is necessary because a global early
        /// in the list may reference a global later in the list and therefore expect its IteratorDescriptor to already
        /// be initialized.
        /// </summary>
        private void PrepareGlobalValues(QilList globalIterators)
        {
            MethodInfo? methGlobal;
            IteratorDescriptor iterInfo;

            foreach (QilIterator iter in globalIterators)
            {
                Debug.Assert(iter.NodeType == QilNodeType.Let || iter.NodeType == QilNodeType.Parameter);

                // Get metadata for method which computes this global's value
                methGlobal = XmlILAnnotation.Write(iter).FunctionBinding;
                Debug.Assert(methGlobal != null, "Metadata for global value should have already been computed");

                // Create an IteratorDescriptor for this global value
                iterInfo = new IteratorDescriptor(_helper);

                // Iterator items will be stored in a global location
                iterInfo.Storage = StorageDescriptor.Global(methGlobal, GetItemStorageType(iter), !iter.XmlType!.IsSingleton);

                // Associate IteratorDescriptor with parameter
                XmlILAnnotation.Write(iter).CachedIteratorDescriptor = iterInfo;
            }
        }

        /// <summary>
        /// Visit each global variable or parameter.  Create a IteratorDescriptor for each global value.  Generate code for
        /// default values.
        /// </summary>
        private void VisitGlobalValues(QilList globalIterators)
        {
            MethodInfo methGlobal;
            Label lblGetGlobal, lblComputeGlobal;
            bool isCached;
            int idxValue;

            foreach (QilIterator iter in globalIterators)
            {
                QilParameter? param = iter as QilParameter;

                // Get MethodInfo for method that computes the value of this global
                methGlobal = XmlILAnnotation.Write(iter).CachedIteratorDescriptor!.Storage.GlobalLocation!;
                isCached = !iter.XmlType!.IsSingleton;

                // Notify the StaticDataManager of the new global value
                idxValue = _helper.StaticData.DeclareGlobalValue(iter.DebugName!);

                // Generate code for this method
                _helper.MethodBegin(methGlobal, iter.SourceLine, false);

                lblGetGlobal = _helper.DefineLabel();
                lblComputeGlobal = _helper.DefineLabel();

                // if (runtime.IsGlobalComputed(idx)) goto LabelGetGlobal;
                _helper.LoadQueryRuntime();
                _helper.LoadInteger(idxValue);
                _helper.Call(XmlILMethods.GlobalComputed);
                _helper.Emit(OpCodes.Brtrue, lblGetGlobal);

                // Compute value of global value
                StartNestedIterator(iter);

                if (param != null)
                {
                    Debug.Assert(iter.XmlType == TypeFactory.ItemS, "IlGen currently only supports parameters of type item*.");

                    // param = runtime.ExternalContext.GetParameter(localName, namespaceUri);
                    // if (param == null) goto LabelComputeGlobal;
                    LocalBuilder locParam = _helper.DeclareLocal("$$$param", typeof(object));
                    _helper.CallGetParameter(param.Name!.LocalName, param.Name.NamespaceUri);
                    _helper.Emit(OpCodes.Stloc, locParam);
                    _helper.Emit(OpCodes.Ldloc, locParam);
                    _helper.Emit(OpCodes.Brfalse, lblComputeGlobal);

                    // runtime.SetGlobalValue(idxValue, runtime.ChangeTypeXsltResult(idxType, value));
                    // Ensure that the storage type of the parameter corresponds to static type
                    _helper.LoadQueryRuntime();
                    _helper.LoadInteger(idxValue);

                    _helper.LoadQueryRuntime();
                    _helper.LoadInteger(_helper.StaticData.DeclareXmlType(XmlQueryTypeFactory.ItemS));
                    _helper.Emit(OpCodes.Ldloc, locParam);
                    _helper.Call(XmlILMethods.ChangeTypeXsltResult);

                    _helper.CallSetGlobalValue(typeof(object));

                    // goto LabelGetGlobal;
                    _helper.EmitUnconditionalBranch(OpCodes.Br, lblGetGlobal);
                }

                // LabelComputeGlobal:
                _helper.MarkLabel(lblComputeGlobal);

                if (iter.Binding != null)
                {
                    // runtime.SetGlobalValue(idxValue, (object) value);
                    _helper.LoadQueryRuntime();
                    _helper.LoadInteger(idxValue);

                    // Compute value of global value
                    NestedVisitEnsureStack(iter.Binding, GetItemStorageType(iter), isCached);

                    _helper.CallSetGlobalValue(GetStorageType(iter));
                }
                else
                {
                    // Throw exception, as there is no default value for this parameter
                    // XmlQueryRuntime.ThrowException("...");
                    Debug.Assert(iter.NodeType == QilNodeType.Parameter, "Only parameters may not have a default value");
                    _helper.LoadQueryRuntime();
                    _helper.Emit(OpCodes.Ldstr, SR.Format(SR.XmlIl_UnknownParam, new string?[] { param!.Name!.LocalName, param.Name.NamespaceUri }));
                    _helper.Call(XmlILMethods.ThrowException);
                }

                EndNestedIterator(iter);

                // LabelGetGlobal:
                // return (T) runtime.GetGlobalValue(idxValue);
                _helper.MarkLabel(lblGetGlobal);
                _helper.CallGetGlobalValue(idxValue, GetStorageType(iter));

                _helper.MethodEnd();
            }
        }

        /// <summary>
        /// Generate code for the specified function.
        /// </summary>
        private void Function(QilFunction ndFunc)
        {
            MethodInfo methFunc;
            int paramId;
            IteratorDescriptor iterInfo;
            bool useWriter;

            // Annotate each function parameter with a IteratorDescriptor
            foreach (QilIterator iter in ndFunc.Arguments)
            {
                Debug.Assert(iter.NodeType == QilNodeType.Parameter);

                // Create an IteratorDescriptor for this parameter
                iterInfo = new IteratorDescriptor(_helper);

                // Add one to parameter index, as 0th parameter is always "this"
                paramId = XmlILAnnotation.Write(iter).ArgumentPosition + 1;

                // The ParameterInfo for each argument should be set as its location
                iterInfo.Storage = StorageDescriptor.Parameter(paramId, GetItemStorageType(iter), !iter.XmlType!.IsSingleton);

                // Associate IteratorDescriptor with Let iterator
                XmlILAnnotation.Write(iter).CachedIteratorDescriptor = iterInfo;
            }

            methFunc = XmlILAnnotation.Write(ndFunc).FunctionBinding!;
            useWriter = (XmlILConstructInfo.Read(ndFunc).ConstructMethod == XmlILConstructMethod.Writer);

            // Generate query code from QilExpression tree
            _helper.MethodBegin(methFunc, ndFunc.SourceLine, useWriter);

            foreach (QilIterator iter in ndFunc.Arguments)
            {
                // DebugInfo: Sequence point just before generating code for the bound expression
                if (_qil.IsDebug && iter.SourceLine != null)
                    _helper.DebugSequencePoint(iter.SourceLine);

                // Calculate default value of this parameter
                if (iter.Binding != null)
                {
                    Debug.Assert(iter.XmlType == TypeFactory.ItemS, "IlGen currently only supports default values in parameters of type item*.");
                    paramId = (iter.Annotation as XmlILAnnotation)!.ArgumentPosition + 1;

                    // runtime.MatchesXmlType(param, XmlTypeCode.QName);
                    Label lblLocalComputed = _helper.DefineLabel();
                    _helper.LoadQueryRuntime();
                    _helper.LoadParameter(paramId);
                    _helper.LoadInteger((int)XmlTypeCode.QName);
                    _helper.Call(XmlILMethods.SeqMatchesCode);

                    _helper.Emit(OpCodes.Brfalse, lblLocalComputed);

                    // Compute default value of this parameter
                    StartNestedIterator(iter);
                    NestedVisitEnsureStack(iter.Binding, GetItemStorageType(iter), /*isCached:*/!iter.XmlType.IsSingleton);
                    EndNestedIterator(iter);

                    _helper.SetParameter(paramId);
                    _helper.MarkLabel(lblLocalComputed);
                }
            }

            StartNestedIterator(ndFunc);

            // If function did not push results to writer, then function will return value(s) (rather than void)
            if (useWriter)
                NestedVisit(ndFunc.Definition);
            else
                NestedVisitEnsureStack(ndFunc.Definition, GetItemStorageType(ndFunc), !ndFunc.XmlType!.IsSingleton);

            EndNestedIterator(ndFunc);

            _helper.MethodEnd();
        }

        //-----------------------------------------------
        // QilVisitor
        //-----------------------------------------------

        /// <summary>
        /// Generate a query plan for the QilExpression subgraph.
        /// </summary>
        protected override QilNode Visit(QilNode nd)
        {
            if (nd == null)
                return null!;

            // DebugInfo: Sequence point just before generating code for this expression
            if (_qil.IsDebug && nd.SourceLine != null && !(nd is QilIterator))
                _helper.DebugSequencePoint(nd.SourceLine);

            // Expressions are constructed using one of several possible methods
            switch (XmlILConstructInfo.Read(nd).ConstructMethod)
            {
                case XmlILConstructMethod.WriterThenIterator:
                    // Push results of expression to cached writer; then iterate over cached results
                    NestedConstruction(nd);
                    break;

                case XmlILConstructMethod.IteratorThenWriter:
                    // Iterate over items in the sequence; send items to writer
                    CopySequence(nd);
                    break;

                case XmlILConstructMethod.Iterator:
                    Debug.Assert(nd.XmlType!.IsSingleton || CachesResult(nd) || _iterCurr.HasLabelNext,
                                 "When generating code for a non-singleton expression, LabelNext must be defined.");
                    goto default;

                default:
                    // Allow base internal class to dispatch to correct Visit method
                    base.Visit(nd);
                    break;
            }

            return nd;
        }

        /// <summary>
        /// VisitChildren should never be called.
        /// </summary>
        protected override QilNode VisitChildren(QilNode parent)
        {
            Debug.Fail("Visit" + parent.NodeType + " should never be called");
            return parent;
        }

        /// <summary>
        /// Generate code to cache a sequence of items that are pushed to output.
        /// </summary>
        private void NestedConstruction(QilNode nd)
        {
            // Start nested construction of a sequence of items
            _helper.CallStartSequenceConstruction();

            // Allow base internal class to dispatch to correct Visit method
            base.Visit(nd);

            // Get the result sequence
            _helper.CallEndSequenceConstruction();
            _iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathItem), true);
        }

        /// <summary>
        /// Iterate over items produced by the "nd" expression and copy each item to output.
        /// </summary>
        private void CopySequence(QilNode nd)
        {
            XmlQueryType typ = nd.XmlType!;
            bool hasOnEnd;
            Label lblOnEnd;

            StartWriterLoop(nd, out hasOnEnd, out lblOnEnd);

            if (typ.IsSingleton)
            {
                // Always write atomic values via XmlQueryOutput
                _helper.LoadQueryOutput();

                // Allow base internal class to dispatch to correct Visit method
                base.Visit(nd);
                _iterCurr.EnsureItemStorageType(nd.XmlType!, typeof(XPathItem));
            }
            else
            {
                // Allow base internal class to dispatch to correct Visit method
                base.Visit(nd);
                _iterCurr.EnsureItemStorageType(nd.XmlType!, typeof(XPathItem));

                // Save any stack values in a temporary local
                _iterCurr.EnsureNoStackNoCache("$$$copyTemp");

                _helper.LoadQueryOutput();
            }

            // Write value to output
            _iterCurr.EnsureStackNoCache();
            _helper.Call(XmlILMethods.WriteItem);

            EndWriterLoop(nd, hasOnEnd, lblOnEnd);
        }

        /// <summary>
        /// Generate code for QilNodeType.DataSource.
        /// </summary>
        /// <remarks>
        /// Generates code to retrieve a document using the XmlResolver.
        /// </remarks>
        protected override QilNode VisitDataSource(QilDataSource ndSrc)
        {
            LocalBuilder locNav;

            // XPathNavigator navDoc = runtime.ExternalContext.GetEntity(uri)
            _helper.LoadQueryContext();
            NestedVisitEnsureStack(ndSrc.Name);
            NestedVisitEnsureStack(ndSrc.BaseUri);
            _helper.Call(XmlILMethods.GetDataSource);

            locNav = _helper.DeclareLocal("$$$navDoc", typeof(XPathNavigator));
            _helper.Emit(OpCodes.Stloc, locNav);

            // if (navDoc == null) goto LabelNextCtxt;
            _helper.Emit(OpCodes.Ldloc, locNav);
            _helper.Emit(OpCodes.Brfalse, _iterCurr.GetLabelNext());

            _iterCurr.Storage = StorageDescriptor.Local(locNav, typeof(XPathNavigator), false);

            return ndSrc;
        }

        /// <summary>
        /// Generate code for QilNodeType.Nop.
        /// </summary>
        protected override QilNode VisitNop(QilUnary ndNop)
        {
            return Visit(ndNop.Child);
        }

        /// <summary>
        /// Generate code for QilNodeType.OptimizeBarrier.
        /// </summary>
        protected override QilNode VisitOptimizeBarrier(QilUnary ndBarrier)
        {
            return Visit(ndBarrier.Child);
        }

        /// <summary>
        /// Generate code for QilNodeType.Error.
        /// </summary>
        protected override QilNode VisitError(QilUnary ndErr)
        {
            // XmlQueryRuntime.ThrowException(strErr);
            _helper.LoadQueryRuntime();
            NestedVisitEnsureStack(ndErr.Child);
            _helper.Call(XmlILMethods.ThrowException);

            if (XmlILConstructInfo.Read(ndErr).ConstructMethod == XmlILConstructMethod.Writer)
            {
                _iterCurr.Storage = StorageDescriptor.None();
            }
            else
            {
                // Push dummy value so that Location is not None and IL rules are met
                _helper.Emit(OpCodes.Ldnull);
                _iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathItem), false);
            }

            return ndErr;
        }

        /// <summary>
        /// Generate code for QilNodeType.Warning.
        /// </summary>
        protected override QilNode VisitWarning(QilUnary ndWarning)
        {
            // runtime.SendMessage(strWarning);
            _helper.LoadQueryRuntime();
            NestedVisitEnsureStack(ndWarning.Child);
            _helper.Call(XmlILMethods.SendMessage);

            if (XmlILConstructInfo.Read(ndWarning).ConstructMethod == XmlILConstructMethod.Writer)
                _iterCurr.Storage = StorageDescriptor.None();
            else
                VisitEmpty(ndWarning);

            return ndWarning;
        }

        /// <summary>
        /// Generate code for QilNodeType.True.
        /// </summary>
        /// <remarks>
        /// BranchingContext.OnFalse context: [nothing]
        /// BranchingContext.OnTrue context:  goto LabelParent;
        /// BranchingContext.None context:  push true();
        /// </remarks>
        protected override QilNode VisitTrue(QilNode ndTrue)
        {
            if (_iterCurr.CurrentBranchingContext != BranchingContext.None)
            {
                // Make sure there's an IL code path to both the true and false branches in order to avoid dead
                // code which can cause IL verification errors.
                _helper.EmitUnconditionalBranch(_iterCurr.CurrentBranchingContext == BranchingContext.OnTrue ?
                        OpCodes.Brtrue : OpCodes.Brfalse, _iterCurr.LabelBranch);

                _iterCurr.Storage = StorageDescriptor.None();
            }
            else
            {
                // Push boolean result onto the stack
                _helper.LoadBoolean(true);
                _iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), false);
            }

            return ndTrue;
        }

        /// <summary>
        /// Generate code for QilNodeType.False.
        /// </summary>
        /// <remarks>
        /// BranchingContext.OnFalse context: goto LabelParent;
        /// BranchingContext.OnTrue context:  [nothing]
        /// BranchingContext.None context:  push false();
        /// </remarks>
        protected override QilNode VisitFalse(QilNode ndFalse)
        {
            if (_iterCurr.CurrentBranchingContext != BranchingContext.None)
            {
                // Make sure there's an IL code path to both the true and false branches in order to avoid dead
                // code which can cause IL verification errors.
                _helper.EmitUnconditionalBranch(_iterCurr.CurrentBranchingContext == BranchingContext.OnFalse ?
                        OpCodes.Brtrue : OpCodes.Brfalse, _iterCurr.LabelBranch);

                _iterCurr.Storage = StorageDescriptor.None();
            }
            else
            {
                // Push boolean result onto the stack
                _helper.LoadBoolean(false);
                _iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), false);
            }

            return ndFalse;
        }

        /// <summary>
        /// Generate code for QilNodeType.LiteralString.
        /// </summary>
        protected override QilNode VisitLiteralString(QilLiteral ndStr)
        {
            _helper.Emit(OpCodes.Ldstr, (string)ndStr);
            _iterCurr.Storage = StorageDescriptor.Stack(typeof(string), false);
            return ndStr;
        }

        /// <summary>
        /// Generate code for QilNodeType.LiteralInt32.
        /// </summary>
        protected override QilNode VisitLiteralInt32(QilLiteral ndInt)
        {
            _helper.LoadInteger((int)ndInt);
            _iterCurr.Storage = StorageDescriptor.Stack(typeof(int), false);
            return ndInt;
        }

        /// <summary>
        /// Generate code for QilNodeType.LiteralInt64.
        /// </summary>
        protected override QilNode VisitLiteralInt64(QilLiteral ndLong)
        {
            _helper.Emit(OpCodes.Ldc_I8, (long)ndLong);
            _iterCurr.Storage = StorageDescriptor.Stack(typeof(long), false);
            return ndLong;
        }

        /// <summary>
        /// Generate code for QilNodeType.LiteralDouble.
        /// </summary>
        protected override QilNode VisitLiteralDouble(QilLiteral ndDbl)
        {
            _helper.Emit(OpCodes.Ldc_R8, (double)ndDbl);
            _iterCurr.Storage = StorageDescriptor.Stack(typeof(double), false);
            return ndDbl;
        }

        /// <summary>
        /// Generate code for QilNodeType.LiteralDecimal.
        /// </summary>
        protected override QilNode VisitLiteralDecimal(QilLiteral ndDec)
        {
            _helper.ConstructLiteralDecimal((decimal)ndDec);
            _iterCurr.Storage = StorageDescriptor.Stack(typeof(decimal), false);
            return ndDec;
        }

        /// <summary>
        /// Generate code for QilNodeType.LiteralQName.
        /// </summary>
        protected override QilNode VisitLiteralQName(QilName ndQName)
        {
            _helper.ConstructLiteralQName(ndQName.LocalName, ndQName.NamespaceUri);
            _iterCurr.Storage = StorageDescriptor.Stack(typeof(XmlQualifiedName), false);
            return ndQName;
        }

        /// <summary>
        /// Generate code for QilNodeType.And.
        /// </summary>
        /// <remarks>
        /// BranchingContext.OnFalse context: (expr1) and (expr2)
        /// ==> if (!expr1) goto LabelParent;
        ///     if (!expr2) goto LabelParent;
        ///
        /// BranchingContext.OnTrue context: (expr1) and (expr2)
        /// ==> if (!expr1) goto LabelTemp;
        ///     if (expr1) goto LabelParent;
        ///     LabelTemp:
        ///
        /// BranchingContext.None context: (expr1) and (expr2)
        /// ==> if (!expr1) goto LabelTemp;
        ///     if (!expr1) goto LabelTemp;
        ///     push true();
        ///     goto LabelSkip;
        ///     LabelTemp:
        ///     push false();
        ///     LabelSkip:
        ///
        /// </remarks>
        protected override QilNode VisitAnd(QilBinary ndAnd)
        {
            IteratorDescriptor iterParent = _iterCurr;
            Label lblOnFalse;

            // Visit left branch
            StartNestedIterator(ndAnd.Left);
            lblOnFalse = StartConjunctiveTests(iterParent.CurrentBranchingContext, iterParent.LabelBranch);
            Visit(ndAnd.Left);
            EndNestedIterator(ndAnd.Left);

            // Visit right branch
            StartNestedIterator(ndAnd.Right);
            StartLastConjunctiveTest(iterParent.CurrentBranchingContext, iterParent.LabelBranch, lblOnFalse);
            Visit(ndAnd.Right);
            EndNestedIterator(ndAnd.Right);

            // End And expression
            EndConjunctiveTests(iterParent.CurrentBranchingContext, iterParent.LabelBranch, lblOnFalse);

            return ndAnd;
        }

        /// <summary>
        /// Fixup branching context for all but the last test in a conjunctive (Logical And) expression.
        /// Return a temporary label which will be passed to StartLastAndBranch() and EndAndBranch().
        /// </summary>
        private Label StartConjunctiveTests(BranchingContext brctxt, Label lblBranch)
        {
            Label lblOnFalse;

            switch (brctxt)
            {
                case BranchingContext.OnFalse:
                    // If condition evaluates to false, branch to false label
                    _iterCurr.SetBranching(BranchingContext.OnFalse, lblBranch);
                    return lblBranch;

                default:
                    // If condition evaluates to false:
                    //   1. Jump to new false label that will be fixed just beyond the second condition
                    //   2. Or, jump to code that pushes "false"
                    lblOnFalse = _helper.DefineLabel();
                    _iterCurr.SetBranching(BranchingContext.OnFalse, lblOnFalse);
                    return lblOnFalse;
            }
        }

        /// <summary>
        /// Fixup branching context for the last test in a conjunctive (Logical And) expression.
        /// </summary>
        private void StartLastConjunctiveTest(BranchingContext brctxt, Label lblBranch, Label lblOnFalse)
        {
            switch (brctxt)
            {
                case BranchingContext.OnTrue:
                    // If last condition evaluates to true, branch to true label
                    _iterCurr.SetBranching(BranchingContext.OnTrue, lblBranch);
                    break;

                default:
                    // If last condition evalutes to false, branch to false label
                    // Else fall through to true code path
                    _iterCurr.SetBranching(BranchingContext.OnFalse, lblOnFalse);
                    break;
            }
        }

        /// <summary>
        /// Anchor any remaining labels.
        /// </summary>
        private void EndConjunctiveTests(BranchingContext brctxt, Label lblBranch, Label lblOnFalse)
        {
            switch (brctxt)
            {
                case BranchingContext.OnTrue:
                    // Anchor false label
                    _helper.MarkLabel(lblOnFalse);
                    goto case BranchingContext.OnFalse;

                case BranchingContext.OnFalse:
                    _iterCurr.Storage = StorageDescriptor.None();
                    break;

                case BranchingContext.None:
                    // Convert branch targets into push of true/false
                    _helper.ConvBranchToBool(lblOnFalse, false);
                    _iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), false);
                    break;
            }
        }

        /// <summary>
        /// Generate code for QilNodeType.Or.
        /// </summary>
        /// <remarks>
        /// BranchingContext.OnFalse context: (expr1) or (expr2)
        /// ==> if (expr1) goto LabelTemp;
        ///     if (!expr2) goto LabelParent;
        ///     LabelTemp:
        ///
        /// BranchingContext.OnTrue context: (expr1) or (expr2)
        /// ==> if (expr1) goto LabelParent;
        ///     if (expr1) goto LabelParent;
        ///
        /// BranchingContext.None context: (expr1) or (expr2)
        /// ==> if (expr1) goto LabelTemp;
        ///     if (expr1) goto LabelTemp;
        ///     push false();
        ///     goto LabelSkip;
        ///     LabelTemp:
        ///     push true();
        ///     LabelSkip:
        ///
        /// </remarks>
        protected override QilNode VisitOr(QilBinary ndOr)
        {
            Label lblTemp = default;

            // Visit left branch
            switch (_iterCurr.CurrentBranchingContext)
            {
                case BranchingContext.OnFalse:
                    // If left condition evaluates to true, jump to new label that will be fixed
                    // just beyond the second condition
                    lblTemp = _helper.DefineLabel();
                    NestedVisitWithBranch(ndOr.Left, BranchingContext.OnTrue, lblTemp);
                    break;

                case BranchingContext.OnTrue:
                    // If left condition evaluates to true, branch to true label
                    NestedVisitWithBranch(ndOr.Left, BranchingContext.OnTrue, _iterCurr.LabelBranch);
                    break;

                default:
                    // If left condition evalutes to true, jump to code that pushes "true"
                    Debug.Assert(_iterCurr.CurrentBranchingContext == BranchingContext.None);
                    lblTemp = _helper.DefineLabel();
                    NestedVisitWithBranch(ndOr.Left, BranchingContext.OnTrue, lblTemp);
                    break;
            }

            // Visit right branch
            switch (_iterCurr.CurrentBranchingContext)
            {
                case BranchingContext.OnFalse:
                    // If right condition evaluates to false, branch to false label
                    NestedVisitWithBranch(ndOr.Right, BranchingContext.OnFalse, _iterCurr.LabelBranch);
                    break;

                case BranchingContext.OnTrue:
                    // If right condition evaluates to true, branch to true label
                    NestedVisitWithBranch(ndOr.Right, BranchingContext.OnTrue, _iterCurr.LabelBranch);
                    break;

                default:
                    // If right condition evalutes to true, jump to code that pushes "true".
                    // Otherwise, if both conditions evaluate to false, fall through code path
                    // will push "false".
                    NestedVisitWithBranch(ndOr.Right, BranchingContext.OnTrue, lblTemp);
                    break;
            }

            switch (_iterCurr.CurrentBranchingContext)
            {
                case BranchingContext.OnFalse:
                    // Anchor true label
                    _helper.MarkLabel(lblTemp);
                    goto case BranchingContext.OnTrue;

                case BranchingContext.OnTrue:
                    _iterCurr.Storage = StorageDescriptor.None();
                    break;

                case BranchingContext.None:
                    // Convert branch targets into push of true/false
                    _helper.ConvBranchToBool(lblTemp, true);
                    _iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), false);
                    break;
            }

            return ndOr;
        }

        /// <summary>
        /// Generate code for QilNodeType.Not.
        /// </summary>
        /// <remarks>
        /// BranchingContext.OnFalse context: not(expr1)
        /// ==> if (expr1) goto LabelParent;
        ///
        /// BranchingContext.OnTrue context: not(expr1)
        /// ==> if (!expr1) goto LabelParent;
        ///
        /// BranchingContext.None context: not(expr1)
        /// ==> if (expr1) goto LabelTemp;
        ///     push false();
        ///     goto LabelSkip;
        ///     LabelTemp:
        ///     push true();
        ///     LabelSkip:
        ///
        /// </remarks>
        protected override QilNode VisitNot(QilUnary ndNot)
        {
            Label lblTemp = default;

            // Visit operand
            // Reverse branch types
            switch (_iterCurr.CurrentBranchingContext)
            {
                case BranchingContext.OnFalse:
                    NestedVisitWithBranch(ndNot.Child, BranchingContext.OnTrue, _iterCurr.LabelBranch);
                    break;

                case BranchingContext.OnTrue:
                    NestedVisitWithBranch(ndNot.Child, BranchingContext.OnFalse, _iterCurr.LabelBranch);
                    break;

                default:
                    // Replace boolean argument on top of stack with its inverse
                    Debug.Assert(_iterCurr.CurrentBranchingContext == BranchingContext.None);
                    lblTemp = _helper.DefineLabel();
                    NestedVisitWithBranch(ndNot.Child, BranchingContext.OnTrue, lblTemp);
                    break;
            }

            if (_iterCurr.CurrentBranchingContext == BranchingContext.None)
            {
                // If condition evaluates to true, then jump to code that pushes false
                _helper.ConvBranchToBool(lblTemp, false);
                _iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), false);
            }
            else
            {
                _iterCurr.Storage = StorageDescriptor.None();
            }

            return ndNot;
        }

        /// <summary>
        /// Generate code for QilNodeType.Conditional.
        /// </summary>
        protected override QilNode VisitConditional(QilTernary ndCond)
        {
            XmlILConstructInfo info = XmlILConstructInfo.Read(ndCond);

            if (info.ConstructMethod == XmlILConstructMethod.Writer)
            {
                Label lblFalse, lblDone;

                // Evaluate if test
                lblFalse = _helper.DefineLabel();
                NestedVisitWithBranch(ndCond.Left, BranchingContext.OnFalse, lblFalse);

                // Generate true branch code
                NestedVisit(ndCond.Center);

                // Generate false branch code.  If false branch is the empty list,
                if (ndCond.Right.NodeType == QilNodeType.Sequence && ndCond.Right.Count == 0)
                {
                    // Then generate simplified code that doesn't contain a false branch
                    _helper.MarkLabel(lblFalse);
                    NestedVisit(ndCond.Right);
                }
                else
                {
                    // Jump past false branch
                    lblDone = _helper.DefineLabel();
                    _helper.EmitUnconditionalBranch(OpCodes.Br, lblDone);

                    // Generate false branch code
                    _helper.MarkLabel(lblFalse);
                    NestedVisit(ndCond.Right);

                    _helper.MarkLabel(lblDone);
                }

                _iterCurr.Storage = StorageDescriptor.None();
            }
            else
            {
                IteratorDescriptor? iterInfoTrue;
                LocalBuilder? locBool = null, locCond = null;
                Label lblFalse, lblDone, lblNext;
                Type itemStorageType = GetItemStorageType(ndCond);
                Debug.Assert(info.ConstructMethod == XmlILConstructMethod.Iterator);

                // Evaluate conditional test -- save boolean result in boolResult
                Debug.Assert(ndCond.Left.XmlType!.TypeCode == XmlTypeCode.Boolean);
                lblFalse = _helper.DefineLabel();

                if (ndCond.XmlType!.IsSingleton)
                {
                    // if (!bool-expr) goto LabelFalse;
                    NestedVisitWithBranch(ndCond.Left, BranchingContext.OnFalse, lblFalse);
                }
                else
                {
                    // CondType itemCond;
                    // int boolResult = bool-expr;
                    locCond = _helper.DeclareLocal("$$$cond", itemStorageType);
                    locBool = _helper.DeclareLocal("$$$boolResult", typeof(bool));
                    NestedVisitEnsureLocal(ndCond.Left, locBool);

                    // if (!boolResult) goto LabelFalse;
                    _helper.Emit(OpCodes.Ldloc, locBool);
                    _helper.Emit(OpCodes.Brfalse, lblFalse);
                }

                // Generate code for true branch
                ConditionalBranch(ndCond.Center, itemStorageType, locCond);
                iterInfoTrue = _iterNested;

                // goto LabelDone;
                lblDone = _helper.DefineLabel();
                _helper.EmitUnconditionalBranch(OpCodes.Br, lblDone);

                // Generate code for false branch
                // LabelFalse:
                _helper.MarkLabel(lblFalse);
                ConditionalBranch(ndCond.Right, itemStorageType, locCond);

                // If conditional is not cardinality one, then need to iterate through all values
                if (!ndCond.XmlType.IsSingleton)
                {
                    Debug.Assert(!ndCond.Center.XmlType!.IsSingleton || !ndCond.Right.XmlType!.IsSingleton);

                    // IL's rules do not allow OpCodes.Br here
                    // goto LabelDone;
                    _helper.EmitUnconditionalBranch(OpCodes.Brtrue, lblDone);

                    // LabelNext:
                    lblNext = _helper.DefineLabel();
                    _helper.MarkLabel(lblNext);

                    // if (boolResult) goto LabelNextTrue else goto LabelNextFalse;
                    _helper.Emit(OpCodes.Ldloc, locBool!);
                    _helper.Emit(OpCodes.Brtrue, iterInfoTrue!.GetLabelNext());
                    _helper.EmitUnconditionalBranch(OpCodes.Br, _iterNested!.GetLabelNext());

                    _iterCurr.SetIterator(lblNext, StorageDescriptor.Local(locCond!, itemStorageType, false));
                }

                // LabelDone:
                _helper.MarkLabel(lblDone);
            }

            return ndCond;
        }

        /// <summary>
        /// Generate code for one of the branches of QilNodeType.Conditional.
        /// </summary>
        private void ConditionalBranch(QilNode ndBranch, Type itemStorageType, LocalBuilder? locResult)
        {
            if (locResult == null)
            {
                Debug.Assert(ndBranch.XmlType!.IsSingleton, "Conditional must produce a singleton");

                // If in a branching context, then inherit branch target from parent context
                if (_iterCurr.IsBranching)
                {
                    Debug.Assert(itemStorageType == typeof(bool));
                    NestedVisitWithBranch(ndBranch, _iterCurr.CurrentBranchingContext, _iterCurr.LabelBranch);
                }
                else
                {
                    NestedVisitEnsureStack(ndBranch, itemStorageType, false);
                }
            }
            else
            {
                // Link nested iterator to parent conditional's iterator
                NestedVisit(ndBranch, _iterCurr.GetLabelNext());
                _iterCurr.EnsureItemStorageType(ndBranch.XmlType!, itemStorageType);
                _iterCurr.EnsureLocalNoCache(locResult);
            }
        }

        /// <summary>
        /// Generate code for QilNodeType.Choice.
        /// </summary>
        protected override QilNode VisitChoice(QilChoice ndChoice)
        {
            QilNode ndBranches;
            Label[] switchLabels;
            Label lblOtherwise, lblDone;
            int regBranches, idx;
            Debug.Assert(XmlILConstructInfo.Read(ndChoice).PushToWriterFirst);

            // Evaluate the expression
            NestedVisit(ndChoice.Expression);

            // Generate switching code
            ndBranches = ndChoice.Branches;
            regBranches = ndBranches.Count - 1;
            switchLabels = new Label[regBranches];
            for (idx = 0; idx < regBranches; idx++)
                switchLabels[idx] = _helper.DefineLabel();

            lblOtherwise = _helper.DefineLabel();
            lblDone = _helper.DefineLabel();

            // switch (value)
            //   case 0: goto Label[0];
            //   ...
            //   case N-1: goto Label[N-1];
            //   default: goto LabelOtherwise;
            _helper.Emit(OpCodes.Switch, switchLabels);
            _helper.EmitUnconditionalBranch(OpCodes.Br, lblOtherwise);

            for (idx = 0; idx < regBranches; idx++)
            {
                // Label[i]:
                _helper.MarkLabel(switchLabels[idx]);

                // Generate regular branch code
                NestedVisit(ndBranches[idx]);

                // goto LabelDone
                _helper.EmitUnconditionalBranch(OpCodes.Br, lblDone);
            }

            // LabelOtherwise:
            _helper.MarkLabel(lblOtherwise);

            // Generate otherwise branch code
            NestedVisit(ndBranches[idx]);

            // LabelDone:
            _helper.MarkLabel(lblDone);

            _iterCurr.Storage = StorageDescriptor.None();

            return ndChoice;
        }

        /// <summary>
        /// Generate code for QilNodeType.Length.
        /// </summary>
        /// <remarks>
        /// int length = 0;
        /// foreach (item in expr)
        ///   length++;
        /// </remarks>
        protected override QilNode VisitLength(QilUnary ndSetLen)
        {
            Label lblOnEnd = _helper.DefineLabel();
            OptimizerPatterns patt = OptimizerPatterns.Read(ndSetLen);

            if (CachesResult(ndSetLen.Child))
            {
                NestedVisitEnsureStack(ndSetLen.Child);
                _helper.CallCacheCount(_iterNested!.Storage.ItemStorageType);
            }
            else
            {
                // length = 0;
                _helper.Emit(OpCodes.Ldc_I4_0);

                StartNestedIterator(ndSetLen.Child, lblOnEnd);

                // foreach (item in expr) {
                Visit(ndSetLen.Child);

                // Pop values of SetLength expression from the stack if necessary
                _iterCurr.EnsureNoCache();
                _iterCurr.DiscardStack();

                // length++;
                _helper.Emit(OpCodes.Ldc_I4_1);
                _helper.Emit(OpCodes.Add);

                if (patt.MatchesPattern(OptimizerPatternName.MaxPosition))
                {
                    // Short-circuit rest of loop if max position has been exceeded
                    _helper.Emit(OpCodes.Dup);
                    _helper.LoadInteger((int)patt.GetArgument(OptimizerPatternArgument.MaxPosition));
                    _helper.Emit(OpCodes.Bgt, lblOnEnd);
                }

                // }
                _iterCurr.LoopToEnd(lblOnEnd);

                EndNestedIterator(ndSetLen.Child);
            }

            _iterCurr.Storage = StorageDescriptor.Stack(typeof(int), false);

            return ndSetLen;
        }

        /// <summary>
        /// Find physical query plan for QilNodeType.Sequence.
        /// </summary>
        protected override QilNode VisitSequence(QilList ndSeq)
        {
            if (XmlILConstructInfo.Read(ndSeq).ConstructMethod == XmlILConstructMethod.Writer)
            {
                // Push each item in the list to output
                foreach (QilNode nd in ndSeq)
                    NestedVisit(nd);
            }
            else
            {
                // Empty sequence is special case
                if (ndSeq.Count == 0)
                    VisitEmpty(ndSeq);
                else
                    Sequence(ndSeq);
            }

            return ndSeq;
        }

        /// <summary>
        /// Generate code for the empty sequence.
        /// </summary>
        private void VisitEmpty(QilNode nd)
        {
            Debug.Assert(XmlILConstructInfo.Read(nd).PullFromIteratorFirst, "VisitEmpty should only be called if items are iterated");

            // IL's rules prevent OpCodes.Br here
            // Empty sequence
            _helper.EmitUnconditionalBranch(OpCodes.Brtrue, _iterCurr.GetLabelNext());

            // Push dummy value so that Location is not None and IL rules are met
            _helper.Emit(OpCodes.Ldnull);
            _iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathItem), false);
        }

        /// <summary>
        /// Generate code for QilNodeType.Sequence, when sort-merging to retain document order is not necessary.
        /// </summary>
        private void Sequence(QilList ndSeq)
        {
            LocalBuilder locIdx, locList;
            Label lblStart, lblNext, lblOnEnd = default;
            Label[] arrSwitchLabels;
            int i;
            Type itemStorageType = GetItemStorageType(ndSeq);
            Debug.Assert(XmlILConstructInfo.Read(ndSeq).ConstructMethod == XmlILConstructMethod.Iterator, "This method should only be called if items in list are pulled from a code iterator.");

            // Singleton list is a special case if in addition to the singleton there are warnings or errors which should be executed
            if (ndSeq.XmlType.IsSingleton)
            {
                foreach (QilNode nd in ndSeq)
                {
                    // Generate nested iterator's code
                    if (nd.XmlType!.IsSingleton)
                    {
                        NestedVisitEnsureStack(nd);
                    }
                    else
                    {
                        lblOnEnd = _helper.DefineLabel();
                        NestedVisit(nd, lblOnEnd);
                        _iterCurr.DiscardStack();
                        _helper.MarkLabel(lblOnEnd);
                    }
                }
                _iterCurr.Storage = StorageDescriptor.Stack(itemStorageType, false);
            }
            else
            {
                // Type itemList;
                // int idxList;
                locList = _helper.DeclareLocal("$$$itemList", itemStorageType);
                locIdx = _helper.DeclareLocal("$$$idxList", typeof(int));

                arrSwitchLabels = new Label[ndSeq.Count];
                lblStart = _helper.DefineLabel();

                for (i = 0; i < ndSeq.Count; i++)
                {
                    // LabelOnEnd[i - 1]:
                    // When previous nested iterator is exhausted, it should jump to this (the next) iterator
                    if (i != 0)
                        _helper.MarkLabel(lblOnEnd);

                    // Create new LabelOnEnd for all but the last iterator, which jumps back to parent iterator when exhausted
                    if (i == ndSeq.Count - 1)
                        lblOnEnd = _iterCurr.GetLabelNext();
                    else
                        lblOnEnd = _helper.DefineLabel();

                    // idxList = [i];
                    _helper.LoadInteger(i);
                    _helper.Emit(OpCodes.Stloc, locIdx);

                    // Generate nested iterator's code
                    NestedVisit(ndSeq[i], lblOnEnd);

                    // Result of list should be saved to a common type and location
                    _iterCurr.EnsureItemStorageType(ndSeq[i].XmlType!, itemStorageType);
                    _iterCurr.EnsureLocalNoCache(locList);

                    // Switch statement will jump to nested iterator's LabelNext
                    arrSwitchLabels[i] = _iterNested!.GetLabelNext();

                    // IL's rules prevent OpCodes.Br here
                    // goto LabelStart;
                    _helper.EmitUnconditionalBranch(OpCodes.Brtrue, lblStart);
                }

                // LabelNext:
                lblNext = _helper.DefineLabel();
                _helper.MarkLabel(lblNext);

                // switch (idxList)
                //   case 0: goto LabelNext1;
                //   ...
                //   case N-1: goto LabelNext[N];
                _helper.Emit(OpCodes.Ldloc, locIdx);
                _helper.Emit(OpCodes.Switch, arrSwitchLabels);

                // LabelStart:
                _helper.MarkLabel(lblStart);

                _iterCurr.SetIterator(lblNext, StorageDescriptor.Local(locList, itemStorageType, false));
            }
        }

        /// <summary>
        /// Generate code for QilNodeType.Union.
        /// </summary>
        protected override QilNode VisitUnion(QilBinary ndUnion)
        {
            return CreateSetIterator(ndUnion, "$$$iterUnion", typeof(UnionIterator), XmlILMethods.UnionCreate, XmlILMethods.UnionNext, XmlILMethods.UnionCurrent);
        }

        /// <summary>
        /// Generate code for QilNodeType.Intersection.
        /// </summary>
        protected override QilNode VisitIntersection(QilBinary ndInter)
        {
            return CreateSetIterator(ndInter, "$$$iterInter", typeof(IntersectIterator), XmlILMethods.InterCreate, XmlILMethods.InterNext, XmlILMethods.InterCurrent);
        }

        /// <summary>
        /// Generate code for QilNodeType.Difference.
        /// </summary>
        protected override QilNode VisitDifference(QilBinary ndDiff)
        {
            return CreateSetIterator(ndDiff, "$$$iterDiff", typeof(DifferenceIterator), XmlILMethods.DiffCreate, XmlILMethods.DiffNext, XmlILMethods.DiffCurrent);
        }

        /// <summary>
        /// Generate code to combine nodes from two nested iterators using Union, Intersection, or Difference semantics.
        /// </summary>
        private QilNode CreateSetIterator(QilBinary ndSet, string iterName, Type iterType, MethodInfo methCreate, MethodInfo methNext, MethodInfo methCurrent)
        {
            LocalBuilder locIter, locNav;
            Label lblNext, lblCall, lblNextLeft, lblNextRight, lblInitRight;

            // SetIterator iterSet;
            // XPathNavigator navSet;
            locIter = _helper.DeclareLocal(iterName, iterType);
            locNav = _helper.DeclareLocal("$$$navSet", typeof(XPathNavigator));

            // iterSet.Create(runtime);
            _helper.Emit(OpCodes.Ldloca, locIter);
            _helper.LoadQueryRuntime();
            _helper.Call(methCreate);

            // Define labels that will be used
            lblNext = _helper.DefineLabel();
            lblCall = _helper.DefineLabel();
            lblInitRight = _helper.DefineLabel();

            // Generate left nested iterator.  When it is empty, it will branch to lblNext.
            // goto LabelCall;
            NestedVisit(ndSet.Left, lblNext);
            lblNextLeft = _iterNested!.GetLabelNext();
            _iterCurr.EnsureLocal(locNav);
            _helper.EmitUnconditionalBranch(OpCodes.Brtrue, lblCall);

            // Generate right nested iterator.  When it is empty, it will branch to lblNext.
            // LabelInitRight:
            // goto LabelCall;
            _helper.MarkLabel(lblInitRight);
            NestedVisit(ndSet.Right, lblNext);
            lblNextRight = _iterNested.GetLabelNext();
            _iterCurr.EnsureLocal(locNav);
            _helper.EmitUnconditionalBranch(OpCodes.Brtrue, lblCall);

            // LabelNext:
            _helper.MarkLabel(lblNext);
            _helper.Emit(OpCodes.Ldnull);
            _helper.Emit(OpCodes.Stloc, locNav);

            // LabelCall:
            // switch (iterSet.MoveNext(nestedNested)) {
            //      case SetIteratorResult.NoMoreNodes: goto LabelNextCtxt;
            //      case SetIteratorResult.InitRightIterator: goto LabelInitRight;
            //      case SetIteratorResult.NeedLeftNode: goto LabelNextLeft;
            //      case SetIteratorResult.NeedRightNode: goto LabelNextRight;
            // }
            _helper.MarkLabel(lblCall);
            _helper.Emit(OpCodes.Ldloca, locIter);
            _helper.Emit(OpCodes.Ldloc, locNav);
            _helper.Call(methNext);

            // If this iterator always returns a single node, then NoMoreNodes will never be returned
            // Don't expose Next label if this iterator always returns a single node
            if (ndSet.XmlType!.IsSingleton)
            {
                _helper.Emit(OpCodes.Switch, new Label[] { lblInitRight, lblNextLeft, lblNextRight });
                _iterCurr.Storage = StorageDescriptor.Current(locIter, methCurrent, typeof(XPathNavigator));
            }
            else
            {
                _helper.Emit(OpCodes.Switch, new Label[] { _iterCurr.GetLabelNext(), lblInitRight, lblNextLeft, lblNextRight });
                _iterCurr.SetIterator(lblNext, StorageDescriptor.Current(locIter, methCurrent, typeof(XPathNavigator)));
            }

            return ndSet;
        }

        /// <summary>
        /// Generate code for QilNodeType.Average.
        /// </summary>
        protected override QilNode VisitAverage(QilUnary ndAvg)
        {
            XmlILStorageMethods meths = XmlILMethods.StorageMethods[GetItemStorageType(ndAvg)];
            return CreateAggregator(ndAvg, "$$$aggAvg", meths, meths.AggAvg!, meths.AggAvgResult!);
        }

        /// <summary>
        /// Generate code for QilNodeType.Sum.
        /// </summary>
        protected override QilNode VisitSum(QilUnary ndSum)
        {
            XmlILStorageMethods meths = XmlILMethods.StorageMethods[GetItemStorageType(ndSum)];
            return CreateAggregator(ndSum, "$$$aggSum", meths, meths.AggSum!, meths.AggSumResult!);
        }

        /// <summary>
        /// Generate code for QilNodeType.Minimum.
        /// </summary>
        protected override QilNode VisitMinimum(QilUnary ndMin)
        {
            XmlILStorageMethods meths = XmlILMethods.StorageMethods[GetItemStorageType(ndMin)];
            return CreateAggregator(ndMin, "$$$aggMin", meths, meths.AggMin!, meths.AggMinResult!);
        }

        /// <summary>
        /// Generate code for QilNodeType.Maximum.
        /// </summary>
        protected override QilNode VisitMaximum(QilUnary ndMax)
        {
            XmlILStorageMethods meths = XmlILMethods.StorageMethods[GetItemStorageType(ndMax)];
            return CreateAggregator(ndMax, "$$$aggMax", meths, meths.AggMax!, meths.AggMaxResult!);
        }

        /// <summary>
        /// Generate code for QilNodeType.Sum, QilNodeType.Average, QilNodeType.Minimum, and QilNodeType.Maximum.
        /// </summary>
        private QilNode CreateAggregator(QilUnary ndAgg, string aggName, XmlILStorageMethods methods, MethodInfo methAgg, MethodInfo methResult)
        {
            Label lblOnEnd = _helper.DefineLabel();
            Type typAgg = methAgg.DeclaringType!;
            LocalBuilder locAgg;

            // Aggregate agg;
            // agg.Create();
            locAgg = _helper.DeclareLocal(aggName, typAgg);
            _helper.Emit(OpCodes.Ldloca, locAgg);
            _helper.Call(methods.AggCreate!);

            // foreach (num in expr) {
            StartNestedIterator(ndAgg.Child, lblOnEnd);
            _helper.Emit(OpCodes.Ldloca, locAgg);
            Visit(ndAgg.Child);

            //   agg.Aggregate(num);
            _iterCurr.EnsureStackNoCache();
            _iterCurr.EnsureItemStorageType(ndAgg.XmlType!, GetItemStorageType(ndAgg));
            _helper.Call(methAgg);
            _helper.Emit(OpCodes.Ldloca, locAgg);

            // }
            _iterCurr.LoopToEnd(lblOnEnd);

            // End nested iterator
            EndNestedIterator(ndAgg.Child);

            // If aggregate might be empty sequence, then generate code to handle this possibility
            if (ndAgg.XmlType!.MaybeEmpty)
            {
                // if (agg.IsEmpty) goto LabelNextCtxt;
                _helper.Call(methods.AggIsEmpty!);
                _helper.Emit(OpCodes.Brtrue, _iterCurr.GetLabelNext());
                _helper.Emit(OpCodes.Ldloca, locAgg);
            }

            // result = agg.Result;
            _helper.Call(methResult);
            _iterCurr.Storage = StorageDescriptor.Stack(GetItemStorageType(ndAgg), false);

            return ndAgg;
        }

        /// <summary>
        /// Generate code for QilNodeType.Negate.
        /// </summary>
        protected override QilNode VisitNegate(QilUnary ndNeg)
        {
            NestedVisitEnsureStack(ndNeg.Child);
            _helper.CallArithmeticOp(QilNodeType.Negate, ndNeg.XmlType!.TypeCode);
            _iterCurr.Storage = StorageDescriptor.Stack(GetItemStorageType(ndNeg), false);
            return ndNeg;
        }

        /// <summary>
        /// Generate code for QilNodeType.Add.
        /// </summary>
        protected override QilNode VisitAdd(QilBinary ndPlus)
        {
            return ArithmeticOp(ndPlus);
        }

        /// <summary>
        /// Generate code for QilNodeType.Subtract.
        /// </summary>
        protected override QilNode VisitSubtract(QilBinary ndMinus)
        {
            return ArithmeticOp(ndMinus);
        }

        /// <summary>
        /// Generate code for QilNodeType.Multiply.
        /// </summary>
        protected override QilNode VisitMultiply(QilBinary ndMul)
        {
            return ArithmeticOp(ndMul);
        }

        /// <summary>
        /// Generate code for QilNodeType.Divide.
        /// </summary>
        protected override QilNode VisitDivide(QilBinary ndDiv)
        {
            return ArithmeticOp(ndDiv);
        }

        /// <summary>
        /// Generate code for QilNodeType.Modulo.
        /// </summary>
        protected override QilNode VisitModulo(QilBinary ndMod)
        {
            return ArithmeticOp(ndMod);
        }

        /// <summary>
        /// Generate code for two-argument arithmetic operations.
        /// </summary>
        private QilNode ArithmeticOp(QilBinary ndOp)
        {
            NestedVisitEnsureStack(ndOp.Left, ndOp.Right);
            _helper.CallArithmeticOp(ndOp.NodeType, ndOp.XmlType!.TypeCode);
            _iterCurr.Storage = StorageDescriptor.Stack(GetItemStorageType(ndOp), false);
            return ndOp;
        }

        /// <summary>
        /// Generate code for QilNodeType.StrLength.
        /// </summary>
        protected override QilNode VisitStrLength(QilUnary ndLen)
        {
            NestedVisitEnsureStack(ndLen.Child);
            _helper.Call(XmlILMethods.StrLen);
            _iterCurr.Storage = StorageDescriptor.Stack(typeof(int), false);
            return ndLen;
        }

        /// <summary>
        /// Generate code for QilNodeType.StrConcat.
        /// </summary>
        protected override QilNode VisitStrConcat(QilStrConcat ndStrConcat)
        {
            LocalBuilder locStringConcat;
            bool fasterConcat;
            QilNode? delimiter;
            QilNode listStrings;
            Debug.Assert(!ndStrConcat.Values.XmlType!.IsSingleton, "Optimizer should have folded StrConcat of a singleton value");

            // Get delimiter (assuming it's not the empty string)
            delimiter = ndStrConcat.Delimiter;
            if (delimiter.NodeType == QilNodeType.LiteralString && ((string)(QilLiteral)delimiter).Length == 0)
            {
                delimiter = null;
            }

            listStrings = ndStrConcat.Values;
            if (listStrings.NodeType == QilNodeType.Sequence && listStrings.Count < 5)
            {
                // Faster concat possible only if cardinality can be guaranteed at compile-time and there's no delimiter
                fasterConcat = true;
                foreach (QilNode ndStr in listStrings)
                {
                    if (!ndStr.XmlType!.IsSingleton)
                        fasterConcat = false;
                }
            }
            else
            {
                // If more than 4 strings, array will need to be built
                fasterConcat = false;
            }

            if (fasterConcat)
            {
                foreach (QilNode ndStr in listStrings)
                    NestedVisitEnsureStack(ndStr);

                _helper.CallConcatStrings(listStrings.Count);
            }
            else
            {
                // Create StringConcat helper internal class
                locStringConcat = _helper.DeclareLocal("$$$strcat", typeof(StringConcat));
                _helper.Emit(OpCodes.Ldloca, locStringConcat);
                _helper.Call(XmlILMethods.StrCatClear);

                // Set delimiter, if it's not empty string
                if (delimiter != null)
                {
                    _helper.Emit(OpCodes.Ldloca, locStringConcat);
                    NestedVisitEnsureStack(delimiter);
                    _helper.Call(XmlILMethods.StrCatDelim);
                }

                _helper.Emit(OpCodes.Ldloca, locStringConcat);

                if (listStrings.NodeType == QilNodeType.Sequence)
                {
                    foreach (QilNode ndStr in listStrings)
                        GenerateConcat(ndStr, locStringConcat);
                }
                else
                {
                    GenerateConcat(listStrings, locStringConcat);
                }

                // Push resulting string onto stack
                _helper.Call(XmlILMethods.StrCatResult);
            }

            _iterCurr.Storage = StorageDescriptor.Stack(typeof(string), false);

            return ndStrConcat;
        }

        /// <summary>
        /// Generate code to concatenate string values returned by expression "ndStr" using the StringConcat helper class.
        /// </summary>
        private void GenerateConcat(QilNode ndStr, LocalBuilder locStringConcat)
        {
            Label lblOnEnd;

            // str = each string;
            lblOnEnd = _helper.DefineLabel();
            StartNestedIterator(ndStr, lblOnEnd);
            Visit(ndStr);

            // strcat.Concat(str);
            _iterCurr.EnsureStackNoCache();
            _iterCurr.EnsureItemStorageType(ndStr.XmlType!, typeof(string));
            _helper.Call(XmlILMethods.StrCatCat);
            _helper.Emit(OpCodes.Ldloca, locStringConcat);

            // Get next string
            // goto LabelNext;
            // LabelOnEnd:
            _iterCurr.LoopToEnd(lblOnEnd);

            // End nested iterator
            EndNestedIterator(ndStr);
        }

        /// <summary>
        /// Generate code for QilNodeType.StrParseQName.
        /// </summary>
        protected override QilNode VisitStrParseQName(QilBinary ndParsedTagName)
        {
            VisitStrParseQName(ndParsedTagName, false);
            return ndParsedTagName;
        }

        /// <summary>
        /// Generate code for QilNodeType.StrParseQName.
        /// </summary>
        private void VisitStrParseQName(QilBinary ndParsedTagName, bool preservePrefix)
        {
            // If QName prefix should be preserved, then don't create an XmlQualifiedName, which discards the prefix
            if (!preservePrefix)
                _helper.LoadQueryRuntime();

            // Push (possibly computed) tag name onto the stack
            NestedVisitEnsureStack(ndParsedTagName.Left);

            // If type of second parameter is string,
            if (ndParsedTagName.Right.XmlType!.TypeCode == XmlTypeCode.String)
            {
                // Then push (possibly computed) namespace onto the stack
                Debug.Assert(ndParsedTagName.Right.XmlType.IsSingleton);
                NestedVisitEnsureStack(ndParsedTagName.Right);

                if (!preservePrefix)
                    _helper.CallParseTagName(GenerateNameType.TagNameAndNamespace);
            }
            else
            {
                // Else push index of set of prefix mappings to use in resolving the prefix
                if (ndParsedTagName.Right.NodeType == QilNodeType.Sequence)
                    _helper.LoadInteger(_helper.StaticData.DeclarePrefixMappings(ndParsedTagName.Right));
                else
                    _helper.LoadInteger(_helper.StaticData.DeclarePrefixMappings(new QilNode[] { ndParsedTagName.Right }));

                // If QName prefix should be preserved, then don't create an XmlQualifiedName, which discards the prefix
                if (!preservePrefix)
                    _helper.CallParseTagName(GenerateNameType.TagNameAndMappings);
            }

            _iterCurr.Storage = StorageDescriptor.Stack(typeof(XmlQualifiedName), false);
        }

        /// <summary>
        /// Generate code for QilNodeType.Ne.
        /// </summary>
        protected override QilNode VisitNe(QilBinary ndNe)
        {
            Compare(ndNe);
            return ndNe;
        }

        /// <summary>
        /// Generate code for QilNodeType.Eq.
        /// </summary>
        protected override QilNode VisitEq(QilBinary ndEq)
        {
            Compare(ndEq);
            return ndEq;
        }

        /// <summary>
        /// Generate code for QilNodeType.Gt.
        /// </summary>
        protected override QilNode VisitGt(QilBinary ndGt)
        {
            Compare(ndGt);
            return ndGt;
        }

        /// <summary>
        /// Generate code for QilNodeType.Ne.
        /// </summary>
        protected override QilNode VisitGe(QilBinary ndGe)
        {
            Compare(ndGe);
            return ndGe;
        }

        /// <summary>
        /// Generate code for QilNodeType.Lt.
        /// </summary>
        protected override QilNode VisitLt(QilBinary ndLt)
        {
            Compare(ndLt);
            return ndLt;
        }

        /// <summary>
        /// Generate code for QilNodeType.Le.
        /// </summary>
        protected override QilNode VisitLe(QilBinary ndLe)
        {
            Compare(ndLe);
            return ndLe;
        }

        /// <summary>
        /// Generate code for comparison operations.
        /// </summary>
        private void Compare(QilBinary ndComp)
        {
            QilNodeType relOp = ndComp.NodeType;
            XmlTypeCode code;
            Debug.Assert(ndComp.Left.XmlType!.IsAtomicValue && ndComp.Right.XmlType!.IsAtomicValue, "Operands to compare must be atomic values.");
            Debug.Assert(ndComp.Left.XmlType.IsSingleton && ndComp.Right.XmlType.IsSingleton, "Operands to compare must be cardinality one.");
            Debug.Assert(ndComp.Left.XmlType == ndComp.Right.XmlType, "Operands to compare may not be heterogenous.");

            if (relOp == QilNodeType.Eq || relOp == QilNodeType.Ne)
            {
                // Generate better code for certain special cases
                if (TryZeroCompare(relOp, ndComp.Left, ndComp.Right))
                    return;

                if (TryZeroCompare(relOp, ndComp.Right, ndComp.Left))
                    return;

                if (TryNameCompare(relOp, ndComp.Left, ndComp.Right))
                    return;

                if (TryNameCompare(relOp, ndComp.Right, ndComp.Left))
                    return;
            }

            // Push two operands onto the stack
            NestedVisitEnsureStack(ndComp.Left, ndComp.Right);

            // Perform comparison
            code = ndComp.Left.XmlType.TypeCode;
            switch (code)
            {
                case XmlTypeCode.String:
                case XmlTypeCode.Decimal:
                case XmlTypeCode.QName:
                    if (relOp == QilNodeType.Eq || relOp == QilNodeType.Ne)
                    {
                        _helper.CallCompareEquals(code);

                        // If relOp is Eq, then branch to true label or push "true" if Equals function returns true (non-zero)
                        // If relOp is Ne, then branch to true label or push "true" if Equals function returns false (zero)
                        ZeroCompare((relOp == QilNodeType.Eq) ? QilNodeType.Ne : QilNodeType.Eq, true);
                    }
                    else
                    {
                        Debug.Assert(code != XmlTypeCode.QName, $"QName values do not support the {relOp} operation");

                        // Push -1, 0, or 1 onto the stack depending upon the result of the comparison
                        _helper.CallCompare(code);

                        // Compare result to 0 (e.g. Ge is >= 0)
                        _helper.Emit(OpCodes.Ldc_I4_0);
                        ClrCompare(relOp, code);
                    }
                    break;

                case XmlTypeCode.Integer:
                case XmlTypeCode.Int:
                case XmlTypeCode.Boolean:
                case XmlTypeCode.Double:
                    ClrCompare(relOp, code);
                    break;

                default:
                    Debug.Fail("Comparisons for datatype " + code + " are invalid.");
                    break;
            }
        }

        /// <summary>
        /// Generate code for QilNodeType.VisitIs.
        /// </summary>
        protected override QilNode VisitIs(QilBinary ndIs)
        {
            // Generate code to push arguments onto stack
            NestedVisitEnsureStack(ndIs.Left, ndIs.Right);
            _helper.Call(XmlILMethods.NavSamePos);

            // navThis.IsSamePosition(navThat);
            ZeroCompare(QilNodeType.Ne, true);
            return ndIs;
        }

        /// <summary>
        /// Generate code for QilNodeType.VisitBefore.
        /// </summary>
        protected override QilNode VisitBefore(QilBinary ndBefore)
        {
            ComparePosition(ndBefore);
            return ndBefore;
        }

        /// <summary>
        /// Generate code for QilNodeType.VisitAfter.
        /// </summary>
        protected override QilNode VisitAfter(QilBinary ndAfter)
        {
            ComparePosition(ndAfter);
            return ndAfter;
        }

        /// <summary>
        /// Generate code for QilNodeType.VisitBefore and QilNodeType.VisitAfter.
        /// </summary>
        private void ComparePosition(QilBinary ndComp)
        {
            // Generate code to push arguments onto stack
            _helper.LoadQueryRuntime();
            NestedVisitEnsureStack(ndComp.Left, ndComp.Right);
            _helper.Call(XmlILMethods.CompPos);

            // XmlQueryRuntime.ComparePosition(navThis, navThat) < 0;
            _helper.LoadInteger(0);
            ClrCompare(ndComp.NodeType == QilNodeType.Before ? QilNodeType.Lt : QilNodeType.Gt, XmlTypeCode.String);
        }

        /// <summary>
        /// Generate code for a QilNodeType.For.
        /// </summary>
        protected override QilNode VisitFor(QilIterator ndFor)
        {
            IteratorDescriptor iterInfo;

            // Reference saved location
            iterInfo = XmlILAnnotation.Write(ndFor).CachedIteratorDescriptor!;
            _iterCurr.Storage = iterInfo.Storage;

            // If the iterator is a reference to a global variable or parameter,
            if (_iterCurr.Storage.Location == ItemLocation.Global)
            {
                // Then compute global value and push it onto the stack
                _iterCurr.EnsureStack();
            }

            return ndFor;
        }

        /// <summary>
        /// Generate code for a QilNodeType.Let.
        /// </summary>
        protected override QilNode VisitLet(QilIterator ndLet)
        {
            // Same as For
            return VisitFor(ndLet);
        }

        /// <summary>
        /// Generate code for a QilNodeType.Parameter.
        /// </summary>
        protected override QilNode VisitParameter(QilParameter ndParameter)
        {
            // Same as For
            return VisitFor(ndParameter);
        }

        /// <summary>
        /// Generate code for a QilNodeType.Loop.
        /// </summary>
        protected override QilNode VisitLoop(QilLoop ndLoop)
        {
            bool hasOnEnd;
            Label lblOnEnd;

            StartWriterLoop(ndLoop, out hasOnEnd, out lblOnEnd);

            StartBinding(ndLoop.Variable);

            // Unnest loop body as part of the current iterator
            Visit(ndLoop.Body);

            EndBinding(ndLoop.Variable);

            EndWriterLoop(ndLoop, hasOnEnd, lblOnEnd);

            return ndLoop;
        }

        /// <summary>
        /// Generate code for a QilNodeType.Filter.
        /// </summary>
        protected override QilNode VisitFilter(QilLoop ndFilter)
        {
            // Handle any special-case patterns that are rooted at Filter
            if (HandleFilterPatterns(ndFilter))
                return ndFilter;

            StartBinding(ndFilter.Variable);

            // Result of filter is the sequence bound to the iterator
            _iterCurr.SetIterator(_iterNested!);

            // If filter is false, skip the current item
            StartNestedIterator(ndFilter.Body);
            _iterCurr.SetBranching(BranchingContext.OnFalse, _iterCurr.ParentIterator!.GetLabelNext());
            Visit(ndFilter.Body);
            EndNestedIterator(ndFilter.Body);

            EndBinding(ndFilter.Variable);

            return ndFilter;
        }

        /// <summary>
        /// There are a number of path patterns that can be rooted at Filter nodes.  Determine whether one of these patterns
        /// has been previously matched on "ndFilter".  If so, generate code for the pattern and return true.  Otherwise, just
        /// return false.
        /// </summary>
        private bool HandleFilterPatterns(QilLoop ndFilter)
        {
            OptimizerPatterns patt = OptimizerPatterns.Read(ndFilter);
            LocalBuilder locIter;
            XmlNodeKindFlags kinds;
            QilName? name;
            QilNode input, step;
            bool isFilterElements;

            // Handle FilterElements and FilterContentKind patterns
            isFilterElements = patt.MatchesPattern(OptimizerPatternName.FilterElements);
            if (isFilterElements || patt.MatchesPattern(OptimizerPatternName.FilterContentKind))
            {
                if (isFilterElements)
                {
                    // FilterElements pattern, so Kind = Element and Name = Argument
                    kinds = XmlNodeKindFlags.Element;
                    name = (QilName)patt.GetArgument(OptimizerPatternArgument.ElementQName);
                }
                else
                {
                    // FilterKindTest pattern, so Kind = Argument and Name = null
                    kinds = ((XmlQueryType)patt.GetArgument(OptimizerPatternArgument.KindTestType)).NodeKinds;
                    name = null;
                }

                step = (QilNode)patt.GetArgument(OptimizerPatternArgument.StepNode);
                input = (QilNode)patt.GetArgument(OptimizerPatternArgument.StepInput);
                switch (step.NodeType)
                {
                    case QilNodeType.Content:
                        if (isFilterElements)
                        {
                            // Iterator iter;
                            locIter = _helper.DeclareLocal("$$$iterElemContent", typeof(ElementContentIterator));

                            // iter.Create(navCtxt, locName, ns);
                            _helper.Emit(OpCodes.Ldloca, locIter);
                            NestedVisitEnsureStack(input);
                            _helper.CallGetAtomizedName(_helper.StaticData.DeclareName(name!.LocalName));
                            _helper.CallGetAtomizedName(_helper.StaticData.DeclareName(name.NamespaceUri));
                            _helper.Call(XmlILMethods.ElemContentCreate);

                            GenerateSimpleIterator(typeof(XPathNavigator), locIter, XmlILMethods.ElemContentNext, XmlILMethods.ElemContentCurrent);
                        }
                        else
                        {
                            if (kinds == XmlNodeKindFlags.Content)
                            {
                                CreateSimpleIterator(input, "$$$iterContent", typeof(ContentIterator), XmlILMethods.ContentCreate, XmlILMethods.ContentNext, XmlILMethods.ContentCurrent);
                            }
                            else
                            {
                                // Iterator iter;
                                locIter = _helper.DeclareLocal("$$$iterContent", typeof(NodeKindContentIterator));

                                // iter.Create(navCtxt, nodeType);
                                _helper.Emit(OpCodes.Ldloca, locIter);
                                NestedVisitEnsureStack(input);
                                _helper.LoadInteger((int)QilXmlToXPathNodeType(kinds));
                                _helper.Call(XmlILMethods.KindContentCreate);

                                GenerateSimpleIterator(typeof(XPathNavigator), locIter, XmlILMethods.KindContentNext, XmlILMethods.KindContentCurrent);
                            }
                        }
                        return true;

                    case QilNodeType.Parent:
                        CreateFilteredIterator(input, "$$$iterPar", typeof(ParentIterator), XmlILMethods.ParentCreate, XmlILMethods.ParentNext, XmlILMethods.ParentCurrent,
                                               kinds, name, TriState.Unknown, null);
                        return true;

                    case QilNodeType.Ancestor:
                    case QilNodeType.AncestorOrSelf:
                        CreateFilteredIterator(input, "$$$iterAnc", typeof(AncestorIterator), XmlILMethods.AncCreate, XmlILMethods.AncNext, XmlILMethods.AncCurrent,
                                               kinds, name, (step.NodeType == QilNodeType.Ancestor) ? TriState.False : TriState.True, null);
                        return true;

                    case QilNodeType.Descendant:
                    case QilNodeType.DescendantOrSelf:
                        CreateFilteredIterator(input, "$$$iterDesc", typeof(DescendantIterator), XmlILMethods.DescCreate, XmlILMethods.DescNext, XmlILMethods.DescCurrent,
                                               kinds, name, (step.NodeType == QilNodeType.Descendant) ? TriState.False : TriState.True, null);
                        return true;

                    case QilNodeType.Preceding:
                        CreateFilteredIterator(input, "$$$iterPrec", typeof(PrecedingIterator), XmlILMethods.PrecCreate, XmlILMethods.PrecNext, XmlILMethods.PrecCurrent,
                                               kinds, name, TriState.Unknown, null);
                        return true;

                    case QilNodeType.FollowingSibling:
                        CreateFilteredIterator(input, "$$$iterFollSib", typeof(FollowingSiblingIterator), XmlILMethods.FollSibCreate, XmlILMethods.FollSibNext, XmlILMethods.FollSibCurrent,
                                               kinds, name, TriState.Unknown, null);
                        return true;

                    case QilNodeType.PrecedingSibling:
                        CreateFilteredIterator(input, "$$$iterPreSib", typeof(PrecedingSiblingIterator), XmlILMethods.PreSibCreate, XmlILMethods.PreSibNext, XmlILMethods.PreSibCurrent,
                                               kinds, name, TriState.Unknown, null);
                        return true;

                    case QilNodeType.NodeRange:
                        CreateFilteredIterator(input, "$$$iterRange", typeof(NodeRangeIterator), XmlILMethods.NodeRangeCreate, XmlILMethods.NodeRangeNext, XmlILMethods.NodeRangeCurrent,
                                               kinds, name, TriState.Unknown, ((QilBinary)step).Right);
                        return true;

                    case QilNodeType.XPathFollowing:
                        CreateFilteredIterator(input, "$$$iterFoll", typeof(XPathFollowingIterator), XmlILMethods.XPFollCreate, XmlILMethods.XPFollNext, XmlILMethods.XPFollCurrent,
                                               kinds, name, TriState.Unknown, null);
                        return true;

                    case QilNodeType.XPathPreceding:
                        CreateFilteredIterator(input, "$$$iterPrec", typeof(XPathPrecedingIterator), XmlILMethods.XPPrecCreate, XmlILMethods.XPPrecNext, XmlILMethods.XPPrecCurrent,
                                               kinds, name, TriState.Unknown, null);
                        return true;

                    default:
                        Debug.Fail($"Pattern {step.NodeType} should have been handled.");
                        break;
                }
            }
            else if (patt.MatchesPattern(OptimizerPatternName.FilterAttributeKind))
            {
                // Handle FilterAttributeKind pattern
                input = (QilNode)patt.GetArgument(OptimizerPatternArgument.StepInput);
                CreateSimpleIterator(input, "$$$iterAttr", typeof(AttributeIterator), XmlILMethods.AttrCreate, XmlILMethods.AttrNext, XmlILMethods.AttrCurrent);
                return true;
            }
            else if (patt.MatchesPattern(OptimizerPatternName.EqualityIndex))
            {
                // Handle EqualityIndex pattern
                Label lblOnEnd = _helper.DefineLabel();
                Label lblLookup = _helper.DefineLabel();
                QilIterator nodes = (QilIterator)patt.GetArgument(OptimizerPatternArgument.IndexedNodes);
                QilNode keys = (QilNode)patt.GetArgument(OptimizerPatternArgument.KeyExpression);

                // XmlILIndex index;
                // if (runtime.FindIndex(navCtxt, indexId, out index)) goto LabelLookup;
                LocalBuilder locIndex = _helper.DeclareLocal("$$$index", typeof(XmlILIndex));
                _helper.LoadQueryRuntime();
                _helper.Emit(OpCodes.Ldarg_1);
                _helper.LoadInteger(_indexId);
                _helper.Emit(OpCodes.Ldloca, locIndex);
                _helper.Call(XmlILMethods.FindIndex);
                _helper.Emit(OpCodes.Brtrue, lblLookup);

                // runtime.AddNewIndex(navCtxt, indexId, [build index]);
                _helper.LoadQueryRuntime();
                _helper.Emit(OpCodes.Ldarg_1);
                _helper.LoadInteger(_indexId);
                _helper.Emit(OpCodes.Ldloc, locIndex);

                // Generate code to iterate over the nodes which are being indexed ($iterNodes in the pattern)
                StartNestedIterator(nodes, lblOnEnd);
                StartBinding(nodes);

                // Generate code to iterate over the keys for each node ($bindingKeys in the pattern)
                Visit(keys);

                // index.Add(key, value);
                _iterCurr.EnsureStackNoCache();
                VisitFor(nodes);
                _iterCurr.EnsureStackNoCache();
                _iterCurr.EnsureItemStorageType(nodes.XmlType!, typeof(XPathNavigator));
                _helper.Call(XmlILMethods.IndexAdd);
                _helper.Emit(OpCodes.Ldloc, locIndex);

                // LabelOnEnd:
                _iterCurr.LoopToEnd(lblOnEnd);
                EndBinding(nodes);
                EndNestedIterator(nodes);

                // runtime.AddNewIndex(navCtxt, indexId, [build index]);
                _helper.Call(XmlILMethods.AddNewIndex);

                // LabelLookup:
                // results = index.Lookup(keyValue);
                _helper.MarkLabel(lblLookup);
                _helper.Emit(OpCodes.Ldloc, locIndex);
                _helper.Emit(OpCodes.Ldarg_2);
                _helper.Call(XmlILMethods.IndexLookup);
                _iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathNavigator), true);

                _indexId++;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Generate code for a Let, For, or Parameter iterator.  Bind iterated value to a variable.
        /// </summary>
        private void StartBinding(QilIterator ndIter)
        {
            OptimizerPatterns patt = OptimizerPatterns.Read(ndIter);
            Debug.Assert(ndIter != null);

            // DebugInfo: Sequence point just before generating code for the bound expression
            if (_qil.IsDebug && ndIter.SourceLine != null)
                _helper.DebugSequencePoint(ndIter.SourceLine);

            // Treat cardinality one Let iterators as if they were For iterators (no nesting necessary)
            if (ndIter.NodeType == QilNodeType.For || ndIter.XmlType!.IsSingleton)
            {
                StartForBinding(ndIter, patt);
            }
            else
            {
                Debug.Assert(ndIter.NodeType == QilNodeType.Let || ndIter.NodeType == QilNodeType.Parameter);
                Debug.Assert(!patt.MatchesPattern(OptimizerPatternName.IsPositional));

                // Bind Let values (nested iterator) to variable
                StartLetBinding(ndIter);
            }

            // Attach IteratorDescriptor to the iterator
            XmlILAnnotation.Write(ndIter).CachedIteratorDescriptor = _iterNested;
        }

        /// <summary>
        /// Bind values produced by the "ndFor" expression to a non-stack location that can later
        /// be referenced.
        /// </summary>
        private void StartForBinding(QilIterator ndFor, OptimizerPatterns patt)
        {
            LocalBuilder? locPos = null;
            Debug.Assert(ndFor.XmlType!.IsSingleton);

            // For expression iterator will be unnested as part of parent iterator
            if (_iterCurr.HasLabelNext)
                StartNestedIterator(ndFor.Binding, _iterCurr.GetLabelNext());
            else
                StartNestedIterator(ndFor.Binding);

            if (patt.MatchesPattern(OptimizerPatternName.IsPositional))
            {
                // Need to track loop index so initialize it to 0 before starting loop
                locPos = _helper.DeclareLocal("$$$pos", typeof(int));
                _helper.Emit(OpCodes.Ldc_I4_0);
                _helper.Emit(OpCodes.Stloc, locPos);
            }

            // Allow base internal class to dispatch based on QilExpression node type
            Visit(ndFor.Binding!);

            // DebugInfo: Open variable scope
            // DebugInfo: Ensure that for variable is stored in a local and tag it with the user-defined name
            if (_qil.IsDebug && ndFor.DebugName != null)
            {
                _helper.DebugStartScope();

                // Ensure that values are stored in a local variable with a user-defined name
                _iterCurr.EnsureLocalNoCache("$$$for");
            }
            else
            {
                // Ensure that values are not stored on the stack
                _iterCurr.EnsureNoStackNoCache("$$$for");
            }

            if (patt.MatchesPattern(OptimizerPatternName.IsPositional))
            {
                // Increment position
                _helper.Emit(OpCodes.Ldloc, locPos!);
                _helper.Emit(OpCodes.Ldc_I4_1);
                _helper.Emit(OpCodes.Add);
                _helper.Emit(OpCodes.Stloc, locPos!);

                if (patt.MatchesPattern(OptimizerPatternName.MaxPosition))
                {
                    // Short-circuit rest of loop if max position has already been reached
                    _helper.Emit(OpCodes.Ldloc, locPos!);
                    _helper.LoadInteger((int)patt.GetArgument(OptimizerPatternArgument.MaxPosition));
                    _helper.Emit(OpCodes.Bgt, _iterCurr.ParentIterator!.GetLabelNext());
                }

                _iterCurr.LocalPosition = locPos;
            }

            EndNestedIterator(ndFor.Binding!);
            _iterCurr.SetIterator(_iterNested!);
        }

        /// <summary>
        /// Bind values in the "ndLet" expression to a non-stack location that can later be referenced.
        /// </summary>
        public void StartLetBinding(QilIterator ndLet)
        {
            Debug.Assert(!ndLet.XmlType!.IsSingleton);

            // Construct nested iterator
            StartNestedIterator(ndLet);

            // Allow base internal class to dispatch based on QilExpression node type
            NestedVisit(ndLet.Binding!, GetItemStorageType(ndLet), !ndLet.XmlType.IsSingleton);

            // DebugInfo: Open variable scope
            // DebugInfo: Ensure that for variable is stored in a local and tag it with the user-defined name
            if (_qil.IsDebug && ndLet.DebugName != null)
            {
                _helper.DebugStartScope();

                // Ensure that cache is stored in a local variable with a user-defined name
                _iterCurr.EnsureLocal("$$$cache");
            }
            else
            {
                // Ensure that cache is not stored on the stack
                _iterCurr.EnsureNoStack("$$$cache");
            }

            EndNestedIterator(ndLet);
        }

        /// <summary>
        /// Mark iterator variables as out-of-scope.
        /// </summary>
        private void EndBinding(QilIterator ndIter)
        {
            Debug.Assert(ndIter != null);

            // Variables go out of scope here
            if (_qil.IsDebug && ndIter.DebugName != null)
                _helper.DebugEndScope();
        }

        /// <summary>
        /// Generate code for QilNodeType.PositionOf.
        /// </summary>
        protected override QilNode VisitPositionOf(QilUnary ndPos)
        {
            QilIterator ndIter = (ndPos.Child as QilIterator)!;
            LocalBuilder locPos;
            Debug.Assert(ndIter.NodeType == QilNodeType.For);

            locPos = XmlILAnnotation.Write(ndIter).CachedIteratorDescriptor!.LocalPosition!;
            Debug.Assert(locPos != null);
            _iterCurr.Storage = StorageDescriptor.Local(locPos, typeof(int), false);

            return ndPos;
        }

        /// <summary>
        /// Generate code for QilNodeType.Sort.
        /// </summary>
        protected override QilNode VisitSort(QilLoop ndSort)
        {
            Type itemStorageType = GetItemStorageType(ndSort);
            LocalBuilder locCache, locKeys;
            Label lblOnEndSort = _helper.DefineLabel();
            Debug.Assert(ndSort.Variable.NodeType == QilNodeType.For);

            // XmlQuerySequence<T> cache;
            // cache = XmlQuerySequence.CreateOrReuse(cache);
            XmlILStorageMethods methods = XmlILMethods.StorageMethods[itemStorageType];
            locCache = _helper.DeclareLocal("$$$cache", methods.SeqType);
            _helper.Emit(OpCodes.Ldloc, locCache);
            _helper.Call(methods.SeqReuse);
            _helper.Emit(OpCodes.Stloc, locCache);
            _helper.Emit(OpCodes.Ldloc, locCache);

            // XmlSortKeyAccumulator keys;
            // keys.Create(runtime);
            locKeys = _helper.DeclareLocal("$$$keys", typeof(XmlSortKeyAccumulator));
            _helper.Emit(OpCodes.Ldloca, locKeys);
            _helper.Call(XmlILMethods.SortKeyCreate);

            // Construct nested iterator
            // foreach (item in sort-expr) {
            StartNestedIterator(ndSort.Variable, lblOnEndSort);
            StartBinding(ndSort.Variable);
            Debug.Assert(!_iterNested!.Storage.IsCached);

            // cache.Add(item);
            _iterCurr.EnsureStackNoCache();
            _iterCurr.EnsureItemStorageType(ndSort.Variable.XmlType!, GetItemStorageType(ndSort.Variable));
            _helper.Call(methods.SeqAdd);

            _helper.Emit(OpCodes.Ldloca, locKeys);

            // Add keys to accumulator (there may be several keys)
            foreach (QilSortKey ndKey in ndSort.Body)
                VisitSortKey(ndKey, locKeys);

            // keys.FinishSortKeys();
            _helper.Call(XmlILMethods.SortKeyFinish);

            // }
            _helper.Emit(OpCodes.Ldloc, locCache);
            _iterCurr.LoopToEnd(lblOnEndSort);

            // Remove cache reference from stack
            _helper.Emit(OpCodes.Pop);

            // cache.SortByKeys(keys.Keys);
            _helper.Emit(OpCodes.Ldloc, locCache);
            _helper.Emit(OpCodes.Ldloca, locKeys);
            _helper.Call(XmlILMethods.SortKeyKeys);
            _helper.Call(methods.SeqSortByKeys);

            // End nested iterator
            _iterCurr.Storage = StorageDescriptor.Local(locCache, itemStorageType, true);
            EndBinding(ndSort.Variable);
            EndNestedIterator(ndSort.Variable);
            _iterCurr.SetIterator(_iterNested);

            return ndSort;
        }

        /// <summary>
        /// Generate code to add a (value, collation) sort key to the XmlSortKeyAccumulator.
        /// </summary>
        private void VisitSortKey(QilSortKey ndKey, LocalBuilder locKeys)
        {
            Label lblOnEndKey;
            Debug.Assert(ndKey.Key.XmlType!.IsAtomicValue, "Sort key must be an atomic value.");

            // Push collation onto the stack
            _helper.Emit(OpCodes.Ldloca, locKeys);
            if (ndKey.Collation.NodeType == QilNodeType.LiteralString)
            {
                // collation = runtime.GetCollation(idx);
                _helper.CallGetCollation(_helper.StaticData.DeclareCollation((string)(QilLiteral)ndKey.Collation));
            }
            else
            {
                // collation = runtime.CreateCollation(str);
                _helper.LoadQueryRuntime();
                NestedVisitEnsureStack(ndKey.Collation);
                _helper.Call(XmlILMethods.CreateCollation);
            }

            if (ndKey.XmlType!.IsSingleton)
            {
                NestedVisitEnsureStack(ndKey.Key);

                // keys.AddSortKey(collation, value);
                _helper.AddSortKey(ndKey.Key.XmlType);
            }
            else
            {
                lblOnEndKey = _helper.DefineLabel();
                StartNestedIterator(ndKey.Key, lblOnEndKey);
                Visit(ndKey.Key);
                _iterCurr.EnsureStackNoCache();
                _iterCurr.EnsureItemStorageType(ndKey.Key.XmlType, GetItemStorageType(ndKey.Key));

                // Non-empty sort key
                // keys.AddSortKey(collation, value);
                _helper.AddSortKey(ndKey.Key.XmlType);

                // goto LabelDone;
                // LabelOnEnd:
                Label lblDone = _helper.DefineLabel();
                _helper.EmitUnconditionalBranch(OpCodes.Br_S, lblDone);
                _helper.MarkLabel(lblOnEndKey);

                // Empty sequence key
                // keys.AddSortKey(collation);
                _helper.AddSortKey(null);

                _helper.MarkLabel(lblDone);

                EndNestedIterator(ndKey.Key);
            }
        }

        /// <summary>
        /// Generate code for QilNodeType.DocOrderDistinct.
        /// </summary>
        protected override QilNode VisitDocOrderDistinct(QilUnary ndDod)
        {
            // DocOrderDistinct applied to a singleton is a no-op
            if (ndDod.XmlType!.IsSingleton)
                return Visit(ndDod.Child);

            // Handle any special-case patterns that are rooted at DocOrderDistinct
            if (HandleDodPatterns(ndDod))
                return ndDod;

            // Sort results of child expression by document order and remove duplicate nodes
            // cache = runtime.DocOrderDistinct(cache);
            _helper.LoadQueryRuntime();
            NestedVisitEnsureCache(ndDod.Child, typeof(XPathNavigator));
            _iterCurr.EnsureStack();
            _helper.Call(XmlILMethods.DocOrder);
            return ndDod;
        }

        /// <summary>
        /// There are a number of path patterns that can be rooted at DocOrderDistinct nodes.  Determine whether one of these
        /// patterns has been previously matched on "ndDod".  If so, generate code for the pattern and return true.  Otherwise,
        /// just return false.
        /// </summary>
        private bool HandleDodPatterns(QilUnary ndDod)
        {
            OptimizerPatterns pattDod = OptimizerPatterns.Read(ndDod);
            XmlNodeKindFlags kinds;
            QilName? name;
            QilNode input, step;
            bool isJoinAndDod;

            // Handle JoinAndDod and DodReverse patterns
            isJoinAndDod = pattDod.MatchesPattern(OptimizerPatternName.JoinAndDod);
            if (isJoinAndDod || pattDod.MatchesPattern(OptimizerPatternName.DodReverse))
            {
                OptimizerPatterns pattStep = OptimizerPatterns.Read((QilNode)pattDod.GetArgument(OptimizerPatternArgument.DodStep));

                if (pattStep.MatchesPattern(OptimizerPatternName.FilterElements))
                {
                    // FilterElements pattern, so Kind = Element and Name = Argument
                    kinds = XmlNodeKindFlags.Element;
                    name = (QilName)pattStep.GetArgument(OptimizerPatternArgument.ElementQName);
                }
                else if (pattStep.MatchesPattern(OptimizerPatternName.FilterContentKind))
                {
                    // FilterKindTest pattern, so Kind = Argument and Name = null
                    kinds = ((XmlQueryType)pattStep.GetArgument(OptimizerPatternArgument.KindTestType)).NodeKinds;
                    name = null;
                }
                else
                {
                    Debug.Assert(pattStep.MatchesPattern(OptimizerPatternName.Axis), "Dod patterns should only match if step is FilterElements or FilterKindTest or Axis");
                    kinds = ((ndDod.XmlType!.NodeKinds & XmlNodeKindFlags.Attribute) != 0) ? XmlNodeKindFlags.Any : XmlNodeKindFlags.Content;
                    name = null;
                }

                step = (QilNode)pattStep.GetArgument(OptimizerPatternArgument.StepNode);
                if (isJoinAndDod)
                {
                    switch (step.NodeType)
                    {
                        case QilNodeType.Content:
                            CreateContainerIterator(ndDod, "$$$iterContent", typeof(ContentMergeIterator), XmlILMethods.ContentMergeCreate, XmlILMethods.ContentMergeNext, XmlILMethods.ContentMergeCurrent,
                                                    kinds, name, TriState.Unknown);
                            return true;

                        case QilNodeType.Descendant:
                        case QilNodeType.DescendantOrSelf:
                            CreateContainerIterator(ndDod, "$$$iterDesc", typeof(DescendantMergeIterator), XmlILMethods.DescMergeCreate, XmlILMethods.DescMergeNext, XmlILMethods.DescMergeCurrent,
                                                    kinds, name, (step.NodeType == QilNodeType.Descendant) ? TriState.False : TriState.True);
                            return true;

                        case QilNodeType.XPathFollowing:
                            CreateContainerIterator(ndDod, "$$$iterFoll", typeof(XPathFollowingMergeIterator), XmlILMethods.XPFollMergeCreate, XmlILMethods.XPFollMergeNext, XmlILMethods.XPFollMergeCurrent,
                                                    kinds, name, TriState.Unknown);
                            return true;

                        case QilNodeType.FollowingSibling:
                            CreateContainerIterator(ndDod, "$$$iterFollSib", typeof(FollowingSiblingMergeIterator), XmlILMethods.FollSibMergeCreate, XmlILMethods.FollSibMergeNext, XmlILMethods.FollSibMergeCurrent,
                                                    kinds, name, TriState.Unknown);
                            return true;

                        case QilNodeType.XPathPreceding:
                            CreateContainerIterator(ndDod, "$$$iterPrec", typeof(XPathPrecedingMergeIterator), XmlILMethods.XPPrecMergeCreate, XmlILMethods.XPPrecMergeNext, XmlILMethods.XPPrecMergeCurrent,
                                                    kinds, name, TriState.Unknown);
                            return true;

                        default:
                            Debug.Fail($"Pattern {step.NodeType} should have been handled.");
                            break;
                    }
                }
                else
                {
                    input = (QilNode)pattStep.GetArgument(OptimizerPatternArgument.StepInput);
                    switch (step.NodeType)
                    {
                        case QilNodeType.Ancestor:
                        case QilNodeType.AncestorOrSelf:
                            CreateFilteredIterator(input, "$$$iterAnc", typeof(AncestorDocOrderIterator), XmlILMethods.AncDOCreate, XmlILMethods.AncDONext, XmlILMethods.AncDOCurrent,
                                                   kinds, name, (step.NodeType == QilNodeType.Ancestor) ? TriState.False : TriState.True, null);
                            return true;

                        case QilNodeType.PrecedingSibling:
                            CreateFilteredIterator(input, "$$$iterPreSib", typeof(PrecedingSiblingDocOrderIterator), XmlILMethods.PreSibDOCreate, XmlILMethods.PreSibDONext, XmlILMethods.PreSibDOCurrent,
                                                   kinds, name, TriState.Unknown, null);
                            return true;

                        case QilNodeType.XPathPreceding:
                            CreateFilteredIterator(input, "$$$iterPrec", typeof(XPathPrecedingDocOrderIterator), XmlILMethods.XPPrecDOCreate, XmlILMethods.XPPrecDONext, XmlILMethods.XPPrecDOCurrent,
                                                   kinds, name, TriState.Unknown, null);
                            return true;

                        default:
                            Debug.Fail($"Pattern {step.NodeType} should have been handled.");
                            break;
                    }
                }
            }
            else if (pattDod.MatchesPattern(OptimizerPatternName.DodMerge))
            {
                // DodSequenceMerge dodMerge;
                LocalBuilder locMerge = _helper.DeclareLocal("$$$dodMerge", typeof(DodSequenceMerge));
                Label lblOnEnd = _helper.DefineLabel();

                // dodMerge.Create(runtime);
                _helper.Emit(OpCodes.Ldloca, locMerge);
                _helper.LoadQueryRuntime();
                _helper.Call(XmlILMethods.DodMergeCreate);
                _helper.Emit(OpCodes.Ldloca, locMerge);

                StartNestedIterator(ndDod.Child, lblOnEnd);

                // foreach (seq in expr) {
                Visit(ndDod.Child);

                // dodMerge.AddSequence(seq);
                Debug.Assert(_iterCurr.Storage.IsCached, "DodMerge pattern should only be matched when cached sequences are returned from loop");
                _iterCurr.EnsureStack();
                _helper.Call(XmlILMethods.DodMergeAdd);
                _helper.Emit(OpCodes.Ldloca, locMerge);

                // }
                _iterCurr.LoopToEnd(lblOnEnd);

                EndNestedIterator(ndDod.Child);

                // mergedSequence = dodMerge.MergeSequences();
                _helper.Call(XmlILMethods.DodMergeSeq);

                _iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathNavigator), true);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Generate code for QilNodeType.Invoke.
        /// </summary>
        protected override QilNode VisitInvoke(QilInvoke ndInvoke)
        {
            QilFunction ndFunc = ndInvoke.Function;
            MethodInfo methInfo = XmlILAnnotation.Write(ndFunc).FunctionBinding!;
            bool useWriter = (XmlILConstructInfo.Read(ndFunc).ConstructMethod == XmlILConstructMethod.Writer);
            Debug.Assert(!XmlILConstructInfo.Read(ndInvoke).PushToWriterFirst || useWriter);

            // Push XmlQueryRuntime onto the stack as the first parameter
            _helper.LoadQueryRuntime();

            // Generate code to push each Invoke argument onto the stack
            for (int iArg = 0; iArg < ndInvoke.Arguments.Count; iArg++)
            {
                QilNode ndActualArg = ndInvoke.Arguments[iArg];
                QilNode ndFormalArg = ndInvoke.Function.Arguments[iArg];
                NestedVisitEnsureStack(ndActualArg, GetItemStorageType(ndFormalArg), !ndFormalArg.XmlType!.IsSingleton);
            }

            // Check whether this call should compiled using the .tailcall instruction
            if (OptimizerPatterns.Read(ndInvoke).MatchesPattern(OptimizerPatternName.TailCall))
                _helper.TailCall(methInfo);
            else
                _helper.Call(methInfo);

            // If function's results are not pushed to Writer,
            if (!useWriter)
            {
                // Return value is on the stack; ensure it has the correct storage type
                _iterCurr.Storage = StorageDescriptor.Stack(GetItemStorageType(ndInvoke), !ndInvoke.XmlType!.IsSingleton);
            }
            else
            {
                _iterCurr.Storage = StorageDescriptor.None();
            }

            return ndInvoke;
        }

        /// <summary>
        /// Generate code for QilNodeType.Content.
        /// </summary>
        protected override QilNode VisitContent(QilUnary ndContent)
        {
            CreateSimpleIterator(ndContent.Child, "$$$iterAttrContent", typeof(AttributeContentIterator), XmlILMethods.AttrContentCreate, XmlILMethods.AttrContentNext, XmlILMethods.AttrContentCurrent);
            return ndContent;
        }

        /// <summary>
        /// Generate code for QilNodeType.Attribute.
        /// </summary>
        protected override QilNode VisitAttribute(QilBinary ndAttr)
        {
            QilName? ndName = ndAttr.Right as QilName;
            Debug.Assert(ndName != null, "Attribute node must have a literal QName as its second argument");

            // XPathNavigator navAttr;
            LocalBuilder locNav = _helper.DeclareLocal("$$$navAttr", typeof(XPathNavigator));

            // navAttr = SyncToNavigator(navAttr, navCtxt);
            SyncToNavigator(locNav, ndAttr.Left);

            // if (!navAttr.MoveToAttribute(localName, namespaceUri)) goto LabelNextCtxt;
            _helper.Emit(OpCodes.Ldloc, locNav);
            _helper.CallGetAtomizedName(_helper.StaticData.DeclareName(ndName.LocalName));
            _helper.CallGetAtomizedName(_helper.StaticData.DeclareName(ndName.NamespaceUri));
            _helper.Call(XmlILMethods.NavMoveAttr);
            _helper.Emit(OpCodes.Brfalse, _iterCurr.GetLabelNext());

            _iterCurr.Storage = StorageDescriptor.Local(locNav, typeof(XPathNavigator), false);
            return ndAttr;
        }

        /// <summary>
        /// Generate code for QilNodeType.Parent.
        /// </summary>
        protected override QilNode VisitParent(QilUnary ndParent)
        {
            // XPathNavigator navParent;
            LocalBuilder locNav = _helper.DeclareLocal("$$$navParent", typeof(XPathNavigator));

            // navParent = SyncToNavigator(navParent, navCtxt);
            SyncToNavigator(locNav, ndParent.Child);

            // if (!navParent.MoveToParent()) goto LabelNextCtxt;
            _helper.Emit(OpCodes.Ldloc, locNav);
            _helper.Call(XmlILMethods.NavMoveParent);
            _helper.Emit(OpCodes.Brfalse, _iterCurr.GetLabelNext());

            _iterCurr.Storage = StorageDescriptor.Local(locNav, typeof(XPathNavigator), false);
            return ndParent;
        }

        /// <summary>
        /// Generate code for QilNodeType.Root.
        /// </summary>
        protected override QilNode VisitRoot(QilUnary ndRoot)
        {
            // XPathNavigator navRoot;
            LocalBuilder locNav = _helper.DeclareLocal("$$$navRoot", typeof(XPathNavigator));

            // navRoot = SyncToNavigator(navRoot, navCtxt);
            SyncToNavigator(locNav, ndRoot.Child);

            // navRoot.MoveToRoot();
            _helper.Emit(OpCodes.Ldloc, locNav);
            _helper.Call(XmlILMethods.NavMoveRoot);

            _iterCurr.Storage = StorageDescriptor.Local(locNav, typeof(XPathNavigator), false);
            return ndRoot;
        }

        /// <summary>
        /// Generate code for QilNodeType.XmlContext.
        /// </summary>
        /// <remarks>
        /// Generates code to retrieve the default document using the XmlResolver.
        /// </remarks>
        protected override QilNode VisitXmlContext(QilNode ndCtxt)
        {
            // runtime.ExternalContext.DefaultDataSource
            _helper.LoadQueryContext();
            _helper.Call(XmlILMethods.GetDefaultDataSource);
            _iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathNavigator), false);
            return ndCtxt;
        }

        /// <summary>
        /// Find physical query plan for QilNodeType.Descendant.
        /// </summary>
        protected override QilNode VisitDescendant(QilUnary ndDesc)
        {
            CreateFilteredIterator(ndDesc.Child, "$$$iterDesc", typeof(DescendantIterator), XmlILMethods.DescCreate, XmlILMethods.DescNext, XmlILMethods.DescCurrent,
                                   XmlNodeKindFlags.Any, null, TriState.False, null);
            return ndDesc;
        }

        /// <summary>
        /// Generate code for QilNodeType.DescendantOrSelf.
        /// </summary>
        protected override QilNode VisitDescendantOrSelf(QilUnary ndDesc)
        {
            CreateFilteredIterator(ndDesc.Child, "$$$iterDesc", typeof(DescendantIterator), XmlILMethods.DescCreate, XmlILMethods.DescNext, XmlILMethods.DescCurrent,
                                   XmlNodeKindFlags.Any, null, TriState.True, null);
            return ndDesc;
        }

        /// <summary>
        /// Find physical query plan for QilNodeType.Ancestor.
        /// </summary>
        protected override QilNode VisitAncestor(QilUnary ndAnc)
        {
            CreateFilteredIterator(ndAnc.Child, "$$$iterAnc", typeof(AncestorIterator), XmlILMethods.AncCreate, XmlILMethods.AncNext, XmlILMethods.AncCurrent,
                                   XmlNodeKindFlags.Any, null, TriState.False, null);
            return ndAnc;
        }

        /// <summary>
        /// Find physical query plan for QilNodeType.AncestorOrSelf.
        /// </summary>
        protected override QilNode VisitAncestorOrSelf(QilUnary ndAnc)
        {
            CreateFilteredIterator(ndAnc.Child, "$$$iterAnc", typeof(AncestorIterator), XmlILMethods.AncCreate, XmlILMethods.AncNext, XmlILMethods.AncCurrent,
                                   XmlNodeKindFlags.Any, null, TriState.True, null);
            return ndAnc;
        }

        /// <summary>
        /// Find physical query plan for QilNodeType.Preceding.
        /// </summary>
        protected override QilNode VisitPreceding(QilUnary ndPrec)
        {
            CreateFilteredIterator(ndPrec.Child, "$$$iterPrec", typeof(PrecedingIterator), XmlILMethods.PrecCreate, XmlILMethods.PrecNext, XmlILMethods.PrecCurrent,
                                   XmlNodeKindFlags.Any, null, TriState.Unknown, null);
            return ndPrec;
        }

        /// <summary>
        /// Find physical query plan for QilNodeType.FollowingSibling.
        /// </summary>
        protected override QilNode VisitFollowingSibling(QilUnary ndFollSib)
        {
            CreateFilteredIterator(ndFollSib.Child, "$$$iterFollSib", typeof(FollowingSiblingIterator), XmlILMethods.FollSibCreate, XmlILMethods.FollSibNext, XmlILMethods.FollSibCurrent,
                                   XmlNodeKindFlags.Any, null, TriState.Unknown, null);
            return ndFollSib;
        }

        /// <summary>
        /// Find physical query plan for QilNodeType.PrecedingSibling.
        /// </summary>
        protected override QilNode VisitPrecedingSibling(QilUnary ndPreSib)
        {
            CreateFilteredIterator(ndPreSib.Child, "$$$iterPreSib", typeof(PrecedingSiblingIterator), XmlILMethods.PreSibCreate, XmlILMethods.PreSibNext, XmlILMethods.PreSibCurrent,
                                   XmlNodeKindFlags.Any, null, TriState.Unknown, null);
            return ndPreSib;
        }

        /// <summary>
        /// Find physical query plan for QilNodeType.NodeRange.
        /// </summary>
        protected override QilNode VisitNodeRange(QilBinary ndRange)
        {
            CreateFilteredIterator(ndRange.Left, "$$$iterRange", typeof(NodeRangeIterator), XmlILMethods.NodeRangeCreate, XmlILMethods.NodeRangeNext, XmlILMethods.NodeRangeCurrent,
                                   XmlNodeKindFlags.Any, null, TriState.Unknown, ndRange.Right);
            return ndRange;
        }

        /// <summary>
        /// Generate code for QilNodeType.Deref.
        /// </summary>
        protected override QilNode VisitDeref(QilBinary ndDeref)
        {
            // IdIterator iterId;
            LocalBuilder locIter = _helper.DeclareLocal("$$$iterId", typeof(IdIterator));

            // iterId.Create(navCtxt, value);
            _helper.Emit(OpCodes.Ldloca, locIter);
            NestedVisitEnsureStack(ndDeref.Left);
            NestedVisitEnsureStack(ndDeref.Right);
            _helper.Call(XmlILMethods.IdCreate);

            GenerateSimpleIterator(typeof(XPathNavigator), locIter, XmlILMethods.IdNext, XmlILMethods.IdCurrent);

            return ndDeref;
        }

        /// <summary>
        /// Generate code for QilNodeType.ElementCtor.
        /// </summary>
        protected override QilNode VisitElementCtor(QilBinary ndElem)
        {
            XmlILConstructInfo info = XmlILConstructInfo.Read(ndElem);
            bool callChk;
            GenerateNameType nameType;
            Debug.Assert(XmlILConstructInfo.Read(ndElem).PushToWriterFirst, "Element contruction should always be pushed to writer.");

            // Runtime checks must be made in the following cases:
            //   1. Xml state is not known at compile-time, or is illegal
            //   2. Element's namespace must be declared
            //   3. Element's attributes might be duplicates of one another, or namespaces might follow attributes
            callChk = CheckWithinContent(info) || !info.IsNamespaceInScope || ElementCachesAttributes(info);

            // If it is not known whether element content was output, then make this check at run-time
            if (XmlILConstructInfo.Read(ndElem.Right).FinalStates == PossibleXmlStates.Any)
                callChk = true;

            // If runtime state after EndElement is called is not known, then call XmlQueryOutput.WriteEndElementChk
            if (info.FinalStates == PossibleXmlStates.Any)
                callChk = true;

            // If WriteStartElementChk will *not* be called, then code must be generated to ensure valid state transitions
            if (!callChk)
                BeforeStartChecks(ndElem);

            // Generate call to WriteStartElement
            nameType = LoadNameAndType(XPathNodeType.Element, ndElem.Left, true, callChk);
            _helper.CallWriteStartElement(nameType, callChk);

            // Recursively construct content
            NestedVisit(ndElem.Right);

            // If runtime state is guaranteed to be EnumAttrs, and an element is being constructed, call XmlQueryOutput.StartElementContent
            if (XmlILConstructInfo.Read(ndElem.Right).FinalStates == PossibleXmlStates.EnumAttrs && !callChk)
                _helper.CallStartElementContent();

            // Generate call to WriteEndElement
            nameType = LoadNameAndType(XPathNodeType.Element, ndElem.Left, false, callChk);
            _helper.CallWriteEndElement(nameType, callChk);

            if (!callChk)
                AfterEndChecks(ndElem);

            _iterCurr.Storage = StorageDescriptor.None();
            return ndElem;
        }

        /// <summary>
        /// Generate code for QilNodeType.AttributeCtor.
        /// </summary>
        protected override QilNode VisitAttributeCtor(QilBinary ndAttr)
        {
            XmlILConstructInfo info = XmlILConstructInfo.Read(ndAttr);
            bool callChk;
            GenerateNameType nameType;
            Debug.Assert(XmlILConstructInfo.Read(ndAttr).PushToWriterFirst, "Attribute construction should always be pushed to writer.");

            // Runtime checks must be made in the following cases:
            //   1. Xml state is not known at compile-time, or is illegal
            //   2. Attribute's namespace must be declared
            callChk = CheckEnumAttrs(info) || !info.IsNamespaceInScope;

            // If WriteStartAttributeChk will *not* be called, then code must be generated to ensure well-formedness
            // and track namespace scope.
            if (!callChk)
                BeforeStartChecks(ndAttr);

            // Generate call to WriteStartAttribute
            nameType = LoadNameAndType(XPathNodeType.Attribute, ndAttr.Left, true, callChk);
            _helper.CallWriteStartAttribute(nameType, callChk);

            // Recursively construct content
            NestedVisit(ndAttr.Right);

            // Generate call to WriteEndAttribute
            _helper.CallWriteEndAttribute(callChk);

            if (!callChk)
                AfterEndChecks(ndAttr);

            _iterCurr.Storage = StorageDescriptor.None();
            return ndAttr;
        }

        /// <summary>
        /// Generate code for QilNodeType.CommentCtor.
        /// </summary>
        protected override QilNode VisitCommentCtor(QilUnary ndComment)
        {
            Debug.Assert(XmlILConstructInfo.Read(ndComment).PushToWriterFirst, "Comment construction should always be pushed to writer.");

            // Always call XmlQueryOutput.WriteStartComment
            _helper.CallWriteStartComment();

            // Recursively construct content
            NestedVisit(ndComment.Child);

            // Always call XmlQueryOutput.WriteEndComment
            _helper.CallWriteEndComment();

            _iterCurr.Storage = StorageDescriptor.None();
            return ndComment;
        }

        /// <summary>
        /// Generate code for QilNodeType.PICtor.
        /// </summary>
        protected override QilNode VisitPICtor(QilBinary ndPI)
        {
            Debug.Assert(XmlILConstructInfo.Read(ndPI).PushToWriterFirst, "PI construction should always be pushed to writer.");

            // Always call XmlQueryOutput.WriteStartPI
            _helper.LoadQueryOutput();
            NestedVisitEnsureStack(ndPI.Left);
            _helper.CallWriteStartPI();

            // Recursively construct content
            NestedVisit(ndPI.Right);

            // Always call XmlQueryOutput.WriteEndPI
            _helper.CallWriteEndPI();

            _iterCurr.Storage = StorageDescriptor.None();
            return ndPI;
        }

        /// <summary>
        /// Generate code for QilNodeType.TextCtor.
        /// </summary>
        protected override QilNode VisitTextCtor(QilUnary ndText)
        {
            return VisitTextCtor(ndText, false);
        }

        /// <summary>
        /// Generate code for QilNodeType.RawTextCtor.
        /// </summary>
        protected override QilNode VisitRawTextCtor(QilUnary ndText)
        {
            return VisitTextCtor(ndText, true);
        }

        /// <summary>
        /// Generate code for QilNodeType.TextCtor and QilNodeType.RawTextCtor.
        /// </summary>
        private QilNode VisitTextCtor(QilUnary ndText, bool disableOutputEscaping)
        {
            XmlILConstructInfo info = XmlILConstructInfo.Read(ndText);
            bool callChk;
            Debug.Assert(info.PushToWriterFirst, "Text construction should always be pushed to writer.");

            // Write out text in different contexts (within attribute, within element, within comment, etc.)
            switch (info.InitialStates)
            {
                case PossibleXmlStates.WithinAttr:
                case PossibleXmlStates.WithinComment:
                case PossibleXmlStates.WithinPI:
                    callChk = false;
                    break;

                default:
                    callChk = CheckWithinContent(info);
                    break;
            }

            if (!callChk)
                BeforeStartChecks(ndText);

            _helper.LoadQueryOutput();

            // Push string value of text onto IL stack
            NestedVisitEnsureStack(ndText.Child);

            // Write out text in different contexts (within attribute, within element, within comment, etc.)
            switch (info.InitialStates)
            {
                case PossibleXmlStates.WithinAttr:
                    // Ignore hints when writing out attribute text
                    _helper.CallWriteString(false, callChk);
                    break;

                case PossibleXmlStates.WithinComment:
                    // Call XmlQueryOutput.WriteCommentString
                    _helper.Call(XmlILMethods.CommentText);
                    break;

                case PossibleXmlStates.WithinPI:
                    // Call XmlQueryOutput.WriteProcessingInstructionString
                    _helper.Call(XmlILMethods.PIText);
                    break;

                default:
                    // Call XmlQueryOutput.WriteTextBlockChk, XmlQueryOutput.WriteTextBlockNoEntities, or XmlQueryOutput.WriteTextBlock
                    _helper.CallWriteString(disableOutputEscaping, callChk);
                    break;
            }

            if (!callChk)
                AfterEndChecks(ndText);

            _iterCurr.Storage = StorageDescriptor.None();
            return ndText;
        }

        /// <summary>
        /// Generate code for QilNodeType.DocumentCtor.
        /// </summary>
        protected override QilNode VisitDocumentCtor(QilUnary ndDoc)
        {
            Debug.Assert(XmlILConstructInfo.Read(ndDoc).PushToWriterFirst, "Document root construction should always be pushed to writer.");

            // Generate call to XmlQueryOutput.WriteStartRootChk
            _helper.CallWriteStartRoot();

            // Recursively construct content
            NestedVisit(ndDoc.Child);

            // Generate call to XmlQueryOutput.WriteEndRootChk
            _helper.CallWriteEndRoot();

            _iterCurr.Storage = StorageDescriptor.None();

            return ndDoc;
        }

        /// <summary>
        /// Generate code for QilNodeType.NamespaceDecl.
        /// </summary>
        protected override QilNode VisitNamespaceDecl(QilBinary ndNmsp)
        {
            XmlILConstructInfo info = XmlILConstructInfo.Read(ndNmsp);
            bool callChk;
            Debug.Assert(info.PushToWriterFirst, "Namespace construction should always be pushed to writer.");

            // Runtime checks must be made in the following cases:
            //   1. Xml state is not known at compile-time, or is illegal
            //   2. Namespaces might be added to element after attributes have already been added
            callChk = CheckEnumAttrs(info) || MightHaveNamespacesAfterAttributes(info);

            // If WriteNamespaceDeclarationChk will *not* be called, then code must be generated to ensure well-formedness
            // and track namespace scope.
            if (!callChk)
                BeforeStartChecks(ndNmsp);

            _helper.LoadQueryOutput();

            // Recursively construct prefix and ns
            NestedVisitEnsureStack(ndNmsp.Left);
            NestedVisitEnsureStack(ndNmsp.Right);

            // Generate call to WriteNamespaceDecl
            _helper.CallWriteNamespaceDecl(callChk);

            if (!callChk)
                AfterEndChecks(ndNmsp);

            _iterCurr.Storage = StorageDescriptor.None();
            return ndNmsp;
        }

        /// <summary>
        /// Generate code for QilNodeType.RtfCtor.
        /// </summary>
        protected override QilNode VisitRtfCtor(QilBinary ndRtf)
        {
            OptimizerPatterns patt = OptimizerPatterns.Read(ndRtf);
            string baseUri = (string)(QilLiteral)ndRtf.Right;

            if (patt.MatchesPattern(OptimizerPatternName.SingleTextRtf))
            {
                // Special-case Rtf containing a root node and a single text node child
                _helper.LoadQueryRuntime();
                NestedVisitEnsureStack((QilNode)patt.GetArgument(OptimizerPatternArgument.RtfText));
                _helper.Emit(OpCodes.Ldstr, baseUri);
                _helper.Call(XmlILMethods.RtfConstr);
            }
            else
            {
                // Start nested construction of an Rtf
                _helper.CallStartRtfConstruction(baseUri);

                // Write content of Rtf to writer
                NestedVisit(ndRtf.Left);

                // Get the result Rtf
                _helper.CallEndRtfConstruction();
            }

            _iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathNavigator), false);
            return ndRtf;
        }

        /// <summary>
        /// Generate code for QilNodeType.NameOf.
        /// </summary>
        protected override QilNode VisitNameOf(QilUnary ndName)
        {
            return VisitNodeProperty(ndName);
        }

        /// <summary>
        /// Generate code for QilNodeType.LocalNameOf.
        /// </summary>
        protected override QilNode VisitLocalNameOf(QilUnary ndName)
        {
            return VisitNodeProperty(ndName);
        }

        /// <summary>
        /// Generate code for QilNodeType.NamespaceUriOf.
        /// </summary>
        protected override QilNode VisitNamespaceUriOf(QilUnary ndName)
        {
            return VisitNodeProperty(ndName);
        }

        /// <summary>
        /// Generate code for QilNodeType.PrefixOf.
        /// </summary>
        protected override QilNode VisitPrefixOf(QilUnary ndName)
        {
            return VisitNodeProperty(ndName);
        }

        /// <summary>
        /// Generate code to push the local name, namespace uri, or qname of the context navigator.
        /// </summary>
        private QilNode VisitNodeProperty(QilUnary ndProp)
        {
            // Generate code to push argument onto stack
            NestedVisitEnsureStack(ndProp.Child);

            switch (ndProp.NodeType)
            {
                case QilNodeType.NameOf:
                    // push new XmlQualifiedName(navigator.LocalName, navigator.NamespaceURI);
                    _helper.Emit(OpCodes.Dup);
                    _helper.Call(XmlILMethods.NavLocalName);
                    _helper.Call(XmlILMethods.NavNmsp);
                    _helper.Construct(XmlILConstructors.QName);
                    _iterCurr.Storage = StorageDescriptor.Stack(typeof(XmlQualifiedName), false);
                    break;

                case QilNodeType.LocalNameOf:
                    // push navigator.Name;
                    _helper.Call(XmlILMethods.NavLocalName);
                    _iterCurr.Storage = StorageDescriptor.Stack(typeof(string), false);
                    break;

                case QilNodeType.NamespaceUriOf:
                    // push navigator.NamespaceURI;
                    _helper.Call(XmlILMethods.NavNmsp);
                    _iterCurr.Storage = StorageDescriptor.Stack(typeof(string), false);
                    break;

                case QilNodeType.PrefixOf:
                    // push navigator.Prefix;
                    _helper.Call(XmlILMethods.NavPrefix);
                    _iterCurr.Storage = StorageDescriptor.Stack(typeof(string), false);
                    break;

                default:
                    Debug.Fail($"Unexpected node type {ndProp.NodeType}");
                    break;
            }

            return ndProp;
        }

        /// <summary>
        /// Find physical query plan for QilNodeType.TypeAssert.
        /// </summary>
        protected override QilNode VisitTypeAssert(QilTargetType ndTypeAssert)
        {
            if (!ndTypeAssert.Source.XmlType!.IsSingleton && ndTypeAssert.XmlType!.IsSingleton && !_iterCurr.HasLabelNext)
            {
                // This case occurs when a non-singleton expression is treated as cardinality One.
                // The trouble is that the expression will branch to an end label when it's done iterating, so
                // an end label must be provided.  But there is no next label in the current iteration context,
                // so we've got to create a dummy label instead (IL requires it).  This creates an infinite loop,
                // but since it's known statically that the expression is cardinality One, this branch will never
                // be taken.
                Label lblDummy = _helper.DefineLabel();
                _helper.MarkLabel(lblDummy);
                NestedVisit(ndTypeAssert.Source, lblDummy);
            }
            else
            {
                // Generate code for child expression
                Visit(ndTypeAssert.Source);
            }

            _iterCurr.EnsureItemStorageType(ndTypeAssert.Source.XmlType, GetItemStorageType(ndTypeAssert));
            return ndTypeAssert;
        }

        /// <summary>
        /// Generate code for QilNodeType.IsType.
        /// </summary>
        protected override QilNode VisitIsType(QilTargetType ndIsType)
        {
            XmlQueryType typDerived, typBase;
            XmlTypeCode codeBase;

            typDerived = ndIsType.Source.XmlType!;
            typBase = ndIsType.TargetType;
            Debug.Assert(!typDerived!.NeverSubtypeOf(typBase), "Normalizer should have eliminated IsType where source can never be a subtype of destination type.");

            // Special Case: Test whether singleton item is a Node
            if (typDerived.IsSingleton && (object)typBase == (object)TypeFactory.Node)
            {
                NestedVisitEnsureStack(ndIsType.Source);
                Debug.Assert(_iterCurr.Storage.ItemStorageType == typeof(XPathItem), "If !IsNode, then storage type should be Item");

                // if (item.IsNode op true) goto LabelBranch;
                _helper.Call(XmlILMethods.ItemIsNode);
                ZeroCompare(QilNodeType.Ne, true);

                return ndIsType;
            }

            // Special Case: Source value is a singleton Node, and we're testing whether it is an Element, Attribute, PI, etc.
            if (MatchesNodeKinds(ndIsType, typDerived, typBase))
                return ndIsType;

            // Special Case: XmlTypeCode is sufficient to describe destination type
            if ((object)typBase == (object)TypeFactory.Double) codeBase = XmlTypeCode.Double;
            else if ((object)typBase == (object)TypeFactory.String) codeBase = XmlTypeCode.String;
            else if ((object)typBase == (object)TypeFactory.Boolean) codeBase = XmlTypeCode.Boolean;
            else if ((object)typBase == (object)TypeFactory.Node) codeBase = XmlTypeCode.Node;
            else codeBase = XmlTypeCode.None;

            if (codeBase != XmlTypeCode.None)
            {
                // if (runtime.MatchesXmlType(value, code) op true) goto LabelBranch;
                _helper.LoadQueryRuntime();
                NestedVisitEnsureStack(ndIsType.Source, typeof(XPathItem), !typDerived.IsSingleton);
                _helper.LoadInteger((int)codeBase);
                _helper.Call(typDerived.IsSingleton ? XmlILMethods.ItemMatchesCode : XmlILMethods.SeqMatchesCode);
                ZeroCompare(QilNodeType.Ne, true);

                return ndIsType;
            }

            // if (runtime.MatchesXmlType(value, idxType) op true) goto LabelBranch;
            _helper.LoadQueryRuntime();
            NestedVisitEnsureStack(ndIsType.Source, typeof(XPathItem), !typDerived.IsSingleton);
            _helper.LoadInteger(_helper.StaticData.DeclareXmlType(typBase));
            _helper.Call(typDerived.IsSingleton ? XmlILMethods.ItemMatchesType : XmlILMethods.SeqMatchesType);
            ZeroCompare(QilNodeType.Ne, true);

            return ndIsType;
        }

        /// <summary>
        /// Faster code can be generated if type test is just a node kind test.  If this special case is detected, then generate code and return true.
        /// Otherwise, return false, and a call to MatchesXmlType will be generated instead.
        /// </summary>
        private bool MatchesNodeKinds(QilTargetType ndIsType, XmlQueryType typDerived, XmlQueryType typBase)
        {
            XmlNodeKindFlags kinds;
            bool allowKinds = true;
            XPathNodeType kindsRuntime;
            int kindsUnion;

            // If not checking whether typDerived is some kind of singleton node, then fallback to MatchesXmlType
            if (!typBase.IsNode || !typBase.IsSingleton)
                return false;

            // If typDerived is not statically guaranteed to be a singleton node (and not an rtf), then fallback to MatchesXmlType
            if (!typDerived.IsNode || !typDerived.IsSingleton || !typDerived.IsNotRtf)
                return false;

            // Now we are guaranteed that typDerived is a node, and typBase is a node, so check node kinds
            // Ensure that typBase is only composed of kind-test prime types (no name-test, no schema-test, etc.)
            kinds = XmlNodeKindFlags.None;
            foreach (XmlQueryType typItem in typBase)
            {
                if ((object)typItem == (object)TypeFactory.Element) kinds |= XmlNodeKindFlags.Element;
                else if ((object)typItem == (object)TypeFactory.Attribute) kinds |= XmlNodeKindFlags.Attribute;
                else if ((object)typItem == (object)TypeFactory.Text) kinds |= XmlNodeKindFlags.Text;
                else if ((object)typItem == (object)TypeFactory.Document) kinds |= XmlNodeKindFlags.Document;
                else if ((object)typItem == (object)TypeFactory.Comment) kinds |= XmlNodeKindFlags.Comment;
                else if ((object)typItem == (object)TypeFactory.PI) kinds |= XmlNodeKindFlags.PI;
                else if ((object)typItem == (object)TypeFactory.Namespace) kinds |= XmlNodeKindFlags.Namespace;
                else return false;
            }

            Debug.Assert((typDerived.NodeKinds & kinds) != XmlNodeKindFlags.None, "Normalizer should have taken care of case where node kinds are disjoint.");

            kinds = typDerived.NodeKinds & kinds;

            // Attempt to allow or disallow exactly one kind
            if (!Bits.ExactlyOne((uint)kinds))
            {
                // Not possible to allow one kind, so try to disallow one kind
                kinds = ~kinds & XmlNodeKindFlags.Any;
                allowKinds = !allowKinds;
            }

            switch (kinds)
            {
                case XmlNodeKindFlags.Element: kindsRuntime = XPathNodeType.Element; break;
                case XmlNodeKindFlags.Attribute: kindsRuntime = XPathNodeType.Attribute; break;
                case XmlNodeKindFlags.Namespace: kindsRuntime = XPathNodeType.Namespace; break;
                case XmlNodeKindFlags.PI: kindsRuntime = XPathNodeType.ProcessingInstruction; break;
                case XmlNodeKindFlags.Comment: kindsRuntime = XPathNodeType.Comment; break;
                case XmlNodeKindFlags.Document: kindsRuntime = XPathNodeType.Root; break;

                default:
                    // Union of several types (when testing for Text, we need to test for Whitespace as well)

                    // if (((1 << navigator.NodeType) & nodesDisallow) op 0) goto LabelBranch;
                    _helper.Emit(OpCodes.Ldc_I4_1);
                    kindsRuntime = XPathNodeType.All;
                    break;
            }

            // Push navigator.NodeType onto the stack
            NestedVisitEnsureStack(ndIsType.Source);
            _helper.Call(XmlILMethods.NavType);

            if (kindsRuntime == XPathNodeType.All)
            {
                // if (((1 << navigator.NodeType) & kindsUnion) op 0) goto LabelBranch;
                _helper.Emit(OpCodes.Shl);

                kindsUnion = 0;
                if ((kinds & XmlNodeKindFlags.Document) != 0) kindsUnion |= (1 << (int)XPathNodeType.Root);
                if ((kinds & XmlNodeKindFlags.Element) != 0) kindsUnion |= (1 << (int)XPathNodeType.Element);
                if ((kinds & XmlNodeKindFlags.Attribute) != 0) kindsUnion |= (1 << (int)XPathNodeType.Attribute);
                if ((kinds & XmlNodeKindFlags.Text) != 0)
                    kindsUnion |= (1 << (int)(int)XPathNodeType.Text) |
                                (1 << (int)(int)XPathNodeType.SignificantWhitespace) |
                                (1 << (int)(int)XPathNodeType.Whitespace);
                if ((kinds & XmlNodeKindFlags.Comment) != 0) kindsUnion |= (1 << (int)XPathNodeType.Comment);
                if ((kinds & XmlNodeKindFlags.PI) != 0) kindsUnion |= (1 << (int)XPathNodeType.ProcessingInstruction);
                if ((kinds & XmlNodeKindFlags.Namespace) != 0) kindsUnion |= (1 << (int)XPathNodeType.Namespace);

                _helper.LoadInteger(kindsUnion);
                _helper.Emit(OpCodes.And);
                ZeroCompare(allowKinds ? QilNodeType.Ne : QilNodeType.Eq, false);
            }
            else
            {
                // if (navigator.NodeType op runtimeItem) goto LabelBranch;
                _helper.LoadInteger((int)kindsRuntime);
                ClrCompare(allowKinds ? QilNodeType.Eq : QilNodeType.Ne, XmlTypeCode.Int);
            }

            return true;
        }

        /// <summary>
        /// Generate code for QilNodeType.IsEmpty.
        /// </summary>
        /// <remarks>
        /// BranchingContext.OnFalse context: is-empty(expr)
        /// ==> foreach (item in expr)
        ///         goto LabelBranch;
        ///
        /// BranchingContext.OnTrue context: is-empty(expr)
        /// ==> foreach (item in expr)
        ///         break;
        ///     ...
        ///     LabelOnEnd: (called if foreach is empty)
        ///     goto LabelBranch;
        ///
        /// BranchingContext.None context: is-empty(expr)
        /// ==> foreach (item in expr)
        ///         break;
        ///     push true();
        ///     ...
        ///     LabelOnEnd: (called if foreach is empty)
        ///     push false();
        /// </remarks>
        protected override QilNode VisitIsEmpty(QilUnary ndIsEmpty)
        {
            Label lblTrue;

            // If the child expression returns a cached result,
            if (CachesResult(ndIsEmpty.Child))
            {
                // Then get the count directly from the cache
                NestedVisitEnsureStack(ndIsEmpty.Child);
                _helper.CallCacheCount(_iterNested!.Storage.ItemStorageType);

                switch (_iterCurr.CurrentBranchingContext)
                {
                    case BranchingContext.OnFalse:
                        // Take false path if count != 0
                        _helper.TestAndBranch(0, _iterCurr.LabelBranch, OpCodes.Bne_Un);
                        break;

                    case BranchingContext.OnTrue:
                        // Take true path if count == 0
                        _helper.TestAndBranch(0, _iterCurr.LabelBranch, OpCodes.Beq);
                        break;

                    default:
                        Debug.Assert(_iterCurr.CurrentBranchingContext == BranchingContext.None);

                        // if (count == 0) goto LabelTrue;
                        lblTrue = _helper.DefineLabel();
                        _helper.Emit(OpCodes.Brfalse_S, lblTrue);

                        // Convert branch targets into push of true/false
                        _helper.ConvBranchToBool(lblTrue, true);
                        break;
                }
            }
            else
            {
                Label lblOnEnd = _helper.DefineLabel();
                IteratorDescriptor iterParent = _iterCurr;

                // Forward any LabelOnEnd jumps to LabelBranch if BranchingContext.OnTrue
                if (iterParent.CurrentBranchingContext == BranchingContext.OnTrue)
                    StartNestedIterator(ndIsEmpty.Child, _iterCurr.LabelBranch);
                else
                    StartNestedIterator(ndIsEmpty.Child, lblOnEnd);

                Visit(ndIsEmpty.Child);

                // Pop value of IsEmpty expression from the stack if necessary
                _iterCurr.EnsureNoCache();
                _iterCurr.DiscardStack();

                switch (iterParent.CurrentBranchingContext)
                {
                    case BranchingContext.OnFalse:
                        // Reverse polarity of iterator
                        _helper.EmitUnconditionalBranch(OpCodes.Br, iterParent.LabelBranch);
                        _helper.MarkLabel(lblOnEnd);
                        break;

                    case BranchingContext.OnTrue:
                        // Nothing to do
                        break;

                    case BranchingContext.None:
                        // Convert branch targets into push of true/false
                        _helper.ConvBranchToBool(lblOnEnd, true);
                        break;
                }

                // End nested iterator
                EndNestedIterator(ndIsEmpty.Child);
            }

            if (_iterCurr.IsBranching)
                _iterCurr.Storage = StorageDescriptor.None();
            else
                _iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), false);

            return ndIsEmpty;
        }

        /// <summary>
        /// Generate code for QilNodeType.XPathNodeValue.
        /// </summary>
        protected override QilNode VisitXPathNodeValue(QilUnary ndVal)
        {
            Label lblOnEnd, lblDone;
            Debug.Assert(ndVal.Child.XmlType!.IsNode, "XPathNodeValue node may only be applied to a sequence of Nodes.");

            // If the expression is a singleton,
            if (ndVal.Child.XmlType.IsSingleton)
            {
                // Then generate code to push expresion result onto the stack
                NestedVisitEnsureStack(ndVal.Child, typeof(XPathNavigator), false);

                // navigator.Value;
                _helper.Call(XmlILMethods.Value);
            }
            else
            {
                lblOnEnd = _helper.DefineLabel();

                // Construct nested iterator and iterate over results
                StartNestedIterator(ndVal.Child, lblOnEnd);
                Visit(ndVal.Child);
                _iterCurr.EnsureStackNoCache();

                // navigator.Value;
                _helper.Call(XmlILMethods.Value);

                // Handle empty sequence by pushing empty string onto the stack
                lblDone = _helper.DefineLabel();
                _helper.EmitUnconditionalBranch(OpCodes.Br, lblDone);
                _helper.MarkLabel(lblOnEnd);
                _helper.Emit(OpCodes.Ldstr, "");
                _helper.MarkLabel(lblDone);

                // End nested iterator
                EndNestedIterator(ndVal.Child);
            }

            _iterCurr.Storage = StorageDescriptor.Stack(typeof(string), false);

            return ndVal;
        }

        /// <summary>
        /// Find physical query plan for QilNodeType.XPathFollowing.
        /// </summary>
        protected override QilNode VisitXPathFollowing(QilUnary ndFoll)
        {
            CreateFilteredIterator(ndFoll.Child, "$$$iterFoll", typeof(XPathFollowingIterator), XmlILMethods.XPFollCreate, XmlILMethods.XPFollNext, XmlILMethods.XPFollCurrent,
                                   XmlNodeKindFlags.Any, null, TriState.Unknown, null);
            return ndFoll;
        }

        /// <summary>
        /// Find physical query plan for QilNodeType.XPathPreceding.
        /// </summary>
        protected override QilNode VisitXPathPreceding(QilUnary ndPrec)
        {
            CreateFilteredIterator(ndPrec.Child, "$$$iterPrec", typeof(XPathPrecedingIterator), XmlILMethods.XPPrecCreate, XmlILMethods.XPPrecNext, XmlILMethods.XPPrecCurrent,
                                   XmlNodeKindFlags.Any, null, TriState.Unknown, null);
            return ndPrec;
        }

        /// <summary>
        /// Find physical query plan for QilNodeType.XPathNamespace.
        /// </summary>
        protected override QilNode VisitXPathNamespace(QilUnary ndNmsp)
        {
            CreateSimpleIterator(ndNmsp.Child, "$$$iterNmsp", typeof(NamespaceIterator), XmlILMethods.NmspCreate, XmlILMethods.NmspNext, XmlILMethods.NmspCurrent);
            return ndNmsp;
        }

        /// <summary>
        /// Generate code for QilNodeType.XsltGenerateId.
        /// </summary>
        protected override QilNode VisitXsltGenerateId(QilUnary ndGenId)
        {
            Label lblOnEnd, lblDone;

            _helper.LoadQueryRuntime();

            // If the expression is a singleton,
            if (ndGenId.Child.XmlType!.IsSingleton)
            {
                // Then generate code to push expresion result onto the stack
                NestedVisitEnsureStack(ndGenId.Child, typeof(XPathNavigator), false);

                // runtime.GenerateId(value);
                _helper.Call(XmlILMethods.GenId);
            }
            else
            {
                lblOnEnd = _helper.DefineLabel();

                // Construct nested iterator and iterate over results
                StartNestedIterator(ndGenId.Child, lblOnEnd);
                Visit(ndGenId.Child);
                _iterCurr.EnsureStackNoCache();
                _iterCurr.EnsureItemStorageType(ndGenId.Child.XmlType, typeof(XPathNavigator));

                // runtime.GenerateId(value);
                _helper.Call(XmlILMethods.GenId);

                // Handle empty sequence by pushing empty string onto the stack
                lblDone = _helper.DefineLabel();
                _helper.EmitUnconditionalBranch(OpCodes.Br, lblDone);
                _helper.MarkLabel(lblOnEnd);
                _helper.Emit(OpCodes.Pop);
                _helper.Emit(OpCodes.Ldstr, "");
                _helper.MarkLabel(lblDone);

                // End nested iterator
                EndNestedIterator(ndGenId.Child);
            }

            _iterCurr.Storage = StorageDescriptor.Stack(typeof(string), false);

            return ndGenId;
        }

        /// <summary>
        /// Generate code for QilNodeType.XsltInvokeLateBound.
        /// </summary>
        protected override QilNode VisitXsltInvokeLateBound(QilInvokeLateBound ndInvoke)
        {
            LocalBuilder locArgs = _helper.DeclareLocal("$$$args", typeof(IList<XPathItem>[]));
            QilName ndName = (QilName)ndInvoke.Name;
            Debug.Assert(XmlILConstructInfo.Read(ndInvoke).ConstructMethod != XmlILConstructMethod.Writer);

            // runtime.ExternalContext.InvokeXsltLateBoundFunction(name, ns, args);
            _helper.LoadQueryContext();
            _helper.Emit(OpCodes.Ldstr, ndName.LocalName);
            _helper.Emit(OpCodes.Ldstr, ndName.NamespaceUri);

            // args = new IList<XPathItem>[argCount];
            _helper.LoadInteger(ndInvoke.Arguments.Count);
            _helper.Emit(OpCodes.Newarr, typeof(IList<XPathItem>));
            _helper.Emit(OpCodes.Stloc, locArgs);

            for (int iArg = 0; iArg < ndInvoke.Arguments.Count; iArg++)
            {
                QilNode ndArg = ndInvoke.Arguments[iArg];

                // args[0] = arg0;
                // ...
                // args[N] = argN;
                _helper.Emit(OpCodes.Ldloc, locArgs);
                _helper.LoadInteger(iArg);
                _helper.Emit(OpCodes.Ldelema, typeof(IList<XPathItem>));

                NestedVisitEnsureCache(ndArg, typeof(XPathItem));
                _iterCurr.EnsureStack();

                _helper.Emit(OpCodes.Stobj, typeof(IList<XPathItem>));
            }

            _helper.Emit(OpCodes.Ldloc, locArgs);

            _helper.Call(XmlILMethods.InvokeXsltLate);

            // Returned item sequence is on the stack
            _iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathItem), true);

            return ndInvoke;
        }

        /// <summary>
        /// Generate code for QilNodeType.XsltInvokeEarlyBound.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:RequiresUnreferencedCode",
            Justification = "Supressing warning about not having the RequiresUnreferencedCode attribute since we added " +
            "the attribute to this subclass' constructor. This allows us to not have to annotate the whole QilNode hirerarchy.")]
        protected override QilNode VisitXsltInvokeEarlyBound(QilInvokeEarlyBound ndInvoke)
        {
            QilName ndName = ndInvoke.Name;
            XmlExtensionFunction extFunc;
            Type clrTypeRetSrc, clrTypeRetDst;

            // Retrieve metadata from the extension function
            extFunc = new XmlExtensionFunction(ndName.LocalName, ndName.NamespaceUri, ndInvoke.ClrMethod);
            clrTypeRetSrc = extFunc.ClrReturnType;
            clrTypeRetDst = GetStorageType(ndInvoke);

            // Prepare to call runtime.ChangeTypeXsltResult
            if (clrTypeRetSrc != clrTypeRetDst && !ndInvoke.XmlType!.IsEmpty)
            {
                _helper.LoadQueryRuntime();
                _helper.LoadInteger(_helper.StaticData.DeclareXmlType(ndInvoke.XmlType));
            }

            // If this is not a static method, then get the instance object
            if (!extFunc.Method.IsStatic)
            {
                // Special-case the XsltLibrary object
                if (ndName.NamespaceUri.Length == 0)
                    _helper.LoadXsltLibrary();
                else
                    _helper.CallGetEarlyBoundObject(_helper.StaticData.DeclareEarlyBound(ndName.NamespaceUri, extFunc.Method.DeclaringType!), extFunc.Method.DeclaringType!);
            }

            // Generate code to push each Invoke argument onto the stack
            for (int iArg = 0; iArg < ndInvoke.Arguments.Count; iArg++)
            {
                QilNode ndActualArg;
                XmlQueryType xmlTypeFormalArg;
                Type clrTypeActualArg, clrTypeFormalArg;

                ndActualArg = ndInvoke.Arguments[iArg];

                // Infer Xml type and Clr type of formal argument
                xmlTypeFormalArg = extFunc.GetXmlArgumentType(iArg);
                clrTypeFormalArg = extFunc.GetClrArgumentType(iArg);

                Debug.Assert(ndActualArg.XmlType!.IsSubtypeOf(xmlTypeFormalArg), "Xml type of actual arg must be a subtype of the Xml type of the formal arg");

                // Use different conversion rules for internal Xslt libraries.  If the actual argument is
                // stored using Clr type T, then library must use type T, XPathItem, IList<T>, or IList<XPathItem>.
                // If the actual argument is stored using Clr type IList<T>, then library must use type
                // IList<T> or IList<XPathItem>.  This is to ensure that there will not be unnecessary
                // conversions that take place when calling into an internal library.
                if (ndName.NamespaceUri.Length == 0)
                {
                    Type itemType = GetItemStorageType(ndActualArg);

                    if (clrTypeFormalArg == XmlILMethods.StorageMethods[itemType].IListType)
                    {
                        // Formal type is IList<T>
                        NestedVisitEnsureStack(ndActualArg, itemType, true);
                    }
                    else if (clrTypeFormalArg == XmlILMethods.StorageMethods[typeof(XPathItem)].IListType)
                    {
                        // Formal type is IList<XPathItem>
                        NestedVisitEnsureStack(ndActualArg, typeof(XPathItem), true);
                    }
                    else if ((ndActualArg.XmlType.IsSingleton && clrTypeFormalArg == itemType) || ndActualArg.XmlType.TypeCode == XmlTypeCode.None)
                    {
                        // Formal type is T
                        NestedVisitEnsureStack(ndActualArg, clrTypeFormalArg, false);
                    }
                    else if (ndActualArg.XmlType.IsSingleton && clrTypeFormalArg == typeof(XPathItem))
                    {
                        // Formal type is XPathItem
                        NestedVisitEnsureStack(ndActualArg, typeof(XPathItem), false);
                    }
                    else
                        Debug.Fail("Internal Xslt library may not use parameters of type " + clrTypeFormalArg);
                }
                else
                {
                    // There is an implicit upcast to the Xml type of the formal argument.  This can change the Clr storage type.
                    clrTypeActualArg = GetStorageType(xmlTypeFormalArg);

                    // If the formal Clr type is typeof(object) or if it is not a supertype of the actual Clr type, then call ChangeTypeXsltArgument
                    if (xmlTypeFormalArg.TypeCode == XmlTypeCode.Item || !clrTypeFormalArg.IsAssignableFrom(clrTypeActualArg))
                    {
                        // (clrTypeFormalArg) runtime.ChangeTypeXsltArgument(xmlTypeFormalArg, (object) value, clrTypeFormalArg);
                        _helper.LoadQueryRuntime();
                        _helper.LoadInteger(_helper.StaticData.DeclareXmlType(xmlTypeFormalArg));
                        NestedVisitEnsureStack(ndActualArg, GetItemStorageType(xmlTypeFormalArg), !xmlTypeFormalArg.IsSingleton);
                        _helper.TreatAs(clrTypeActualArg, typeof(object));
                        _helper.LoadType(clrTypeFormalArg);
                        _helper.Call(XmlILMethods.ChangeTypeXsltArg);
                        _helper.TreatAs(typeof(object), clrTypeFormalArg);
                    }
                    else
                    {
                        NestedVisitEnsureStack(ndActualArg, GetItemStorageType(xmlTypeFormalArg), !xmlTypeFormalArg.IsSingleton);
                    }
                }
            }

            // Invoke the target method
            _helper.Call(extFunc.Method);

            // Return value is on the stack; convert it to canonical ILGen storage type
            if (ndInvoke.XmlType!.IsEmpty)
            {
                _helper.Emit(OpCodes.Ldsfld, XmlILMethods.StorageMethods[typeof(XPathItem)].SeqEmpty);
            }
            else if (clrTypeRetSrc != clrTypeRetDst)
            {
                // (T) runtime.ChangeTypeXsltResult(idxType, (object) value);
                _helper.TreatAs(clrTypeRetSrc, typeof(object));
                _helper.Call(XmlILMethods.ChangeTypeXsltResult);
                _helper.TreatAs(typeof(object), clrTypeRetDst);
            }
            else if (ndName.NamespaceUri.Length != 0 && !clrTypeRetSrc.IsValueType)
            {
                // Check for null if a user-defined extension function returns a reference type
                Label lblSkip = _helper.DefineLabel();
                _helper.Emit(OpCodes.Dup);
                _helper.Emit(OpCodes.Brtrue, lblSkip);
                _helper.LoadQueryRuntime();
                _helper.Emit(OpCodes.Ldstr, SR.Xslt_ItemNull);
                _helper.Call(XmlILMethods.ThrowException);
                _helper.MarkLabel(lblSkip);
            }

            _iterCurr.Storage = StorageDescriptor.Stack(GetItemStorageType(ndInvoke), !ndInvoke.XmlType.IsSingleton);

            return ndInvoke;
        }

        /// <summary>
        /// Generate code for QilNodeType.XsltCopy.
        /// </summary>
        protected override QilNode VisitXsltCopy(QilBinary ndCopy)
        {
            Label lblSkipContent = _helper.DefineLabel();
            Debug.Assert(XmlILConstructInfo.Read(ndCopy).PushToWriterFirst);

            // if (!xwrtChk.StartCopyChk(navCopy)) goto LabelSkipContent;
            _helper.LoadQueryOutput();

            NestedVisitEnsureStack(ndCopy.Left);
            Debug.Assert(ndCopy.Left.XmlType!.IsNode);

            _helper.Call(XmlILMethods.StartCopy);
            _helper.Emit(OpCodes.Brfalse, lblSkipContent);

            // Recursively construct content
            NestedVisit(ndCopy.Right);

            // xwrtChk.EndCopyChk(navCopy);
            _helper.LoadQueryOutput();

            NestedVisitEnsureStack(ndCopy.Left);
            Debug.Assert(ndCopy.Left.XmlType.IsNode);

            _helper.Call(XmlILMethods.EndCopy);

            // LabelSkipContent:
            _helper.MarkLabel(lblSkipContent);

            _iterCurr.Storage = StorageDescriptor.None();
            return ndCopy;
        }

        /// <summary>
        /// Generate code for QilNodeType.XsltCopyOf.
        /// </summary>
        protected override QilNode VisitXsltCopyOf(QilUnary ndCopyOf)
        {
            Debug.Assert(XmlILConstructInfo.Read(ndCopyOf).PushToWriterFirst, "XsltCopyOf should always be pushed to writer.");

            _helper.LoadQueryOutput();

            // XmlQueryOutput.XsltCopyOf(navigator);
            NestedVisitEnsureStack(ndCopyOf.Child);
            _helper.Call(XmlILMethods.CopyOf);

            _iterCurr.Storage = StorageDescriptor.None();
            return ndCopyOf;
        }

        /// <summary>
        /// Generate code for QilNodeType.XsltConvert.
        /// </summary>
        protected override QilNode VisitXsltConvert(QilTargetType ndConv)
        {
            XmlQueryType typSrc, typDst;
            MethodInfo? meth;

            typSrc = ndConv.Source.XmlType!;
            typDst = ndConv.TargetType;

            if (GetXsltConvertMethod(typSrc, typDst, out meth))
            {
                NestedVisitEnsureStack(ndConv.Source);
            }
            else
            {
                // If a conversion could not be found, then convert the source expression to item or item* and try again
                NestedVisitEnsureStack(ndConv.Source, typeof(XPathItem), !typSrc.IsSingleton);
                if (!GetXsltConvertMethod(typSrc.IsSingleton ? TypeFactory.Item : TypeFactory.ItemS, typDst, out meth))
                    Debug.Fail("Conversion from " + ndConv.Source.XmlType + " to " + ndConv.TargetType + " is not supported.");
            }

            // XsltConvert.XXXToYYY(value);
            if (meth != null)
                _helper.Call(meth);

            _iterCurr.Storage = StorageDescriptor.Stack(GetItemStorageType(typDst), !typDst.IsSingleton);
            return ndConv;
        }

        /// <summary>
        /// Get the XsltConvert method that converts from "typSrc" to "typDst".  Return false if no
        /// such method exists.  This conversion matrix should match the one in XsltConvert.ExternalValueToExternalValue.
        /// </summary>
        private bool GetXsltConvertMethod(XmlQueryType typSrc, XmlQueryType typDst, out MethodInfo? meth)
        {
            meth = null;

            // Note, Ref.Equals is OK to use here, since we will always fall back to Item or Item* in the
            // case where the source or destination types do not match the static types exposed on the
            // XmlQueryTypeFactory.  This is bad for perf if it accidentally occurs, but the results
            // should still be correct.

            // => xs:boolean
            if ((object)typDst == (object)TypeFactory.BooleanX)
            {
                if ((object)typSrc == (object)TypeFactory.Item) meth = XmlILMethods.ItemToBool;
                else if ((object)typSrc == (object)TypeFactory.ItemS) meth = XmlILMethods.ItemsToBool;
            }
            // => xs:dateTime
            else if ((object)typDst == (object)TypeFactory.DateTimeX)
            {
                if ((object)typSrc == (object)TypeFactory.StringX) meth = XmlILMethods.StrToDT;
            }
            // => xs:decimal
            else if ((object)typDst == (object)TypeFactory.DecimalX)
            {
                if ((object)typSrc == (object)TypeFactory.DoubleX) meth = XmlILMethods.DblToDec;
            }
            // => xs:double
            else if ((object)typDst == (object)TypeFactory.DoubleX)
            {
                if ((object)typSrc == (object)TypeFactory.DecimalX) meth = XmlILMethods.DecToDbl;
                else if ((object)typSrc == (object)TypeFactory.IntX) meth = XmlILMethods.IntToDbl;
                else if ((object)typSrc == (object)TypeFactory.Item) meth = XmlILMethods.ItemToDbl;
                else if ((object)typSrc == (object)TypeFactory.ItemS) meth = XmlILMethods.ItemsToDbl;
                else if ((object)typSrc == (object)TypeFactory.LongX) meth = XmlILMethods.LngToDbl;
                else if ((object)typSrc == (object)TypeFactory.StringX) meth = XmlILMethods.StrToDbl;
            }
            // => xs:int
            else if ((object)typDst == (object)TypeFactory.IntX)
            {
                if ((object)typSrc == (object)TypeFactory.DoubleX) meth = XmlILMethods.DblToInt;
            }
            // => xs:long
            else if ((object)typDst == (object)TypeFactory.LongX)
            {
                if ((object)typSrc == (object)TypeFactory.DoubleX) meth = XmlILMethods.DblToLng;
            }
            // => node
            else if ((object)typDst == (object)TypeFactory.NodeNotRtf)
            {
                if ((object)typSrc == (object)TypeFactory.Item) meth = XmlILMethods.ItemToNode;
                else if ((object)typSrc == (object)TypeFactory.ItemS) meth = XmlILMethods.ItemsToNode;
            }
            // => node*
            else if ((object)typDst == (object)TypeFactory.NodeSDod ||
                     (object)typDst == (object)TypeFactory.NodeNotRtfS)
            {
                if ((object)typSrc == (object)TypeFactory.Item) meth = XmlILMethods.ItemToNodes;
                else if ((object)typSrc == (object)TypeFactory.ItemS) meth = XmlILMethods.ItemsToNodes;
            }
            // => xs:string
            else if ((object)typDst == (object)TypeFactory.StringX)
            {
                if ((object)typSrc == (object)TypeFactory.DateTimeX) meth = XmlILMethods.DTToStr;
                else if ((object)typSrc == (object)TypeFactory.DoubleX) meth = XmlILMethods.DblToStr;
                else if ((object)typSrc == (object)TypeFactory.Item) meth = XmlILMethods.ItemToStr;
                else if ((object)typSrc == (object)TypeFactory.ItemS) meth = XmlILMethods.ItemsToStr;
            }

            return meth != null;
        }


        //-----------------------------------------------
        // Helper methods
        //-----------------------------------------------

        /// <summary>
        /// Ensure that the "locNav" navigator is positioned to the context node "ndCtxt".
        /// </summary>
        private void SyncToNavigator(LocalBuilder locNav, QilNode ndCtxt)
        {
            _helper.Emit(OpCodes.Ldloc, locNav);
            NestedVisitEnsureStack(ndCtxt);
            _helper.CallSyncToNavigator();
            _helper.Emit(OpCodes.Stloc, locNav);
        }

        /// <summary>
        /// Generate boiler-plate code to create a simple Xml iterator.
        /// </summary>
        /// <remarks>
        ///     Iterator iter;
        ///     iter.Create(navCtxt);
        /// LabelNext:
        ///     if (!iter.MoveNext())
        ///         goto LabelNextCtxt;
        /// </remarks>
        private void CreateSimpleIterator(QilNode ndCtxt, string iterName, Type iterType, MethodInfo methCreate, MethodInfo methNext, MethodInfo methCurrent)
        {
            // Iterator iter;
            LocalBuilder locIter = _helper.DeclareLocal(iterName, iterType);

            // iter.Create(navCtxt);
            _helper.Emit(OpCodes.Ldloca, locIter);
            NestedVisitEnsureStack(ndCtxt);
            _helper.Call(methCreate);

            GenerateSimpleIterator(typeof(XPathNavigator), locIter, methNext, methCurrent);
        }

        /// <summary>
        /// Generate boiler-plate code to create an Xml iterator that uses an XmlNavigatorFilter to filter items.
        /// </summary>
        /// <remarks>
        ///     Iterator iter;
        ///     iter.Create(navCtxt, filter [, orSelf] [, navEnd]);
        /// LabelNext:
        ///     if (!iter.MoveNext())
        ///         goto LabelNextCtxt;
        /// </remarks>
        private void CreateFilteredIterator(QilNode ndCtxt, string iterName, Type iterType, MethodInfo methCreate, MethodInfo methNext, MethodInfo methCurrent,
                                                XmlNodeKindFlags kinds, QilName? ndName, TriState orSelf, QilNode? ndEnd)
        {
            // Iterator iter;
            LocalBuilder locIter = _helper.DeclareLocal(iterName, iterType);

            // iter.Create(navCtxt, filter [, orSelf], [, navEnd]);
            _helper.Emit(OpCodes.Ldloca, locIter);
            NestedVisitEnsureStack(ndCtxt);
            LoadSelectFilter(kinds, ndName);
            if (orSelf != TriState.Unknown)
                _helper.LoadBoolean(orSelf == TriState.True);
            if (ndEnd != null)
                NestedVisitEnsureStack(ndEnd);
            _helper.Call(methCreate);

            GenerateSimpleIterator(typeof(XPathNavigator), locIter, methNext, methCurrent);
        }

        /// <summary>
        /// Generate boiler-plate code to create an Xml iterator that controls a nested iterator.
        /// </summary>
        /// <remarks>
        ///     Iterator iter;
        ///     iter.Create(filter [, orSelf]);
        ///         ...nested iterator...
        ///     navInput = nestedNested;
        ///     goto LabelCall;
        /// LabelNext:
        ///     navInput = null;
        /// LabelCall:
        ///     switch (iter.MoveNext(navInput)) {
        ///         case IteratorState.NoMoreNodes: goto LabelNextCtxt;
        ///         case IteratorState.NextInputNode: goto LabelNextNested;
        ///     }
        /// </remarks>
        private void CreateContainerIterator(QilUnary ndDod, string iterName, Type iterType, MethodInfo methCreate, MethodInfo methNext, MethodInfo methCurrent,
                                                   XmlNodeKindFlags kinds, QilName? ndName, TriState orSelf)
        {
            // Iterator iter;
            LocalBuilder locIter = _helper.DeclareLocal(iterName, iterType);
            Label lblOnEndNested;
            QilLoop ndLoop = (QilLoop)ndDod.Child;
            Debug.Assert(ndDod.NodeType == QilNodeType.DocOrderDistinct && ndLoop != null);

            // iter.Create(filter [, orSelf]);
            _helper.Emit(OpCodes.Ldloca, locIter);
            LoadSelectFilter(kinds, ndName);
            if (orSelf != TriState.Unknown)
                _helper.LoadBoolean(orSelf == TriState.True);
            _helper.Call(methCreate);

            // Generate nested iterator (branch to lblOnEndNested when iteration is complete)
            lblOnEndNested = _helper.DefineLabel();
            StartNestedIterator(ndLoop, lblOnEndNested);
            StartBinding(ndLoop.Variable);
            EndBinding(ndLoop.Variable);
            EndNestedIterator(ndLoop.Variable);
            _iterCurr.Storage = _iterNested!.Storage;

            GenerateContainerIterator(ndDod, locIter, lblOnEndNested, methNext, methCurrent, typeof(XPathNavigator));
        }

        /// <summary>
        /// Generate boiler-plate code that calls MoveNext on a simple Xml iterator.  Iterator should have already been
        /// created by calling code.
        /// </summary>
        /// <remarks>
        ///     ...
        /// LabelNext:
        ///     if (!iter.MoveNext())
        ///         goto LabelNextCtxt;
        /// </remarks>
        private void GenerateSimpleIterator(Type itemStorageType, LocalBuilder locIter, MethodInfo methNext, MethodInfo methCurrent)
        {
            Label lblNext;

            // LabelNext:
            lblNext = _helper.DefineLabel();
            _helper.MarkLabel(lblNext);

            // if (!iter.MoveNext()) goto LabelNextCtxt;
            _helper.Emit(OpCodes.Ldloca, locIter);
            _helper.Call(methNext);
            _helper.Emit(OpCodes.Brfalse, _iterCurr.GetLabelNext());

            _iterCurr.SetIterator(lblNext, StorageDescriptor.Current(locIter, methCurrent, itemStorageType));
        }

        /// <summary>
        /// Generate boiler-plate code that calls MoveNext on an Xml iterator that controls a nested iterator.  Iterator should
        /// have already been created by calling code.
        /// </summary>
        /// <remarks>
        ///     ...
        ///     goto LabelCall;
        /// LabelNext:
        ///     navCtxt = null;
        /// LabelCall:
        ///     switch (iter.MoveNext(navCtxt)) {
        ///         case IteratorState.NoMoreNodes: goto LabelNextCtxt;
        ///         case IteratorState.NextInputNode: goto LabelNextNested;
        ///     }
        /// </remarks>
        private void GenerateContainerIterator(QilNode nd, LocalBuilder locIter, Label lblOnEndNested,
                                                       MethodInfo methNext, MethodInfo methCurrent, Type itemStorageType)
        {
            Label lblCall;

            // Define labels that will be used
            lblCall = _helper.DefineLabel();

            // iter.MoveNext(input);
            // goto LabelCall;
            _iterCurr.EnsureNoStackNoCache(nd.XmlType!.IsNode ? "$$$navInput" : "$$$itemInput");
            _helper.Emit(OpCodes.Ldloca, locIter);
            _iterCurr.PushValue();
            _helper.EmitUnconditionalBranch(OpCodes.Br, lblCall);

            // LabelNext:
            // iterSet.MoveNext(null);
            _helper.MarkLabel(lblOnEndNested);
            _helper.Emit(OpCodes.Ldloca, locIter);
            _helper.Emit(OpCodes.Ldnull);

            // LabelCall:
            // result = iter.MoveNext(input);
            _helper.MarkLabel(lblCall);
            _helper.Call(methNext);

            // If this iterator always returns a single node, then NoMoreNodes will never be returned
            if (nd.XmlType.IsSingleton)
            {
                // if (result == IteratorResult.NeedInputNode) goto LabelNextInput;
                _helper.LoadInteger((int)IteratorResult.NeedInputNode);
                _helper.Emit(OpCodes.Beq, _iterNested!.GetLabelNext());

                _iterCurr.Storage = StorageDescriptor.Current(locIter, methCurrent, itemStorageType);
            }
            else
            {
                // switch (iter.MoveNext(input)) {
                //      case IteratorResult.NoMoreNodes: goto LabelNextCtxt;
                //      case IteratorResult.NeedInputNode: goto LabelNextInput;
                // }
                _helper.Emit(OpCodes.Switch, new Label[] { _iterCurr.GetLabelNext(), _iterNested!.GetLabelNext() });

                _iterCurr.SetIterator(lblOnEndNested, StorageDescriptor.Current(locIter, methCurrent, itemStorageType));
            }
        }

        /// <summary>
        /// Load XmlQueryOutput, load a name (computed or literal) and load an index to an Xml schema type.
        /// Return an enumeration that specifies what kind of name was loaded.
        /// </summary>
        private GenerateNameType LoadNameAndType(XPathNodeType nodeType, QilNode ndName, bool isStart, bool callChk)
        {
            QilName ndLiteralName;
            string prefix, localName, ns;
            GenerateNameType nameType;
            Debug.Assert(ndName.XmlType!.TypeCode == XmlTypeCode.QName, "Element or attribute name must have QName type.");

            _helper.LoadQueryOutput();

            // 0. Default is to pop names off stack
            nameType = GenerateNameType.StackName;

            // 1. Literal names
            if (ndName.NodeType == QilNodeType.LiteralQName)
            {
                // If checks need to be made on End construction, then always pop names from stack
                if (isStart || !callChk)
                {
                    ndLiteralName = (ndName as QilName)!;
                    prefix = ndLiteralName.Prefix;
                    localName = ndLiteralName.LocalName;
                    ns = ndLiteralName.NamespaceUri;

                    // Check local name, namespace parts in debug code
                    Debug.Assert(ValidateNames.ValidateName(prefix, localName, ns, nodeType, ValidateNames.Flags.AllExceptPrefixMapping));

                    // If the namespace is empty,
                    if (ndLiteralName.NamespaceUri.Length == 0)
                    {
                        // Then always call method on XmlQueryOutput
                        _helper.Emit(OpCodes.Ldstr, ndLiteralName.LocalName);
                        return GenerateNameType.LiteralLocalName;
                    }

                    // If prefix is not valid for the node type,
                    if (!ValidateNames.ValidateName(prefix, localName, ns, nodeType, ValidateNames.Flags.CheckPrefixMapping))
                    {
                        // Then construct a new prefix at run-time
                        if (isStart)
                        {
                            _helper.Emit(OpCodes.Ldstr, localName);
                            _helper.Emit(OpCodes.Ldstr, ns);
                            _helper.Construct(XmlILConstructors.QName);

                            nameType = GenerateNameType.QName;
                        }
                    }
                    else
                    {
                        // Push string parts
                        _helper.Emit(OpCodes.Ldstr, prefix);
                        _helper.Emit(OpCodes.Ldstr, localName);
                        _helper.Emit(OpCodes.Ldstr, ns);

                        nameType = GenerateNameType.LiteralName;
                    }
                }
            }
            else
            {
                if (isStart)
                {
                    // 2. Copied names
                    if (ndName.NodeType == QilNodeType.NameOf)
                    {
                        // Preserve prefix of source node, so just push navigator onto stack
                        NestedVisitEnsureStack((ndName as QilUnary)!.Child);
                        nameType = GenerateNameType.CopiedName;
                    }
                    // 3. Parsed tag names (foo:bar)
                    else if (ndName.NodeType == QilNodeType.StrParseQName)
                    {
                        // Preserve prefix from parsed tag name
                        VisitStrParseQName((ndName as QilBinary)!, true);

                        // Type of name depends upon data-type of name argument
                        if ((ndName as QilBinary)!.Right.XmlType!.TypeCode == XmlTypeCode.String)
                            nameType = GenerateNameType.TagNameAndNamespace;
                        else
                            nameType = GenerateNameType.TagNameAndMappings;
                    }
                    // 4. Other computed qnames
                    else
                    {
                        // Push XmlQualifiedName onto the stack
                        NestedVisitEnsureStack(ndName);
                        nameType = GenerateNameType.QName;
                    }
                }
            }

            return nameType;
        }

        /// <summary>
        /// If the first argument is a constant value that evaluates to zero, then a more optimal instruction sequence
        /// can be generated that does not have to push the zero onto the stack.  Instead, a Brfalse or Brtrue instruction
        /// can be used.
        /// </summary>
        private bool TryZeroCompare(QilNodeType relOp, QilNode ndFirst, QilNode ndSecond)
        {
            Debug.Assert(relOp == QilNodeType.Eq || relOp == QilNodeType.Ne);

            switch (ndFirst.NodeType)
            {
                case QilNodeType.LiteralInt64:
                    if ((int)(QilLiteral)ndFirst != 0) return false;
                    break;

                case QilNodeType.LiteralInt32:
                    if ((int)(QilLiteral)ndFirst != 0) return false;
                    break;

                case QilNodeType.False:
                    break;

                case QilNodeType.True:
                    // Inverse of QilNodeType.False
                    relOp = (relOp == QilNodeType.Eq) ? QilNodeType.Ne : QilNodeType.Eq;
                    break;

                default:
                    return false;
            }

            // Generate code to push second argument on stack
            NestedVisitEnsureStack(ndSecond);

            // Generate comparison code -- op == 0 or op != 0
            ZeroCompare(relOp, ndSecond.XmlType!.TypeCode == XmlTypeCode.Boolean);

            return true;
        }

        /// <summary>
        /// If the comparison involves a qname, then perform comparison using atoms and return true.
        /// Otherwise, return false (caller will perform comparison).
        /// </summary>
        private bool TryNameCompare(QilNodeType relOp, QilNode ndFirst, QilNode ndSecond)
        {
            Debug.Assert(relOp == QilNodeType.Eq || relOp == QilNodeType.Ne);

            if (ndFirst.NodeType == QilNodeType.NameOf)
            {
                switch (ndSecond.NodeType)
                {
                    case QilNodeType.NameOf:
                    case QilNodeType.LiteralQName:
                        {
                            _helper.LoadQueryRuntime();

                            // Push left navigator onto the stack
                            NestedVisitEnsureStack((ndFirst as QilUnary)!.Child);

                            // Push the local name and namespace uri of the right argument onto the stack
                            if (ndSecond.NodeType == QilNodeType.LiteralQName)
                            {
                                QilName ndName = (ndSecond as QilName)!;
                                _helper.LoadInteger(_helper.StaticData.DeclareName(ndName.LocalName));
                                _helper.LoadInteger(_helper.StaticData.DeclareName(ndName.NamespaceUri));

                                // push runtime.IsQNameEqual(navigator, localName, namespaceUri)
                                _helper.Call(XmlILMethods.QNameEqualLit);
                            }
                            else
                            {
                                // Generate code to locate the navigator argument of NameOf operator
                                Debug.Assert(ndSecond.NodeType == QilNodeType.NameOf);
                                NestedVisitEnsureStack(ndSecond);

                                // push runtime.IsQNameEqual(nav1, nav2)
                                _helper.Call(XmlILMethods.QNameEqualNav);
                            }

                            // Branch based on boolean result or push boolean value
                            ZeroCompare((relOp == QilNodeType.Eq) ? QilNodeType.Ne : QilNodeType.Eq, true);
                            return true;
                        }
                }
            }

            // Caller must perform comparison
            return false;
        }

        /// <summary>
        /// For QilExpression types that map directly to CLR primitive types, the built-in CLR comparison operators can
        /// be used to perform the specified relational operation.
        /// </summary>
        private void ClrCompare(QilNodeType relOp, XmlTypeCode code)
        {
            OpCode opcode;
            Label lblTrue;

            switch (_iterCurr.CurrentBranchingContext)
            {
                case BranchingContext.OnFalse:
                    // Reverse the comparison operator
                    // Use Bxx_Un OpCodes to handle NaN case for double and single types
                    if (code == XmlTypeCode.Double || code == XmlTypeCode.Float)
                    {
                        switch (relOp)
                        {
                            case QilNodeType.Gt: opcode = OpCodes.Ble_Un; break;
                            case QilNodeType.Ge: opcode = OpCodes.Blt_Un; break;
                            case QilNodeType.Lt: opcode = OpCodes.Bge_Un; break;
                            case QilNodeType.Le: opcode = OpCodes.Bgt_Un; break;
                            case QilNodeType.Eq: opcode = OpCodes.Bne_Un; break;
                            case QilNodeType.Ne: opcode = OpCodes.Beq; break;
                            default: Debug.Fail($"Unexpected rel op {relOp}"); opcode = OpCodes.Nop; break;
                        }
                    }
                    else
                    {
                        switch (relOp)
                        {
                            case QilNodeType.Gt: opcode = OpCodes.Ble; break;
                            case QilNodeType.Ge: opcode = OpCodes.Blt; break;
                            case QilNodeType.Lt: opcode = OpCodes.Bge; break;
                            case QilNodeType.Le: opcode = OpCodes.Bgt; break;
                            case QilNodeType.Eq: opcode = OpCodes.Bne_Un; break;
                            case QilNodeType.Ne: opcode = OpCodes.Beq; break;
                            default: Debug.Fail($"Unexpected rel op {relOp}"); opcode = OpCodes.Nop; break;
                        }
                    }
                    _helper.Emit(opcode, _iterCurr.LabelBranch);
                    _iterCurr.Storage = StorageDescriptor.None();
                    break;

                case BranchingContext.OnTrue:
                    switch (relOp)
                    {
                        case QilNodeType.Gt: opcode = OpCodes.Bgt; break;
                        case QilNodeType.Ge: opcode = OpCodes.Bge; break;
                        case QilNodeType.Lt: opcode = OpCodes.Blt; break;
                        case QilNodeType.Le: opcode = OpCodes.Ble; break;
                        case QilNodeType.Eq: opcode = OpCodes.Beq; break;
                        case QilNodeType.Ne: opcode = OpCodes.Bne_Un; break;
                        default: Debug.Fail($"Unexpected rel op {relOp}"); opcode = OpCodes.Nop; break;
                    }
                    _helper.Emit(opcode, _iterCurr.LabelBranch);
                    _iterCurr.Storage = StorageDescriptor.None();
                    break;

                default:
                    Debug.Assert(_iterCurr.CurrentBranchingContext == BranchingContext.None);
                    switch (relOp)
                    {
                        case QilNodeType.Gt: _helper.Emit(OpCodes.Cgt); break;
                        case QilNodeType.Lt: _helper.Emit(OpCodes.Clt); break;
                        case QilNodeType.Eq: _helper.Emit(OpCodes.Ceq); break;
                        default:
                            switch (relOp)
                            {
                                case QilNodeType.Ge: opcode = OpCodes.Bge_S; break;
                                case QilNodeType.Le: opcode = OpCodes.Ble_S; break;
                                case QilNodeType.Ne: opcode = OpCodes.Bne_Un_S; break;
                                default: Debug.Fail($"Unexpected rel op {relOp}"); opcode = OpCodes.Nop; break;
                            }

                            // Push "true" if comparison succeeds, "false" otherwise
                            lblTrue = _helper.DefineLabel();
                            _helper.Emit(opcode, lblTrue);
                            _helper.ConvBranchToBool(lblTrue, true);
                            break;
                    }
                    _iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), false);
                    break;
            }
        }

        /// <summary>
        /// Generate code to compare the top stack value to 0 by using the Brfalse or Brtrue instructions,
        /// which avoid pushing zero onto the stack.  Both of these instructions test for null/zero/false.
        /// </summary>
        private void ZeroCompare(QilNodeType relOp, bool isBoolVal)
        {
            Label lblTrue;
            Debug.Assert(relOp == QilNodeType.Eq || relOp == QilNodeType.Ne);

            // Test to determine if top stack value is zero (if relOp is Eq) or is not zero (if relOp is Ne)
            switch (_iterCurr.CurrentBranchingContext)
            {
                case BranchingContext.OnTrue:
                    // If relOp is Eq, jump to true label if top value is zero (Brfalse)
                    // If relOp is Ne, jump to true label if top value is non-zero (Brtrue)
                    _helper.Emit((relOp == QilNodeType.Eq) ? OpCodes.Brfalse : OpCodes.Brtrue, _iterCurr.LabelBranch);
                    _iterCurr.Storage = StorageDescriptor.None();
                    break;

                case BranchingContext.OnFalse:
                    // If relOp is Eq, jump to false label if top value is non-zero (Brtrue)
                    // If relOp is Ne, jump to false label if top value is zero (Brfalse)
                    _helper.Emit((relOp == QilNodeType.Eq) ? OpCodes.Brtrue : OpCodes.Brfalse, _iterCurr.LabelBranch);
                    _iterCurr.Storage = StorageDescriptor.None();
                    break;

                default:
                    Debug.Assert(_iterCurr.CurrentBranchingContext == BranchingContext.None);

                    // Since (boolval != 0) = boolval, value on top of the stack is already correct
                    if (!isBoolVal || relOp == QilNodeType.Eq)
                    {
                        // If relOp is Eq, push "true" if top value is zero, "false" otherwise
                        // If relOp is Ne, push "true" if top value is non-zero, "false" otherwise
                        lblTrue = _helper.DefineLabel();
                        _helper.Emit((relOp == QilNodeType.Eq) ? OpCodes.Brfalse : OpCodes.Brtrue, lblTrue);
                        _helper.ConvBranchToBool(lblTrue, true);
                    }

                    _iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), false);
                    break;
            }
        }

        /// <summary>
        /// Construction within a loop is starting.  If transition from non-Any to Any state occurs, then ensure
        /// that runtime state will be set.
        /// </summary>
        private void StartWriterLoop(QilNode nd, out bool hasOnEnd, out Label lblOnEnd)
        {
            XmlILConstructInfo info = XmlILConstructInfo.Read(nd);

            // By default, do not create a new iteration label
            hasOnEnd = false;
            lblOnEnd = default;

            // If loop is not involved in Xml construction, or if loop returns exactly one value, then do nothing
            if (!info.PushToWriterLast || nd.XmlType!.IsSingleton)
                return;

            if (!_iterCurr.HasLabelNext)
            {
                // Iterate until all items are constructed
                hasOnEnd = true;
                lblOnEnd = _helper.DefineLabel();
                _iterCurr.SetIterator(lblOnEnd, StorageDescriptor.None());
            }
        }

        /// <summary>
        /// Construction within a loop is ending.  If transition from non-Any to Any state occurs, then ensure that
        /// runtime state will be set.
        /// </summary>
        private void EndWriterLoop(QilNode nd, bool hasOnEnd, Label lblOnEnd)
        {
            XmlILConstructInfo info = XmlILConstructInfo.Read(nd);

            // If loop is not involved in Xml construction, then do nothing
            if (!info.PushToWriterLast)
                return;

            // Since results of construction were pushed to writer, there are no values to return
            _iterCurr.Storage = StorageDescriptor.None();

            // If loop returns exactly one value, then do nothing further
            if (nd.XmlType!.IsSingleton)
                return;

            if (hasOnEnd)
            {
                // Loop over all items in the list, sending each to the output writer
                _iterCurr.LoopToEnd(lblOnEnd);
            }
        }

        /// <summary>
        /// Returns true if the specified node's owner element might have local namespaces added to it
        /// after attributes have already been added.
        /// </summary>
        private bool MightHaveNamespacesAfterAttributes(XmlILConstructInfo? info)
        {
            // Get parent element
            if (info != null)
                info = info.ParentElementInfo;

            // If a parent element has not been statically identified, then assume that the runtime
            // element will have namespaces added after attributes.
            if (info == null)
                return true;

            return info.MightHaveNamespacesAfterAttributes;
        }

        /// <summary>
        /// Returns true if the specified element should cache attributes.
        /// </summary>
        private bool ElementCachesAttributes(XmlILConstructInfo info)
        {
            // Attributes will be cached if namespaces might be constructed after the attributes
            return info.MightHaveDuplicateAttributes || info.MightHaveNamespacesAfterAttributes;
        }

        /// <summary>
        /// This method is called before calling any WriteEnd??? method.  It generates code to perform runtime
        /// construction checks separately.  This should only be called if the XmlQueryOutput::StartElementChk
        /// method will *not* be called.
        /// </summary>
        private void BeforeStartChecks(QilNode ndCtor)
        {
            switch (XmlILConstructInfo.Read(ndCtor).InitialStates)
            {
                case PossibleXmlStates.WithinSequence:
                    // If runtime state is guaranteed to be WithinSequence, then call XmlQueryOutput.StartTree
                    _helper.CallStartTree(QilConstructorToNodeType(ndCtor.NodeType));
                    break;

                case PossibleXmlStates.EnumAttrs:
                    switch (ndCtor.NodeType)
                    {
                        case QilNodeType.ElementCtor:
                        case QilNodeType.TextCtor:
                        case QilNodeType.RawTextCtor:
                        case QilNodeType.PICtor:
                        case QilNodeType.CommentCtor:
                            // If runtime state is guaranteed to be EnumAttrs, and content is being constructed, call
                            // XmlQueryOutput.StartElementContent
                            _helper.CallStartElementContent();
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// This method is called after calling any WriteEnd??? method.  It generates code to perform runtime
        /// construction checks separately.  This should only be called if the XmlQueryOutput::EndElementChk
        /// method will *not* be called.
        /// </summary>
        private void AfterEndChecks(QilNode ndCtor)
        {
            if (XmlILConstructInfo.Read(ndCtor).FinalStates == PossibleXmlStates.WithinSequence)
            {
                // If final runtime state is guaranteed to be WithinSequence, then call XmlQueryOutput.StartTree
                _helper.CallEndTree();
            }
        }

        /// <summary>
        /// Return true if a runtime check needs to be made in order to transition into the WithinContent state.
        /// </summary>
        private bool CheckWithinContent(XmlILConstructInfo info)
        {
            switch (info.InitialStates)
            {
                case PossibleXmlStates.WithinSequence:
                case PossibleXmlStates.EnumAttrs:
                case PossibleXmlStates.WithinContent:
                    // Transition to WithinContent can be ensured at compile-time
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Return true if a runtime check needs to be made in order to transition into the EnumAttrs state.
        /// </summary>
        private bool CheckEnumAttrs(XmlILConstructInfo info)
        {
            switch (info.InitialStates)
            {
                case PossibleXmlStates.WithinSequence:
                case PossibleXmlStates.EnumAttrs:
                    // Transition to EnumAttrs can be ensured at compile-time
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Map the XmlNodeKindFlags enumeration into the XPathNodeType enumeration.
        /// </summary>
        private XPathNodeType QilXmlToXPathNodeType(XmlNodeKindFlags xmlTypes)
        {
            switch (xmlTypes)
            {
                case XmlNodeKindFlags.Element: return XPathNodeType.Element;
                case XmlNodeKindFlags.Attribute: return XPathNodeType.Attribute;
                case XmlNodeKindFlags.Text: return XPathNodeType.Text;
                case XmlNodeKindFlags.Comment: return XPathNodeType.Comment;
            }
            Debug.Assert(xmlTypes == XmlNodeKindFlags.PI);
            return XPathNodeType.ProcessingInstruction;
        }

        /// <summary>
        /// Map a QilExpression constructor type into the XPathNodeType enumeration.
        /// </summary>
        private XPathNodeType QilConstructorToNodeType(QilNodeType typ)
        {
            switch (typ)
            {
                case QilNodeType.DocumentCtor: return XPathNodeType.Root;
                case QilNodeType.ElementCtor: return XPathNodeType.Element;
                case QilNodeType.TextCtor: return XPathNodeType.Text;
                case QilNodeType.RawTextCtor: return XPathNodeType.Text;
                case QilNodeType.PICtor: return XPathNodeType.ProcessingInstruction;
                case QilNodeType.CommentCtor: return XPathNodeType.Comment;
                case QilNodeType.AttributeCtor: return XPathNodeType.Attribute;
                case QilNodeType.NamespaceDecl: return XPathNodeType.Namespace;
            }

            Debug.Fail($"Cannot map QilNodeType {typ} to an XPathNodeType");
            return XPathNodeType.All;
        }

        /// <summary>
        /// Load an XmlNavigatorFilter that matches only the specified name and types onto the stack.
        /// </summary>
        private void LoadSelectFilter(XmlNodeKindFlags xmlTypes, QilName? ndName)
        {
            if (ndName != null)
            {
                // Push NameFilter
                Debug.Assert(xmlTypes == XmlNodeKindFlags.Element);
                _helper.CallGetNameFilter(_helper.StaticData.DeclareNameFilter(ndName.LocalName, ndName.NamespaceUri));
            }
            else
            {
                // Either type cannot be a union, or else it must be >= union of all Content types
                bool isXmlTypeUnion = IsNodeTypeUnion(xmlTypes);
                Debug.Assert(!isXmlTypeUnion || (xmlTypes & XmlNodeKindFlags.Content) == XmlNodeKindFlags.Content);

                if (isXmlTypeUnion)
                {
                    if ((xmlTypes & XmlNodeKindFlags.Attribute) != 0)
                    {
                        // Union includes attributes, so allow all node kinds
                        _helper.CallGetTypeFilter(XPathNodeType.All);
                    }
                    else
                    {
                        // Filter attributes
                        _helper.CallGetTypeFilter(XPathNodeType.Attribute);
                    }
                }
                else
                {
                    // Filter nodes of all but one type
                    _helper.CallGetTypeFilter(QilXmlToXPathNodeType(xmlTypes));
                }
            }
        }

        /// <summary>
        /// Return true if more than one node type is set.
        /// </summary>
        private static bool IsNodeTypeUnion(XmlNodeKindFlags xmlTypes)
        {
            return ((int)xmlTypes & ((int)xmlTypes - 1)) != 0;
        }

        /// <summary>
        /// Start construction of a new nested iterator.  If this.iterCurr == null, then the new iterator
        /// is a top-level, or root iterator.  Otherwise, the new iterator will be nested within the
        /// current iterator.
        /// </summary>
        [MemberNotNull(nameof(_iterCurr))]
        private void StartNestedIterator(QilNode? nd)
        {
            IteratorDescriptor? iterParent = _iterCurr;

            // Create a new, nested iterator
            if (iterParent == null)
            {
                // Create a "root" iterator info that has no parernt
                _iterCurr = new IteratorDescriptor(_helper);
            }
            else
            {
                // Create a nested iterator
                _iterCurr = new IteratorDescriptor(iterParent);
            }

            _iterNested = null;
        }

        /// <summary>
        /// Calls StartNestedIterator(nd) and also sets up the nested iterator to branch to "lblOnEnd" when iteration
        /// is complete.
        /// </summary>
        private void StartNestedIterator(QilNode? nd, Label lblOnEnd)
        {
            StartNestedIterator(nd);
            _iterCurr.SetIterator(lblOnEnd, StorageDescriptor.None());
        }

        /// <summary>
        /// End construction of the current iterator.
        /// </summary>
        private void EndNestedIterator(QilNode nd)
        {
            Debug.Assert(_iterCurr.Storage.Location == ItemLocation.None ||
                         _iterCurr.Storage.ItemStorageType == GetItemStorageType(nd) ||
                         _iterCurr.Storage.ItemStorageType == typeof(XPathItem) ||
                         nd.XmlType!.TypeCode == XmlTypeCode.None,
                         "QilNodeType " + nd.NodeType + " cannot be stored using type " + _iterCurr.Storage.ItemStorageType + ".");

            // If the nested iterator was constructed in branching mode,
            if (_iterCurr.IsBranching)
            {
                // Then if branching hasn't already taken place, do so now
                if (_iterCurr.Storage.Location != ItemLocation.None)
                {
                    _iterCurr.EnsureItemStorageType(nd.XmlType!, typeof(bool));
                    _iterCurr.EnsureStackNoCache();

                    if (_iterCurr.CurrentBranchingContext == BranchingContext.OnTrue)
                        _helper.Emit(OpCodes.Brtrue, _iterCurr.LabelBranch);
                    else
                        _helper.Emit(OpCodes.Brfalse, _iterCurr.LabelBranch);

                    _iterCurr.Storage = StorageDescriptor.None();
                }
            }

            // Save current iterator as nested iterator
            _iterNested = _iterCurr;

            // Update current iterator to be parent iterator
            _iterCurr = _iterCurr.ParentIterator!;
        }

        /// <summary>
        /// Recursively generate code to iterate over the results of the "nd" expression.  If "nd" is pushed
        /// to the writer, then there are no results.  If "nd" is a singleton expression and isCached is false,
        /// then generate code to construct the singleton.  Otherwise, cache the sequence in an XmlQuerySequence
        /// object.  Ensure that all items are converted to the specified "itemStorageType".
        /// </summary>
        private void NestedVisit(QilNode nd, Type itemStorageType, bool isCached)
        {
            if (XmlILConstructInfo.Read(nd).PushToWriterLast)
            {
                // Push results to output, so nothing is left to store
                StartNestedIterator(nd);
                Visit(nd);
                EndNestedIterator(nd);
                _iterCurr.Storage = StorageDescriptor.None();
            }
            else if (!isCached && nd.XmlType!.IsSingleton)
            {
                // Storage of result will be a non-cached singleton
                StartNestedIterator(nd);
                Visit(nd);
                _iterCurr.EnsureNoCache();
                _iterCurr.EnsureItemStorageType(nd.XmlType, itemStorageType);
                EndNestedIterator(nd);
                _iterCurr.Storage = _iterNested!.Storage;
            }
            else
            {
                NestedVisitEnsureCache(nd, itemStorageType);
            }
        }

        /// <summary>
        /// Calls NestedVisit(QilNode, Type, bool), storing result in the default storage type for "nd".
        /// </summary>
        private void NestedVisit(QilNode nd)
        {
            NestedVisit(nd, GetItemStorageType(nd), !nd.XmlType!.IsSingleton);
        }

        /// <summary>
        /// Recursively generate code to iterate over the results of the "nd" expression.  When the expression
        /// has been fully iterated, it will jump to "lblOnEnd".
        /// </summary>
        private void NestedVisit(QilNode nd, Label lblOnEnd)
        {
            Debug.Assert(!XmlILConstructInfo.Read(nd).PushToWriterLast);
            StartNestedIterator(nd, lblOnEnd);
            Visit(nd);
            _iterCurr.EnsureNoCache();
            _iterCurr.EnsureItemStorageType(nd.XmlType!, GetItemStorageType(nd));
            EndNestedIterator(nd);
            _iterCurr.Storage = _iterNested!.Storage;
        }

        /// <summary>
        /// Call NestedVisit(QilNode) and ensure that result is pushed onto the IL stack.
        /// </summary>
        private void NestedVisitEnsureStack(QilNode nd)
        {
            Debug.Assert(!XmlILConstructInfo.Read(nd).PushToWriterLast);
            NestedVisit(nd);
            _iterCurr.EnsureStack();
        }

        /// <summary>
        /// Generate code for both QilExpression nodes and ensure that each result is pushed onto the IL stack.
        /// </summary>
        private void NestedVisitEnsureStack(QilNode ndLeft, QilNode ndRight)
        {
            NestedVisitEnsureStack(ndLeft);
            NestedVisitEnsureStack(ndRight);
        }

        /// <summary>
        /// Call NestedVisit(QilNode, Type, bool) and ensure that result is pushed onto the IL stack.
        /// </summary>
        private void NestedVisitEnsureStack(QilNode nd, Type itemStorageType, bool isCached)
        {
            Debug.Assert(!XmlILConstructInfo.Read(nd).PushToWriterLast);
            NestedVisit(nd, itemStorageType, isCached);
            _iterCurr.EnsureStack();
        }

        /// <summary>
        /// Call NestedVisit(QilNode) and ensure that result is stored in local variable "loc".
        /// </summary>
        private void NestedVisitEnsureLocal(QilNode nd, LocalBuilder loc)
        {
            Debug.Assert(!XmlILConstructInfo.Read(nd).PushToWriterLast);
            NestedVisit(nd);
            _iterCurr.EnsureLocal(loc);
        }

        /// <summary>
        /// Start a nested iterator in a branching context and recursively generate code for the specified QilExpression node.
        /// </summary>
        private void NestedVisitWithBranch(QilNode nd, BranchingContext brctxt, Label lblBranch)
        {
            Debug.Assert(nd.XmlType!.IsSingleton && !XmlILConstructInfo.Read(nd).PushToWriterLast);
            StartNestedIterator(nd);
            _iterCurr.SetBranching(brctxt, lblBranch);
            Visit(nd);
            EndNestedIterator(nd);
            _iterCurr.Storage = StorageDescriptor.None();
        }

        /// <summary>
        /// Generate code for the QilExpression node and ensure that results are fully cached as an XmlQuerySequence.  All results
        /// should be converted to "itemStorageType" before being added to the cache.
        /// </summary>
        private void NestedVisitEnsureCache(QilNode nd, Type itemStorageType)
        {
            Debug.Assert(!XmlILConstructInfo.Read(nd).PushToWriterLast);
            bool cachesResult = CachesResult(nd);
            LocalBuilder locCache;
            Label lblOnEnd = _helper.DefineLabel();
            Type cacheType;
            XmlILStorageMethods methods;

            // If bound expression will already be cached correctly, then don't create an XmlQuerySequence
            if (cachesResult)
            {
                StartNestedIterator(nd);
                Visit(nd);
                EndNestedIterator(nd);
                _iterCurr.Storage = _iterNested!.Storage;
                Debug.Assert(_iterCurr.Storage.IsCached, "Expression result should be cached.  CachesResult() might have a bug in it.");

                // If type of items in the cache matches "itemStorageType", then done
                if (_iterCurr.Storage.ItemStorageType == itemStorageType)
                    return;

                // If the cache has navigators in it, or if converting to a cache of navigators, then EnsureItemStorageType
                // can directly convert without needing to create a new cache.
                if (_iterCurr.Storage.ItemStorageType == typeof(XPathNavigator) || itemStorageType == typeof(XPathNavigator))
                {
                    _iterCurr.EnsureItemStorageType(nd.XmlType!, itemStorageType);
                    return;
                }

                _iterCurr.EnsureNoStack("$$$cacheResult");
            }

            // Always store navigators in XmlQueryNodeSequence (which implements IList<XPathItem>)
            cacheType = (GetItemStorageType(nd) == typeof(XPathNavigator)) ? typeof(XPathNavigator) : itemStorageType;

            // XmlQuerySequence<T> cache;
            methods = XmlILMethods.StorageMethods[cacheType];
            locCache = _helper.DeclareLocal("$$$cache", methods.SeqType);
            _helper.Emit(OpCodes.Ldloc, locCache);

            // Special case non-navigator singletons to use overload of CreateOrReuse
            if (nd.XmlType!.IsSingleton)
            {
                // cache = XmlQuerySequence.CreateOrReuse(cache, item);
                NestedVisitEnsureStack(nd, cacheType, false);
                _helper.Call(methods.SeqReuseSgl);
                _helper.Emit(OpCodes.Stloc, locCache);
            }
            else
            {
                // XmlQuerySequence<T> cache;
                // cache = XmlQuerySequence.CreateOrReuse(cache);
                _helper.Call(methods.SeqReuse);
                _helper.Emit(OpCodes.Stloc, locCache);
                _helper.Emit(OpCodes.Ldloc, locCache);

                StartNestedIterator(nd, lblOnEnd);

                if (cachesResult)
                    _iterCurr.Storage = _iterCurr.ParentIterator!.Storage;
                else
                    Visit(nd);

                // cache.Add(item);
                _iterCurr.EnsureItemStorageType(nd.XmlType, cacheType);
                _iterCurr.EnsureStackNoCache();
                _helper.Call(methods.SeqAdd);
                _helper.Emit(OpCodes.Ldloc, locCache);

                // }
                _iterCurr.LoopToEnd(lblOnEnd);

                EndNestedIterator(nd);

                // Remove cache reference from stack
                _helper.Emit(OpCodes.Pop);
            }

            _iterCurr.Storage = StorageDescriptor.Local(locCache, itemStorageType, true);
        }

        /// <summary>
        /// Returns true if the specified QilExpression node type is *guaranteed* to cache its results in an XmlQuerySequence,
        /// where items in the cache are stored using the default storage type.
        /// </summary>
        private bool CachesResult(QilNode nd)
        {
            OptimizerPatterns patt;

            switch (nd.NodeType)
            {
                case QilNodeType.Let:
                case QilNodeType.Parameter:
                case QilNodeType.Invoke:
                case QilNodeType.XsltInvokeLateBound:
                case QilNodeType.XsltInvokeEarlyBound:
                    return !nd.XmlType!.IsSingleton;

                case QilNodeType.Filter:
                    // EqualityIndex pattern caches results
                    patt = OptimizerPatterns.Read(nd);
                    return patt.MatchesPattern(OptimizerPatternName.EqualityIndex);

                case QilNodeType.DocOrderDistinct:
                    if (nd.XmlType!.IsSingleton)
                        return false;

                    // JoinAndDod and DodReverse patterns don't cache results
                    patt = OptimizerPatterns.Read(nd);
                    return !patt.MatchesPattern(OptimizerPatternName.JoinAndDod) && !patt.MatchesPattern(OptimizerPatternName.DodReverse);

                case QilNodeType.TypeAssert:
                    QilTargetType ndTypeAssert = (QilTargetType)nd;
                    // Check if TypeAssert would be no-op
                    return CachesResult(ndTypeAssert.Source) && GetItemStorageType(ndTypeAssert.Source) == GetItemStorageType(ndTypeAssert);
            }

            return false;
        }

        /// <summary>
        /// Shortcut call to XmlILTypeHelper.GetStorageType.
        /// </summary>
        private Type GetStorageType(QilNode nd)
        {
            return XmlILTypeHelper.GetStorageType(nd.XmlType!);
        }

        /// <summary>
        /// Shortcut call to XmlILTypeHelper.GetStorageType.
        /// </summary>
        private Type GetStorageType(XmlQueryType typ)
        {
            return XmlILTypeHelper.GetStorageType(typ);
        }

        /// <summary>
        /// Shortcut call to XmlILTypeHelper.GetStorageType, using an expression's prime type.
        /// </summary>
        private Type GetItemStorageType(QilNode nd)
        {
            return XmlILTypeHelper.GetStorageType(nd.XmlType!.Prime);
        }

        /// <summary>
        /// Shortcut call to XmlILTypeHelper.GetStorageType, using the prime type.
        /// </summary>
        private Type GetItemStorageType(XmlQueryType typ)
        {
            return XmlILTypeHelper.GetStorageType(typ.Prime);
        }
    }
}
