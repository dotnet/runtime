// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class CopyCodeAction : Action
    {
        // Execution states:
        private const int Outputting = 2;

        private readonly ArrayList _copyEvents;   // Copy code action events

        internal CopyCodeAction()
        {
            _copyEvents = new ArrayList();
        }

        internal void AddEvent(Event copyEvent)
        {
            _copyEvents.Add(copyEvent);
        }

        internal void AddEvents(ArrayList copyEvents)
        {
            Debug.Assert(copyEvents != null);
            _copyEvents.AddRange(copyEvents);
        }

        internal override void ReplaceNamespaceAlias(Compiler compiler)
        {
            int count = _copyEvents.Count;
            for (int i = 0; i < count; i++)
            {
                ((Event)_copyEvents[i]!).ReplaceNamespaceAlias(compiler);
            }
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);
            Debug.Assert(_copyEvents != null && _copyEvents.Count > 0);

            switch (frame.State)
            {
                case Initialized:
                    frame.Counter = 0;
                    frame.State = Outputting;
                    goto case Outputting;

                case Outputting:
                    Debug.Assert(frame.State == Outputting);

                    while (processor.CanContinue)
                    {
                        Debug.Assert(frame.Counter < _copyEvents.Count);
                        Event copyEvent = (Event)_copyEvents[frame.Counter]!;

                        if (copyEvent.Output(processor, frame) == false)
                        {
                            // This event wasn't processed
                            break;
                        }

                        if (frame.IncrementCounter() >= _copyEvents.Count)
                        {
                            frame.Finished();
                            break;
                        }
                    }
                    break;
                default:
                    Debug.Fail("Invalid CopyCodeAction execution state");
                    break;
            }
        }
    }
}
