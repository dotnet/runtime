// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.XPath;
using T = System.Xml.Xsl.XmlQueryTypeFactory;

namespace System.Xml.Xsl.Xslt
{
    internal sealed class XPathPatternBuilder : XPathPatternParser.IPatternBuilder
    {
        private readonly XPathPredicateEnvironment _predicateEnvironment;
        private readonly XPathBuilder _predicateBuilder;
        private bool _inTheBuild;
        private readonly XPathQilFactory _f;
        private readonly QilNode _fixupNode;
        private readonly IXPathEnvironment _environment;

        public XPathPatternBuilder(IXPathEnvironment environment)
        {
            Debug.Assert(environment != null);
            _environment = environment;
            _f = environment.Factory;
            _predicateEnvironment = new XPathPredicateEnvironment(environment);
            _predicateBuilder = new XPathBuilder(_predicateEnvironment);

            _fixupNode = _f.Unknown(T.NodeNotRtfS);
        }

        public QilNode FixupNode
        {
            get { return _fixupNode; }
        }

        public void StartBuild()
        {
            Debug.Assert(!_inTheBuild, "XPathBuilder is busy!");
            _inTheBuild = true;
            return;
        }

        [Conditional("DEBUG")]
        public static void AssertFilter(QilLoop filter)
        {
            Debug.Assert(filter.NodeType == QilNodeType.Filter, "XPathPatternBuilder expected to generate list of Filters on top level");
            Debug.Assert(filter.Variable.XmlType!.IsSubtypeOf(T.NodeNotRtf));
            Debug.Assert(filter.Variable.Binding!.NodeType == QilNodeType.Unknown);  // fixupNode
            Debug.Assert(filter.Body.XmlType!.IsSubtypeOf(T.Boolean));
        }

        private static void FixupFilterBinding(QilLoop filter, QilNode newBinding)
        {
            AssertFilter(filter);
            filter.Variable.Binding = newBinding;
        }

        [return: NotNullIfNotNull(nameof(result))]
        public QilNode? EndBuild(QilNode? result)
        {
            Debug.Assert(_inTheBuild, "StartBuild() wasn't called");

            // All these variables will be positive for "false() and (. = position() + last())"
            // since QilPatternFactory eliminates the right operand of 'and'
            Debug.Assert(_predicateEnvironment.numFixupCurrent >= 0, "Context fixup error");
            Debug.Assert(_predicateEnvironment.numFixupPosition >= 0, "Context fixup error");
            Debug.Assert(_predicateEnvironment.numFixupLast >= 0, "Context fixup error");
            _inTheBuild = false;
            return result;
        }

        public QilNode Operator(XPathOperator op, QilNode? left, QilNode? right)
        {
            Debug.Assert(op == XPathOperator.Union);
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            // It is important to not create nested lists here
            Debug.Assert(right.NodeType == QilNodeType.Filter, "LocationPathPattern must be compiled into a filter");
            if (left.NodeType == QilNodeType.Sequence)
            {
                ((QilList)left).Add(right);
                return left;
            }
            else
            {
                Debug.Assert(left.NodeType == QilNodeType.Filter, "LocationPathPattern must be compiled into a filter");
                return _f.Sequence(left, right);
            }
        }

        private static QilLoop BuildAxisFilter(QilPatternFactory f, QilIterator itr, XPathAxis xpathAxis, XPathNodeType nodeType, string? name, string? nsUri)
        {
            QilNode nameTest = (
                name != null && nsUri != null ? f.Eq(f.NameOf(itr), f.QName(name, nsUri)) : // ns:bar || bar
                nsUri != null ? f.Eq(f.NamespaceUriOf(itr), f.String(nsUri)) : // ns:*
                name != null ? f.Eq(f.LocalNameOf(itr), f.String(name)) : // *:foo
                                                                          /*name  == nsUri == null*/       f.True()                                       // *
            );

            XmlNodeKindFlags intersection = XPathBuilder.AxisTypeMask(itr.XmlType!.NodeKinds, nodeType, xpathAxis);

            QilNode typeTest = (
                intersection == 0 ? f.False() :  // input & required doesn't intersect
                intersection == itr.XmlType.NodeKinds ? f.True() :  // input is subset of required
                                                                    /*else*/                                f.IsType(itr, T.NodeChoice(intersection))
            );

            QilLoop filter = f.BaseFactory.Filter(itr, f.And(typeTest, nameTest));
            filter.XmlType = T.PrimeProduct(T.NodeChoice(intersection), filter.XmlType!.Cardinality);

            return filter;
        }

