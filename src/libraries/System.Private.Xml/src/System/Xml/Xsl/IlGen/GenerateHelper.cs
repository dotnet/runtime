// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace System.Xml.Xsl.IlGen
{
    /// <summary>
    /// List of all XmlIL runtime constructors.
    /// </summary>
    internal class XmlILStorageMethods
    {
        // Aggregates
        public MethodInfo AggAvg;
        public MethodInfo AggAvgResult;
        public MethodInfo AggCreate;
        public MethodInfo AggIsEmpty;
        public MethodInfo AggMax;
        public MethodInfo AggMaxResult;
        public MethodInfo AggMin;
        public MethodInfo AggMinResult;
        public MethodInfo AggSum;
        public MethodInfo AggSumResult;

        // Sequences
        public Type SeqType;
        public FieldInfo SeqEmpty;
        public MethodInfo SeqReuse;
        public MethodInfo SeqReuseSgl;
        public MethodInfo SeqAdd;
        public MethodInfo SeqSortByKeys;

        // IList<>
        public Type IListType;
        public MethodInfo IListCount;
        public MethodInfo IListItem;

        // XPathItem
        public MethodInfo ValueAs;

        // ToAtomicValue
        public MethodInfo ToAtomicValue;

        public XmlILStorageMethods(Type storageType)
        {
            // Aggregates
            if (storageType == typeof(int) || storageType == typeof(long) ||
                storageType == typeof(decimal) || storageType == typeof(double))
            {
                Type aggType = Type.GetType("System.Xml.Xsl.Runtime." + storageType.Name + "Aggregator");
                AggAvg = XmlILMethods.GetMethod(aggType, "Average");
                AggAvgResult = XmlILMethods.GetMethod(aggType, "get_AverageResult");
                AggCreate = XmlILMethods.GetMethod(aggType, "Create");
                AggIsEmpty = XmlILMethods.GetMethod(aggType, "get_IsEmpty");
                AggMax = XmlILMethods.GetMethod(aggType, "Maximum");
                AggMaxResult = XmlILMethods.GetMethod(aggType, "get_MaximumResult");
                AggMin = XmlILMethods.GetMethod(aggType, "Minimum");
                AggMinResult = XmlILMethods.GetMethod(aggType, "get_MinimumResult");
                AggSum = XmlILMethods.GetMethod(aggType, "Sum");
                AggSumResult = XmlILMethods.GetMethod(aggType, "get_SumResult");
            }

            // Sequences
            if (storageType == typeof(XPathNavigator))
            {
                SeqType = typeof(XmlQueryNodeSequence);
                SeqAdd = XmlILMethods.GetMethod(SeqType, "AddClone");
            }
            else if (storageType == typeof(XPathItem))
            {
                SeqType = typeof(XmlQueryItemSequence);
                SeqAdd = XmlILMethods.GetMethod(SeqType, "AddClone");
            }
            else
            {
                SeqType = typeof(XmlQuerySequence<>).MakeGenericType(storageType);
                SeqAdd = XmlILMethods.GetMethod(SeqType, "Add");
            }

            SeqEmpty = SeqType.GetField("Empty");
            SeqReuse = XmlILMethods.GetMethod(SeqType, "CreateOrReuse", SeqType);
            SeqReuseSgl = XmlILMethods.GetMethod(SeqType, "CreateOrReuse", SeqType, storageType);
            SeqSortByKeys = XmlILMethods.GetMethod(SeqType, "SortByKeys");

            // IList<>
            IListType = typeof(IList<>).MakeGenericType(storageType);
            IListItem = XmlILMethods.GetMethod(IListType, "get_Item");
            IListCount = XmlILMethods.GetMethod(typeof(ICollection<>).MakeGenericType(storageType), "get_Count");

            // XPathItem.ValueAsXXX
            if (storageType == typeof(string))
                ValueAs = XmlILMethods.GetMethod(typeof(XPathItem), "get_Value");
            else if (storageType == typeof(int))
                ValueAs = XmlILMethods.GetMethod(typeof(XPathItem), "get_ValueAsInt");
            else if (storageType == typeof(long))
                ValueAs = XmlILMethods.GetMethod(typeof(XPathItem), "get_ValueAsLong");
            else if (storageType == typeof(DateTime))
                ValueAs = XmlILMethods.GetMethod(typeof(XPathItem), "get_ValueAsDateTime");
            else if (storageType == typeof(double))
                ValueAs = XmlILMethods.GetMethod(typeof(XPathItem), "get_ValueAsDouble");
            else if (storageType == typeof(bool))
                ValueAs = XmlILMethods.GetMethod(typeof(XPathItem), "get_ValueAsBoolean");

            // XmlILStorageConverter.XXXToAtomicValue
            if (storageType == typeof(byte[]))
                ToAtomicValue = XmlILMethods.GetMethod(typeof(XmlILStorageConverter), "BytesToAtomicValue");
            else if (storageType != typeof(XPathItem) && storageType != typeof(XPathNavigator))
                ToAtomicValue = XmlILMethods.GetMethod(typeof(XmlILStorageConverter), storageType.Name + "ToAtomicValue");
        }
    }

    /// <summary>
    /// List of all XmlIL runtime constructors.
    /// </summary>
    internal static class XmlILConstructors
    {
        public static readonly ConstructorInfo DecFromParts = GetConstructor(typeof(decimal), typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte));
        public static readonly ConstructorInfo DecFromInt32 = GetConstructor(typeof(decimal), typeof(int));
        public static readonly ConstructorInfo DecFromInt64 = GetConstructor(typeof(decimal), typeof(long));
        public static readonly ConstructorInfo Debuggable = GetConstructor(typeof(DebuggableAttribute), typeof(DebuggableAttribute.DebuggingModes));
        public static readonly ConstructorInfo NonUserCode = GetConstructor(typeof(DebuggerNonUserCodeAttribute));
        public static readonly ConstructorInfo QName = GetConstructor(typeof(XmlQualifiedName), typeof(string), typeof(string));
        public static readonly ConstructorInfo StepThrough = GetConstructor(typeof(DebuggerStepThroughAttribute));
        public static readonly ConstructorInfo Transparent = GetConstructor(typeof(SecurityTransparentAttribute));

        private static ConstructorInfo GetConstructor(Type className)
        {
            ConstructorInfo constrInfo = className.GetConstructor(new Type[] { });
            Debug.Assert(constrInfo != null, "Constructor " + className + " cannot be null.");
            return constrInfo;
        }

        private static ConstructorInfo GetConstructor(Type className, params Type[] args)
        {
            ConstructorInfo constrInfo = className.GetConstructor(args);
            Debug.Assert(constrInfo != null, "Constructor " + className + " cannot be null.");
            return constrInfo;
        }
    }


    /// <summary>
    /// List of all XmlIL runtime methods.
    /// </summary>
    internal static class XmlILMethods
    {
        // Iterators
        public static readonly MethodInfo AncCreate = GetMethod(typeof(AncestorIterator), "Create");
        public static readonly MethodInfo AncNext = GetMethod(typeof(AncestorIterator), "MoveNext");
        public static readonly MethodInfo AncDOCreate = GetMethod(typeof(AncestorDocOrderIterator), "Create");
        public static readonly MethodInfo AncDONext = GetMethod(typeof(AncestorDocOrderIterator), "MoveNext");
        public static readonly MethodInfo AttrContentCreate = GetMethod(typeof(AttributeContentIterator), "Create");
        public static readonly MethodInfo AttrContentNext = GetMethod(typeof(AttributeContentIterator), "MoveNext");
        public static readonly MethodInfo AttrCreate = GetMethod(typeof(AttributeIterator), "Create");
        public static readonly MethodInfo AttrNext = GetMethod(typeof(AttributeIterator), "MoveNext");
        public static readonly MethodInfo ContentCreate = GetMethod(typeof(ContentIterator), "Create");
        public static readonly MethodInfo ContentNext = GetMethod(typeof(ContentIterator), "MoveNext");
        public static readonly MethodInfo ContentMergeCreate = GetMethod(typeof(ContentMergeIterator), "Create");
        public static readonly MethodInfo ContentMergeNext = GetMethod(typeof(ContentMergeIterator), "MoveNext");
        public static readonly MethodInfo DescCreate = GetMethod(typeof(DescendantIterator), "Create");
        public static readonly MethodInfo DescNext = GetMethod(typeof(DescendantIterator), "MoveNext");
        public static readonly MethodInfo DescMergeCreate = GetMethod(typeof(DescendantMergeIterator), "Create");
        public static readonly MethodInfo DescMergeNext = GetMethod(typeof(DescendantMergeIterator), "MoveNext");
        public static readonly MethodInfo DiffCreate = GetMethod(typeof(DifferenceIterator), "Create");
        public static readonly MethodInfo DiffNext = GetMethod(typeof(DifferenceIterator), "MoveNext");
        public static readonly MethodInfo DodMergeCreate = GetMethod(typeof(DodSequenceMerge), "Create");
        public static readonly MethodInfo DodMergeAdd = GetMethod(typeof(DodSequenceMerge), "AddSequence");
        public static readonly MethodInfo DodMergeSeq = GetMethod(typeof(DodSequenceMerge), "MergeSequences");
        public static readonly MethodInfo ElemContentCreate = GetMethod(typeof(ElementContentIterator), "Create");
        public static readonly MethodInfo ElemContentNext = GetMethod(typeof(ElementContentIterator), "MoveNext");
        public static readonly MethodInfo FollSibCreate = GetMethod(typeof(FollowingSiblingIterator), "Create");
        public static readonly MethodInfo FollSibNext = GetMethod(typeof(FollowingSiblingIterator), "MoveNext");
        public static readonly MethodInfo FollSibMergeCreate = GetMethod(typeof(FollowingSiblingMergeIterator), "Create");
        public static readonly MethodInfo FollSibMergeNext = GetMethod(typeof(FollowingSiblingMergeIterator), "MoveNext");
        public static readonly MethodInfo IdCreate = GetMethod(typeof(IdIterator), "Create");
        public static readonly MethodInfo IdNext = GetMethod(typeof(IdIterator), "MoveNext");
        public static readonly MethodInfo InterCreate = GetMethod(typeof(IntersectIterator), "Create");
        public static readonly MethodInfo InterNext = GetMethod(typeof(IntersectIterator), "MoveNext");
        public static readonly MethodInfo KindContentCreate = GetMethod(typeof(NodeKindContentIterator), "Create");
        public static readonly MethodInfo KindContentNext = GetMethod(typeof(NodeKindContentIterator), "MoveNext");
        public static readonly MethodInfo NmspCreate = GetMethod(typeof(NamespaceIterator), "Create");
        public static readonly MethodInfo NmspNext = GetMethod(typeof(NamespaceIterator), "MoveNext");
        public static readonly MethodInfo NodeRangeCreate = GetMethod(typeof(NodeRangeIterator), "Create");
        public static readonly MethodInfo NodeRangeNext = GetMethod(typeof(NodeRangeIterator), "MoveNext");
        public static readonly MethodInfo ParentCreate = GetMethod(typeof(ParentIterator), "Create");
        public static readonly MethodInfo ParentNext = GetMethod(typeof(ParentIterator), "MoveNext");
        public static readonly MethodInfo PrecCreate = GetMethod(typeof(PrecedingIterator), "Create");
        public static readonly MethodInfo PrecNext = GetMethod(typeof(PrecedingIterator), "MoveNext");
        public static readonly MethodInfo PreSibCreate = GetMethod(typeof(PrecedingSiblingIterator), "Create");
        public static readonly MethodInfo PreSibNext = GetMethod(typeof(PrecedingSiblingIterator), "MoveNext");
        public static readonly MethodInfo PreSibDOCreate = GetMethod(typeof(PrecedingSiblingDocOrderIterator), "Create");
        public static readonly MethodInfo PreSibDONext = GetMethod(typeof(PrecedingSiblingDocOrderIterator), "MoveNext");
        public static readonly MethodInfo SortKeyCreate = GetMethod(typeof(XmlSortKeyAccumulator), "Create");
        public static readonly MethodInfo SortKeyDateTime = GetMethod(typeof(XmlSortKeyAccumulator), "AddDateTimeSortKey");
        public static readonly MethodInfo SortKeyDecimal = GetMethod(typeof(XmlSortKeyAccumulator), "AddDecimalSortKey");
        public static readonly MethodInfo SortKeyDouble = GetMethod(typeof(XmlSortKeyAccumulator), "AddDoubleSortKey");
        public static readonly MethodInfo SortKeyEmpty = GetMethod(typeof(XmlSortKeyAccumulator), "AddEmptySortKey");
        public static readonly MethodInfo SortKeyFinish = GetMethod(typeof(XmlSortKeyAccumulator), "FinishSortKeys");
        public static readonly MethodInfo SortKeyInt = GetMethod(typeof(XmlSortKeyAccumulator), "AddIntSortKey");
        public static readonly MethodInfo SortKeyInteger = GetMethod(typeof(XmlSortKeyAccumulator), "AddIntegerSortKey");
        public static readonly MethodInfo SortKeyKeys = GetMethod(typeof(XmlSortKeyAccumulator), "get_Keys");
        public static readonly MethodInfo SortKeyString = GetMethod(typeof(XmlSortKeyAccumulator), "AddStringSortKey");
        public static readonly MethodInfo UnionCreate = GetMethod(typeof(UnionIterator), "Create");
        public static readonly MethodInfo UnionNext = GetMethod(typeof(UnionIterator), "MoveNext");
        public static readonly MethodInfo XPFollCreate = GetMethod(typeof(XPathFollowingIterator), "Create");
        public static readonly MethodInfo XPFollNext = GetMethod(typeof(XPathFollowingIterator), "MoveNext");
        public static readonly MethodInfo XPFollMergeCreate = GetMethod(typeof(XPathFollowingMergeIterator), "Create");
        public static readonly MethodInfo XPFollMergeNext = GetMethod(typeof(XPathFollowingMergeIterator), "MoveNext");
        public static readonly MethodInfo XPPrecCreate = GetMethod(typeof(XPathPrecedingIterator), "Create");
        public static readonly MethodInfo XPPrecNext = GetMethod(typeof(XPathPrecedingIterator), "MoveNext");
        public static readonly MethodInfo XPPrecDOCreate = GetMethod(typeof(XPathPrecedingDocOrderIterator), "Create");
        public static readonly MethodInfo XPPrecDONext = GetMethod(typeof(XPathPrecedingDocOrderIterator), "MoveNext");
        public static readonly MethodInfo XPPrecMergeCreate = GetMethod(typeof(XPathPrecedingMergeIterator), "Create");
        public static readonly MethodInfo XPPrecMergeNext = GetMethod(typeof(XPathPrecedingMergeIterator), "MoveNext");

        // XmlQueryRuntime
        public static readonly MethodInfo AddNewIndex = GetMethod(typeof(XmlQueryRuntime), "AddNewIndex");
        public static readonly MethodInfo ChangeTypeXsltArg = GetMethod(typeof(XmlQueryRuntime), "ChangeTypeXsltArgument", typeof(int), typeof(object), typeof(Type));
        public static readonly MethodInfo ChangeTypeXsltResult = GetMethod(typeof(XmlQueryRuntime), "ChangeTypeXsltResult");
        public static readonly MethodInfo CompPos = GetMethod(typeof(XmlQueryRuntime), "ComparePosition");
        public static readonly MethodInfo Context = GetMethod(typeof(XmlQueryRuntime), "get_ExternalContext");
        public static readonly MethodInfo CreateCollation = GetMethod(typeof(XmlQueryRuntime), "CreateCollation");
        public static readonly MethodInfo DocOrder = GetMethod(typeof(XmlQueryRuntime), "DocOrderDistinct");
        public static readonly MethodInfo EndRtfConstr = GetMethod(typeof(XmlQueryRuntime), "EndRtfConstruction");
        public static readonly MethodInfo EndSeqConstr = GetMethod(typeof(XmlQueryRuntime), "EndSequenceConstruction");
        public static readonly MethodInfo FindIndex = GetMethod(typeof(XmlQueryRuntime), "FindIndex");
        public static readonly MethodInfo GenId = GetMethod(typeof(XmlQueryRuntime), "GenerateId");
        public static readonly MethodInfo GetAtomizedName = GetMethod(typeof(XmlQueryRuntime), "GetAtomizedName");
        public static readonly MethodInfo GetCollation = GetMethod(typeof(XmlQueryRuntime), "GetCollation");
        public static readonly MethodInfo GetEarly = GetMethod(typeof(XmlQueryRuntime), "GetEarlyBoundObject");
        public static readonly MethodInfo GetNameFilter = GetMethod(typeof(XmlQueryRuntime), "GetNameFilter");
        public static readonly MethodInfo GetOutput = GetMethod(typeof(XmlQueryRuntime), "get_Output");
        public static readonly MethodInfo GetGlobalValue = GetMethod(typeof(XmlQueryRuntime), "GetGlobalValue");
        public static readonly MethodInfo GetTypeFilter = GetMethod(typeof(XmlQueryRuntime), "GetTypeFilter");
        public static readonly MethodInfo GlobalComputed = GetMethod(typeof(XmlQueryRuntime), "IsGlobalComputed");
        public static readonly MethodInfo ItemMatchesCode = GetMethod(typeof(XmlQueryRuntime), "MatchesXmlType", typeof(XPathItem), typeof(XmlTypeCode));
        public static readonly MethodInfo ItemMatchesType = GetMethod(typeof(XmlQueryRuntime), "MatchesXmlType", typeof(XPathItem), typeof(int));
        public static readonly MethodInfo QNameEqualLit = GetMethod(typeof(XmlQueryRuntime), "IsQNameEqual", typeof(XPathNavigator), typeof(int), typeof(int));
        public static readonly MethodInfo QNameEqualNav = GetMethod(typeof(XmlQueryRuntime), "IsQNameEqual", typeof(XPathNavigator), typeof(XPathNavigator));
        public static readonly MethodInfo RtfConstr = GetMethod(typeof(XmlQueryRuntime), "TextRtfConstruction");
        public static readonly MethodInfo SendMessage = GetMethod(typeof(XmlQueryRuntime), "SendMessage");
        public static readonly MethodInfo SeqMatchesCode = GetMethod(typeof(XmlQueryRuntime), "MatchesXmlType", typeof(IList<XPathItem>), typeof(XmlTypeCode));
        public static readonly MethodInfo SeqMatchesType = GetMethod(typeof(XmlQueryRuntime), "MatchesXmlType", typeof(IList<XPathItem>), typeof(int));
        public static readonly MethodInfo SetGlobalValue = GetMethod(typeof(XmlQueryRuntime), "SetGlobalValue");
        public static readonly MethodInfo StartRtfConstr = GetMethod(typeof(XmlQueryRuntime), "StartRtfConstruction");
        public static readonly MethodInfo StartSeqConstr = GetMethod(typeof(XmlQueryRuntime), "StartSequenceConstruction");
        public static readonly MethodInfo TagAndMappings = GetMethod(typeof(XmlQueryRuntime), "ParseTagName", typeof(string), typeof(int));
        public static readonly MethodInfo TagAndNamespace = GetMethod(typeof(XmlQueryRuntime), "ParseTagName", typeof(string), typeof(string));
        public static readonly MethodInfo ThrowException = GetMethod(typeof(XmlQueryRuntime), "ThrowException");
        public static readonly MethodInfo XsltLib = GetMethod(typeof(XmlQueryRuntime), "get_XsltFunctions");

        // XmlQueryContext
        public static readonly MethodInfo GetDataSource = GetMethod(typeof(XmlQueryContext), "GetDataSource");
        public static readonly MethodInfo GetDefaultDataSource = GetMethod(typeof(XmlQueryContext), "get_DefaultDataSource");
        public static readonly MethodInfo GetParam = GetMethod(typeof(XmlQueryContext), "GetParameter");
        public static readonly MethodInfo InvokeXsltLate = GetMethod(typeof(XmlQueryContext), "InvokeXsltLateBoundFunction");

        // XmlILIndex
        public static readonly MethodInfo IndexAdd = GetMethod(typeof(XmlILIndex), "Add");
        public static readonly MethodInfo IndexLookup = GetMethod(typeof(XmlILIndex), "Lookup");

        // XPathItem
        public static readonly MethodInfo ItemIsNode = GetMethod(typeof(XPathItem), "get_IsNode");
        public static readonly MethodInfo Value = GetMethod(typeof(XPathItem), "get_Value");
        public static readonly MethodInfo ValueAsAny = GetMethod(typeof(XPathItem), "ValueAs", typeof(Type), typeof(IXmlNamespaceResolver));

        // XPathNavigator
        public static readonly MethodInfo NavClone = GetMethod(typeof(XPathNavigator), "Clone");
        public static readonly MethodInfo NavLocalName = GetMethod(typeof(XPathNavigator), "get_LocalName");
        public static readonly MethodInfo NavMoveAttr = GetMethod(typeof(XPathNavigator), "MoveToAttribute", typeof(string), typeof(string));
        public static readonly MethodInfo NavMoveId = GetMethod(typeof(XPathNavigator), "MoveToId");
        public static readonly MethodInfo NavMoveParent = GetMethod(typeof(XPathNavigator), "MoveToParent");
        public static readonly MethodInfo NavMoveRoot = GetMethod(typeof(XPathNavigator), "MoveToRoot");
        public static readonly MethodInfo NavMoveTo = GetMethod(typeof(XPathNavigator), "MoveTo");
        public static readonly MethodInfo NavNmsp = GetMethod(typeof(XPathNavigator), "get_NamespaceURI");
        public static readonly MethodInfo NavPrefix = GetMethod(typeof(XPathNavigator), "get_Prefix");
        public static readonly MethodInfo NavSamePos = GetMethod(typeof(XPathNavigator), "IsSamePosition");
        public static readonly MethodInfo NavType = GetMethod(typeof(XPathNavigator), "get_NodeType");

        // XmlQueryOutput methods
        public static readonly MethodInfo StartElemLitName = GetMethod(typeof(XmlQueryOutput), "WriteStartElement", typeof(string), typeof(string), typeof(string));
        public static readonly MethodInfo StartElemLocName = GetMethod(typeof(XmlQueryOutput), "WriteStartElementLocalName", typeof(string));
        public static readonly MethodInfo EndElemStackName = GetMethod(typeof(XmlQueryOutput), "WriteEndElement");
        public static readonly MethodInfo StartAttrLitName = GetMethod(typeof(XmlQueryOutput), "WriteStartAttribute", typeof(string), typeof(string), typeof(string));
        public static readonly MethodInfo StartAttrLocName = GetMethod(typeof(XmlQueryOutput), "WriteStartAttributeLocalName", typeof(string));
        public static readonly MethodInfo EndAttr = GetMethod(typeof(XmlQueryOutput), "WriteEndAttribute");
        public static readonly MethodInfo Text = GetMethod(typeof(XmlQueryOutput), "WriteString");
        public static readonly MethodInfo NoEntText = GetMethod(typeof(XmlQueryOutput), "WriteRaw", typeof(string));

        public static readonly MethodInfo StartTree = GetMethod(typeof(XmlQueryOutput), "StartTree");
        public static readonly MethodInfo EndTree = GetMethod(typeof(XmlQueryOutput), "EndTree");

        public static readonly MethodInfo StartElemLitNameUn = GetMethod(typeof(XmlQueryOutput), "WriteStartElementUnchecked", typeof(string), typeof(string), typeof(string));
        public static readonly MethodInfo StartElemLocNameUn = GetMethod(typeof(XmlQueryOutput), "WriteStartElementUnchecked", typeof(string));
        public static readonly MethodInfo StartContentUn = GetMethod(typeof(XmlQueryOutput), "StartElementContentUnchecked");
        public static readonly MethodInfo EndElemLitNameUn = GetMethod(typeof(XmlQueryOutput), "WriteEndElementUnchecked", typeof(string), typeof(string), typeof(string));
        public static readonly MethodInfo EndElemLocNameUn = GetMethod(typeof(XmlQueryOutput), "WriteEndElementUnchecked", typeof(string));
        public static readonly MethodInfo StartAttrLitNameUn = GetMethod(typeof(XmlQueryOutput), "WriteStartAttributeUnchecked", typeof(string), typeof(string), typeof(string));
        public static readonly MethodInfo StartAttrLocNameUn = GetMethod(typeof(XmlQueryOutput), "WriteStartAttributeUnchecked", typeof(string));
        public static readonly MethodInfo EndAttrUn = GetMethod(typeof(XmlQueryOutput), "WriteEndAttributeUnchecked");
        public static readonly MethodInfo NamespaceDeclUn = GetMethod(typeof(XmlQueryOutput), "WriteNamespaceDeclarationUnchecked");
        public static readonly MethodInfo TextUn = GetMethod(typeof(XmlQueryOutput), "WriteStringUnchecked");
        public static readonly MethodInfo NoEntTextUn = GetMethod(typeof(XmlQueryOutput), "WriteRawUnchecked");

        public static readonly MethodInfo StartRoot = GetMethod(typeof(XmlQueryOutput), "WriteStartRoot");
        public static readonly MethodInfo EndRoot = GetMethod(typeof(XmlQueryOutput), "WriteEndRoot");
        public static readonly MethodInfo StartElemCopyName = GetMethod(typeof(XmlQueryOutput), "WriteStartElementComputed", typeof(XPathNavigator));
        public static readonly MethodInfo StartElemMapName = GetMethod(typeof(XmlQueryOutput), "WriteStartElementComputed", typeof(string), typeof(int));
        public static readonly MethodInfo StartElemNmspName = GetMethod(typeof(XmlQueryOutput), "WriteStartElementComputed", typeof(string), typeof(string));
        public static readonly MethodInfo StartElemQName = GetMethod(typeof(XmlQueryOutput), "WriteStartElementComputed", typeof(XmlQualifiedName));
        public static readonly MethodInfo StartAttrCopyName = GetMethod(typeof(XmlQueryOutput), "WriteStartAttributeComputed", typeof(XPathNavigator));
        public static readonly MethodInfo StartAttrMapName = GetMethod(typeof(XmlQueryOutput), "WriteStartAttributeComputed", typeof(string), typeof(int));
        public static readonly MethodInfo StartAttrNmspName = GetMethod(typeof(XmlQueryOutput), "WriteStartAttributeComputed", typeof(string), typeof(string));
        public static readonly MethodInfo StartAttrQName = GetMethod(typeof(XmlQueryOutput), "WriteStartAttributeComputed", typeof(XmlQualifiedName));
        public static readonly MethodInfo NamespaceDecl = GetMethod(typeof(XmlQueryOutput), "WriteNamespaceDeclaration");
        public static readonly MethodInfo StartComment = GetMethod(typeof(XmlQueryOutput), "WriteStartComment");
        public static readonly MethodInfo CommentText = GetMethod(typeof(XmlQueryOutput), "WriteCommentString");
        public static readonly MethodInfo EndComment = GetMethod(typeof(XmlQueryOutput), "WriteEndComment");
        public static readonly MethodInfo StartPI = GetMethod(typeof(XmlQueryOutput), "WriteStartProcessingInstruction");
        public static readonly MethodInfo PIText = GetMethod(typeof(XmlQueryOutput), "WriteProcessingInstructionString");
        public static readonly MethodInfo EndPI = GetMethod(typeof(XmlQueryOutput), "WriteEndProcessingInstruction");
        public static readonly MethodInfo WriteItem = GetMethod(typeof(XmlQueryOutput), "WriteItem");
        public static readonly MethodInfo CopyOf = GetMethod(typeof(XmlQueryOutput), "XsltCopyOf");
        public static readonly MethodInfo StartCopy = GetMethod(typeof(XmlQueryOutput), "StartCopy");
        public static readonly MethodInfo EndCopy = GetMethod(typeof(XmlQueryOutput), "EndCopy");

        // Datatypes
        public static readonly MethodInfo DecAdd = GetMethod(typeof(decimal), "Add");
        public static readonly MethodInfo DecCmp = GetMethod(typeof(decimal), "Compare", typeof(decimal), typeof(decimal));
        public static readonly MethodInfo DecEq = GetMethod(typeof(decimal), "Equals", typeof(decimal), typeof(decimal));
        public static readonly MethodInfo DecSub = GetMethod(typeof(decimal), "Subtract");
        public static readonly MethodInfo DecMul = GetMethod(typeof(decimal), "Multiply");
        public static readonly MethodInfo DecDiv = GetMethod(typeof(decimal), "Divide");
        public static readonly MethodInfo DecRem = GetMethod(typeof(decimal), "Remainder");
        public static readonly MethodInfo DecNeg = GetMethod(typeof(decimal), "Negate");
        public static readonly MethodInfo QNameEq = GetMethod(typeof(XmlQualifiedName), "Equals");
        public static readonly MethodInfo StrEq = GetMethod(typeof(string), "Equals", typeof(string), typeof(string));
        public static readonly MethodInfo StrCat2 = GetMethod(typeof(string), "Concat", typeof(string), typeof(string));
        public static readonly MethodInfo StrCat3 = GetMethod(typeof(string), "Concat", typeof(string), typeof(string), typeof(string));
        public static readonly MethodInfo StrCat4 = GetMethod(typeof(string), "Concat", typeof(string), typeof(string), typeof(string), typeof(string));
        public static readonly MethodInfo StrCmp = GetMethod(typeof(string), "CompareOrdinal", typeof(string), typeof(string));
        public static readonly MethodInfo StrLen = GetMethod(typeof(string), "get_Length");

        // XsltConvert
        public static readonly MethodInfo DblToDec = GetMethod(typeof(XsltConvert), "ToDecimal", typeof(double));
        public static readonly MethodInfo DblToInt = GetMethod(typeof(XsltConvert), "ToInt", typeof(double));
        public static readonly MethodInfo DblToLng = GetMethod(typeof(XsltConvert), "ToLong", typeof(double));
        public static readonly MethodInfo DblToStr = GetMethod(typeof(XsltConvert), "ToString", typeof(double));
        public static readonly MethodInfo DecToDbl = GetMethod(typeof(XsltConvert), "ToDouble", typeof(decimal));
        public static readonly MethodInfo DTToStr = GetMethod(typeof(XsltConvert), "ToString", typeof(DateTime));
        public static readonly MethodInfo IntToDbl = GetMethod(typeof(XsltConvert), "ToDouble", typeof(int));
        public static readonly MethodInfo LngToDbl = GetMethod(typeof(XsltConvert), "ToDouble", typeof(long));
        public static readonly MethodInfo StrToDbl = GetMethod(typeof(XsltConvert), "ToDouble", typeof(string));
        public static readonly MethodInfo StrToDT = GetMethod(typeof(XsltConvert), "ToDateTime", typeof(string));

        public static readonly MethodInfo ItemToBool = GetMethod(typeof(XsltConvert), "ToBoolean", typeof(XPathItem));
        public static readonly MethodInfo ItemToDbl = GetMethod(typeof(XsltConvert), "ToDouble", typeof(XPathItem));
        public static readonly MethodInfo ItemToStr = GetMethod(typeof(XsltConvert), "ToString", typeof(XPathItem));
        public static readonly MethodInfo ItemToNode = GetMethod(typeof(XsltConvert), "ToNode", typeof(XPathItem));
        public static readonly MethodInfo ItemToNodes = GetMethod(typeof(XsltConvert), "ToNodeSet", typeof(XPathItem));

        public static readonly MethodInfo ItemsToBool = GetMethod(typeof(XsltConvert), "ToBoolean", typeof(IList<XPathItem>));
        public static readonly MethodInfo ItemsToDbl = GetMethod(typeof(XsltConvert), "ToDouble", typeof(IList<XPathItem>));
        public static readonly MethodInfo ItemsToNode = GetMethod(typeof(XsltConvert), "ToNode", typeof(IList<XPathItem>));
        public static readonly MethodInfo ItemsToNodes = GetMethod(typeof(XsltConvert), "ToNodeSet", typeof(IList<XPathItem>));
        public static readonly MethodInfo ItemsToStr = GetMethod(typeof(XsltConvert), "ToString", typeof(IList<XPathItem>));

        // StringConcat
        public static readonly MethodInfo StrCatCat = GetMethod(typeof(StringConcat), "Concat");
        public static readonly MethodInfo StrCatClear = GetMethod(typeof(StringConcat), "Clear");
        public static readonly MethodInfo StrCatResult = GetMethod(typeof(StringConcat), "GetResult");
        public static readonly MethodInfo StrCatDelim = GetMethod(typeof(StringConcat), "set_Delimiter");

        // XmlILStorageConverter
        public static readonly MethodInfo NavsToItems = GetMethod(typeof(XmlILStorageConverter), "NavigatorsToItems");
        public static readonly MethodInfo ItemsToNavs = GetMethod(typeof(XmlILStorageConverter), "ItemsToNavigators");

        // XmlQueryNodeSequence
        public static readonly MethodInfo SetDod = GetMethod(typeof(XmlQueryNodeSequence), "set_IsDocOrderDistinct");

        // Miscellaneous
        public static readonly MethodInfo GetTypeFromHandle = GetMethod(typeof(Type), "GetTypeFromHandle");
        public static readonly MethodInfo InitializeArray = GetMethod(typeof(System.Runtime.CompilerServices.RuntimeHelpers), "InitializeArray");
        public static readonly Dictionary<Type, XmlILStorageMethods> StorageMethods;

        static XmlILMethods()
        {
            StorageMethods = new Dictionary<Type, XmlILStorageMethods>();
            StorageMethods[typeof(string)] = new XmlILStorageMethods(typeof(string));
            StorageMethods[typeof(bool)] = new XmlILStorageMethods(typeof(bool));
            StorageMethods[typeof(int)] = new XmlILStorageMethods(typeof(int));
            StorageMethods[typeof(long)] = new XmlILStorageMethods(typeof(long));
            StorageMethods[typeof(decimal)] = new XmlILStorageMethods(typeof(decimal));
            StorageMethods[typeof(double)] = new XmlILStorageMethods(typeof(double));
            StorageMethods[typeof(float)] = new XmlILStorageMethods(typeof(float));
            StorageMethods[typeof(DateTime)] = new XmlILStorageMethods(typeof(DateTime));
            StorageMethods[typeof(byte[])] = new XmlILStorageMethods(typeof(byte[]));
            StorageMethods[typeof(XmlQualifiedName)] = new XmlILStorageMethods(typeof(XmlQualifiedName));
            StorageMethods[typeof(TimeSpan)] = new XmlILStorageMethods(typeof(TimeSpan));
            StorageMethods[typeof(XPathItem)] = new XmlILStorageMethods(typeof(XPathItem));
            StorageMethods[typeof(XPathNavigator)] = new XmlILStorageMethods(typeof(XPathNavigator));
        }

        public static MethodInfo GetMethod(Type className, string methName)
        {
            MethodInfo methInfo = className.GetMethod(methName);
            Debug.Assert(methInfo != null, "Method " + className.Name + "." + methName + " cannot be null.");
            return methInfo;
        }

        public static MethodInfo GetMethod(Type className, string methName, params Type[] args)
        {
            MethodInfo methInfo = className.GetMethod(methName, args);
            Debug.Assert(methInfo != null, "Method " + methName + " cannot be null.");
            return methInfo;
        }
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
    internal class GenerateHelper
    {
        private MethodBase _methInfo;
        private ILGenerator _ilgen;
        private LocalBuilder _locXOut;
        private XmlILModule _module;
        private bool _isDebug, _initWriters;
        private StaticDataManager _staticData;
        private ISourceLineInfo _lastSourceInfo;
        private MethodInfo _methSyncToNav;

#if DEBUG
        private int _lblNum;
        private Hashtable _symbols;
        private int _numLocals;
        private string _sourceFile;
        private TextWriter _writerDump;
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
        public void MethodBegin(MethodBase methInfo, ISourceLineInfo sourceInfo, bool initWriters)
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
                _writerDump.WriteLine(".method {0}()", methInfo.Name);
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
                _writerDump.WriteLine("}");
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

            Call(_methSyncToNav);
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
            OpCode opcode;

            if (intVal >= -1 && intVal < 9)
            {
                switch (intVal)
                {
                    case -1: opcode = OpCodes.Ldc_I4_M1; break;
                    case 0: opcode = OpCodes.Ldc_I4_0; break;
                    case 1: opcode = OpCodes.Ldc_I4_1; break;
                    case 2: opcode = OpCodes.Ldc_I4_2; break;
                    case 3: opcode = OpCodes.Ldc_I4_3; break;
                    case 4: opcode = OpCodes.Ldc_I4_4; break;
                    case 5: opcode = OpCodes.Ldc_I4_5; break;
                    case 6: opcode = OpCodes.Ldc_I4_6; break;
                    case 7: opcode = OpCodes.Ldc_I4_7; break;
                    case 8: opcode = OpCodes.Ldc_I4_8; break;
                    default: Debug.Assert(false); return;
                }
                Emit(opcode);
            }
            else if (intVal >= -128 && intVal <= 127)
                Emit(OpCodes.Ldc_I4_S, (sbyte)intVal);
            else
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
            LocalBuilder locBldr = _ilgen.DeclareLocal(type);
#if DEBUG
            if (XmlILTrace.IsEnabled)
            {
                _symbols.Add(locBldr, name + _numLocals.ToString(CultureInfo.InvariantCulture));
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
            Emit(OpCodes.Ldloc, _locXOut);
        }


        //-----------------------------------------------
        // Parameters
        //-----------------------------------------------

        public void LoadParameter(int paramPos)
        {
            switch (paramPos)
            {
                case 0: Emit(OpCodes.Ldarg_0); break;
                case 1: Emit(OpCodes.Ldarg_1); break;
                case 2: Emit(OpCodes.Ldarg_2); break;
                case 3: Emit(OpCodes.Ldarg_3); break;
                default:
                    if (paramPos <= 255)
                    {
                        Emit(OpCodes.Ldarg_S, (byte)paramPos);
                    }
                    else if (paramPos <= ushort.MaxValue)
                    {
                        Emit(OpCodes.Ldarg, paramPos);
                    }
                    else
                    {
                        throw new XslTransformException(SR.XmlIl_TooManyParameters);
                    }
                    break;
            }
        }

        public void SetParameter(object paramId)
        {
            int paramPos = (int)paramId;

            if (paramPos <= 255)
            {
                Emit(OpCodes.Starg_S, (byte)paramPos);
            }
            else if (paramPos <= ushort.MaxValue)
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

                _writerDump.WriteLine("  {0, -10} {1} {2}({3})", new object[] { opcode.Name, retType, meth.Name, strBldr.ToString() });
            }
#endif
        }

        public void Call(MethodInfo meth)
        {
            OpCode opcode = meth.IsVirtual || meth.IsAbstract ? OpCodes.Callvirt : OpCodes.Call;

            TraceCall(opcode, meth);
            _ilgen.Emit(opcode, meth);

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
                    Debug.Assert(false, "Shouldn't be called");
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
                int[] bits = Decimal.GetBits(dec);

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
            MethodInfo meth = null;

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
                        default: Debug.Assert(false, opType + " must be an arithmetic operation."); break;
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
                        default: Debug.Assert(false, opType + " must be an arithmetic operation."); break;
                    }

                    Call(meth);
                    break;

                default:
                    Debug.Assert(false, "The " + opType + " arithmetic operation cannot be performed on values of type " + code + ".");
                    break;
            }
        }

        public void CallCompareEquals(XmlTypeCode code)
        {
            MethodInfo meth = null;

            switch (code)
            {
                case XmlTypeCode.String: meth = XmlILMethods.StrEq; break;
                case XmlTypeCode.QName: meth = XmlILMethods.QNameEq; break;
                case XmlTypeCode.Decimal: meth = XmlILMethods.DecEq; break;
                default:
                    Debug.Assert(false, "Type " + code + " does not support the equals operation.");
                    break;
            }

            Call(meth);
        }

        public void CallCompare(XmlTypeCode code)
        {
            MethodInfo meth = null;

            switch (code)
            {
                case XmlTypeCode.String: meth = XmlILMethods.StrCmp; break;
                case XmlTypeCode.Decimal: meth = XmlILMethods.DecCmp; break;
                default:
                    Debug.Assert(false, "Type " + code + " does not support the equals operation.");
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
            Emit(OpCodes.Ldloca, _locXOut);
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
            Emit(OpCodes.Ldloca, _locXOut);
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

        private void EnsureWriter()
        {
            // If write variable has not yet been initialized, do it now
            if (!_initWriters)
            {
                _locXOut = DeclareLocal("$$$xwrtChk", typeof(XmlQueryOutput));
                _initWriters = true;
            }
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
            MethodInfo meth = null;

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
                    default: Debug.Assert(false, nameType + " is invalid here."); break;
                }
            }
            else
            {
                // Else call XmlQueryOutput.WriteStartElementUnchecked
                switch (nameType)
                {
                    case GenerateNameType.LiteralLocalName: meth = XmlILMethods.StartElemLocNameUn; break;
                    case GenerateNameType.LiteralName: meth = XmlILMethods.StartElemLitNameUn; break;
                    default: Debug.Assert(false, nameType + " is invalid here."); break;
                }
            }

            Call(meth);
        }

        public void CallWriteEndElement(GenerateNameType nameType, bool callChk)
        {
            MethodInfo meth = null;

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
                    default: Debug.Assert(false, nameType + " is invalid here."); break;
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
            MethodInfo meth = null;

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
                    default: Debug.Assert(false, nameType + " is invalid here."); break;
                }
            }
            else
            {
                // Else call XmlQueryOutput.WriteStartAttributeUnchecked
                switch (nameType)
                {
                    case GenerateNameType.LiteralLocalName: meth = XmlILMethods.StartAttrLocNameUn; break;
                    case GenerateNameType.LiteralName: meth = XmlILMethods.StartAttrLitNameUn; break;
                    default: Debug.Assert(false, nameType + " is invalid here."); break;
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
            MethodInfo meth;

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

        public void AddSortKey(XmlQueryType keyType)
        {
            MethodInfo meth = null;

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
                        Debug.Assert(false, "Heterogenous sort key is not allowed.");
                        return;

                    default:
                        Debug.Assert(false, "Sorting over datatype " + keyType.TypeCode + " is not allowed.");
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
            _ilgen.BeginScope();
        }

        /// <summary>
        /// End a new debugging scope.
        /// </summary>
        public void DebugEndScope()
        {
            _ilgen.EndScope();
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

        private string _lastUriString = null;
        private string _lastFileName = null;

        // SQLBUDT 278010: debugger does not work with network paths in uri format, like file://server/share/dir/file
        private string GetFileName(ISourceLineInfo sourceInfo)
        {
            string uriString = sourceInfo.Uri;
            if ((object)uriString == (object)_lastUriString)
            {
                return _lastFileName;
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
                    _writerDump.WriteLine("//[no source]");
                else
                {
                    if (sourceFile != _sourceFile)
                    {
                        _sourceFile = sourceFile;
                        _writerDump.WriteLine("// Source File '{0}'", _sourceFile);
                    }
                    _writerDump.WriteLine("//[{0},{1} -- {2},{3}]", sourceInfo.Start.Line, sourceInfo.Start.Pos, sourceInfo.End.Line, sourceInfo.End.Pos);
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
            Label lbl = _ilgen.DefineLabel();

#if DEBUG
            if (XmlILTrace.IsEnabled)
                _symbols.Add(lbl, ++_lblNum);
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
                _writerDump.WriteLine("Label {0}:", _symbols[lbl]);
#endif

            _ilgen.MarkLabel(lbl);
        }

        public void Emit(OpCode opcode)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump.WriteLine("  {0}", opcode.Name);
#endif
            _ilgen.Emit(opcode);
        }

        public void Emit(OpCode opcode, byte byteVal)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump.WriteLine("  {0, -10} {1}", opcode.Name, byteVal);
#endif
            _ilgen.Emit(opcode, byteVal);
        }

        public void Emit(OpCode opcode, ConstructorInfo constrInfo)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump.WriteLine("  {0, -10} {1}", opcode.Name, constrInfo);
#endif
            _ilgen.Emit(opcode, constrInfo);
        }

        public void Emit(OpCode opcode, double dblVal)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump.WriteLine("  {0, -10} {1}", opcode.Name, dblVal);
#endif
            _ilgen.Emit(opcode, dblVal);
        }

        public void Emit(OpCode opcode, FieldInfo fldInfo)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump.WriteLine("  {0, -10} {1}", opcode.Name, fldInfo.Name);
#endif
            _ilgen.Emit(opcode, fldInfo);
        }

        public void Emit(OpCode opcode, int intVal)
        {
            Debug.Assert(opcode.OperandType == OperandType.InlineI || opcode.OperandType == OperandType.InlineVar);
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump.WriteLine("  {0, -10} {1}", opcode.Name, intVal);
#endif
            _ilgen.Emit(opcode, intVal);
        }

        public void Emit(OpCode opcode, long longVal)
        {
            Debug.Assert(opcode.OperandType == OperandType.InlineI8);
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump.WriteLine("  {0, -10} {1}", opcode.Name, longVal);
#endif
            _ilgen.Emit(opcode, longVal);
        }

        public void Emit(OpCode opcode, Label lblVal)
        {
            Debug.Assert(!opcode.Equals(OpCodes.Br) && !opcode.Equals(OpCodes.Br_S), "Use EmitUnconditionalBranch and be careful not to emit unverifiable code.");
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump.WriteLine("  {0, -10} Label {1}", opcode.Name, _symbols[lblVal]);
#endif
            _ilgen.Emit(opcode, lblVal);
        }

        public void Emit(OpCode opcode, Label[] arrLabels)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
            {
                _writerDump.Write("  {0, -10} (Label {1}", opcode.Name, arrLabels.Length != 0 ? _symbols[arrLabels[0]].ToString() : "");
                for (int i = 1; i < arrLabels.Length; i++)
                {
                    _writerDump.Write(", Label {0}", _symbols[arrLabels[i]]);
                }
                _writerDump.WriteLine(")");
            }
#endif
            _ilgen.Emit(opcode, arrLabels);
        }

        public void Emit(OpCode opcode, LocalBuilder locBldr)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump.WriteLine("  {0, -10} {1} ({2})", opcode.Name, _symbols[locBldr], locBldr.LocalType.Name);
#endif
            _ilgen.Emit(opcode, locBldr);
        }

        public void Emit(OpCode opcode, sbyte sbyteVal)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump.WriteLine("  {0, -10} {1}", opcode.Name, sbyteVal);
#endif
            _ilgen.Emit(opcode, sbyteVal);
        }

        public void Emit(OpCode opcode, string strVal)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump.WriteLine("  {0, -10} \"{1}\"", opcode.Name, strVal);
#endif
            _ilgen.Emit(opcode, strVal);
        }

        public void Emit(OpCode opcode, Type typVal)
        {
#if DEBUG
            if (XmlILTrace.IsEnabled)
                _writerDump.WriteLine("  {0, -10} {1}", opcode.Name, typVal);
#endif
            _ilgen.Emit(opcode, typVal);
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
                _writerDump.WriteLine("  {0, -10} Label {1}", opcode.Name, _symbols[lblTarget]);
#endif
            _ilgen.Emit(opcode, lblTarget);

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
