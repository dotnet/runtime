// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    public class XmlNodeChangedEventArgs : EventArgs
    {
        private XmlNodeChangedAction action;
        private XmlNode node;
        private XmlNode oldParent;
        private XmlNode newParent;
        private string oldValue;
        private string newValue;

        public XmlNodeChangedEventArgs(XmlNode node, XmlNode oldParent, XmlNode newParent, string oldValue, string newValue, XmlNodeChangedAction action)
        {
            this.node = node;
            this.oldParent = oldParent;
            this.newParent = newParent;
            this.action = action;
            this.oldValue = oldValue;
            this.newValue = newValue;
        }

        public XmlNodeChangedAction Action { get { return action; } }

        public XmlNode Node { get { return node; } }

        public XmlNode OldParent { get { return oldParent; } }

        public XmlNode NewParent { get { return newParent; } }

        public string OldValue { get { return oldValue; } }

        public string NewValue { get { return newValue; } }
    }
}
