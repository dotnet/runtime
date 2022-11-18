// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using FT = MS.Internal.Xml.XPath.Function.FunctionType;

namespace MS.Internal.Xml.XPath
{
    internal sealed class QueryBuilder
    {
        // Note: Up->Down, Down->Up:
        //       For operators order is normal: 1 + 2 --> Operator+(1, 2)
        //       For paths order is reversed: a/b -> ChildQuery_B(input: ChildQuery_A(input: ContextQuery()))
        // Input flags. We pass them Up->Down.
        // Using them upper query set states which controls how inner query will be built.
        private enum Flags
        {
            None = 0x00,
            SmartDesc = 0x01,
            PosFilter = 0x02,  // Node has this flag set when it has position predicate applied to it
            Filter = 0x04,  // Subtree we compiling will be filtered. i.e. Flag not set on rightmost filter.
        }
        // Output props. We return them Down->Up.
        // These are properties of Query tree we have built already.
        // These properties are closely related to QueryProps exposed by Query node itself.
        // They have the following difference:
        //      QueryProps describe property of node they are (belong like Reverse)
        //      these Props describe accumulated properties of the tree (like nonFlat)
        private enum Props
        {
            None = 0x00,
            PosFilter = 0x01,  // This filter or inner filter was positional: foo[1] or foo[1][true()]
            HasPosition = 0x02,  // Expression may ask position() of the context
            HasLast = 0x04,  // Expression may ask last() of the context
            NonFlat = 0x08,  // Some nodes may be descendent of others
        }

        // comment are approximate. This is my best understanding:
        private string? _query;
        private bool _allowVar;
        private bool _allowKey;
        private bool _allowCurrent;
        private bool _needContext;
        private BaseAxisQuery? _firstInput; // Input of the leftmost predicate. Set by leftmost predicate, used in rightmost one

        private void Reset()
        {
            _parseDepth = 0;
            _needContext = false;
        }

        private Query ProcessAxis(Axis root, Flags flags, out Props props)
        {
            Query? result;
            if (root.Prefix.Length > 0)
            {
                _needContext = true;
            }
            _firstInput = null;
            Query qyInput;
            {
                if (root.Input != null)
                {
                    Flags inputFlags = Flags.None;
                    if ((flags & Flags.PosFilter) == 0)
                    {
                        Axis? input = root.Input as Axis;
                        if (input != null)
                        {
                            if (
                                root.TypeOfAxis == Axis.AxisType.Child &&
                                input.TypeOfAxis == Axis.AxisType.DescendantOrSelf && input.NodeType == XPathNodeType.All
                            )
                            {
                                Query qyGrandInput;
                                if (input.Input != null)
                                {
                                    qyGrandInput = ProcessNode(input.Input, Flags.SmartDesc, out props);
                                }
                                else
                                {
                                    qyGrandInput = new ContextQuery();
                                    props = Props.None;
                                }
                                result = new DescendantQuery(qyGrandInput, root.Name, root.Prefix, root.NodeType, false, input.AbbrAxis);
                                if ((props & Props.NonFlat) != 0)
                                {
                                    result = new DocumentOrderQuery(result);
                                }
                                props |= Props.NonFlat;
                                return result;
                            }
                        }
                        if (root.TypeOfAxis == Axis.AxisType.Descendant || root.TypeOfAxis == Axis.AxisType.DescendantOrSelf)
                        {
                            inputFlags |= Flags.SmartDesc;
                        }
                    }

                    qyInput = ProcessNode(root.Input, inputFlags, out props);
                }
                else
                {
                    qyInput = new ContextQuery();
                    props = Props.None;
                }
            }

            switch (root.TypeOfAxis)
            {
                case Axis.AxisType.Ancestor:
                    result = new XPathAncestorQuery(qyInput, root.Name, root.Prefix, root.NodeType, false);
                    props |= Props.NonFlat;
                    break;
                case Axis.AxisType.AncestorOrSelf:
                    result = new XPathAncestorQuery(qyInput, root.Name, root.Prefix, root.NodeType, true);
                    props |= Props.NonFlat;
                    break;
                case Axis.AxisType.Child:
                    if ((props & Props.NonFlat) != 0)
                    {
                        result = new CacheChildrenQuery(qyInput, root.Name, root.Prefix, root.NodeType);
                    }
                    else
                    {
                        result = new ChildrenQuery(qyInput, root.Name, root.Prefix, root.NodeType);
                    }
                    break;
                case Axis.AxisType.Parent:
                    result = new ParentQuery(qyInput, root.Name, root.Prefix, root.NodeType);
                    break;
                case Axis.AxisType.Descendant:
                    if ((flags & Flags.SmartDesc) != 0)
                    {
                        result = new DescendantOverDescendantQuery(qyInput, false, root.Name, root.Prefix, root.NodeType, /*abbrAxis:*/false);
                    }
                    else
                    {
                        result = new DescendantQuery(qyInput, root.Name, root.Prefix, root.NodeType, false, /*abbrAxis:*/false);
                        if ((props & Props.NonFlat) != 0)
                        {
                            result = new DocumentOrderQuery(result);
                        }
                    }
                    props |= Props.NonFlat;
                    break;
                case Axis.AxisType.DescendantOrSelf:
                    if ((flags & Flags.SmartDesc) != 0)
                    {
                        result = new DescendantOverDescendantQuery(qyInput, true, root.Name, root.Prefix, root.NodeType, root.AbbrAxis);
                    }
                    else
                    {
                        result = new DescendantQuery(qyInput, root.Name, root.Prefix, root.NodeType, true, root.AbbrAxis);
                        if ((props & Props.NonFlat) != 0)
                        {
                            result = new DocumentOrderQuery(result);
                        }
                    }
                    props |= Props.NonFlat;
                    break;
                case Axis.AxisType.Preceding:
                    result = new PrecedingQuery(qyInput, root.Name, root.Prefix, root.NodeType);
                    props |= Props.NonFlat;
                    break;
                case Axis.AxisType.Following:
                    result = new FollowingQuery(qyInput, root.Name, root.Prefix, root.NodeType);
                    props |= Props.NonFlat;
                    break;
                case Axis.AxisType.FollowingSibling:
                    result = new FollSiblingQuery(qyInput, root.Name, root.Prefix, root.NodeType);
                    if ((props & Props.NonFlat) != 0)
                    {
                        result = new DocumentOrderQuery(result);
                    }
                    break;
                case Axis.AxisType.PrecedingSibling:
                    result = new PreSiblingQuery(qyInput, root.Name, root.Prefix, root.NodeType);
                    break;
                case Axis.AxisType.Attribute:
                    result = new AttributeQuery(qyInput, root.Name, root.Prefix, root.NodeType);
                    break;
                case Axis.AxisType.Self:
                    result = new XPathSelfQuery(qyInput, root.Name, root.Prefix, root.NodeType);
                    break;
                case Axis.AxisType.Namespace:
                    if ((root.NodeType == XPathNodeType.All || root.NodeType == XPathNodeType.Element || root.NodeType == XPathNodeType.Attribute) && root.Prefix.Length == 0)
                    {
                        result = new NamespaceQuery(qyInput, root.Name, root.Prefix, root.NodeType);
                    }
                    else
                    {
                        result = new EmptyQuery();
                    }
                    break;
                default:
                    throw XPathException.Create(SR.Xp_NotSupported, _query!);
            }

            return result;
        }

