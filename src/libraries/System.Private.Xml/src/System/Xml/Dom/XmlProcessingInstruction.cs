// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml.XPath;

namespace System.Xml
{
    // Represents a processing instruction, which XML defines to keep
    // processor-specific information in the text of the document.
    public class XmlProcessingInstruction : XmlLinkedNode
    {
        private readonly string _target;
        private string _data;

        protected internal XmlProcessingInstruction(string target, string? data, XmlDocument doc) : base(doc)
        {
            ArgumentException.ThrowIfNullOrEmpty(target);

            _target = target;
            _data = data ?? string.Empty;
        }

        /// <inheritdoc />
        public override string Name => _target;

        /// <inheritdoc />
        public override string LocalName => Name;

        /// <inheritdoc />
        [AllowNull]
        public override string Value
        {
            get => _data;
            set { Data = value; } // uses Data instead of data so that event will be fired and null will be normalized to empty string
        }

        // Gets the target of the processing instruction.
        public string Target => _target;

        // Gets or sets the content of processing instruction,
        // excluding the target.
        [AllowNull]
        public string Data
        {
            get => _data;
            set
            {
                XmlNode? parent = ParentNode;
                string val = value ?? string.Empty;
                XmlNodeChangedEventArgs? args = GetEventArgs(this, parent, parent, _data, val, XmlNodeChangedAction.Change);

                if (args != null)
                {
                    BeforeEvent(args);
                }

                _data = val;

                if (args != null)
                {
                    AfterEvent(args);
                }
            }
        }

        /// <inheritdoc />
        [AllowNull]
        public override string InnerText
        {
            get => _data;
            set { Data = value; } // uses Data instead of data so that event will be fired and null will be normalized to empty string
        }

        /// <inheritdoc />
        public override XmlNodeType NodeType => XmlNodeType.ProcessingInstruction;

        /// <inheritdoc />
        public override XmlNode CloneNode(bool deep)
        {
            Debug.Assert(OwnerDocument != null);
            return OwnerDocument.CreateProcessingInstruction(_target, _data);
        }

        /// <inheritdoc />
        public override void WriteTo(XmlWriter w)
        {
            w.WriteProcessingInstruction(_target, _data);
        }

        /// <inheritdoc />
        public override void WriteContentTo(XmlWriter w)
        {
            // Intentionally do nothing
        }

        internal override string XPLocalName => Name;
        internal override XPathNodeType XPNodeType => XPathNodeType.ProcessingInstruction;
    }
}
