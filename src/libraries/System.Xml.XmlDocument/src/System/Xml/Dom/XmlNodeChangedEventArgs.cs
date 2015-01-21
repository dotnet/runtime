// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    public class XmlNodeChangedEventArgs : EventArgs
    {
        private readonly XmlNodeChangedAction _action;
        private readonly XmlNode _node;
        private readonly XmlNode _oldParent;
        private readonly XmlNode _newParent;
        private readonly string _oldValue;
        private readonly string _newValue;

        public XmlNodeChangedEventArgs(XmlNode node, XmlNode oldParent, XmlNode newParent, string oldValue, string newValue, XmlNodeChangedAction action)
        {
            _node = node;
            _oldParent = oldParent;
            _newParent = newParent;
            _action = action;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public XmlNodeChangedAction Action { get { return _action; } }

        public XmlNode Node { get { return _node; } }

        public XmlNode OldParent { get { return _oldParent; } }

        public XmlNode NewParent { get { return _newParent; } }

        public string OldValue { get { return _oldValue; } }

        public string NewValue { get { return _newValue; } }
    }
}
