// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Collections;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class UseAttributeSetsAction : CompiledAction
    {
        private XmlQualifiedName[]? _useAttributeSets;
        private string? _useString;

        private const int ProcessingSets = 2;

        internal XmlQualifiedName[]? UsedSets
        {
            get { return _useAttributeSets; }
        }

        internal override void Compile(Compiler compiler)
        {
            Debug.Assert(Ref.Equal(compiler.Input.LocalName, compiler.Atoms.UseAttributeSets));
            _useString = compiler.Input.Value;

            Debug.Assert(_useAttributeSets == null);

            if (_useString.Length == 0)
            {
                // Split creates empty node is spliting empty string
                _useAttributeSets = Array.Empty<XmlQualifiedName>();
                return;
            }

            string[] qnames = XmlConvert.SplitString(_useString);

            try
            {
                _useAttributeSets = new XmlQualifiedName[qnames.Length];
                {
                    for (int i = 0; i < qnames.Length; i++)
                    {
                        _useAttributeSets[i] = compiler.CreateXPathQName(qnames[i]);
                    }
                }
            }
            catch (XsltException)
            {
                if (!compiler.ForwardCompatibility)
                {
                    // Rethrow the exception if we're not in forwards-compatible mode
                    throw;
                }
                // Ignore the whole list in forwards-compatible mode
                _useAttributeSets = Array.Empty<XmlQualifiedName>();
            }
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            switch (frame.State)
            {
                case Initialized:
                    frame.Counter = 0;
                    frame.State = ProcessingSets;
                    goto case ProcessingSets;

                case ProcessingSets:
                    if (frame.Counter < _useAttributeSets!.Length)
                    {
                        AttributeSetAction action = processor.RootAction!.GetAttributeSet(_useAttributeSets[frame.Counter]);
                        frame.IncrementCounter();
                        processor.PushActionFrame(action, frame.NodeSet);
                    }
                    else
                    {
                        frame.Finished();
                    }
                    break;

                default:
                    Debug.Fail("Invalid Container action execution state");
                    break;
            }
        }
    }
}
