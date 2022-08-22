// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Text;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class TextOutput : SequentialOutput
    {
        private TextWriter _writer;

        internal TextOutput(Processor processor, Stream stream)
            : base(processor)
        {
            ArgumentNullException.ThrowIfNull(stream);

            this.encoding = processor.Output.Encoding;
            _writer = new StreamWriter(stream, this.encoding);
        }

        internal TextOutput(Processor processor, TextWriter writer)
            : base(processor)
        {
            ArgumentNullException.ThrowIfNull(writer);

            this.encoding = writer.Encoding;
            _writer = writer;
        }

        internal override void Write(char outputChar)
        {
            _writer.Write(outputChar);
        }

        internal override void Write(string? outputText)
        {
            _writer.Write(outputText);
        }

        internal override void Close()
        {
            _writer.Flush();
            _writer = null!;
        }
    }
}
