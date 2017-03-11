// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;

    internal class AttributeAction : ContainerAction
    {
        private const int NameDone = 2;

        private Avt _nameAvt;
        private Avt _nsAvt;
        private InputScopeManager _manager;
        // Compile time precalculated AVTs
        private string _name;
        private string _nsUri;
        private PrefixQName _qname; // When we not have AVTs at all we can do this. null otherwise.

        private static PrefixQName CreateAttributeQName(string name, string nsUri, InputScopeManager manager)
        {
            // if name == "xmlns" we don't need to generate this attribute.
            // to avoid its generation we can return false and not add AttributeCreation to it's parent container action
            // for now not creating this.qname will do the trick at execution time
            if (name == "xmlns") return null;
            if (nsUri == XmlReservedNs.NsXmlNs)
            {
                throw XsltException.Create(SR.Xslt_ReservedNS, nsUri);
            }

            PrefixQName qname = new PrefixQName();
            qname.SetQName(name);

            qname.Namespace = nsUri != null ? nsUri : manager.ResolveXPathNamespace(qname.Prefix);

            if (qname.Prefix.StartsWith("xml", StringComparison.Ordinal))
            {
                if (qname.Prefix.Length == 3)
                { // prefix == "xml"
                    if (qname.Namespace == XmlReservedNs.NsXml && (qname.Name == "lang" || qname.Name == "space"))
                    {
                        // preserve prefix for xml:lang and xml:space
                    }
                    else
                    {
                        qname.ClearPrefix();
                    }
                }
                else if (qname.Prefix == "xmlns")
                {
                    if (qname.Namespace == XmlReservedNs.NsXmlNs)
                    {
                        // if NS wasn't specified we have to use prefix to find it and this is imposible for 'xmlns' 
                        throw XsltException.Create(SR.Xslt_InvalidPrefix, qname.Prefix);
                    }
                    else
                    {
                        qname.ClearPrefix();
                    }
                }
            }
            return qname;
        }

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);
            CheckRequiredAttribute(compiler, _nameAvt, "name");

            _name = PrecalculateAvt(ref _nameAvt);
            _nsUri = PrecalculateAvt(ref _nsAvt);

            // if both name and ns are not AVT we can calculate qname at compile time and will not need namespace manager anymore
            if (_nameAvt == null && _nsAvt == null)
            {
                if (_name != "xmlns")
                {
                    _qname = CreateAttributeQName(_name, _nsUri, compiler.CloneScopeManager());
                }
            }
            else
            {
                _manager = compiler.CloneScopeManager();
            }

            if (compiler.Recurse())
            {
                CompileTemplate(compiler);
                compiler.ToParent();
            }
        }

        internal override bool CompileAttribute(Compiler compiler)
        {
            string name = compiler.Input.LocalName;
            string value = compiler.Input.Value;
            if (Ref.Equal(name, compiler.Atoms.Name))
            {
                _nameAvt = Avt.CompileAvt(compiler, value);
            }
            else if (Ref.Equal(name, compiler.Atoms.Namespace))
            {
                _nsAvt = Avt.CompileAvt(compiler, value);
            }
            else
            {
                return false;
            }

            return true;
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);

            switch (frame.State)
            {
                case Initialized:
                    if (_qname != null)
                    {
                        frame.CalulatedName = _qname;
                    }
                    else
                    {
                        frame.CalulatedName = CreateAttributeQName(
                            _nameAvt == null ? _name : _nameAvt.Evaluate(processor, frame),
                            _nsAvt == null ? _nsUri : _nsAvt.Evaluate(processor, frame),
                            _manager
                        );
                        if (frame.CalulatedName == null)
                        {
                            // name == "xmlns" case. Ignore xsl:attribute
                            frame.Finished();
                            break;
                        }
                    }
                    goto case NameDone;
                case NameDone:
                    {
                        PrefixQName qname = frame.CalulatedName;
                        if (processor.BeginEvent(XPathNodeType.Attribute, qname.Prefix, qname.Name, qname.Namespace, false) == false)
                        {
                            // Come back later
                            frame.State = NameDone;
                            break;
                        }

                        processor.PushActionFrame(frame);
                        frame.State = ProcessingChildren;
                        break;                              // Allow children to run
                    }
                case ProcessingChildren:
                    if (processor.EndEvent(XPathNodeType.Attribute) == false)
                    {
                        frame.State = ProcessingChildren;
                        break;
                    }
                    frame.Finished();
                    break;
                default:
                    Debug.Fail("Invalid ElementAction execution state");
                    break;
            }
        }
    }
}
