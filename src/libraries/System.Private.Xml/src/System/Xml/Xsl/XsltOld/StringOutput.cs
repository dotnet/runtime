// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Xml;
    using System.Text;

    internal sealed class StringOutput : SequentialOutput
    {
        private readonly StringBuilder _builder;
        private string? _result;

        internal string? Result
        {
            get
            {
                return _result;
            }
        }

        internal StringOutput(Processor processor)
        : base(processor)
        {
            _builder = new StringBuilder();
        }

        internal override void Write(char outputChar)
        {
            _builder.Append(outputChar);

#if DEBUG
            _result = _builder.ToString();
#endif
        }

        internal override void Write(string? outputText)
        {
            _builder.Append(outputText);

#if DEBUG
            _result = _builder.ToString();
#endif
        }

        internal override void Close()
        {
            _result = _builder.ToString();
        }
    }
}