        public QilNode Axis(XPathAxis xpathAxis, XPathNodeType nodeType, string? prefix, string? name)
        {
            Debug.Assert(
                xpathAxis == XPathAxis.Child ||
                xpathAxis == XPathAxis.Attribute ||
                xpathAxis == XPathAxis.DescendantOrSelf ||
                xpathAxis == XPathAxis.Root
            );
            QilLoop result;
            double priority;
            switch (xpathAxis)
            {
                case XPathAxis.DescendantOrSelf:
                    Debug.Assert(nodeType == XPathNodeType.All && prefix == null && name == null, " // is the only d-o-s axes that we can have in pattern");
                    return _f.Nop(_fixupNode); // We using Nop as a flag that DescendantOrSelf exis was used between steps.
                case XPathAxis.Root:
                    QilIterator i;
                    result = _f.BaseFactory.Filter(i = _f.For(_fixupNode), _f.IsType(i, T.Document));
                    priority = 0.5;
                    break;
                default:
                    string? nsUri = prefix == null ? null : _environment.ResolvePrefix(prefix);
                    result = BuildAxisFilter(_f, _f.For(_fixupNode), xpathAxis, nodeType, name, nsUri);
                    switch (nodeType)
                    {
                        case XPathNodeType.Element:
                        case XPathNodeType.Attribute:
                            if (name != null)
                            {
                                priority = 0;
                            }
                            else
                            {
                                if (prefix != null)
                                {
                                    priority = -0.25;
                                }
                                else
                                {
                                    priority = -0.5;
                                }
                            }
                            break;
                        case XPathNodeType.ProcessingInstruction:
                            priority = name != null ? 0 : -0.5;
                            break;
                        default:
                            priority = -0.5;
                            break;
                    }
                    break;
            }

            SetPriority(result, priority);
            SetLastParent(result, result);
            return result;
        }

        // a/b/c -> self::c[parent::b[parent::a]]
        // a/b//c -> self::c[ancestor::b[parent::a]]
        // a/b -> self::b[parent::a]
        //  -> JoinStep(Axis('a'), Axis('b'))
        //   -> Filter('b' & Parent(Filter('a')))
        // a//b
        //  -> JoinStep(Axis('a'), JoingStep(Axis(DescendantOrSelf), Axis('b')))
        //   -> JoinStep(Filter('a'), JoingStep(Nop(null), Filter('b')))
        //    -> JoinStep(Filter('a'), Nop(Filter('b')))
        //     -> Filter('b' & Ancestor(Filter('a')))
        public QilNode JoinStep(QilNode left, QilNode right)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            if (left.NodeType == QilNodeType.Nop)
            {
                QilUnary nop = (QilUnary)left;
                Debug.Assert(nop.Child == _fixupNode);
                nop.Child = right;  // We use Nop as a flag that DescendantOrSelf axis was used between steps.
                return nop;
            }
            Debug.Assert(GetLastParent(left) == left, "Left is always single axis and never the step");
            Debug.Assert(left.NodeType == QilNodeType.Filter);
            CleanAnnotation(left);
            QilLoop parentFilter = (QilLoop)left;
            bool ancestor = false;
            {
                if (right.NodeType == QilNodeType.Nop)
                {
                    ancestor = true;
                    QilUnary nop = (QilUnary)right;
                    Debug.Assert(nop.Child != null);
                    right = nop.Child;
                }
            }
            Debug.Assert(right.NodeType == QilNodeType.Filter);
            QilLoop? lastParent = GetLastParent(right);
            Debug.Assert(lastParent != null);

            FixupFilterBinding(parentFilter, ancestor ? _f.Ancestor(lastParent.Variable) : _f.Parent(lastParent.Variable));
            lastParent.Body = _f.And(lastParent.Body, _f.Not(_f.IsEmpty(parentFilter)));
            SetPriority(right, 0.5);
            SetLastParent(right, parentFilter);
            return right;
        }

        QilNode IXPathBuilder<QilNode>.Predicate(QilNode node, QilNode condition, bool isReverseStep)
        {
            Debug.Fail("Should not call to this function.");
            return null;
        }