        private static bool CanBeNumber(Query q)
        {
            return (
                q.StaticType == XPathResultType.Any ||
                q.StaticType == XPathResultType.Number
            );
        }

        private Query ProcessFilter(Filter root, Flags flags, out Props props)
        {
            bool first = ((flags & Flags.Filter) == 0);

            Props propsCond;
            Query cond = ProcessNode(root.Condition, Flags.None, out propsCond);

            if (
                CanBeNumber(cond) ||
                (propsCond & (Props.HasPosition | Props.HasLast)) != 0
            )
            {
                propsCond |= Props.HasPosition;
                flags |= Flags.PosFilter;
            }

            // We don't want DescendantOverDescendant pattern to be recognized here (in case descendent::foo[expr]/descendant::bar)
            // So we clean this flag here:
            flags &= ~Flags.SmartDesc;
            // Suggestion: Instead it would be nice to wrap descendent::foo[expr] into special query that will flatten it -- i.e.
            //       remove all nodes that are descendant of other nodes. This is very easy because for sorted nodesets all children
            //       follow its parent. One step caching. This can be easily done by rightmost DescendantQuery itself.
            //       Interesting note! Can we guarantee that DescendantOverDescendant returns flat nodeset? This definitely true if it's input is flat.

            Query qyInput = ProcessNode(root.Input, flags | Flags.Filter, out props);

            if (root.Input.Type != AstNode.AstType.Filter)
            {
                // Props.PosFilter is for nested filters only.
                // We clean it here to avoid cleaning it in all other ast nodes.
                props &= ~Props.PosFilter;
            }
            if ((propsCond & Props.HasPosition) != 0)
            {
                // this condition is positional rightmost filter should be aware of this.
                props |= Props.PosFilter;
            }

            /*merging predicates*/
            {
                FilterQuery? qyFilter = qyInput as FilterQuery;
                if (qyFilter != null && (propsCond & Props.HasPosition) == 0 && qyFilter.Condition.StaticType != XPathResultType.Any)
                {
                    Query prevCond = qyFilter.Condition;
                    if (prevCond.StaticType == XPathResultType.Number)
                    {
                        prevCond = new LogicalExpr(Operator.Op.EQ, new NodeFunctions(FT.FuncPosition, null), prevCond);
                    }
                    cond = new BooleanExpr(Operator.Op.AND, prevCond, cond);
                    qyInput = qyFilter.qyInput;
                }
            }

            if ((props & Props.PosFilter) != 0 && qyInput is DocumentOrderQuery)
            {
                qyInput = ((DocumentOrderQuery)qyInput).input;
            }
            _firstInput ??= qyInput as BaseAxisQuery;

            bool merge = (qyInput.Properties & QueryProps.Merge) != 0;
            bool reverse = (qyInput.Properties & QueryProps.Reverse) != 0;
            if ((propsCond & Props.HasPosition) != 0)
            {
                if (reverse)
                {
                    qyInput = new ReversePositionQuery(qyInput);
                }
                else if ((propsCond & Props.HasLast) != 0)
                {
                    qyInput = new ForwardPositionQuery(qyInput);
                }
            }

            if (first && _firstInput != null)
            {
                if (merge && (props & Props.PosFilter) != 0)
                {
                    qyInput = new FilterQuery(qyInput, cond, /*noPosition:*/false);
                    Query parent = _firstInput.qyInput;
                    if (!(parent is ContextQuery))
                    { // we don't need to wrap filter with MergeFilterQuery when cardinality is parent <: ?
                        _firstInput.qyInput = new ContextQuery();
                        _firstInput = null;
                        return new MergeFilterQuery(parent, qyInput);
                    }
                    _firstInput = null;
                    return qyInput;
                }
                _firstInput = null;
            }
            return new FilterQuery(qyInput, cond, /*noPosition:*/(propsCond & Props.HasPosition) == 0);
        }

