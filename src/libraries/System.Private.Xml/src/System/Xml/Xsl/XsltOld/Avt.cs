// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections;
using System.Xml;
using System.Xml.XPath;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class Avt
    {
        private readonly string? _constAvt;
        private readonly TextEvent[]? _events;

        private Avt(string constAvt)
        {
            Debug.Assert(constAvt != null);
            _constAvt = constAvt;
        }

        private Avt(ArrayList eventList)
        {
            Debug.Assert(eventList != null);
            _events = new TextEvent[eventList.Count];
            for (int i = 0; i < eventList.Count; i++)
            {
                _events[i] = (TextEvent)eventList[i]!;
            }
        }

        [MemberNotNullWhen(false, nameof(_events))]
        public bool IsConstant
        {
            get { return _events == null; }
        }

        internal string Evaluate(Processor? processor, ActionFrame? frame)
        {
            if (IsConstant)
            {
                Debug.Assert(_constAvt != null);
                return _constAvt;
            }
            else
            {
                Debug.Assert(processor != null && frame != null);

                StringBuilder builder = processor.GetSharedStringBuilder();

                for (int i = 0; i < _events.Length; i++)
                {
                    builder.Append(_events[i].Evaluate(processor, frame));
                }
                processor.ReleaseSharedStringBuilder();
                return builder.ToString();
            }
        }

        internal static Avt CompileAvt(Compiler compiler, string avtText)
        {
            Debug.Assert(compiler != null);
            Debug.Assert(avtText != null);

            bool constant;
            ArrayList list = compiler.CompileAvt(avtText, out constant);
            return constant ? new Avt(avtText) : new Avt(list);
        }
    }
}
