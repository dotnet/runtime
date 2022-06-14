// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace System.Xml
{
    internal sealed class XmlElementList : XmlNodeList
    {
        private readonly string _asterisk = null!;
        private int _changeCount; //recording the total number that the dom tree has been changed ( insertion and deletion )
        //the member vars below are saved for further reconstruction
        private readonly string? _name;         //only one of 2 string groups will be initialized depends on which constructor is called.
        private string? _localName;
        private string? _namespaceURI;
        private readonly XmlNode _rootNode;
        // the member vars below serves the optimization of accessing of the elements in the list
        private int _curInd;       // -1 means the starting point for a new search round
        private XmlNode _curElem;      // if sets to rootNode, means the starting point for a new search round
        private bool _empty;        // whether the list is empty
        private bool _atomized;     //whether the localname and namespaceuri are atomized
        private int _matchCount;   // cached list count. -1 means it needs reconstruction

        private WeakReference<XmlElementListListener>? _listener;

        private XmlElementList(XmlNode parent)
        {
            Debug.Assert(parent != null);
            Debug.Assert(parent.NodeType == XmlNodeType.Element || parent.NodeType == XmlNodeType.Document);
            _rootNode = parent;
            Debug.Assert(parent.Document != null);
            _curInd = -1;
            _curElem = _rootNode;
            _changeCount = 0;
            _empty = false;
            _atomized = true;
            _matchCount = -1;
            // This can be a regular reference, but it would cause some kind of loop inside the GC
            _listener = new WeakReference<XmlElementListListener>(new XmlElementListListener(parent.Document, this));
        }

        ~XmlElementList()
        {
            Dispose(false);
        }

        internal void ConcurrencyCheck(XmlNodeChangedEventArgs args)
        {
            if (_atomized == false)
            {
                XmlNameTable nameTable = _rootNode.Document.NameTable;
                _localName = nameTable.Add(_localName!);
                _namespaceURI = nameTable.Add(_namespaceURI!);
                _atomized = true;
            }

            if (IsMatch(args.Node!))
            {
                _changeCount++;
                _curInd = -1;
                _curElem = _rootNode;
                if (args.Action == XmlNodeChangedAction.Insert)
                    _empty = false;
            }

            _matchCount = -1;
        }

        internal XmlElementList(XmlNode parent, string name) : this(parent)
        {
            Debug.Assert(parent.Document != null);
            XmlNameTable nt = parent.Document.NameTable;
            Debug.Assert(nt != null);
            _asterisk = nt.Add("*");
            _name = nt.Add(name);
            _localName = null;
            _namespaceURI = null;
        }

        internal XmlElementList(XmlNode parent, string localName, string namespaceURI) : this(parent)
        {
            Debug.Assert(parent.Document != null);
            XmlNameTable nt = parent.Document.NameTable;
            Debug.Assert(nt != null);
            _asterisk = nt.Add("*");
            _localName = nt.Get(localName);
            _namespaceURI = nt.Get(namespaceURI);
            if ((_localName == null) || (_namespaceURI == null))
            {
                _empty = true;
                _atomized = false;
                _localName = localName;
                _namespaceURI = namespaceURI;
            }

            _name = null;
        }

        internal int ChangeCount
        {
            get { return _changeCount; }
        }

        // return the next element node that is in PreOrder
        private XmlNode? NextElemInPreOrder(XmlNode curNode)
        {
            Debug.Assert(curNode != null);
            //For preorder walking, first try its child
            XmlNode? retNode = curNode.FirstChild;
            if (retNode == null)
            {
                //if no child, the next node forward will the be the NextSibling of the first ancestor which has NextSibling
                //so, first while-loop find out such an ancestor (until no more ancestor or the ancestor is the rootNode
                retNode = curNode;
                while (retNode != null
                        && retNode != _rootNode
                        && retNode.NextSibling == null)
                {
                    retNode = retNode.ParentNode;
                }
                //then if such ancestor exists, set the retNode to its NextSibling
                if (retNode != null && retNode != _rootNode)
                    retNode = retNode.NextSibling;
            }
            if (retNode == _rootNode)
                //if reach the rootNode, consider having walked through the whole tree and no more element after the curNode
                retNode = null;
            return retNode;
        }

        // return the previous element node that is in PreOrder
        private XmlNode? PrevElemInPreOrder(XmlNode curNode)
        {
            Debug.Assert(curNode != null);
            //For preorder walking, the previous node will be the right-most node in the tree of PreviousSibling of the curNode
            XmlNode? retNode = curNode.PreviousSibling;
            // so if the PreviousSibling is not null, going through the tree down to find the right-most node
            while (retNode != null)
            {
                if (retNode.LastChild == null)
                    break;
                retNode = retNode.LastChild;
            }
            // if no PreviousSibling, the previous node will be the curNode's parentNode
            if (retNode == null)
                retNode = curNode.ParentNode;
            // if the final retNode is rootNode, consider having walked through the tree and no more previous node
            if (retNode == _rootNode)
                retNode = null;
            return retNode;
        }

        // if the current node a matching element node
        private bool IsMatch(XmlNode curNode)
        {
            if (curNode.NodeType == XmlNodeType.Element)
            {
                if (_name != null)
                {
                    if (Ref.Equal(_name, _asterisk) || Ref.Equal(curNode.Name, _name))
                        return true;
                }
                else
                {
                    if (
                        (Ref.Equal(_localName, _asterisk) || Ref.Equal(curNode.LocalName, _localName)) &&
                        (Ref.Equal(_namespaceURI, _asterisk) || curNode.NamespaceURI == _namespaceURI)
                    )
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private XmlNode? GetMatchingNode(XmlNode n, bool bNext)
        {
            Debug.Assert(n != null);
            XmlNode? node = n;
            do
            {
                if (bNext)
                    node = NextElemInPreOrder(node);
                else
                    node = PrevElemInPreOrder(node);
            } while (node != null && !IsMatch(node));
            return node;
        }

        private XmlNode? GetNthMatchingNode(XmlNode n, bool bNext, int nCount)
        {
            Debug.Assert(n != null);
            XmlNode? node = n;
            for (int ind = 0; ind < nCount; ind++)
            {
                node = GetMatchingNode(node, bNext);
                if (node == null)
                    return null;
            }

            return node;
        }

        //the function is for the enumerator to find out the next available matching element node
        public XmlNode? GetNextNode(XmlNode? n)
        {
            if (_empty == true)
                return null;
            XmlNode node = n ?? _rootNode;
            return GetMatchingNode(node, true);
        }

        public override XmlNode? Item(int index)
        {
            if (_rootNode == null || index < 0)
                return null;

            if (_empty == true)
                return null;
            if (_curInd == index)
                return _curElem;
            int nDiff = index - _curInd;
            bool bForward = (nDiff > 0);
            if (nDiff < 0)
                nDiff = -nDiff;

            XmlNode? node;
            if ((node = GetNthMatchingNode(_curElem, bForward, nDiff)) != null)
            {
                _curInd = index;
                _curElem = node;
                return _curElem;
            }

            return null;
        }

        public override int Count
        {
            get
            {
                if (_empty == true)
                    return 0;

                if (_matchCount < 0)
                {
                    int currMatchCount = 0;
                    int currChangeCount = _changeCount;
                    XmlNode? node = _rootNode;
                    while ((node = GetMatchingNode(node, true)) != null)
                    {
                        currMatchCount++;
                    }

                    if (currChangeCount != _changeCount)
                    {
                        return currMatchCount;
                    }

                    _matchCount = currMatchCount;
                }

                return _matchCount;
            }
        }

        public override IEnumerator GetEnumerator()
        {
            if (_empty == true)
                return new XmlEmptyElementListEnumerator(this);

            return new XmlElementListEnumerator(this);
        }

        protected override void PrivateDisposeNodeList()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_listener != null)
            {
                if (_listener.TryGetTarget(out XmlElementListListener? listener))
                {
                    listener.Unregister();
                }

                _listener = null;
            }
        }
    }

    internal sealed class XmlElementListEnumerator : IEnumerator
    {
        private readonly XmlElementList _list;
        private XmlNode? _curElem;
        private int _changeCount; //save the total number that the dom tree has been changed ( insertion and deletion ) when this enumerator is created

        public XmlElementListEnumerator(XmlElementList list)
        {
            _list = list;
            _curElem = null;
            _changeCount = list.ChangeCount;
        }

        public bool MoveNext()
        {
            if (_list.ChangeCount != _changeCount)
            {
                //the number mismatch, there is new change(s) happened since last MoveNext() is called.
                throw new InvalidOperationException(SR.Xdom_Enum_ElementList);
            }
            else
            {
                _curElem = _list.GetNextNode(_curElem);
            }
            return _curElem != null;
        }

        public void Reset()
        {
            _curElem = null;
            //reset the number of changes to be synced with current dom tree as well
            _changeCount = _list.ChangeCount;
        }

        public object? Current
        {
            get { return _curElem; }
        }
    }

    internal sealed class XmlEmptyElementListEnumerator : IEnumerator
    {
        public XmlEmptyElementListEnumerator(XmlElementList list)
        {
        }

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
        }

        public object? Current
        {
            get { return null; }
        }
    }

    internal sealed class XmlElementListListener
    {
        private WeakReference<XmlElementList>? _elemList;
        private readonly XmlDocument _doc;
        private readonly XmlNodeChangedEventHandler _nodeChangeHandler;

        internal XmlElementListListener(XmlDocument doc, XmlElementList elemList)
        {
            _doc = doc;
            _elemList = new WeakReference<XmlElementList>(elemList);
            _nodeChangeHandler = new XmlNodeChangedEventHandler(this.OnListChanged);
            doc.NodeInserted += _nodeChangeHandler;
            doc.NodeRemoved += _nodeChangeHandler;
        }

        private void OnListChanged(object sender, XmlNodeChangedEventArgs args)
        {
            lock (this)
            {
                if (_elemList != null)
                {
                    if (_elemList.TryGetTarget(out XmlElementList? el))
                    {
                        el.ConcurrencyCheck(args);
                    }
                    else
                    {
                        _doc.NodeInserted -= _nodeChangeHandler;
                        _doc.NodeRemoved -= _nodeChangeHandler;
                        _elemList = null;
                    }
                }
            }
        }

        // This method is called from the finalizer of XmlElementList
        internal void Unregister()
        {
            lock (this)
            {
                if (_elemList != null)
                {
                    _doc.NodeInserted -= _nodeChangeHandler;
                    _doc.NodeRemoved -= _nodeChangeHandler;
                    _elemList = null;
                }
            }
        }
    }
}
