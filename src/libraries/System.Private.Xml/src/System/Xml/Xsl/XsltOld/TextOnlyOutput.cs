// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.XPath;
    using System.Text;
    using System.Collections;

    internal class TextOnlyOutput : RecordOutput
    {
        private Processor _processor;
        private TextWriter _writer;

        internal XsltOutput Output
        {
            get { return _processor.Output; }
        }

        public TextWriter Writer
        {
            get { return _writer; }
        }

        //
        // Constructor
        //

        internal TextOnlyOutput(Processor processor, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _processor = processor;
            _writer = new StreamWriter(stream, Output.Encoding);
        }

        internal TextOnlyOutput(Processor processor, TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            _processor = processor;
            _writer = writer;
        }

        //
        // RecordOutput interface method implementation
        //

        public Processor.OutputResult RecordDone(RecordBuilder record)
        {
            BuilderInfo mainNode = record.MainNode;

            switch (mainNode.NodeType)
            {
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    _writer.Write(mainNode.Value);
                    break;
                default:
                    break;
            }

            record.Reset();
            return Processor.OutputResult.Continue;
        }

        public void TheEnd()
        {
            _writer.Flush();
        }
    }
}
