// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Xsl.XPath;
using System.Xml.Xsl.Qil;
using T = System.Xml.Xsl.XmlQueryTypeFactory;

namespace System.Xml.Xsl.Xslt
{
    // <spec>http://www.w3.org/TR/xslt20/#dt-singleton-focus</spec>
    internal enum SingletonFocusType
    {
        // No context set
        // Used to prevent bugs
        None,

        // Document node of the document containing the initial context node
        // Used while compiling global variables and params
        InitialDocumentNode,

        // Initial context node for the transformation
        // Used while compiling initial apply-templates
        InitialContextNode,

        // Context node is specified by iterator
        // Used while compiling keys
        Iterator,
    }

    internal struct SingletonFocus : IFocus
    {
        private readonly XPathQilFactory _f;
        private SingletonFocusType _focusType;
        private QilIterator? _current;

        public SingletonFocus(XPathQilFactory f)
        {
            _f = f;
            _focusType = SingletonFocusType.None;
            _current = null;
        }

        public void SetFocus(SingletonFocusType focusType)
        {
            Debug.Assert(focusType != SingletonFocusType.Iterator);
            _focusType = focusType;
        }

        public void SetFocus(QilIterator? current)
        {
            if (current != null)
            {
                _focusType = SingletonFocusType.Iterator;
                _current = current;
            }
            else
            {
                _focusType = SingletonFocusType.None;
                _current = null;
            }
        }

        [Conditional("DEBUG")]
        private void CheckFocus()
        {
            Debug.Assert(_focusType != SingletonFocusType.None, "Focus is not set, call SetFocus first");
        }

        public QilNode GetCurrent()
        {
            CheckFocus();
            switch (_focusType)
            {
                case SingletonFocusType.InitialDocumentNode: return _f.Root(_f.XmlContext());
                case SingletonFocusType.InitialContextNode: return _f.XmlContext();
                default:
                    Debug.Assert(_focusType == SingletonFocusType.Iterator && _current != null, "Unexpected singleton focus type");
                    return _current;
            }
        }

        public QilNode GetPosition()
        {
            CheckFocus();
            return _f.Double(1);
        }

        public QilNode GetLast()
        {
            CheckFocus();
            return _f.Double(1);
        }
    }

    internal struct FunctionFocus : IFocus
    {
        private bool _isSet;
        private QilParameter? _current, _position, _last;

        public void StartFocus(IList<QilNode> args, XslFlags flags)
        {
            Debug.Assert(!IsFocusSet, "Focus was already set");
            int argNum = 0;
            if ((flags & XslFlags.Current) != 0)
            {
                _current = (QilParameter)args[argNum++];
                Debug.Assert(_current.Name!.NamespaceUri == XmlReservedNs.NsXslDebug && _current.Name.LocalName == "current");
            }
            if ((flags & XslFlags.Position) != 0)
            {
                _position = (QilParameter)args[argNum++];
                Debug.Assert(_position.Name!.NamespaceUri == XmlReservedNs.NsXslDebug && _position.Name.LocalName == "position");
            }
            if ((flags & XslFlags.Last) != 0)
            {
                _last = (QilParameter)args[argNum++];
                Debug.Assert(_last.Name!.NamespaceUri == XmlReservedNs.NsXslDebug && _last.Name.LocalName == "last");
            }
            _isSet = true;
        }
        public void StopFocus()
        {
            Debug.Assert(IsFocusSet, "Focus was not set");
            _isSet = false;
            _current = _position = _last = null;
        }
        public bool IsFocusSet
        {
            get { return _isSet; }
        }

        public QilNode GetCurrent()
        {
            Debug.Assert(_current != null, "Naked current() is not expected in this function");
            return _current;
        }

        public QilNode GetPosition()
        {
            Debug.Assert(_position != null, "Naked position() is not expected in this function");
            return _position;
        }

        public QilNode GetLast()
        {
            Debug.Assert(_last != null, "Naked last() is not expected in this function");
            return _last;
        }
    }

    internal struct LoopFocus : IFocus
    {
        private readonly XPathQilFactory _f;
        private QilIterator? _current, _cached, _last;

        public LoopFocus(XPathQilFactory f)
        {
            _f = f;
            _current = _cached = _last = null;
        }

        public void SetFocus(QilIterator current)
        {
            _current = current;
            _cached = _last = null;
        }

        public bool IsFocusSet
        {
            get { return _current != null; }
        }

        public QilNode? GetCurrent()
        {
            return _current;
        }

        public QilNode GetPosition()
        {
            return _f.XsltConvert(_f.PositionOf(_current!), T.DoubleX);
        }

        public QilNode GetLast() =>
            // Create a let that will be fixed up later in ConstructLoop or by LastFixupVisitor
            _last ??= _f.Let(_f.Double(0));

        public void EnsureCache()
        {
            if (_cached == null)
            {
                _cached = _f.Let(_current!.Binding!);
                _current.Binding = _cached;
            }
        }

        public void Sort(QilNode? sortKeys)
        {
            if (sortKeys != null)
            {
                // If sorting is required, cache the input node-set to support last() within sort key expressions
                EnsureCache();
                // The rest of the loop content must be compiled in the context of already sorted node-set
                _current = _f.For(_f.Sort(_current!, sortKeys));
            }
        }

        public QilLoop ConstructLoop(QilNode body)
        {
            QilLoop result;
            if (_last != null)
            {
                // last() encountered either in the sort keys or in the body of the current loop
                EnsureCache();
                _last.Binding = _f.XsltConvert(_f.Length(_cached!), T.DoubleX);
            }

            result = _f.BaseFactory.Loop(_current!, body);
            if (_last != null)
            {
                result = _f.BaseFactory.Loop(_last, result);
            }
            if (_cached != null)
            {
                result = _f.BaseFactory.Loop(_cached, result);
            }
            return result;
        }
    }
}
