// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Xml
{
    // Represents the xml declaration nodes: <?xml version='1.0' ...?>
    public class XmlDeclaration : XmlLinkedNode
    {
        private const string YES = "yes";
        private const string NO = "no";

        private string _version;
        private string _encoding;
        private string _standalone;

        protected internal XmlDeclaration(string version, string encoding, string standalone, XmlDocument doc) : base(doc)
        {
            if (!IsValidXmlVersion(version))
                throw new ArgumentException(SR.Xdom_Version);
            if ((standalone != null) && (standalone.Length > 0))
                if ((standalone != YES) && (standalone != NO))
                    throw new ArgumentException(SR.Format(SR.Xdom_standalone, standalone));
            this.Encoding = encoding;
            this.Standalone = standalone;
            this.Version = version;
        }


        // The version attribute for <?xml version= '1.0' ... ?>
        public string Version
        {
            get { return _version; }
            internal set { _version = value; }
        }

        // Specifies the value of the encoding attribute, as for
        // <?xml version= '1.0' encoding= 'UTF-8' ?>
        public string Encoding
        {
            get { return _encoding; }
            set { _encoding = ((value == null) ? string.Empty : value); }
        }

        // Specifies the value of the standalone attribute.
        public string Standalone
        {
            get { return _standalone; }
            set
            {
                if (value == null)
                    _standalone = string.Empty;
                else if (value.Length == 0 || value == YES || value == NO)
                    _standalone = value;
                else
                    throw new ArgumentException(SR.Format(SR.Xdom_standalone, value));
            }
        }

        public override string Value
        {
            get { return InnerText; }
            set { InnerText = value; }
        }


        // Gets or sets the concatenated values of the node and
        // all its children.
        public override string InnerText
        {
            get
            {
                StringBuilder strb = StringBuilderCache.Acquire();
                strb.Append("version=\"");
                strb.Append(Version);
                strb.Append('"');
                if (Encoding.Length > 0)
                {
                    strb.Append(" encoding=\"");
                    strb.Append(Encoding);
                    strb.Append('"');
                }
                if (Standalone.Length > 0)
                {
                    strb.Append(" standalone=\"");
                    strb.Append(Standalone);
                    strb.Append('"');
                }
                return StringBuilderCache.GetStringAndRelease(strb);
            }

            set
            {
                string tempVersion = null;
                string tempEncoding = null;
                string tempStandalone = null;
                string orgEncoding = this.Encoding;
                string orgStandalone = this.Standalone;
                string orgVersion = this.Version;

                XmlLoader.ParseXmlDeclarationValue(value, out tempVersion, out tempEncoding, out tempStandalone);

                try
                {
                    if (tempVersion != null && !IsValidXmlVersion(tempVersion))
                        throw new ArgumentException(SR.Xdom_Version);
                    Version = tempVersion;

                    if (tempEncoding != null)
                        Encoding = tempEncoding;
                    if (tempStandalone != null)
                        Standalone = tempStandalone;
                }
                catch
                {
                    Encoding = orgEncoding;
                    Standalone = orgStandalone;
                    Version = orgVersion;
                    throw;
                }
            }
        }

        //override methods and properties from XmlNode

        // Gets the name of the node.
        public override string Name
        {
            get
            {
                return "xml";
            }
        }

        // Gets the name of the current node without the namespace prefix.
        public override string LocalName
        {
            get { return Name; }
        }

        // Gets the type of the current node.
        public override XmlNodeType NodeType
        {
            get { return XmlNodeType.XmlDeclaration; }
        }

        // Creates a duplicate of this node.
        public override XmlNode CloneNode(bool deep)
        {
            Debug.Assert(OwnerDocument != null);
            return OwnerDocument.CreateXmlDeclaration(Version, Encoding, Standalone);
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w)
        {
            w.WriteProcessingInstruction(Name, InnerText);
        }


        // Saves all the children of the node to the specified XmlWriter.
        public override void WriteContentTo(XmlWriter w)
        {
            // Intentionally do nothing since the node doesn't have children.
        }

        private bool IsValidXmlVersion(string ver)
        {
            return ver.Length >= 3 && ver[0] == '1' && ver[1] == '.' && XmlCharType.IsOnlyDigits(ver, 2, ver.Length - 2);
        }
    }
}
