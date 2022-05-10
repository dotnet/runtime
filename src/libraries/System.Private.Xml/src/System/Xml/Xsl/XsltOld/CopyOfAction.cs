// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
    using MS.Internal.Xml.XPath;

    internal sealed class CopyOfAction : CompiledAction
    {
        private const int ResultStored = 2;
        private const int NodeSetCopied = 3;

        private int _selectKey = Compiler.InvalidQueryKey;

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);
            CheckRequiredAttribute(compiler, _selectKey != Compiler.InvalidQueryKey, "select");
            CheckEmpty(compiler);
        }

        internal override bool CompileAttribute(Compiler compiler)
        {
            string name = compiler.Input.LocalName;
            string value = compiler.Input.Value;
            if (Ref.Equal(name, compiler.Atoms.Select))
            {
                _selectKey = compiler.AddQuery(value);
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
                    Debug.Assert(frame.NodeSet != null);
                    Query query = processor.GetValueQuery(_selectKey);
                    object result = query.Evaluate(frame.NodeSet);

                    if (result is XPathNodeIterator)
                    {
                        // we cash this query because otherwise current() works incorrectly. Bug #382166.
                        // To be perfect we should use frame.NewNodeSet here
                        processor.PushActionFrame(CopyNodeSetAction.GetAction(), new XPathArrayIterator(query));
                        frame.State = NodeSetCopied;
                        break;
                    }

                    XPathNavigator? nav = result as XPathNavigator;
                    if (nav != null)
                    {
                        processor.PushActionFrame(CopyNodeSetAction.GetAction(), new XPathSingletonIterator(nav));
                        frame.State = NodeSetCopied;
                        break;
                    }

                    string? value = XmlConvert.ToXPathString(result);
                    if (processor.TextEvent(value))
                    {
                        frame.Finished();
                    }
                    else
                    {
                        frame.StoredOutput = value;
                        frame.State = ResultStored;
                    }
                    break;

                case ResultStored:
                    Debug.Assert(frame.StoredOutput != null);
                    processor.TextEvent(frame.StoredOutput);
                    frame.Finished();
                    break;

                case NodeSetCopied:
                    Debug.Assert(frame.State == NodeSetCopied);
                    frame.Finished();
                    break;
            }
        }
    }
}