        //The structure of result is a Filter, variable is current node, body is the match condition.
        //Previous predicate build logic in XPathPatternBuilder is match from right to left, which have 2^n complexiy when have lots of position predicates. TFS #368771
        //Now change the logic to: If predicates contains position/last predicates, given the current node, filter out all the nodes that match the predicates,
        //and then check if current node is in the result set.
        public QilNode BuildPredicates(QilNode nodeset, List<QilNode> predicates)
        {
            //convert predicates to boolean type
            List<QilNode> convertedPredicates = new List<QilNode>(predicates.Count);
            foreach (var predicate in predicates)
            {
                convertedPredicates.Add(XPathBuilder.PredicateToBoolean(predicate, _f, _predicateEnvironment));
            }

            QilLoop nodeFilter = (QilLoop)nodeset;
            QilIterator current = nodeFilter.Variable;

            //If no last() and position() in predicates, use nodeFilter.Variable to fixup current
            //because all the predicates only based on the input variable, no matter what other predicates are.
            if (_predicateEnvironment.numFixupLast == 0 && _predicateEnvironment.numFixupPosition == 0)
            {
                foreach (var predicate in convertedPredicates)
                {
                    nodeFilter.Body = _f.And(nodeFilter.Body, predicate);
                }
                nodeFilter.Body = _predicateEnvironment.fixupVisitor.Fixup(nodeFilter.Body, current, null);
            }
            //If any preidcate contains last() or position() node, then the current node is based on previous predicates,
            //for instance, a[...][2] is match second node after filter 'a[...]' instead of second 'a'.
            else
            {
                //filter out the siblings
                QilIterator parentIter = _f.For(_f.Parent(current));
                QilNode sibling = _f.Content(parentIter);
                //generate filter based on input filter
                QilLoop siblingFilter = (QilLoop)nodeset.DeepClone(_f.BaseFactory);
                siblingFilter.Variable.Binding = sibling;
                siblingFilter = (QilLoop)_f.Loop(parentIter, siblingFilter);

                //build predicates from left to right to get all the matching nodes
                QilNode matchingSet = siblingFilter;
                foreach (var predicate in convertedPredicates)
                {
                    matchingSet = XPathBuilder.BuildOnePredicate(matchingSet, predicate, /*isReverseStep*/false,
                                                                    _f, _predicateEnvironment.fixupVisitor,
                                                                    ref _predicateEnvironment.numFixupCurrent, ref _predicateEnvironment.numFixupPosition, ref _predicateEnvironment.numFixupLast);
                }

                //check if the matching nodes contains the current node
                QilIterator matchNodeIter = _f.For(matchingSet);
                QilNode filterCurrent = _f.Filter(matchNodeIter, _f.Is(matchNodeIter, current));
                nodeFilter.Body = _f.Not(_f.IsEmpty(filterCurrent));
                //for passing type check, explicit say the result is target type
                nodeFilter.Body = _f.And(_f.IsType(current, nodeFilter.XmlType!), nodeFilter.Body);
            }

            SetPriority(nodeset, 0.5);
            return nodeset;
        }

        public QilNode Function(string prefix, string name, IList<QilNode> args)
        {
            Debug.Assert(prefix.Length == 0);
            QilIterator i = _f.For(_fixupNode);
            QilNode matches;

            if (name == "id")
            {
                Debug.Assert(
                    args.Count == 1 && args[0].NodeType == QilNodeType.LiteralString,
                    "Function id() must have one literal string argument"
                );
                matches = _f.Id(i, args[0]);
            }
            else
            {
                Debug.Assert(name == "key", "Unexpected function");
                Debug.Assert(
                    args.Count == 2 &&
                    args[0].NodeType == QilNodeType.LiteralString && args[1].NodeType == QilNodeType.LiteralString,
                    "Function key() must have two literal string arguments"
                );
                matches = _environment.ResolveFunction(prefix, name, args, new XsltFunctionFocus(i));
            }

            QilIterator j;
            QilLoop result = _f.BaseFactory.Filter(i, _f.Not(_f.IsEmpty(_f.Filter(j = _f.For(matches), _f.Is(j, i)))));
            SetPriority(result, 0.5);
            SetLastParent(result, result);
            return result;
        }

