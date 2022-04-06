// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema
{
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Collections;
    using System.Xml.Schema;
    using MS.Internal.Xml.XPath;
    using System.Diagnostics.CodeAnalysis;

    /*--------------------------------------------------------------------------------------------- *
     * Dynamic Part Below...                                                                        *
     * -------------------------------------------------------------------------------------------- */

    // stack element class
    // this one needn't change, even the parameter in methods
    internal sealed class AxisElement
    {
        internal DoubleLinkAxis curNode;                // current under-checking node during navigating
        internal int rootDepth;                         // root depth -- contextDepth + 1 if ! isDss; context + {1...} if isDss
        internal int curDepth;                          // current depth
        internal bool isMatch;                          // current is already matching or waiting for matching

        internal DoubleLinkAxis CurNode
        {
            get { return this.curNode; }
        }

        // constructor
        internal AxisElement(DoubleLinkAxis node, int depth)
        {
            this.curNode = node;
            this.rootDepth = this.curDepth = depth;
            this.isMatch = false;
        }

        internal void SetDepth(int depth)
        {
            this.rootDepth = this.curDepth = depth;
        }

        // "a/b/c"     pointer from b move to a
        // needn't change even tree structure changes
        internal void MoveToParent(int depth, ForwardAxis parent)
        {
            // "a/b/c", trying to match b (current node), but meet the end of a, so move pointer to a
            if (depth == this.curDepth - 1)
            {
                // really need to move the current node pointer to parent
                // what i did here is for seperating the case of IsDss or only IsChild
                // bcoz in the first case i need to expect "a" from random depth
                // -1 means it doesn't expect some specific depth (referecing the dealing to -1 in movetochild method
                // while in the second case i can't change the root depth which is 1.
                if ((this.curNode.Input == parent.RootNode) && (parent.IsDss))
                {
                    this.curNode = parent.RootNode;
                    this.rootDepth = this.curDepth = -1;
                    return;
                }
                else if (this.curNode.Input != null)
                {      // else cur-depth --, cur-node change
                    this.curNode = (DoubleLinkAxis)(this.curNode.Input);
                    this.curDepth--;
                    return;
                }
                else return;
            }
            // "a/b/c", trying to match b (current node), but meet the end of x (another child of a)
            // or maybe i matched, now move out the current node
            // or move out after failing to match attribute
            // the node i m next expecting is still the current node
            else if (depth == this.curDepth)
            {              // after matched or [2] failed in matching attribute
                if (this.isMatch)
                {
                    this.isMatch = false;
                }
            }
            return;                                         // this node is still what i am expecting
            // ignore
        }

        // equal & ! attribute then move
        // "a/b/c"     pointer from a move to b
        // return true if reach c and c is an element and c is the axis
        internal bool MoveToChild(string name, string? URN, int depth, ForwardAxis parent)
        {
            // an attribute can never be the same as an element
            if (Asttree.IsAttribute(this.curNode))
            {
                return false;
            }

            // either moveToParent or moveToChild status will have to be changed into unmatch...
            if (this.isMatch)
            {
                this.isMatch = false;
            }

            if (!AxisStack.Equal(this.curNode.Name, this.curNode.Urn, name, URN))
            {
                return false;
            }

            if (this.curDepth == -1)
            {
                SetDepth(depth);
            }
            else if (depth > this.curDepth)
            {
                return false;
            }

            // matched ...
            if (this.curNode == parent.TopNode)
            {
                this.isMatch = true;
                return true;
            }

            // move down this.curNode
            DoubleLinkAxis nowNode = (DoubleLinkAxis)(this.curNode.Next!);
            if (Asttree.IsAttribute(nowNode))
            {
                this.isMatch = true;                    // for attribute
                return false;
            }

            this.curNode = nowNode;
            this.curDepth++;
            return false;
        }
    }

    internal sealed class AxisStack
    {
        // property
        private readonly ArrayList _stack;                            // of AxisElement
        private readonly ForwardAxis _subtree;                        // reference to the corresponding subtree
        private readonly ActiveAxis _parent;

        internal ForwardAxis Subtree
        {
            get { return _subtree; }
        }

        internal int Length
        {                               // stack length
            get { return _stack.Count; }
        }

        // instructor
        public AxisStack(ForwardAxis faxis, ActiveAxis parent)
        {
            _subtree = faxis;
            _stack = new ArrayList();
            _parent = parent;       // need to use its contextdepth each time....

            // improvement:
            // if ! isDss, there has nothing to do with Push/Pop, only one copy each time will be kept
            // if isDss, push and pop each time....
            if (!faxis.IsDss)
            {                // keep an instance
                this.Push(1);              // context depth + 1
            }
            // else just keep stack empty
        }

        // method
        internal void Push(int depth)
        {
            AxisElement eaxis = new AxisElement(_subtree.RootNode, depth);
            _stack.Add(eaxis);
        }

        internal void Pop()
        {
            _stack.RemoveAt(Length - 1);
        }

        // used in the beginning of .//  and MoveToChild
        // didn't consider Self, only consider name
        internal static bool Equal(string thisname, string? thisURN, string name, string? URN)
        {
            // which means "b" in xpath, no namespace should be specified
            if (thisURN == null)
            {
                if (!((URN == null) || (URN.Length == 0)))
                {
                    return false;
                }
            }
            // != "*"
            else if ((thisURN.Length != 0) && (thisURN != URN))
            {
                return false;
            }
            // != "a:*" || "*"
            if ((thisname.Length != 0) && (thisname != name))
            {
                return false;
            }
            return true;
        }

        // "a/b/c"     pointer from b move to a
        // needn't change even tree structure changes
        internal void MoveToParent(string name, string? URN, int depth)
        {
            if (_subtree.IsSelfAxis)
            {
                return;
            }

            for (int i = 0; i < _stack.Count; ++i)
            {
                ((AxisElement)_stack[i]!).MoveToParent(depth, _subtree);
            }

            // in ".//"'s case, since each time you push one new element while match, why not pop one too while match?
            if (_subtree.IsDss && Equal(_subtree.RootNode.Name, _subtree.RootNode.Urn, name, URN))
            {
                Pop();
            }   // only the last one
        }

        // "a/b/c"     pointer from a move to b
        // return true if reach c
        internal bool MoveToChild(string name, string? URN, int depth)
        {
            bool result = false;
            // push first
            if (_subtree.IsDss && Equal(_subtree.RootNode.Name, _subtree.RootNode.Urn, name, URN))
            {
                Push(-1);
            }
            for (int i = 0; i < _stack.Count; ++i)
            {
                if (((AxisElement)_stack[i]!).MoveToChild(name, URN, depth, _subtree))
                {
                    result = true;
                }
            }
            return result;
        }

        // attribute can only at the topaxis part
        // dealing with attribute only here, didn't go into stack element at all
        // stack element only deal with moving the pointer around elements
        internal bool MoveToAttribute(string name, string? URN, int depth)
        {
            if (!_subtree.IsAttribute)
            {
                return false;
            }
            if (!Equal(_subtree.TopNode.Name, _subtree.TopNode.Urn, name, URN))
            {
                return false;
            }

            bool result = false;

            // no stack element for single attribute, so dealing with it separately
            if (_subtree.TopNode.Input == null)
            {
                return (_subtree.IsDss || (depth == 1));
            }

            for (int i = 0; i < _stack.Count; ++i)
            {
                AxisElement eaxis = (AxisElement)_stack[i]!;
                if ((eaxis.isMatch) && (eaxis.CurNode == _subtree.TopNode.Input))
                {
                    result = true;
                }
            }
            return result;
        }
    }

    // whenever an element is under identity-constraint, an instance of this class will be called
    // only care about property at this time
    internal class ActiveAxis
    {
        // consider about reactivating....  the stack should be clear right??
        // just reset contextDepth & isActive....
        private int _currentDepth;                       // current depth, trace the depth by myself... movetochild, movetoparent, movetoattribute
        private bool _isActive;                          // not active any more after moving out context node
        private readonly Asttree _axisTree;                       // reference to the whole tree
        // for each subtree i need to keep a stack...
        private readonly ArrayList _axisStack;                    // of AxisStack

        public int CurrentDepth
        {
            get { return _currentDepth; }
        }

        // if an instance is !IsActive, then it can be reactive and reuse
        // still need thinking.....
        internal void Reactivate()
        {
            _isActive = true;
            _currentDepth = -1;
        }

        internal ActiveAxis(Asttree axisTree)
        {
            _axisTree = axisTree;                                               // only a pointer.  do i need it?
            _currentDepth = -1;                                                 // context depth is 0 -- enforce moveToChild for the context node
                                                                                // otherwise can't deal with "." node
            _axisStack = new ArrayList(axisTree.SubtreeArray.Count);            // defined length
            // new one stack element for each one
            for (int i = 0; i < axisTree.SubtreeArray.Count; ++i)
            {
                AxisStack stack = new AxisStack((ForwardAxis)axisTree.SubtreeArray[i]!, this);
                _axisStack.Add(stack);
            }
            _isActive = true;
        }

        public bool MoveToStartElement(string localname, string? URN)
        {
            if (!_isActive)
            {
                return false;
            }

            // for each:
            _currentDepth++;
            bool result = false;
            for (int i = 0; i < _axisStack.Count; ++i)
            {
                AxisStack stack = (AxisStack)_axisStack[i]!;
                // special case for self tree   "." | ".//."
                if (stack.Subtree.IsSelfAxis)
                {
                    if (stack.Subtree.IsDss || (this.CurrentDepth == 0))
                        result = true;
                    continue;
                }

                // otherwise if it's context node then return false
                if (this.CurrentDepth == 0) continue;

                if (stack.MoveToChild(localname, URN, _currentDepth))
                {
                    result = true;
                    // even already know the last result is true, still need to continue...
                    // run everyone once
                }
            }

            return result;
        }

        // return result doesn't have any meaning until in SelectorActiveAxis
        public virtual bool EndElement(string localname, string? URN)
        {
            // need to think if the early quitting will affect reactivating....
            if (_currentDepth == 0)
            {          // leave context node
                _isActive = false;
                _currentDepth--;
            }
            if (!_isActive)
            {
                return false;
            }
            for (int i = 0; i < _axisStack.Count; ++i)
            {
                ((AxisStack)_axisStack[i]!).MoveToParent(localname, URN, _currentDepth);
            }
            _currentDepth--;
            return false;
        }

        // Secondly field interface
        public bool MoveToAttribute(string localname, string? URN)
        {
            if (!_isActive)
            {
                return false;
            }

            bool result = false;
            for (int i = 0; i < _axisStack.Count; ++i)
            {
                if (((AxisStack)_axisStack[i]!).MoveToAttribute(localname, URN, _currentDepth + 1))
                {  // don't change depth for attribute, but depth is add 1
                    result = true;
                }
            }

            return result;
        }
    }

    /* ---------------------------------------------------------------------------------------------- *
     * Static Part Below...                                                                           *
     * ---------------------------------------------------------------------------------------------- */

    // each node in the xpath tree
    internal sealed class DoubleLinkAxis : Axis
    {
        internal Axis? next;

        internal Axis? Next
        {
            get { return this.next; }
            set { this.next = value; }
        }

        //constructor
        internal DoubleLinkAxis(Axis axis, DoubleLinkAxis? inputaxis)
            : base(axis.TypeOfAxis, inputaxis, axis.Prefix, axis.Name, axis.NodeType)
        {
            this.next = null;
            this.Urn = axis.Urn;
            this.abbrAxis = axis.AbbrAxis;
            if (inputaxis != null)
            {
                inputaxis.Next = this;
            }
        }

        // recursive here
        [return: NotNullIfNotNull("axis")]
        internal static DoubleLinkAxis? ConvertTree(Axis? axis)
        {
            if (axis == null)
            {
                return null;
            }

            return new DoubleLinkAxis(axis, ConvertTree((Axis?)axis.Input));
        }
    }



    // only keep axis, rootNode, isAttribute, isDss inside
    // act as an element tree for the Asttree
    internal sealed class ForwardAxis
    {
        // Axis tree
        private readonly DoubleLinkAxis _topNode;
        private readonly DoubleLinkAxis _rootNode;                // the root for reverse Axis

        // Axis tree property
        private readonly bool _isAttribute;                       // element or attribute?      "@"?
        private readonly bool _isDss;                             // has ".//" in front of it?
        private readonly bool _isSelfAxis;                        // only one node in the tree, and it's "." (self) node

        internal DoubleLinkAxis RootNode
        {
            get { return _rootNode; }
        }

        internal DoubleLinkAxis TopNode
        {
            get { return _topNode; }
        }

        internal bool IsAttribute
        {
            get { return _isAttribute; }
        }

        // has ".//" in front of it?
        internal bool IsDss
        {
            get { return _isDss; }
        }

        internal bool IsSelfAxis
        {
            get { return _isSelfAxis; }
        }

        public ForwardAxis(DoubleLinkAxis axis, bool isdesorself)
        {
            _isDss = isdesorself;
            _isAttribute = Asttree.IsAttribute(axis);
            _topNode = axis;
            _rootNode = axis;
            while (_rootNode.Input != null)
            {
                _rootNode = (DoubleLinkAxis)(_rootNode.Input);
            }
            // better to calculate it out, since it's used so often, and if the top is self then the whole tree is self
            _isSelfAxis = Asttree.IsSelf(_topNode);
        }
    }

    // static, including an array of ForwardAxis  (this is the whole picture)
    internal sealed class Asttree
    {
        // set private then give out only get access, to keep it intact all along
        private ArrayList _fAxisArray = null!;
        private readonly string _xpathexpr;
        private readonly bool _isField;                                   // field or selector
        private readonly XmlNamespaceManager _nsmgr;

        internal ArrayList SubtreeArray
        {
            get { return _fAxisArray; }
        }

        // when making a new instance for Asttree, we do the compiling, and create the static tree instance
        public Asttree(string xPath, bool isField, XmlNamespaceManager nsmgr)
        {
            _xpathexpr = xPath;
            _isField = isField;
            _nsmgr = nsmgr;
            // checking grammar... and build fAxisArray
            this.CompileXPath(xPath, isField, nsmgr);          // might throw exception in the middle
        }

        // this part is for parsing restricted xpath from grammar
        private static bool IsNameTest(Axis ast)
        {
            // Type = Element, abbrAxis = false
            // all are the same, has child:: or not
            return ((ast.TypeOfAxis == Axis.AxisType.Child) && (ast.NodeType == XPathNodeType.Element));
        }

        internal static bool IsAttribute(Axis ast)
        {
            return ((ast.TypeOfAxis == Axis.AxisType.Attribute) && (ast.NodeType == XPathNodeType.Attribute));
        }

        private static bool IsDescendantOrSelf(Axis ast)
        {
            return ((ast.TypeOfAxis == Axis.AxisType.DescendantOrSelf) && (ast.NodeType == XPathNodeType.All) && (ast.AbbrAxis));
        }

        internal static bool IsSelf(Axis ast)
        {
            return ((ast.TypeOfAxis == Axis.AxisType.Self) && (ast.NodeType == XPathNodeType.All) && (ast.AbbrAxis));
        }

        // don't return true or false, if it's invalid path, just throw exception during the process
        // for whitespace thing, i will directly trim the tree built here...
        public void CompileXPath(string xPath, bool isField, XmlNamespaceManager nsmgr)
        {
            if ((xPath == null) || (xPath.Length == 0))
            {
                throw new XmlSchemaException(SR.Sch_EmptyXPath, string.Empty);
            }

            // firstly i still need to have an ArrayList to store tree only...
            // can't new ForwardAxis right away
            string[] xpath = xPath.Split('|');
            ArrayList AstArray = new ArrayList(xpath.Length);
            _fAxisArray = new ArrayList(xpath.Length);

            // throw compile exceptions
            // can i only new one builder here then run compile several times??
            try
            {
                for (int i = 0; i < xpath.Length; ++i)
                {
                    // default ! isdesorself (no .//)
                    Axis ast = (Axis)(XPathParser.ParseXPathExpression(xpath[i]));
                    AstArray.Add(ast);
                }
            }
            catch
            {
                throw new XmlSchemaException(SR.Sch_ICXpathError, xPath);
            }

            Axis? stepAst;
            for (int i = 0; i < AstArray.Count; ++i)
            {
                Axis ast = (Axis)AstArray[i]!;
                // Restricted form
                // field can have an attribute:

                // throw exceptions during casting
                if ((stepAst = ast) == null)
                {
                    throw new XmlSchemaException(SR.Sch_ICXpathError, xPath);
                }

                Axis top = stepAst;

                // attribute will have namespace too
                // field can have top attribute
                if (IsAttribute(stepAst))
                {
                    if (!isField)
                    {
                        throw new XmlSchemaException(SR.Sch_SelectorAttr, xPath);
                    }
                    else
                    {
                        SetURN(stepAst, nsmgr);
                        try
                        {
                            stepAst = (Axis?)(stepAst.Input);
                        }
                        catch
                        {
                            throw new XmlSchemaException(SR.Sch_ICXpathError, xPath);
                        }
                    }
                }

                // field or selector
                while ((stepAst != null) && (IsNameTest(stepAst) || IsSelf(stepAst)))
                {
                    // trim tree "." node, if it's not the top one
                    if (IsSelf(stepAst) && (ast != stepAst))
                    {
                        top.Input = stepAst.Input;
                    }
                    else
                    {
                        top = stepAst;
                        // set the URN
                        if (IsNameTest(stepAst))
                        {
                            SetURN(stepAst, nsmgr);
                        }
                    }
                    try
                    {
                        stepAst = (Axis?)(stepAst.Input);
                    }
                    catch
                    {
                        throw new XmlSchemaException(SR.Sch_ICXpathError, xPath);
                    }
                }

                // the rest part can only be .// or null
                // trim the rest part, but need compile the rest part first
                top.Input = null;
                if (stepAst == null)
                {      // top "." and has other element beneath, trim this "." node too
                    if (IsSelf(ast) && (ast.Input != null))
                    {
                        _fAxisArray.Add(new ForwardAxis(DoubleLinkAxis.ConvertTree((Axis)(ast.Input)), false));
                    }
                    else
                    {
                        _fAxisArray.Add(new ForwardAxis(DoubleLinkAxis.ConvertTree(ast), false));
                    }
                    continue;
                }
                if (!IsDescendantOrSelf(stepAst))
                {
                    throw new XmlSchemaException(SR.Sch_ICXpathError, xPath);
                }
                try
                {
                    stepAst = (Axis?)(stepAst.Input);
                }
                catch
                {
                    throw new XmlSchemaException(SR.Sch_ICXpathError, xPath);
                }
                if ((stepAst == null) || (!IsSelf(stepAst)) || (stepAst.Input != null))
                {
                    throw new XmlSchemaException(SR.Sch_ICXpathError, xPath);
                }

                // trim top "." if it's not the only node
                if (IsSelf(ast) && (ast.Input != null))
                {
                    _fAxisArray.Add(new ForwardAxis(DoubleLinkAxis.ConvertTree((Axis)(ast.Input)), true));
                }
                else
                {
                    _fAxisArray.Add(new ForwardAxis(DoubleLinkAxis.ConvertTree(ast), true));
                }
            }
        }

        // depending on axis.Name & axis.Prefix, i will set the axis.URN;
        // also, record urn from prefix during this
        // 4 different types of element or attribute (with @ before it) combinations:
        // (1) a:b (2) b (3) * (4) a:*
        // i will check xpath to be strictly conformed from these forms
        // for (1) & (4) i will have URN set properly
        // for (2) the URN is null
        // for (3) the URN is empty
        private static void SetURN(Axis axis, XmlNamespaceManager nsmgr)
        {
            if (axis.Prefix.Length != 0)
            {      // (1) (4)
                axis.Urn = nsmgr.LookupNamespace(axis.Prefix);

                if (axis.Urn == null)
                {
                    throw new XmlSchemaException(SR.Sch_UnresolvedPrefix, axis.Prefix);
                }
            }
            else if (axis.Name.Length != 0)
            { // (2)
                axis.Urn = null;
            }
            else
            {                            // (3)
                axis.Urn = "";
            }
        }
    }// Asttree
}