        private Query? ProcessOperator(Operator root, out Props props)
        {
            Props props1, props2;
            Query op1 = ProcessNode(root.Operand1, Flags.None, out props1);
            Query op2 = ProcessNode(root.Operand2, Flags.None, out props2);
            props = props1 | props2;
            switch (root.OperatorType)
            {
                case Operator.Op.PLUS:
                case Operator.Op.MINUS:
                case Operator.Op.MUL:
                case Operator.Op.MOD:
                case Operator.Op.DIV:
                    return new NumericExpr(root.OperatorType, op1, op2);
                case Operator.Op.LT:
                case Operator.Op.GT:
                case Operator.Op.LE:
                case Operator.Op.GE:
                case Operator.Op.EQ:
                case Operator.Op.NE:
                    return new LogicalExpr(root.OperatorType, op1, op2);
                case Operator.Op.OR:
                case Operator.Op.AND:
                    return new BooleanExpr(root.OperatorType, op1, op2);
                case Operator.Op.UNION:
                    props |= Props.NonFlat;
                    return new UnionExpr(op1, op2);
                default: return null;
            }
        }

        private Query ProcessVariable(Variable root)
        {
            _needContext = true;
            if (!_allowVar)
            {
                throw XPathException.Create(SR.Xp_InvalidKeyPattern, _query!);
            }
            return new VariableQuery(root.Localname, root.Prefix);
        }