        public QilNode String(string value) { return _f.String(value); }     // As argument of id() or key() function
        public QilNode Number(double value)
        {
            //Internal Error: Literal number is not allowed in XSLT pattern outside of predicate.
            throw new XmlException(SR.Xml_InternalError);
        }
        public QilNode Variable(string prefix, string name)
        {
            //Internal Error: Variable is not allowed in XSLT pattern outside of predicate.
            throw new XmlException(SR.Xml_InternalError);
        }

        // -------------------------------------- Priority / Parent ---------------------------------------

        private sealed class Annotation
        {
            public double Priority;
            public QilLoop? Parent;
        }

        public static void SetPriority(QilNode node, double priority)
        {
            Annotation ann = (Annotation?)node.Annotation ?? new Annotation();
            ann.Priority = priority;
            node.Annotation = ann;
        }

        public static double GetPriority(QilNode node)
        {
            return ((Annotation)node.Annotation!).Priority;
        }

        private static void SetLastParent(QilNode node, QilLoop parent)
        {
            Debug.Assert(parent.NodeType == QilNodeType.Filter);
            Annotation ann = (Annotation?)node.Annotation ?? new Annotation();
            ann.Parent = parent;
            node.Annotation = ann;
        }

        private static QilLoop? GetLastParent(QilNode node)
        {
            return ((Annotation)node.Annotation!).Parent;
        }

        public static void CleanAnnotation(QilNode node)
        {
            node.Annotation = null;
        }

        // -------------------------------------- GetPredicateBuilder() ---------------------------------------

        public IXPathBuilder<QilNode> GetPredicateBuilder(QilNode ctx)
        {
            QilLoop context = (QilLoop)ctx;
            Debug.Assert(context != null, "Predicate always has step so it can't have context == null");
            Debug.Assert(context.Variable.NodeType == QilNodeType.For, "It shouldn't be Let, becaus predicates in PatternBuilder don't produce cached tuples.");
            return _predicateBuilder;
        }

        private sealed class XPathPredicateEnvironment : IXPathEnvironment
        {
            private readonly IXPathEnvironment _baseEnvironment;
            private readonly XPathQilFactory _f;
            public readonly XPathBuilder.FixupVisitor fixupVisitor;
            private readonly QilNode _fixupCurrent, _fixupPosition, _fixupLast;

            // Number of unresolved fixup nodes
            public int numFixupCurrent, numFixupPosition, numFixupLast;

            public XPathPredicateEnvironment(IXPathEnvironment baseEnvironment)
            {
                _baseEnvironment = baseEnvironment;
                _f = baseEnvironment.Factory;
                _fixupCurrent = _f.Unknown(T.NodeNotRtf);
                _fixupPosition = _f.Unknown(T.DoubleX);
                _fixupLast = _f.Unknown(T.DoubleX);
                this.fixupVisitor = new XPathBuilder.FixupVisitor(_f, _fixupCurrent, _fixupPosition, _fixupLast);
            }

            /*  ----------------------------------------------------------------------------
                IXPathEnvironment interface
            */
            public XPathQilFactory Factory { get { return _f; } }

            public QilNode ResolveVariable(string prefix, string name)
            {
                return _baseEnvironment.ResolveVariable(prefix, name);
            }
            public QilNode ResolveFunction(string prefix, string name, IList<QilNode> args, IFocus env)
            {
                return _baseEnvironment.ResolveFunction(prefix, name, args, env);
            }
            public string ResolvePrefix(string prefix)
            {
                return _baseEnvironment.ResolvePrefix(prefix);
            }

            public QilNode GetCurrent() { numFixupCurrent++; return _fixupCurrent; }
            public QilNode GetPosition() { numFixupPosition++; return _fixupPosition; }
            public QilNode GetLast() { numFixupLast++; return _fixupLast; }
        }

        private sealed class XsltFunctionFocus : IFocus
        {
            private readonly QilIterator _current;

            public XsltFunctionFocus(QilIterator current)
            {
                Debug.Assert(current != null);
                _current = current;
            }

            /*  ----------------------------------------------------------------------------
                IFocus interface
            */
            public QilNode GetCurrent()
            {
                return _current;
            }

            public QilNode GetPosition()
            {
                Debug.Fail("GetPosition() must not be called");
                return null;
            }

            public QilNode GetLast()
            {
                Debug.Fail("GetLast() must not be called");
                return null;
            }
        }
    }
}
