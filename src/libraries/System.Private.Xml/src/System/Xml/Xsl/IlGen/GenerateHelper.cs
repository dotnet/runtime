// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Diagnostics;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl.IlGen
{
    /// <summary>
    /// List of all XmlIL runtime constructors.
    /// </summary>
    internal sealed class XmlILStorageMethods
    {
        // Aggregates
        public readonly MethodInfo? AggAvg;
        public readonly MethodInfo? AggAvgResult;
        public readonly MethodInfo? AggCreate;
        public readonly MethodInfo? AggIsEmpty;
        public readonly MethodInfo? AggMax;
        public readonly MethodInfo? AggMaxResult;
        public readonly MethodInfo? AggMin;
        public readonly MethodInfo? AggMinResult;
        public readonly MethodInfo? AggSum;
        public readonly MethodInfo? AggSumResult;

        // Sequences
        public readonly Type SeqType;
        public readonly FieldInfo SeqEmpty;
        public readonly MethodInfo SeqReuse;
        public readonly MethodInfo SeqReuseSgl;
        public readonly MethodInfo SeqAdd;
        public readonly MethodInfo SeqSortByKeys;

        // IList<>
        public readonly Type IListType;
        public readonly MethodInfo IListCount;
        public readonly MethodInfo IListItem;

        // XPathItem
        public readonly MethodInfo? ValueAs;

        // ToAtomicValue
        public readonly MethodInfo? ToAtomicValue;

        public XmlILStorageMethods(Type storageType)
        {
            // Aggregates
            Type? aggType = null;
            if (storageType == typeof(int))
            {
                aggType = typeof(Int32Aggregator);
            }
            else if (storageType == typeof(long))
            {
                aggType = typeof(Int64Aggregator);
            }
            else if (storageType == typeof(decimal))
            {
                aggType = typeof(DecimalAggregator);
            }
            else if (storageType == typeof(double))
            {
                aggType = typeof(DoubleAggregator);
            }

            if (aggType != null)
            {
                AggAvg = aggType.GetMethod("Average");
                AggAvgResult = aggType.GetMethod("get_AverageResult");
                AggCreate = aggType.GetMethod("Create");
                AggIsEmpty = aggType.GetMethod("get_IsEmpty");
                AggMax = aggType.GetMethod("Maximum");
                AggMaxResult = aggType.GetMethod("get_MaximumResult");
                AggMin = aggType.GetMethod("Minimum");
                AggMinResult = aggType.GetMethod("get_MinimumResult");
                AggSum = aggType.GetMethod("Sum");
                AggSumResult = aggType.GetMethod("get_SumResult");
            }

            // Sequences
            // use local 'sequenceType' variable to work around https://github.com/mono/linker/issues/1664
            Type sequenceType;
            if (storageType == typeof(XPathNavigator))
            {
                sequenceType = typeof(XmlQueryNodeSequence);
                SeqAdd = sequenceType.GetMethod("AddClone")!;
            }
            else if (storageType == typeof(XPathItem))
            {
                sequenceType = typeof(XmlQueryItemSequence);
                SeqAdd = sequenceType.GetMethod("AddClone")!;
            }
            else
            {
                sequenceType = typeof(XmlQuerySequence<>).MakeGenericType(storageType);
                SeqAdd = sequenceType.GetMethod("Add")!;
            }

            FieldInfo? seqEmpty = sequenceType.GetField("Empty");
            Debug.Assert(seqEmpty != null, "Field `Empty` could not be found");
            SeqEmpty = seqEmpty;
            SeqReuse = sequenceType.GetMethod("CreateOrReuse", new[] { sequenceType })!;
            SeqReuseSgl = sequenceType.GetMethod("CreateOrReuse", new[] { sequenceType, storageType })!;
            SeqSortByKeys = sequenceType.GetMethod("SortByKeys")!;
            SeqType = sequenceType;

            // IList<>
            Type listType = typeof(IList<>).MakeGenericType(storageType);
            IListItem = listType.GetMethod("get_Item")!;
            IListType = listType;
            IListCount = typeof(ICollection<>).MakeGenericType(storageType).GetMethod("get_Count")!;

            // XPathItem.ValueAsXXX
            if (storageType == typeof(string))
                ValueAs = typeof(XPathItem).GetMethod("get_Value");
            else if (storageType == typeof(int))
                ValueAs = typeof(XPathItem).GetMethod("get_ValueAsInt");
            else if (storageType == typeof(long))
                ValueAs = typeof(XPathItem).GetMethod("get_ValueAsLong");
            else if (storageType == typeof(DateTime))
                ValueAs = typeof(XPathItem).GetMethod("get_ValueAsDateTime");
            else if (storageType == typeof(double))
                ValueAs = typeof(XPathItem).GetMethod("get_ValueAsDouble");
            else if (storageType == typeof(bool))
                ValueAs = typeof(XPathItem).GetMethod("get_ValueAsBoolean");

            // XmlILStorageConverter.XXXToAtomicValue
            if (storageType == typeof(byte[]))
                ToAtomicValue = typeof(XmlILStorageConverter).GetMethod("BytesToAtomicValue");
            else if (storageType != typeof(XPathItem) && storageType != typeof(XPathNavigator))
                ToAtomicValue = typeof(XmlILStorageConverter).GetMethod($"{storageType.Name}ToAtomicValue");
        }
    }

    /// <summary>
    /// List of all XmlIL runtime constructors.
    /// </summary>
    internal static class XmlILConstructors
    {
        public static readonly ConstructorInfo DecFromParts = typeof(decimal).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) })!;
        public static readonly ConstructorInfo DecFromInt32 = typeof(decimal).GetConstructor(new[] { typeof(int) })!;
        public static readonly ConstructorInfo DecFromInt64 = typeof(decimal).GetConstructor(new[] { typeof(long) })!;
        public static readonly ConstructorInfo Debuggable = typeof(DebuggableAttribute).GetConstructor(new[] { typeof(DebuggableAttribute.DebuggingModes) })!;
        public static readonly ConstructorInfo NonUserCode = typeof(DebuggerNonUserCodeAttribute).GetConstructor(Type.EmptyTypes)!;
        public static readonly ConstructorInfo QName = typeof(XmlQualifiedName).GetConstructor(new[] { typeof(string), typeof(string) })!;
        public static readonly ConstructorInfo StepThrough = typeof(DebuggerStepThroughAttribute).GetConstructor(Type.EmptyTypes)!;
        public static readonly ConstructorInfo Transparent = typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes)!;
    }

    /// <summary>
    /// List of all XmlIL runtime methods.
    /// </summary>
    internal static class XmlILMethods
    {
        // Iterators
        public static readonly MethodInfo AncCreate = typeof(AncestorIterator).GetMethod("Create")!;
        public static readonly MethodInfo AncNext = typeof(AncestorIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo AncCurrent = typeof(AncestorIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo AncDOCreate = typeof(AncestorDocOrderIterator).GetMethod("Create")!;
        public static readonly MethodInfo AncDONext = typeof(AncestorDocOrderIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo AncDOCurrent = typeof(AncestorDocOrderIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo AttrContentCreate = typeof(AttributeContentIterator).GetMethod("Create")!;
        public static readonly MethodInfo AttrContentNext = typeof(AttributeContentIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo AttrContentCurrent = typeof(AttributeContentIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo AttrCreate = typeof(AttributeIterator).GetMethod("Create")!;
        public static readonly MethodInfo AttrNext = typeof(AttributeIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo AttrCurrent = typeof(AttributeIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo ContentCreate = typeof(ContentIterator).GetMethod("Create")!;
        public static readonly MethodInfo ContentNext = typeof(ContentIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo ContentCurrent = typeof(ContentIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo ContentMergeCreate = typeof(ContentMergeIterator).GetMethod("Create")!;
        public static readonly MethodInfo ContentMergeNext = typeof(ContentMergeIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo ContentMergeCurrent = typeof(ContentMergeIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo DescCreate = typeof(DescendantIterator).GetMethod("Create")!;
        public static readonly MethodInfo DescNext = typeof(DescendantIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo DescCurrent = typeof(DescendantIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo DescMergeCreate = typeof(DescendantMergeIterator).GetMethod("Create")!;
        public static readonly MethodInfo DescMergeNext = typeof(DescendantMergeIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo DescMergeCurrent = typeof(DescendantMergeIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo DiffCreate = typeof(DifferenceIterator).GetMethod("Create")!;
        public static readonly MethodInfo DiffNext = typeof(DifferenceIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo DiffCurrent = typeof(DifferenceIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo DodMergeCreate = typeof(DodSequenceMerge).GetMethod("Create")!;
        public static readonly MethodInfo DodMergeAdd = typeof(DodSequenceMerge).GetMethod("AddSequence")!;
        public static readonly MethodInfo DodMergeSeq = typeof(DodSequenceMerge).GetMethod("MergeSequences")!;
        public static readonly MethodInfo ElemContentCreate = typeof(ElementContentIterator).GetMethod("Create")!;
        public static readonly MethodInfo ElemContentNext = typeof(ElementContentIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo ElemContentCurrent = typeof(ElementContentIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo FollSibCreate = typeof(FollowingSiblingIterator).GetMethod("Create")!;
        public static readonly MethodInfo FollSibNext = typeof(FollowingSiblingIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo FollSibCurrent = typeof(FollowingSiblingIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo FollSibMergeCreate = typeof(FollowingSiblingMergeIterator).GetMethod("Create")!;
        public static readonly MethodInfo FollSibMergeNext = typeof(FollowingSiblingMergeIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo FollSibMergeCurrent = typeof(FollowingSiblingMergeIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo IdCreate = typeof(IdIterator).GetMethod("Create")!;
        public static readonly MethodInfo IdNext = typeof(IdIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo IdCurrent = typeof(IdIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo InterCreate = typeof(IntersectIterator).GetMethod("Create")!;
        public static readonly MethodInfo InterNext = typeof(IntersectIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo InterCurrent = typeof(IntersectIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo KindContentCreate = typeof(NodeKindContentIterator).GetMethod("Create")!;
        public static readonly MethodInfo KindContentNext = typeof(NodeKindContentIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo KindContentCurrent = typeof(NodeKindContentIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo NmspCreate = typeof(NamespaceIterator).GetMethod("Create")!;
        public static readonly MethodInfo NmspNext = typeof(NamespaceIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo NmspCurrent = typeof(NamespaceIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo NodeRangeCreate = typeof(NodeRangeIterator).GetMethod("Create")!;
        public static readonly MethodInfo NodeRangeNext = typeof(NodeRangeIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo NodeRangeCurrent = typeof(NodeRangeIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo ParentCreate = typeof(ParentIterator).GetMethod("Create")!;
        public static readonly MethodInfo ParentNext = typeof(ParentIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo ParentCurrent = typeof(ParentIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo PrecCreate = typeof(PrecedingIterator).GetMethod("Create")!;
        public static readonly MethodInfo PrecNext = typeof(PrecedingIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo PrecCurrent = typeof(PrecedingIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo PreSibCreate = typeof(PrecedingSiblingIterator).GetMethod("Create")!;
        public static readonly MethodInfo PreSibNext = typeof(PrecedingSiblingIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo PreSibCurrent = typeof(PrecedingSiblingIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo PreSibDOCreate = typeof(PrecedingSiblingDocOrderIterator).GetMethod("Create")!;
        public static readonly MethodInfo PreSibDONext = typeof(PrecedingSiblingDocOrderIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo PreSibDOCurrent = typeof(PrecedingSiblingDocOrderIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo SortKeyCreate = typeof(XmlSortKeyAccumulator).GetMethod("Create")!;
        public static readonly MethodInfo SortKeyDateTime = typeof(XmlSortKeyAccumulator).GetMethod("AddDateTimeSortKey")!;
        public static readonly MethodInfo SortKeyDecimal = typeof(XmlSortKeyAccumulator).GetMethod("AddDecimalSortKey")!;
        public static readonly MethodInfo SortKeyDouble = typeof(XmlSortKeyAccumulator).GetMethod("AddDoubleSortKey")!;
        public static readonly MethodInfo SortKeyEmpty = typeof(XmlSortKeyAccumulator).GetMethod("AddEmptySortKey")!;
        public static readonly MethodInfo SortKeyFinish = typeof(XmlSortKeyAccumulator).GetMethod("FinishSortKeys")!;
        public static readonly MethodInfo SortKeyInt = typeof(XmlSortKeyAccumulator).GetMethod("AddIntSortKey")!;
        public static readonly MethodInfo SortKeyInteger = typeof(XmlSortKeyAccumulator).GetMethod("AddIntegerSortKey")!;
        public static readonly MethodInfo SortKeyKeys = typeof(XmlSortKeyAccumulator).GetMethod("get_Keys")!;
        public static readonly MethodInfo SortKeyString = typeof(XmlSortKeyAccumulator).GetMethod("AddStringSortKey")!;
        public static readonly MethodInfo UnionCreate = typeof(UnionIterator).GetMethod("Create")!;
        public static readonly MethodInfo UnionNext = typeof(UnionIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo UnionCurrent = typeof(UnionIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo XPFollCreate = typeof(XPathFollowingIterator).GetMethod("Create")!;
        public static readonly MethodInfo XPFollNext = typeof(XPathFollowingIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo XPFollCurrent = typeof(XPathFollowingIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo XPFollMergeCreate = typeof(XPathFollowingMergeIterator).GetMethod("Create")!;
        public static readonly MethodInfo XPFollMergeNext = typeof(XPathFollowingMergeIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo XPFollMergeCurrent = typeof(XPathFollowingMergeIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo XPPrecCreate = typeof(XPathPrecedingIterator).GetMethod("Create")!;
        public static readonly MethodInfo XPPrecNext = typeof(XPathPrecedingIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo XPPrecCurrent = typeof(XPathPrecedingIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo XPPrecDOCreate = typeof(XPathPrecedingDocOrderIterator).GetMethod("Create")!;
        public static readonly MethodInfo XPPrecDONext = typeof(XPathPrecedingDocOrderIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo XPPrecDOCurrent = typeof(XPathPrecedingDocOrderIterator).GetMethod("get_Current")!;
        public static readonly MethodInfo XPPrecMergeCreate = typeof(XPathPrecedingMergeIterator).GetMethod("Create")!;
        public static readonly MethodInfo XPPrecMergeNext = typeof(XPathPrecedingMergeIterator).GetMethod("MoveNext")!;
        public static readonly MethodInfo XPPrecMergeCurrent = typeof(XPathPrecedingMergeIterator).GetMethod("get_Current")!;

        // XmlQueryRuntime
        public static readonly MethodInfo AddNewIndex = typeof(XmlQueryRuntime).GetMethod("AddNewIndex")!;
        public static readonly MethodInfo ChangeTypeXsltArg = typeof(XmlQueryRuntime).GetMethod("ChangeTypeXsltArgument", new[] { typeof(int), typeof(object), typeof(Type) })!;
        public static readonly MethodInfo ChangeTypeXsltResult = typeof(XmlQueryRuntime).GetMethod("ChangeTypeXsltResult")!;
        public static readonly MethodInfo CompPos = typeof(XmlQueryRuntime).GetMethod("ComparePosition")!;
        public static readonly MethodInfo Context = typeof(XmlQueryRuntime).GetMethod("get_ExternalContext")!;
        public static readonly MethodInfo CreateCollation = typeof(XmlQueryRuntime).GetMethod("CreateCollation")!;
        public static readonly MethodInfo DocOrder = typeof(XmlQueryRuntime).GetMethod("DocOrderDistinct")!;
        public static readonly MethodInfo EndRtfConstr = typeof(XmlQueryRuntime).GetMethod("EndRtfConstruction")!;
        public static readonly MethodInfo EndSeqConstr = typeof(XmlQueryRuntime).GetMethod("EndSequenceConstruction")!;
        public static readonly MethodInfo FindIndex = typeof(XmlQueryRuntime).GetMethod("FindIndex")!;
        public static readonly MethodInfo GenId = typeof(XmlQueryRuntime).GetMethod("GenerateId")!;
        public static readonly MethodInfo GetAtomizedName = typeof(XmlQueryRuntime).GetMethod("GetAtomizedName")!;
        public static readonly MethodInfo GetCollation = typeof(XmlQueryRuntime).GetMethod("GetCollation")!;
        public static readonly MethodInfo GetEarly = typeof(XmlQueryRuntime).GetMethod("GetEarlyBoundObject")!;
        public static readonly MethodInfo GetNameFilter = typeof(XmlQueryRuntime).GetMethod("GetNameFilter")!;
        public static readonly MethodInfo GetOutput = typeof(XmlQueryRuntime).GetMethod("get_Output")!;
        public static readonly MethodInfo GetGlobalValue = typeof(XmlQueryRuntime).GetMethod("GetGlobalValue")!;
        public static readonly MethodInfo GetTypeFilter = typeof(XmlQueryRuntime).GetMethod("GetTypeFilter")!;
        public static readonly MethodInfo GlobalComputed = typeof(XmlQueryRuntime).GetMethod("IsGlobalComputed")!;
        public static readonly MethodInfo ItemMatchesCode = typeof(XmlQueryRuntime).GetMethod("MatchesXmlType", new[] { typeof(XPathItem), typeof(XmlTypeCode) })!;
        public static readonly MethodInfo ItemMatchesType = typeof(XmlQueryRuntime).GetMethod("MatchesXmlType", new[] { typeof(XPathItem), typeof(int) })!;
        public static readonly MethodInfo QNameEqualLit = typeof(XmlQueryRuntime).GetMethod("IsQNameEqual", new[] { typeof(XPathNavigator), typeof(int), typeof(int) })!;
        public static readonly MethodInfo QNameEqualNav = typeof(XmlQueryRuntime).GetMethod("IsQNameEqual", new[] { typeof(XPathNavigator), typeof(XPathNavigator) })!;
        public static readonly MethodInfo RtfConstr = typeof(XmlQueryRuntime).GetMethod("TextRtfConstruction")!;
        public static readonly MethodInfo SendMessage = typeof(XmlQueryRuntime).GetMethod("SendMessage")!;
        public static readonly MethodInfo SeqMatchesCode = typeof(XmlQueryRuntime).GetMethod("MatchesXmlType", new[] { typeof(IList<XPathItem>), typeof(XmlTypeCode) })!;
        public static readonly MethodInfo SeqMatchesType = typeof(XmlQueryRuntime).GetMethod("MatchesXmlType", new[] { typeof(IList<XPathItem>), typeof(int) })!;
        public static readonly MethodInfo SetGlobalValue = typeof(XmlQueryRuntime).GetMethod("SetGlobalValue")!;
        public static readonly MethodInfo StartRtfConstr = typeof(XmlQueryRuntime).GetMethod("StartRtfConstruction")!;
        public static readonly MethodInfo StartSeqConstr = typeof(XmlQueryRuntime).GetMethod("StartSequenceConstruction")!;
        public static readonly MethodInfo TagAndMappings = typeof(XmlQueryRuntime).GetMethod("ParseTagName", new[] { typeof(string), typeof(int) })!;
        public static readonly MethodInfo TagAndNamespace = typeof(XmlQueryRuntime).GetMethod("ParseTagName", new[] { typeof(string), typeof(string) })!;
        public static readonly MethodInfo ThrowException = typeof(XmlQueryRuntime).GetMethod("ThrowException")!;
        public static readonly MethodInfo XsltLib = typeof(XmlQueryRuntime).GetMethod("get_XsltFunctions")!;

        // XmlQueryContext
        public static readonly MethodInfo GetDataSource = typeof(XmlQueryContext).GetMethod("GetDataSource")!;
        public static readonly MethodInfo GetDefaultDataSource = typeof(XmlQueryContext).GetMethod("get_DefaultDataSource")!;
        public static readonly MethodInfo GetParam = typeof(XmlQueryContext).GetMethod("GetParameter")!;
        public static readonly MethodInfo InvokeXsltLate = GetInvokeXsltLateBoundFunction();
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Supressing warning about not having the RequiresUnreferencedCode attribute since this code path " +
            "will only be emitting IL that will later be called by Transform() method which is already annotated as RequiresUnreferencedCode")]
        private static MethodInfo GetInvokeXsltLateBoundFunction() => typeof(XmlQueryContext).GetMethod("InvokeXsltLateBoundFunction")!;

        // XmlILIndex
        public static readonly MethodInfo IndexAdd = typeof(XmlILIndex).GetMethod("Add")!;
        public static readonly MethodInfo IndexLookup = typeof(XmlILIndex).GetMethod("Lookup")!;

        // XPathItem
        public static readonly MethodInfo ItemIsNode = typeof(XPathItem).GetMethod("get_IsNode")!;
        public static readonly MethodInfo Value = typeof(XPathItem).GetMethod("get_Value")!;
        public static readonly MethodInfo ValueAsAny = typeof(XPathItem).GetMethod("ValueAs", new[] { typeof(Type), typeof(IXmlNamespaceResolver) })!;

        // XPathNavigator
        public static readonly MethodInfo NavClone = typeof(XPathNavigator).GetMethod("Clone")!;
        public static readonly MethodInfo NavLocalName = typeof(XPathNavigator).GetMethod("get_LocalName")!;
        public static readonly MethodInfo NavMoveAttr = typeof(XPathNavigator).GetMethod("MoveToAttribute", new[] { typeof(string), typeof(string) })!;
        public static readonly MethodInfo NavMoveId = typeof(XPathNavigator).GetMethod("MoveToId")!;
        public static readonly MethodInfo NavMoveParent = typeof(XPathNavigator).GetMethod("MoveToParent")!;
        public static readonly MethodInfo NavMoveRoot = typeof(XPathNavigator).GetMethod("MoveToRoot")!;
        public static readonly MethodInfo NavMoveTo = typeof(XPathNavigator).GetMethod("MoveTo")!;
        public static readonly MethodInfo NavNmsp = typeof(XPathNavigator).GetMethod("get_NamespaceURI")!;
        public static readonly MethodInfo NavPrefix = typeof(XPathNavigator).GetMethod("get_Prefix")!;
        public static readonly MethodInfo NavSamePos = typeof(XPathNavigator).GetMethod("IsSamePosition")!;
        public static readonly MethodInfo NavType = typeof(XPathNavigator).GetMethod("get_NodeType")!;

        // XmlQueryOutput methods
        public static readonly MethodInfo StartElemLitName = typeof(XmlQueryOutput).GetMethod("WriteStartElement", new[] { typeof(string), typeof(string), typeof(string) })!;
        public static readonly MethodInfo StartElemLocName = typeof(XmlQueryOutput).GetMethod("WriteStartElementLocalName", new[] { typeof(string) })!;
        public static readonly MethodInfo EndElemStackName = typeof(XmlQueryOutput).GetMethod("WriteEndElement")!;
        public static readonly MethodInfo StartAttrLitName = typeof(XmlQueryOutput).GetMethod("WriteStartAttribute", new[] { typeof(string), typeof(string), typeof(string) })!;
        public static readonly MethodInfo StartAttrLocName = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeLocalName", new[] { typeof(string) })!;
        public static readonly MethodInfo EndAttr = typeof(XmlQueryOutput).GetMethod("WriteEndAttribute")!;
        public static readonly MethodInfo Text = typeof(XmlQueryOutput).GetMethod("WriteString")!;
        public static readonly MethodInfo NoEntText = typeof(XmlQueryOutput).GetMethod("WriteRaw", new[] { typeof(string) })!;

        public static readonly MethodInfo StartTree = typeof(XmlQueryOutput).GetMethod("StartTree")!;
        public static readonly MethodInfo EndTree = typeof(XmlQueryOutput).GetMethod("EndTree")!;

        public static readonly MethodInfo StartElemLitNameUn = typeof(XmlQueryOutput).GetMethod("WriteStartElementUnchecked", new[] { typeof(string), typeof(string), typeof(string) })!;
        public static readonly MethodInfo StartElemLocNameUn = typeof(XmlQueryOutput).GetMethod("WriteStartElementUnchecked", new[] { typeof(string) })!;
        public static readonly MethodInfo StartContentUn = typeof(XmlQueryOutput).GetMethod("StartElementContentUnchecked")!;
        public static readonly MethodInfo EndElemLitNameUn = typeof(XmlQueryOutput).GetMethod("WriteEndElementUnchecked", new[] { typeof(string), typeof(string), typeof(string) })!;
        public static readonly MethodInfo EndElemLocNameUn = typeof(XmlQueryOutput).GetMethod("WriteEndElementUnchecked", new[] { typeof(string) })!;
        public static readonly MethodInfo StartAttrLitNameUn = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeUnchecked", new[] { typeof(string), typeof(string), typeof(string) })!;
        public static readonly MethodInfo StartAttrLocNameUn = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeUnchecked", new[] { typeof(string) })!;
        public static readonly MethodInfo EndAttrUn = typeof(XmlQueryOutput).GetMethod("WriteEndAttributeUnchecked")!;
        public static readonly MethodInfo NamespaceDeclUn = typeof(XmlQueryOutput).GetMethod("WriteNamespaceDeclarationUnchecked")!;
        public static readonly MethodInfo TextUn = typeof(XmlQueryOutput).GetMethod("WriteStringUnchecked")!;
        public static readonly MethodInfo NoEntTextUn = typeof(XmlQueryOutput).GetMethod("WriteRawUnchecked")!;

        public static readonly MethodInfo StartRoot = typeof(XmlQueryOutput).GetMethod("WriteStartRoot")!;
        public static readonly MethodInfo EndRoot = typeof(XmlQueryOutput).GetMethod("WriteEndRoot")!;
        public static readonly MethodInfo StartElemCopyName = typeof(XmlQueryOutput).GetMethod("WriteStartElementComputed", new[] { typeof(XPathNavigator) })!;
        public static readonly MethodInfo StartElemMapName = typeof(XmlQueryOutput).GetMethod("WriteStartElementComputed", new[] { typeof(string), typeof(int) })!;
        public static readonly MethodInfo StartElemNmspName = typeof(XmlQueryOutput).GetMethod("WriteStartElementComputed", new[] { typeof(string), typeof(string) })!;
        public static readonly MethodInfo StartElemQName = typeof(XmlQueryOutput).GetMethod("WriteStartElementComputed", new[] { typeof(XmlQualifiedName) })!;
        public static readonly MethodInfo StartAttrCopyName = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeComputed", new[] { typeof(XPathNavigator) })!;
        public static readonly MethodInfo StartAttrMapName = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeComputed", new[] { typeof(string), typeof(int) })!;
        public static readonly MethodInfo StartAttrNmspName = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeComputed", new[] { typeof(string), typeof(string) })!;
        public static readonly MethodInfo StartAttrQName = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeComputed", new[] { typeof(XmlQualifiedName) })!;
        public static readonly MethodInfo NamespaceDecl = typeof(XmlQueryOutput).GetMethod("WriteNamespaceDeclaration")!;
        public static readonly MethodInfo StartComment = typeof(XmlQueryOutput).GetMethod("WriteStartComment")!;
        public static readonly MethodInfo CommentText = typeof(XmlQueryOutput).GetMethod("WriteCommentString")!;
        public static readonly MethodInfo EndComment = typeof(XmlQueryOutput).GetMethod("WriteEndComment")!;
        public static readonly MethodInfo StartPI = typeof(XmlQueryOutput).GetMethod("WriteStartProcessingInstruction")!;
        public static readonly MethodInfo PIText = typeof(XmlQueryOutput).GetMethod("WriteProcessingInstructionString")!;
        public static readonly MethodInfo EndPI = typeof(XmlQueryOutput).GetMethod("WriteEndProcessingInstruction")!;
        public static readonly MethodInfo WriteItem = typeof(XmlQueryOutput).GetMethod("WriteItem")!;
        public static readonly MethodInfo CopyOf = typeof(XmlQueryOutput).GetMethod("XsltCopyOf")!;
        public static readonly MethodInfo StartCopy = typeof(XmlQueryOutput).GetMethod("StartCopy")!;
        public static readonly MethodInfo EndCopy = typeof(XmlQueryOutput).GetMethod("EndCopy")!;

        // Datatypes
        public static readonly MethodInfo DecAdd = typeof(decimal).GetMethod("Add")!;
        public static readonly MethodInfo DecCmp = typeof(decimal).GetMethod("Compare", new[] { typeof(decimal), typeof(decimal) })!;
        public static readonly MethodInfo DecEq = typeof(decimal).GetMethod("Equals", new[] { typeof(decimal), typeof(decimal) })!;
        public static readonly MethodInfo DecSub = typeof(decimal).GetMethod("Subtract")!;
        public static readonly MethodInfo DecMul = typeof(decimal).GetMethod("Multiply")!;
        public static readonly MethodInfo DecDiv = typeof(decimal).GetMethod("Divide")!;
        public static readonly MethodInfo DecRem = typeof(decimal).GetMethod("Remainder")!;
        public static readonly MethodInfo DecNeg = typeof(decimal).GetMethod("Negate")!;
        public static readonly MethodInfo QNameEq = typeof(XmlQualifiedName).GetMethod("Equals")!;
        public static readonly MethodInfo StrEq = typeof(string).GetMethod("Equals", new[] { typeof(string), typeof(string) })!;
        public static readonly MethodInfo StrCat2 = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })!;
        public static readonly MethodInfo StrCat3 = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string), typeof(string) })!;
        public static readonly MethodInfo StrCat4 = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string), typeof(string), typeof(string) })!;
        public static readonly MethodInfo StrCmp = typeof(string).GetMethod("CompareOrdinal", new[] { typeof(string), typeof(string) })!;
        public static readonly MethodInfo StrLen = typeof(string).GetMethod("get_Length")!;

        // XsltConvert
        public static readonly MethodInfo DblToDec = typeof(XsltConvert).GetMethod("ToDecimal", new[] { typeof(double) })!;
        public static readonly MethodInfo DblToInt = typeof(XsltConvert).GetMethod("ToInt", new[] { typeof(double) })!;
        public static readonly MethodInfo DblToLng = typeof(XsltConvert).GetMethod("ToLong", new[] { typeof(double) })!;
        public static readonly MethodInfo DblToStr = typeof(XsltConvert).GetMethod("ToString", new[] { typeof(double) })!;
        public static readonly MethodInfo DecToDbl = typeof(XsltConvert).GetMethod("ToDouble", new[] { typeof(decimal) })!;
        public static readonly MethodInfo DTToStr = typeof(XsltConvert).GetMethod("ToString", new[] { typeof(DateTime) })!;
        public static readonly MethodInfo IntToDbl = typeof(XsltConvert).GetMethod("ToDouble", new[] { typeof(int) })!;
        public static readonly MethodInfo LngToDbl = typeof(XsltConvert).GetMethod("ToDouble", new[] { typeof(long) })!;
        public static readonly MethodInfo StrToDbl = typeof(XsltConvert).GetMethod("ToDouble", new[] { typeof(string) })!;
        public static readonly MethodInfo StrToDT = typeof(XsltConvert).GetMethod("ToDateTime", new[] { typeof(string) })!;

        public static readonly MethodInfo ItemToBool = typeof(XsltConvert).GetMethod("ToBoolean", new[] { typeof(XPathItem) })!;
        public static readonly MethodInfo ItemToDbl = typeof(XsltConvert).GetMethod("ToDouble", new[] { typeof(XPathItem) })!;
        public static readonly MethodInfo ItemToStr = typeof(XsltConvert).GetMethod("ToString", new[] { typeof(XPathItem) })!;
        public static readonly MethodInfo ItemToNode = typeof(XsltConvert).GetMethod("ToNode", new[] { typeof(XPathItem) })!;
        public static readonly MethodInfo ItemToNodes = typeof(XsltConvert).GetMethod("ToNodeSet", new[] { typeof(XPathItem) })!;

        public static readonly MethodInfo ItemsToBool = typeof(XsltConvert).GetMethod("ToBoolean", new[] { typeof(IList<XPathItem>) })!;
        public static readonly MethodInfo ItemsToDbl = typeof(XsltConvert).GetMethod("ToDouble", new[] { typeof(IList<XPathItem>) })!;
        public static readonly MethodInfo ItemsToNode = typeof(XsltConvert).GetMethod("ToNode", new[] { typeof(IList<XPathItem>) })!;
        public static readonly MethodInfo ItemsToNodes = typeof(XsltConvert).GetMethod("ToNodeSet", new[] { typeof(IList<XPathItem>) })!;
        public static readonly MethodInfo ItemsToStr = typeof(XsltConvert).GetMethod("ToString", new[] { typeof(IList<XPathItem>) })!;

        // StringConcat
        public static readonly MethodInfo StrCatCat = typeof(StringConcat).GetMethod("Concat")!;
        public static readonly MethodInfo StrCatClear = typeof(StringConcat).GetMethod("Clear")!;
        public static readonly MethodInfo StrCatResult = typeof(StringConcat).GetMethod("GetResult")!;
        public static readonly MethodInfo StrCatDelim = typeof(StringConcat).GetMethod("set_Delimiter")!;

        // XmlILStorageConverter
        public static readonly MethodInfo NavsToItems = typeof(XmlILStorageConverter).GetMethod("NavigatorsToItems")!;
        public static readonly MethodInfo ItemsToNavs = typeof(XmlILStorageConverter).GetMethod("ItemsToNavigators")!;

        // XmlQueryNodeSequence
        public static readonly MethodInfo SetDod = typeof(XmlQueryNodeSequence).GetMethod("set_IsDocOrderDistinct")!;

        // Miscellaneous
        public static readonly MethodInfo GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle")!;
        public static readonly MethodInfo InitializeArray = typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("InitializeArray")!;
        public static readonly Dictionary<Type, XmlILStorageMethods> StorageMethods = new Dictionary<Type, XmlILStorageMethods>(13)
        {
            { typeof(string), new XmlILStorageMethods(typeof(string)) },
            { typeof(bool), new XmlILStorageMethods(typeof(bool)) },
            { typeof(int), new XmlILStorageMethods(typeof(int)) },
            { typeof(long), new XmlILStorageMethods(typeof(long)) },
            { typeof(decimal), new XmlILStorageMethods(typeof(decimal)) },
            { typeof(double), new XmlILStorageMethods(typeof(double)) },
            { typeof(float), new XmlILStorageMethods(typeof(float)) },
            { typeof(DateTime), new XmlILStorageMethods(typeof(DateTime)) },
            { typeof(byte[]), new XmlILStorageMethods(typeof(byte[])) },
            { typeof(XmlQualifiedName), new XmlILStorageMethods(typeof(XmlQualifiedName)) },
            { typeof(TimeSpan), new XmlILStorageMethods(typeof(TimeSpan)) },
            { typeof(XPathItem), new XmlILStorageMethods(typeof(XPathItem)) },
            { typeof(XPathNavigator), new XmlILStorageMethods(typeof(XPathNavigator)) },
        };
    }


    /// <summary>
    /// When named nodes are constructed, there are several possible ways for their names to be created.
    /// </summary>
    internal enum GenerateNameType
    {
        LiteralLocalName,       // Local name is a literal string; namespace is null
        LiteralName,            // All parts of the name are literal strings
        CopiedName,             // Name should be copied from a navigator
        TagNameAndMappings,     // Tagname contains prefix:localName and prefix is mapped to a namespace
        TagNameAndNamespace,    // Tagname contains prefix:localName and namespace is provided
        QName,                  // Name is computed QName (no prefix available)
        StackName,              // Element name has already been pushed onto XmlQueryOutput stack
    }

    /// <summary>
    /// Contains helper methods used during the code generation phase.
    /// </summary>
    internal sealed class GenerateHelper
    {
        private MethodBase? _methInfo;
        private ILGenerator? _ilgen;
        private LocalBuilder? _locXOut;
        private readonly XmlILModule _module;
        private readonly bool _isDebug;
        private bool _initWriters;
        private readonly StaticDataManager _staticData;
        private ISourceLineInfo? _lastSourceInfo;
        private MethodInfo? _methSyncToNav;

#if DEBUG
        private int _lblNum;
        private Hashtable? _symbols;
        private int _numLocals;
        private string? _sourceFile;
        private TextWriter? _writerDump;
#endif

        /// <summary>
        /// Cache metadata used during code-generation phase.
        /// </summary>
        // SxS note: Using hardcoded "dump.il" is an SxS issue. Since we are doing this ONLY in debug builds
        // and only for tracing purposes and MakeVersionSafeName does not seem to be able to handle file
        // extensions correctly I decided to suppress the SxS message (as advised by SxS guys).
        public GenerateHelper(XmlILModule module, bool isDebug)
        {
            _isDebug = isDebug;
            _module = module;
            _staticData = new StaticDataManager();

#if DEBUG
            if (XmlILTrace.IsEnabled)
                XmlILTrace.PrepareTraceWriter("dump.il");
#endif
        }

        /// <summary>
        /// Begin generating code within a new method.
        /// </summary>
        // SxS note: Using hardcoded "dump.il" is an SxS issue. Since we are doing this ONLY in debug builds
        // and only for tracing purposes and MakeVersionSafeName does not seem to be able to handle file
        // extensions correctly I decided to suppress the SxS message (as advised by SxS guys).
        public void MethodBegin(MethodBase methInfo, ISourceLineInfo? sourceInfo, bool initWriters)
        {
            _methInfo = methInfo;
            _ilgen = XmlILModule.DefineMethodBody(methInfo);
            _lastSourceInfo = null;

#if DEBUG
            if (XmlILTrace.IsEnabled)
            {
                _numLocals = 0;
                _symbols = new Hashtable();
                _lblNum = 0;
                _sourceFile = null;

                _writerDump = XmlILTrace.GetTraceWriter("dump.il");
                _writerDump!.WriteLine(".method {0}()", methInfo.Name);
                _writerDump.WriteLine("{");
            }
#endif

            if (_isDebug)
            {
                DebugStartScope();

                // DebugInfo: Sequence point just before generating code for this function
                if (sourceInfo != null)
                {
                    // Don't call DebugSequencePoint, as it puts Nop *before* the sequence point.  That is
                    // wrong in this case, because we need source line information to be emitted before any
                    // IL instruction so that stepping into this function won't end up in the assembly window.
                    // We still guarantee that:
                    //   1. Two sequence points are never adjacent, since this is the 1st sequence point
                    //   2. Stack depth is 0, since this is the very beginning of the method
                    MarkSequencePoint(sourceInfo);
                    Emit(OpCodes.Nop);
                }
            }
            else if (_module.EmitSymbols)
            {
                // For a retail build, put source information on methods only
                if (sourceInfo != null)
                {
                    MarkSequencePoint(sourceInfo);
                    // Set this.lastSourceInfo back to null to prevent generating additional sequence points
                    // in this method.
                    _lastSourceInfo = null;
                }
            }

            _initWriters = false;
            if (initWriters)
            {
                EnsureWriter();
                LoadQueryRuntime();
                Call(XmlILMethods.GetOutput);
                Emit(OpCodes.Stloc, _locXOut);
            }
        }

        /// <summary>
        /// Generate "ret" instruction and branch fixup jump table.
        /// </summary>
        public void MethodEnd()
        {
            Emit(OpCodes.Ret);

#if DEBUG
            if (XmlILTrace.IsEnabled)
            {
                _writerDump!.WriteLine("}");
                _writerDump.WriteLine("");
                _writerDump.Close();
            }
#endif

            if (_isDebug)
                DebugEndScope();
        }


        //-----------------------------------------------
        // Helper Global Methods
        //-----------------------------------------------

        /// <summary>
        /// Call a static method which attempts to reuse a navigator.
        /// </summary>
        public void CallSyncToNavigator()
        {
            // Get helper method from module
            if (_methSyncToNav == null)
                _methSyncToNav = _module.FindMethod("SyncToNavigator");

            Call(_methSyncToNav!);
        }

        //-----------------------------------------------
        // StaticDataManager
        //-----------------------------------------------

        /// <summary>
        /// This internal class manages literal names, literal types, and storage for global variables.
        /// </summary>
        public StaticDataManager StaticData
        {
            get { return _staticData; }
        }


        //-----------------------------------------------
        // Constants
        //-----------------------------------------------

        /// <summary>
        /// Generate the optimal Ldc_I4 instruction based on intVal.
        /// </summary>
        public void LoadInteger(int intVal)
        {
            Emit(OpCodes.Ldc_I4, intVal);
        }

        public void LoadBoolean(bool boolVal)
        {
            Emit(boolVal ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        }

        public void LoadType(Type clrTyp)
        {
            Emit(OpCodes.Ldtoken, clrTyp);
            Call(XmlILMethods.GetTypeFromHandle);
        }


        //-----------------------------------------------
        // Local variables
        //-----------------------------------------------

        /// <summary>
        /// Generate a new local variable.  Add a numeric suffix to name that ensures that all
        /// local variable names will be unique (for readability).
        /// </summary>
        public LocalBuilder DeclareLocal(string name, Type type)
        {
            LocalBuilder locBldr = _ilgen!.DeclareLocal(type);
#if DEBUG
            if (XmlILTrace.IsEnabled)
            {
                _symbols!.Add(locBldr, name + _numLocals.ToString(CultureInfo.InvariantCulture));
                _numLocals++;
            }
#endif
            return locBldr;
        }

        public void LoadQueryRuntime()
        {
            Emit(OpCodes.Ldarg_0);
        }

        public void LoadQueryContext()
        {
            Emit(OpCodes.Ldarg_0);
            Call(XmlILMethods.Context);
        }

        public void LoadXsltLibrary()
        {
            Emit(OpCodes.Ldarg_0);
            Call(XmlILMethods.XsltLib);
        }

        public void LoadQueryOutput()
        {
            Emit(OpCodes.Ldloc, _locXOut!);
        }


        //-----------------------------------------------
        // Parameters
        //-----------------------------------------------

        public void LoadParameter(int paramPos)
        {
            if (paramPos <= ushort.MaxValue)
            {
                Emit(OpCodes.Ldarg, paramPos);
            }
            else
            {
                throw new XslTransformException(SR.XmlIl_TooManyParameters);
            }
        }

        public void SetParameter(object paramId)
        {
            int paramPos = (int)paramId;

            if (paramPos <= ushort.MaxValue)
            {
                Emit(OpCodes.Starg, (int)paramPos);
            }
            else
            {
                throw new XslTransformException(SR.XmlIl_TooManyParameters);
            }
        }

        //-----------------------------------------------
        // Labels
        //-----------------------------------------------

        /// <summary>
        /// Branch to lblBranch and anchor lblMark.  If lblBranch = lblMark, then no need
        /// to generate a "br" to the next instruction.
        /// </summary>
        public void BranchAndMark(Label lblBranch, Label lblMark)
        {
            if (!lblBranch.Equals(lblMark))
            {
                EmitUnconditionalBranch(OpCodes.Br, lblBranch);
            }
            MarkLabel(lblMark);
        }


        //-----------------------------------------------
        // Comparison
        //-----------------------------------------------

        /// <summary>
        /// Compare the top value on the stack with the specified i4 using the specified relational
        /// comparison opcode, and branch to lblBranch if the result is true.
        /// </summary>
        public void TestAndBranch(int i4, Label lblBranch, OpCode opcodeBranch)
        {
            switch (i4)
            {
                case 0:
                    // Beq or Bne can be shortened to Brfalse or Brtrue if comparing to 0
                    if (opcodeBranch.Value == OpCodes.Beq.Value)
                        opcodeBranch = OpCodes.Brfalse;
                    else if (opcodeBranch.Value == OpCodes.Beq_S.Value)
                        opcodeBranch = OpCodes.Brfalse_S;
                    else if (opcodeBranch.Value == OpCodes.Bne_Un.Value)
                        opcodeBranch = OpCodes.Brtrue;
                    else if (opcodeBranch.Value == OpCodes.Bne_Un_S.Value)
                        opcodeBranch = OpCodes.Brtrue_S;
                    else
                        goto default;
                    break;

                default:
                    // Cannot use shortcut, so push integer onto the stack
                    LoadInteger(i4);
                    break;
            }

            Emit(opcodeBranch, lblBranch);
        }

        /// <summary>
        /// Assume a branch instruction has already been issued.  If isTrueBranch is true, then the
        /// true path is linked to lblBranch.  Otherwise, the false path is linked to lblBranch.
        /// Convert this "branching" boolean logic into an explicit push of 1 or 0 onto the stack.
        /// </summary>
        public void ConvBranchToBool(Label lblBranch, bool isTrueBranch)
        {
            Label lblDone = DefineLabel();

            Emit(isTrueBranch ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);
            EmitUnconditionalBranch(OpCodes.Br_S, lblDone);
            MarkLabel(lblBranch);
            Emit(isTrueBranch ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            MarkLabel(lblDone);
        }


        //-----------------------------------------------
        // Frequently used method and function calls
        //-----------------------------------------------

        public void TailCall(MethodInfo meth)
        {
            Emit(OpCodes.Tailcall);
            Call(meth);
            Emit(OpCodes.Ret);
        }

#pragma warning disable CA1822
        [Conditional("DEBUG")]
        private void TraceCall(OpCode opcode, MethodInfo meth)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
            {
                StringBuilder strBldr = new StringBuilder();
                bool isFirst = true;
                string retType = "";

                if (!(meth is MethodBuilder))
                {
                    foreach (ParameterInfo paramInfo in meth.GetParameters())
                    {
                        if (isFirst)
                            isFirst = false;
                        else
                            strBldr.Append(", ");
                        strBldr.Append(paramInfo.ParameterType.Name);
                    }
                    retType = meth.ReturnType.Name;
                }

                _writerDump!.WriteLine("  {0, -10} {1} {2}({3})", new object?[] { opcode.Name, retType, meth.Name, strBldr.ToString() });
            }
#endif
        }
#pragma warning restore CA1822

        public void Call(MethodInfo meth)
        {
            OpCode opcode = meth.IsVirtual || meth.IsAbstract ? OpCodes.Callvirt : OpCodes.Call;

            TraceCall(opcode, meth);
            _ilgen!.Emit(opcode, meth);

            if (_lastSourceInfo != null)
            {
                // Emit a "no source" sequence point, otherwise the debugger would return to the wrong line
                // once the call has finished.  We are guaranteed not to emit adjacent sequence points because
                // the Call instruction precedes this sequence point, and a nop instruction precedes other
                // sequence points.
                MarkSequencePoint(SourceLineInfo.NoSource);
            }
        }

        public void Construct(ConstructorInfo constr)
        {
            Emit(OpCodes.Newobj, constr);
        }

        public void CallConcatStrings(int cStrings)
        {
            switch (cStrings)
            {
                case 0:
                    Emit(OpCodes.Ldstr, "");
                    break;
                case 1:
                    break;
                case 2:
                    Call(XmlILMethods.StrCat2);
                    break;
                case 3:
                    Call(XmlILMethods.StrCat3);
                    break;
                case 4:
                    Call(XmlILMethods.StrCat4);
                    break;
                default:
                    Debug.Fail("Shouldn't be called");
                    break;
            }
        }

        /// <summary>
        /// Assume that an object reference is on the IL stack.  Change the static Clr type from "clrTypeSrc" to "clrTypeDst"
        /// </summary>
        public void TreatAs(Type clrTypeSrc, Type clrTypeDst)
        {
            // If source = destination, then no-op
            if (clrTypeSrc == clrTypeDst)
                return;

            if (clrTypeSrc.IsValueType)
            {
                // If source is a value type, then destination may only be typeof(object), so box
                Debug.Assert(clrTypeDst == typeof(object), "Invalid cast, since value types do not allow inheritance.");
                Emit(OpCodes.Box, clrTypeSrc);
            }
            else if (clrTypeDst.IsValueType)
            {
                // If destination type is value type, then source may only be typeof(object), so unbox
                Debug.Assert(clrTypeSrc == typeof(object), "Invalid cast, since value types do not allow inheritance.");
                Emit(OpCodes.Unbox, clrTypeDst);
                Emit(OpCodes.Ldobj, clrTypeDst);
            }
            else if (clrTypeDst != typeof(object))
            {
                // If source is not a value type, and destination type is typeof(object), then no-op
                // Otherwise, use Castclass to change the static type
                Debug.Assert(clrTypeSrc.IsAssignableFrom(clrTypeDst) || clrTypeDst.IsAssignableFrom(clrTypeSrc),
                             "Invalid cast, since source type and destination type are not in same inheritance hierarchy.");
                Emit(OpCodes.Castclass, clrTypeDst);
            }
        }


        //-----------------------------------------------
        // Datatype methods
        //-----------------------------------------------

        public void ConstructLiteralDecimal(decimal dec)
        {
            if (dec >= (decimal)int.MinValue && dec <= (decimal)int.MaxValue && decimal.Truncate(dec) == dec)
            {
                // Decimal can be constructed from a 32-bit integer
                LoadInteger((int)dec);
                Construct(XmlILConstructors.DecFromInt32);
            }
            else
            {
                int[] bits = decimal.GetBits(dec);

                LoadInteger(bits[0]);
                LoadInteger(bits[1]);
                LoadInteger(bits[2]);
                LoadBoolean(bits[3] < 0);
                LoadInteger(bits[3] >> 16);
                Construct(XmlILConstructors.DecFromParts);
            }
        }

        public void ConstructLiteralQName(string localName, string namespaceName)
        {
            Emit(OpCodes.Ldstr, localName);
            Emit(OpCodes.Ldstr, namespaceName);
            Construct(XmlILConstructors.QName);
        }

        public void CallArithmeticOp(QilNodeType opType, XmlTypeCode code)
        {
            MethodInfo? meth = null;

            switch (code)
            {
                case XmlTypeCode.Int:
                case XmlTypeCode.Integer:
                case XmlTypeCode.Double:
                case XmlTypeCode.Float:
                    switch (opType)
                    {
                        case QilNodeType.Add: Emit(OpCodes.Add); break;
                        case QilNodeType.Subtract: Emit(OpCodes.Sub); break;
                        case QilNodeType.Multiply: Emit(OpCodes.Mul); break;
                        case QilNodeType.Divide: Emit(OpCodes.Div); break;
                        case QilNodeType.Modulo: Emit(OpCodes.Rem); break;
                        case QilNodeType.Negate: Emit(OpCodes.Neg); break;
                        default: Debug.Fail($"{opType} must be an arithmetic operation."); break;
                    }
                    break;

                case XmlTypeCode.Decimal:
                    switch (opType)
                    {
                        case QilNodeType.Add: meth = XmlILMethods.DecAdd; break;
                        case QilNodeType.Subtract: meth = XmlILMethods.DecSub; break;
                        case QilNodeType.Multiply: meth = XmlILMethods.DecMul; break;
                        case QilNodeType.Divide: meth = XmlILMethods.DecDiv; break;
                        case QilNodeType.Modulo: meth = XmlILMethods.DecRem; break;
                        case QilNodeType.Negate: meth = XmlILMethods.DecNeg; break;
                        default: Debug.Fail($"{opType} must be an arithmetic operation."); break;
                    }

                    Call(meth);
                    break;

                default:
                    Debug.Fail($"The {opType} arithmetic operation cannot be performed on values of type {code}.");
                    break;
            }
        }

        public void CallCompareEquals(XmlTypeCode code)
        {
            MethodInfo? meth = null;

            switch (code)
            {
                case XmlTypeCode.String: meth = XmlILMethods.StrEq; break;
                case XmlTypeCode.QName: meth = XmlILMethods.QNameEq; break;
                case XmlTypeCode.Decimal: meth = XmlILMethods.DecEq; break;
                default:
                    Debug.Fail($"Type {code} does not support the equals operation.");
                    break;
            }

            Call(meth);
        }

        public void CallCompare(XmlTypeCode code)
        {
            MethodInfo? meth = null;

            switch (code)
            {
                case XmlTypeCode.String: meth = XmlILMethods.StrCmp; break;
                case XmlTypeCode.Decimal: meth = XmlILMethods.DecCmp; break;
                default:
                    Debug.Fail($"Type {code} does not support the equals operation.");
                    break;
            }

            Call(meth);
        }


        //-----------------------------------------------
        // XmlQueryRuntime function calls
        //-----------------------------------------------

        public void CallStartRtfConstruction(string baseUri)
        {
            EnsureWriter();
            LoadQueryRuntime();
            Emit(OpCodes.Ldstr, baseUri);
            Emit(OpCodes.Ldloca, _locXOut);
            Call(XmlILMethods.StartRtfConstr);
        }

        public void CallEndRtfConstruction()
        {
            LoadQueryRuntime();
            Emit(OpCodes.Ldloca, _locXOut!);
            Call(XmlILMethods.EndRtfConstr);
        }

        public void CallStartSequenceConstruction()
        {
            EnsureWriter();
            LoadQueryRuntime();
            Emit(OpCodes.Ldloca, _locXOut);
            Call(XmlILMethods.StartSeqConstr);
        }

        public void CallEndSequenceConstruction()
        {
            LoadQueryRuntime();
            Emit(OpCodes.Ldloca, _locXOut!);
            Call(XmlILMethods.EndSeqConstr);
        }

        public void CallGetEarlyBoundObject(int idxObj, Type clrType)
        {
            LoadQueryRuntime();
            LoadInteger(idxObj);
            Call(XmlILMethods.GetEarly);
            TreatAs(typeof(object), clrType);
        }

        public void CallGetAtomizedName(int idxName)
        {
            LoadQueryRuntime();
            LoadInteger(idxName);
            Call(XmlILMethods.GetAtomizedName);
        }

        public void CallGetNameFilter(int idxFilter)
        {
            LoadQueryRuntime();
            LoadInteger(idxFilter);
            Call(XmlILMethods.GetNameFilter);
        }

        public void CallGetTypeFilter(XPathNodeType nodeType)
        {
            LoadQueryRuntime();
            LoadInteger((int)nodeType);
            Call(XmlILMethods.GetTypeFilter);
        }

        public void CallParseTagName(GenerateNameType nameType)
        {
            if (nameType == GenerateNameType.TagNameAndMappings)
            {
                Call(XmlILMethods.TagAndMappings);
            }
            else
            {
                Debug.Assert(nameType == GenerateNameType.TagNameAndNamespace);
                Call(XmlILMethods.TagAndNamespace);
            }
        }

        public void CallGetGlobalValue(int idxValue, Type clrType)
        {
            LoadQueryRuntime();
            LoadInteger(idxValue);
            Call(XmlILMethods.GetGlobalValue);
            TreatAs(typeof(object), clrType);
        }

        public void CallSetGlobalValue(Type clrType)
        {
            TreatAs(clrType, typeof(object));
            Call(XmlILMethods.SetGlobalValue);
        }

        public void CallGetCollation(int idxName)
        {
            LoadQueryRuntime();
            LoadInteger(idxName);
            Call(XmlILMethods.GetCollation);
        }

        [MemberNotNull(nameof(_locXOut))]
        private void EnsureWriter()
        {
            // If write variable has not yet been initialized, do it now
            if (!_initWriters)
            {
                _locXOut = DeclareLocal("$$$xwrtChk", typeof(XmlQueryOutput));
                _initWriters = true;
            }

            Debug.Assert(_locXOut != null);
        }


        //-----------------------------------------------
        // XmlQueryContext function calls
        //-----------------------------------------------

        public void CallGetParameter(string localName, string namespaceUri)
        {
            LoadQueryContext();
            Emit(OpCodes.Ldstr, localName);
            Emit(OpCodes.Ldstr, namespaceUri);
            Call(XmlILMethods.GetParam);
        }

        //-----------------------------------------------
        // XmlQueryOutput function calls
        //-----------------------------------------------

        public void CallStartTree(XPathNodeType rootType)
        {
            LoadQueryOutput();
            LoadInteger((int)rootType);
            Call(XmlILMethods.StartTree);
        }

        public void CallEndTree()
        {
            LoadQueryOutput();
            Call(XmlILMethods.EndTree);
        }

        public void CallWriteStartRoot()
        {
            // Call XmlQueryOutput.WriteStartRoot
            LoadQueryOutput();
            Call(XmlILMethods.StartRoot);
        }

        public void CallWriteEndRoot()
        {
            // Call XmlQueryOutput.WriteEndRoot
            LoadQueryOutput();
            Call(XmlILMethods.EndRoot);
        }

        public void CallWriteStartElement(GenerateNameType nameType, bool callChk)
        {
            MethodInfo? meth = null;

            // If runtime checks need to be made,
            if (callChk)
            {
                // Then call XmlQueryOutput.WriteStartElement
                switch (nameType)
                {
                    case GenerateNameType.LiteralLocalName: meth = XmlILMethods.StartElemLocName; break;
                    case GenerateNameType.LiteralName: meth = XmlILMethods.StartElemLitName; break;
                    case GenerateNameType.CopiedName: meth = XmlILMethods.StartElemCopyName; break;
                    case GenerateNameType.TagNameAndMappings: meth = XmlILMethods.StartElemMapName; break;
                    case GenerateNameType.TagNameAndNamespace: meth = XmlILMethods.StartElemNmspName; break;
                    case GenerateNameType.QName: meth = XmlILMethods.StartElemQName; break;
                    default: Debug.Fail($"{nameType} is invalid here."); break;
                }
            }
            else
            {
                // Else call XmlQueryOutput.WriteStartElementUnchecked
                switch (nameType)
                {
                    case GenerateNameType.LiteralLocalName: meth = XmlILMethods.StartElemLocNameUn; break;
                    case GenerateNameType.LiteralName: meth = XmlILMethods.StartElemLitNameUn; break;
                    default: Debug.Fail($"{nameType} is invalid here."); break;
                }
            }

            Call(meth);
        }

        public void CallWriteEndElement(GenerateNameType nameType, bool callChk)
        {
            MethodInfo? meth = null;

            // If runtime checks need to be made,
            if (callChk)
            {
                // Then call XmlQueryOutput.WriteEndElement
                meth = XmlILMethods.EndElemStackName;
            }
            else
            {
                // Else call XmlQueryOutput.WriteEndElementUnchecked
                switch (nameType)
                {
                    case GenerateNameType.LiteralLocalName: meth = XmlILMethods.EndElemLocNameUn; break;
                    case GenerateNameType.LiteralName: meth = XmlILMethods.EndElemLitNameUn; break;
                    default: Debug.Fail($"{nameType} is invalid here."); break;
                }
            }

            Call(meth);
        }

        public void CallStartElementContent()
        {
            LoadQueryOutput();
            Call(XmlILMethods.StartContentUn);
        }

        public void CallWriteStartAttribute(GenerateNameType nameType, bool callChk)
        {
            MethodInfo? meth = null;

            // If runtime checks need to be made,
            if (callChk)
            {
                // Then call XmlQueryOutput.WriteStartAttribute
                switch (nameType)
                {
                    case GenerateNameType.LiteralLocalName: meth = XmlILMethods.StartAttrLocName; break;
                    case GenerateNameType.LiteralName: meth = XmlILMethods.StartAttrLitName; break;
                    case GenerateNameType.CopiedName: meth = XmlILMethods.StartAttrCopyName; break;
                    case GenerateNameType.TagNameAndMappings: meth = XmlILMethods.StartAttrMapName; break;
                    case GenerateNameType.TagNameAndNamespace: meth = XmlILMethods.StartAttrNmspName; break;
                    case GenerateNameType.QName: meth = XmlILMethods.StartAttrQName; break;
                    default: Debug.Fail($"{nameType} is invalid here."); break;
                }
            }
            else
            {
                // Else call XmlQueryOutput.WriteStartAttributeUnchecked
                switch (nameType)
                {
                    case GenerateNameType.LiteralLocalName: meth = XmlILMethods.StartAttrLocNameUn; break;
                    case GenerateNameType.LiteralName: meth = XmlILMethods.StartAttrLitNameUn; break;
                    default: Debug.Fail($"{nameType} is invalid here."); break;
                }
            }

            Call(meth);
        }

        public void CallWriteEndAttribute(bool callChk)
        {
            LoadQueryOutput();

            // If runtime checks need to be made,
            if (callChk)
            {
                // Then call XmlQueryOutput.WriteEndAttribute
                Call(XmlILMethods.EndAttr);
            }
            else
            {
                // Else call XmlQueryOutput.WriteEndAttributeUnchecked
                Call(XmlILMethods.EndAttrUn);
            }
        }

        public void CallWriteNamespaceDecl(bool callChk)
        {
            // If runtime checks need to be made,
            if (callChk)
            {
                // Then call XmlQueryOutput.WriteNamespaceDeclaration
                Call(XmlILMethods.NamespaceDecl);
            }
            else
            {
                // Else call XmlQueryOutput.WriteNamespaceDeclarationUnchecked
                Call(XmlILMethods.NamespaceDeclUn);
            }
        }

        public void CallWriteString(bool disableOutputEscaping, bool callChk)
        {
            // If runtime checks need to be made,
            if (callChk)
            {
                // Then call XmlQueryOutput.WriteString, or XmlQueryOutput.WriteRaw
                if (disableOutputEscaping)
                    Call(XmlILMethods.NoEntText);
                else
                    Call(XmlILMethods.Text);
            }
            else
            {
                // Else call XmlQueryOutput.WriteStringUnchecked, or XmlQueryOutput.WriteRawUnchecked
                if (disableOutputEscaping)
                    Call(XmlILMethods.NoEntTextUn);
                else
                    Call(XmlILMethods.TextUn);
            }
        }

        public void CallWriteStartPI()
        {
            Call(XmlILMethods.StartPI);
        }

        public void CallWriteEndPI()
        {
            LoadQueryOutput();
            Call(XmlILMethods.EndPI);
        }

        public void CallWriteStartComment()
        {
            LoadQueryOutput();
            Call(XmlILMethods.StartComment);
        }

        public void CallWriteEndComment()
        {
            LoadQueryOutput();
            Call(XmlILMethods.EndComment);
        }


        //-----------------------------------------------
        // Item caching methods
        //-----------------------------------------------

        public void CallCacheCount(Type itemStorageType)
        {
            XmlILStorageMethods meth = XmlILMethods.StorageMethods[itemStorageType];
            Call(meth.IListCount);
        }

        public void CallCacheItem(Type itemStorageType)
        {
            Call(XmlILMethods.StorageMethods[itemStorageType].IListItem);
        }


        //-----------------------------------------------
        // XPathItem properties and methods
        //-----------------------------------------------

        public void CallValueAs(Type clrType)
        {
            MethodInfo? meth;

            meth = XmlILMethods.StorageMethods[clrType].ValueAs;
            if (meth == null)
            {
                // Call (Type) item.ValueAs(Type, null)
                LoadType(clrType);
                Emit(OpCodes.Ldnull);
                Call(XmlILMethods.ValueAsAny);

                // Unbox or down-cast
                TreatAs(typeof(object), clrType);
            }
            else
            {
                // Call strongly typed ValueAs method
                Call(meth);
            }
        }


        //-----------------------------------------------
        // XmlSortKeyAccumulator methods
        //-----------------------------------------------

        public void AddSortKey(XmlQueryType? keyType)
        {
            MethodInfo? meth = null;

            if (keyType == null)
            {
                meth = XmlILMethods.SortKeyEmpty;
            }
            else
            {
                Debug.Assert(keyType.IsAtomicValue, "Sort key must have atomic value type.");

                switch (keyType.TypeCode)
                {
                    case XmlTypeCode.String: meth = XmlILMethods.SortKeyString; break;
                    case XmlTypeCode.Decimal: meth = XmlILMethods.SortKeyDecimal; break;
                    case XmlTypeCode.Integer: meth = XmlILMethods.SortKeyInteger; break;
                    case XmlTypeCode.Int: meth = XmlILMethods.SortKeyInt; break;
                    case XmlTypeCode.Boolean: meth = XmlILMethods.SortKeyInt; break;
                    case XmlTypeCode.Double: meth = XmlILMethods.SortKeyDouble; break;
                    case XmlTypeCode.DateTime: meth = XmlILMethods.SortKeyDateTime; break;

                    case XmlTypeCode.None:
                        // Empty sequence, so this path will never actually be taken
                        Emit(OpCodes.Pop);
                        meth = XmlILMethods.SortKeyEmpty;
                        break;

                    case XmlTypeCode.AnyAtomicType:
                        Debug.Fail("Heterogenous sort key is not allowed.");
                        return;

                    default:
                        Debug.Fail($"Sorting over datatype {keyType.TypeCode} is not allowed.");
                        break;
                }
            }

            Call(meth);
        }


        //-----------------------------------------------
        // Debugging information output
        //-----------------------------------------------

        /// <summary>
        /// Begin a new variable debugging scope.
        /// </summary>
        public void DebugStartScope()
        {
            _ilgen!.BeginScope();
        }

        /// <summary>
        /// End a new debugging scope.
        /// </summary>
        public void DebugEndScope()
        {
            _ilgen!.EndScope();
        }

        /// <summary>
        /// Correlate the current IL generation position with the current source position.
        /// </summary>
        public void DebugSequencePoint(ISourceLineInfo sourceInfo)
        {
            Debug.Assert(_isDebug && _lastSourceInfo != null);
            Debug.Assert(sourceInfo != null);

            // When emitting sequence points, be careful to always follow two rules:
            // 1. Never emit adjacent sequence points, as this messes up the debugger.  We guarantee this by
            //    always emitting a Nop before every sequence point.
            // 2. The runtime enforces a rule that BP sequence points can only appear at zero stack depth,
            //    or if a NOP instruction is placed before them.  We guarantee this by always emitting a Nop
            //    before every sequence point.
            //    <spec>http://devdiv/Documents/Whidbey/CLR/CurrentSpecs/Debugging%20and%20Profiling/JIT-Determined%20Sequence%20Points.doc</spec>
            Emit(OpCodes.Nop);
            MarkSequencePoint(sourceInfo);
        }

        private string? _lastUriString;
        private string? _lastFileName;

        // SQLBUDT 278010: debugger does not work with network paths in uri format, like file://server/share/dir/file
        private string GetFileName(ISourceLineInfo sourceInfo)
        {
            string uriString = sourceInfo.Uri!;
            if ((object)uriString == (object?)_lastUriString)
            {
                return _lastFileName!;
            }

            _lastUriString = uriString;
            _lastFileName = SourceLineInfo.GetFileName(uriString);
            return _lastFileName;
        }

        private void MarkSequencePoint(ISourceLineInfo sourceInfo)
        {
            Debug.Assert(_module.EmitSymbols);

            // Do not emit adjacent 0xfeefee sequence points, as that slows down stepping in the debugger
            if (sourceInfo.IsNoSource && _lastSourceInfo != null && _lastSourceInfo.IsNoSource)
            {
                return;
            }

            string sourceFile = GetFileName(sourceInfo);

#if DEBUG
            if (XmlILTrace.IsEnabled)
            {
                if (sourceInfo.IsNoSource)
                    _writerDump!.WriteLine("//[no source]");
                else
                {
                    if (sourceFile != _sourceFile)
                    {
                        _sourceFile = sourceFile;
                        _writerDump!.WriteLine("// Source File '{0}'", _sourceFile);
                    }
                    _writerDump!.WriteLine("//[{0},{1} -- {2},{3}]", sourceInfo.Start.Line, sourceInfo.Start.Pos, sourceInfo.End.Line, sourceInfo.End.Pos);
                }
            }
#endif
            //ISymbolDocumentWriter symDoc = this.module.AddSourceDocument(sourceFile);
            //this.ilgen.MarkSequencePoint(symDoc, sourceInfo.Start.Line, sourceInfo.Start.Pos, sourceInfo.End.Line, sourceInfo.End.Pos);
            _lastSourceInfo = sourceInfo;
        }


        //-----------------------------------------------
        // Pass through to ILGenerator
        //-----------------------------------------------

        public Label DefineLabel()
        {
            Label lbl = _ilgen!.DefineLabel();

#if DEBUG
            if (XmlILTrace.IsEnabled)
                _symbols!.Add(lbl, ++_lblNum);
#endif

            return lbl;
        }

        public void MarkLabel(Label lbl)
        {
            if (_lastSourceInfo != null && !_lastSourceInfo.IsNoSource)
            {
                // Emit a "no source" sequence point, otherwise the debugger would show
                // a wrong line if we jumped to this label from another place
                DebugSequencePoint(SourceLineInfo.NoSource);
            }

#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("Label {0}:", _symbols![lbl]);
#endif

            _ilgen!.MarkLabel(lbl);
        }

        public void Emit(OpCode opcode)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0}", opcode.Name);
#endif
            _ilgen!.Emit(opcode);
        }

        public void Emit(OpCode opcode, byte byteVal)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0, -10} {1}", opcode.Name, byteVal);
#endif
            _ilgen!.Emit(opcode, byteVal);
        }

        public void Emit(OpCode opcode, ConstructorInfo constrInfo)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0, -10} {1}", opcode.Name, constrInfo);
#endif
            _ilgen!.Emit(opcode, constrInfo);
        }

        public void Emit(OpCode opcode, double dblVal)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0, -10} {1}", opcode.Name, dblVal);
#endif
            _ilgen!.Emit(opcode, dblVal);
        }

        public void Emit(OpCode opcode, FieldInfo fldInfo)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0, -10} {1}", opcode.Name, fldInfo.Name);
#endif
            _ilgen!.Emit(opcode, fldInfo);
        }

        public void Emit(OpCode opcode, int intVal)
        {
            Debug.Assert(opcode.OperandType == OperandType.InlineI || opcode.OperandType == OperandType.InlineVar);
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0, -10} {1}", opcode.Name, intVal);
#endif
            _ilgen!.Emit(opcode, intVal);
        }

        public void Emit(OpCode opcode, long longVal)
        {
            Debug.Assert(opcode.OperandType == OperandType.InlineI8);
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0, -10} {1}", opcode.Name, longVal);
#endif
            _ilgen!.Emit(opcode, longVal);
        }

        public void Emit(OpCode opcode, Label lblVal)
        {
            Debug.Assert(!opcode.Equals(OpCodes.Br) && !opcode.Equals(OpCodes.Br_S), "Use EmitUnconditionalBranch and be careful not to emit unverifiable code.");
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0, -10} Label {1}", opcode.Name, _symbols![lblVal]);
#endif
            _ilgen!.Emit(opcode, lblVal);
        }

        public void Emit(OpCode opcode, Label[] arrLabels)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
            {
                _writerDump!.Write("  {0, -10} (Label {1}", opcode.Name, arrLabels.Length != 0 ? _symbols![arrLabels[0]]!.ToString() : "");
                for (int i = 1; i < arrLabels.Length; i++)
                {
                    _writerDump.Write(", Label {0}", _symbols![arrLabels[i]]);
                }
                _writerDump.WriteLine(")");
            }
#endif
            _ilgen!.Emit(opcode, arrLabels);
        }

        public void Emit(OpCode opcode, LocalBuilder locBldr)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0, -10} {1} ({2})", opcode.Name, _symbols![locBldr], locBldr.LocalType.Name);
#endif
            _ilgen!.Emit(opcode, locBldr);
        }

        public void Emit(OpCode opcode, sbyte sbyteVal)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0, -10} {1}", opcode.Name, sbyteVal);
#endif
            _ilgen!.Emit(opcode, sbyteVal);
        }

        public void Emit(OpCode opcode, string strVal)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0, -10} \"{1}\"", opcode.Name, strVal);
#endif
            _ilgen!.Emit(opcode, strVal);
        }

        public void Emit(OpCode opcode, Type typVal)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0, -10} {1}", opcode.Name, typVal);
#endif
            _ilgen!.Emit(opcode, typVal);
        }

        /// <summary>
        /// Unconditional branch opcodes (OpCode.Br, OpCode.Br_S) can lead to unverifiable code in the following cases:
        ///
        ///   # DEAD CODE CASE
        ///     ldc_i4  1       # Stack depth == 1
        ///     br      Label2
        ///   Label1:
        ///     nop             # Dead code, so IL rules assume stack depth == 0.  This causes a verification error,
        ///                     # since next instruction has depth == 1
        ///   Label2:
        ///     pop             # Stack depth == 1
        ///     ret
        ///
        ///   # LATE BRANCH CASE
        ///     ldc_i4  1       # Stack depth == 1
        ///     br      Label2
        ///   Label1:
        ///     nop             # Not dead code, but since branch comes from below, IL rules assume stack depth = 0.
        ///                     # This causes a verification error, since next instruction has depth == 1
        ///   Label2:
        ///     pop             # Stack depth == 1
        ///     ret
        ///   Label3:
        ///     br      Label1  # Stack depth == 1
        ///
        /// This method works around the above limitations by using Brtrue or Brfalse in the following way:
        ///
        ///     ldc_i4  1       # Since this test is always true, this is a way of creating a path to the code that
        ///     brtrue  Label   # follows the brtrue instruction.
        ///
        ///     ldc_i4  1       # Since this test is always false, this is a way of creating a path to the code that
        ///     brfalse Label   # starts at Label.
        ///
        /// 1. If opcode == Brtrue or Brtrue_S, then 1 will be pushed and brtrue instruction will be generated.
        /// 2. If opcode == Brfalse or Brfalse_S, then 1 will be pushed and brfalse instruction will be generated.
        /// 3. If opcode == Br or Br_S, then a br instruction will be generated.
        /// </summary>
        public void EmitUnconditionalBranch(OpCode opcode, Label lblTarget)
        {
            if (!opcode.Equals(OpCodes.Br) && !opcode.Equals(OpCodes.Br_S))
            {
                Debug.Assert(opcode.Equals(OpCodes.Brtrue) || opcode.Equals(OpCodes.Brtrue_S) ||
                             opcode.Equals(OpCodes.Brfalse) || opcode.Equals(OpCodes.Brfalse_S));
                Emit(OpCodes.Ldc_I4_1);
            }

#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump!.WriteLine("  {0, -10} Label {1}", opcode.Name, _symbols![lblTarget]);
#endif
            _ilgen!.Emit(opcode, lblTarget);

            if (_lastSourceInfo != null && (opcode.Equals(OpCodes.Br) || opcode.Equals(OpCodes.Br_S)))
            {
                // Emit a "no source" sequence point, otherwise the following label will be preceded
                // with a dead Nop operation, which may lead to unverifiable code (SQLBUDT 423393).
                // We are guaranteed not to emit adjacent sequence points because Br or Br_S
                // instruction precedes this sequence point, and a Nop instruction precedes other
                // sequence points.
                MarkSequencePoint(SourceLineInfo.NoSource);
            }
        }
    }
}
