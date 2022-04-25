// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;

    internal sealed class ElementAction : ContainerAction
    {
        private const int NameDone = 2;

        private Avt? _nameAvt;
        private Avt? _nsAvt;
        private bool _empty;
        private InputScopeManager? _manager;
        // Compile time precalculated AVTs
        private string? _name;
        private string? _nsUri;
        private PrefixQName? _qname; // When we not have AVTs at all we can do this. null otherwise.

        internal ElementAction() { }

        private static PrefixQName CreateElementQName(string name, string? nsUri, InputScopeManager? manager)
        {
            if (nsUri == XmlReservedNs.NsXmlNs)
            {
                throw XsltException.Create(SR.Xslt_ReservedNS, nsUri);
            }

            PrefixQName qname = new PrefixQName();
            qname.SetQName(name);

            if (nsUri == null)
            {
                qname.Namespace = manager!.ResolveXmlNamespace(qname.Prefix);
            }
            else
            {
                qname.Namespace = nsUri;
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
                    _qname = CreateElementQName(_name!, _nsUri, compiler.CloneScopeManager());
                }
            }
            else
            {
                _manager = compiler.CloneScopeManager();
            }

            if (compiler.Recurse())
            {
                Debug.Assert(_empty == false);
                CompileTemplate(compiler);
                compiler.ToParent();
            }
            _empty = (this.containedActions == null);
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
            else if (Ref.Equal(name, compiler.Atoms.UseAttributeSets))
            {
                AddAction(compiler.CreateUseAttributeSetsAction());
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
                        frame.CalulatedName = CreateElementQName(
                            _nameAvt == null ? _name! : _nameAvt.Evaluate(processor, frame),
                            _nsAvt == null ? _nsUri : _nsAvt.Evaluate(processor, frame),
                            _manager
                        );
                    }
                    goto case NameDone;

                case NameDone:
                    {
                        PrefixQName qname = frame.CalulatedName!;
                        if (processor.BeginEvent(XPathNodeType.Element, qname.Prefix, qname.Name, qname.Namespace, _empty) == false)
                        {
                            // Come back later
                            frame.State = NameDone;
                            break;
                        }

                        if (!_empty)
                        {
                            processor.PushActionFrame(frame);
                            frame.State = ProcessingChildren;
                            break;                              // Allow children to run
                        }
                        else
                        {
                            goto case ProcessingChildren;
                        }
                    }
                case ProcessingChildren:
                    if (processor.EndEvent(XPathNodeType.Element) == false)
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
