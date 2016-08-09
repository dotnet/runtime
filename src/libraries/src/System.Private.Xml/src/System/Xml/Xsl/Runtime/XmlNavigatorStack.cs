// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.XPath;
using System.Diagnostics;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// A dynamic stack of IXmlNavigators.
    /// </summary>
    internal struct XmlNavigatorStack
    {
        private XPathNavigator[] _stkNav;    // Stack of XPathNavigators
        private int _sp;                     // Stack pointer (size of stack)

#if DEBUG
        private const int InitialStackSize = 2;
#else
        private const int InitialStackSize = 8;
#endif

        /// <summary>
        /// Push a navigator onto the stack
        /// </summary>
        public void Push(XPathNavigator nav)
        {
            if (_stkNav == null)
            {
                _stkNav = new XPathNavigator[InitialStackSize];
            }
            else
            {
                if (_sp >= _stkNav.Length)
                {
                    // Resize the stack
                    XPathNavigator[] stkOld = _stkNav;
                    _stkNav = new XPathNavigator[2 * _sp];
                    Array.Copy(stkOld, _stkNav, _sp);
                }
            }

            _stkNav[_sp++] = nav;
        }

        /// <summary>
        /// Pop the topmost navigator and return it
        /// </summary>
        public XPathNavigator Pop()
        {
            Debug.Assert(!IsEmpty);
            return _stkNav[--_sp];
        }

        /// <summary>
        /// Returns the navigator at the top of the stack without adjusting the stack pointer
        /// </summary>
        public XPathNavigator Peek()
        {
            Debug.Assert(!IsEmpty);
            return _stkNav[_sp - 1];
        }

        /// <summary>
        /// Remove all navigators from the stack
        /// </summary>
        public void Reset()
        {
            _sp = 0;
        }

        /// <summary>
        /// Returns true if there are no navigators in the stack
        /// </summary>
        public bool IsEmpty
        {
            get { return _sp == 0; }
        }
    }
}