        private Query ProcessFunction(Function root, out Props props)
        {
            props = Props.None;
            Query? qy;
            switch (root.TypeOfFunction)
            {
                case FT.FuncLast:
                    qy = new NodeFunctions(root.TypeOfFunction, null);
                    props |= Props.HasLast;
                    return qy;
                case FT.FuncPosition:
                    qy = new NodeFunctions(root.TypeOfFunction, null);
                    props |= Props.HasPosition;
                    return qy;
                case FT.FuncCount:
                    return new NodeFunctions(FT.FuncCount,
                        ProcessNode((AstNode)(root.ArgumentList[0]), Flags.None, out props)
                    );
                case FT.FuncID:
                    qy = new IDQuery(ProcessNode((AstNode)(root.ArgumentList[0]), Flags.None, out props));
                    props |= Props.NonFlat;
                    return qy;
                case FT.FuncLocalName:
                case FT.FuncNameSpaceUri:
                case FT.FuncName:
                    if (root.ArgumentList != null && root.ArgumentList.Count > 0)
                    {
                        return new NodeFunctions(root.TypeOfFunction,
                            ProcessNode((AstNode)(root.ArgumentList[0]), Flags.None, out props)
                        );
                    }
                    else
                    {
                        return new NodeFunctions(root.TypeOfFunction, null);
                    }
                case FT.FuncString:
                case FT.FuncConcat:
                case FT.FuncStartsWith:
                case FT.FuncContains:
                case FT.FuncSubstringBefore:
                case FT.FuncSubstringAfter:
                case FT.FuncSubstring:
                case FT.FuncStringLength:
                case FT.FuncNormalize:
                case FT.FuncTranslate:
                    return new StringFunctions(root.TypeOfFunction, ProcessArguments(root.ArgumentList, out props));
                case FT.FuncNumber:
                case FT.FuncSum:
                case FT.FuncFloor:
                case FT.FuncCeiling:
                case FT.FuncRound:
                    if (root.ArgumentList != null && root.ArgumentList.Count > 0)
                    {
                        return new NumberFunctions(root.TypeOfFunction,
                            ProcessNode((AstNode)root.ArgumentList[0], Flags.None, out props)
                        );
                    }
                    else
                    {
                        return new NumberFunctions(Function.FunctionType.FuncNumber, null);
                    }
                case FT.FuncTrue:
                case FT.FuncFalse:
                    return new BooleanFunctions(root.TypeOfFunction, null);
                case FT.FuncNot:
                case FT.FuncLang:
                case FT.FuncBoolean:
                    return new BooleanFunctions(root.TypeOfFunction,
                        ProcessNode((AstNode)root.ArgumentList[0], Flags.None, out props)
                    );
                case FT.FuncUserDefined:
                    Debug.Assert(root.Prefix != null);
                    Debug.Assert(root.Name != null);

                    _needContext = true;
                    if (!_allowCurrent && root.Name == "current" && root.Prefix.Length == 0)
                    {
                        throw XPathException.Create(SR.Xp_CurrentNotAllowed);
                    }
                    if (!_allowKey && root.Name == "key" && root.Prefix.Length == 0)
                    {
                        throw XPathException.Create(SR.Xp_InvalidKeyPattern, _query!);
                    }
                    qy = new FunctionQuery(root.Prefix, root.Name, ProcessArguments(root.ArgumentList, out props));
                    props |= Props.NonFlat;
                    return qy;
                default:
                    throw XPathException.Create(SR.Xp_NotSupported, _query!);
            }
        }

        private List<Query> ProcessArguments(List<AstNode> args, out Props props)
        {
            int numArgs = args.Count;
            List<Query> argList = new List<Query>(numArgs);
            props = Props.None;
            for (int count = 0; count < numArgs; count++)
            {
                Props argProps;
                argList.Add(ProcessNode((AstNode)args[count], Flags.None, out argProps));
                props |= argProps;
            }
            return argList;
        }

        private int _parseDepth;
        private const int MaxParseDepth = 1024;

        private Query ProcessNode(AstNode root, Flags flags, out Props props)
        {
            if (++_parseDepth > MaxParseDepth)
            {
                throw XPathException.Create(SR.Xp_QueryTooComplex);
            }

            Debug.Assert(root != null, "root != null");
            Query? result = null;
            props = Props.None;
            switch (root.Type)
            {
                case AstNode.AstType.Axis:
                    result = ProcessAxis((Axis)root, flags, out props);
                    break;
                case AstNode.AstType.Operator:
                    result = ProcessOperator((Operator)root, out props);
                    break;
                case AstNode.AstType.Filter:
                    result = ProcessFilter((Filter)root, flags, out props);
                    break;
                case AstNode.AstType.ConstantOperand:
                    result = new OperandQuery(((Operand)root).OperandValue);
                    break;
                case AstNode.AstType.Variable:
                    result = ProcessVariable((Variable)root);
                    break;
                case AstNode.AstType.Function:
                    result = ProcessFunction((Function)root, out props);
                    break;
                case AstNode.AstType.Group:
                    result = new GroupQuery(ProcessNode(((Group)root).GroupNode, Flags.None, out props));
                    break;
                case AstNode.AstType.Root:
                    result = new AbsoluteQuery();
                    break;
                default:
                    Debug.Fail("Unknown QueryType encountered!!");
                    break;
            }
            --_parseDepth;
            return result!;
        }

        private Query Build(AstNode root, string query)
        {
            Reset();
            _query = query;
            Query result = ProcessNode(root, Flags.None, out _);
            return result;
        }

        internal Query Build(string query, bool allowVar, bool allowKey)
        {
            _allowVar = allowVar;
            _allowKey = allowKey;
            _allowCurrent = true;
            return Build(XPathParser.ParseXPathExpression(query), query);
        }

        internal Query Build(string query, out bool needContext)
        {
            Query result = Build(query, true, true);
            needContext = _needContext;
            return result;
        }

        internal Query BuildPatternQuery(string query, bool allowVar, bool allowKey)
        {
            _allowVar = allowVar;
            _allowKey = allowKey;
            _allowCurrent = false;
            return Build(XPathParser.ParseXPathPattern(query), query);
        }

        internal Query BuildPatternQuery(string query, out bool needContext)
        {
            Query result = BuildPatternQuery(query, true, true);
            needContext = _needContext;
            return result;
        }
    }
}
