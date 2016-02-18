// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    /// <summary>
    ///  special wrapper for the text writer to replace single "\n" with "\n"
    /// </summary>
    internal sealed class CarriageReturnLineFeedReplacer : TextWriter
    {
        private TextWriter _output;
        private int _lineFeedCount;
        private bool _hasCarriageReturn;

        internal CarriageReturnLineFeedReplacer(TextWriter output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            _output = output;
        }

        public int LineFeedCount
        {
            get { return _lineFeedCount; }
        }

        public override Encoding Encoding
        {
            get { return _output.Encoding; }
        }

        public override IFormatProvider FormatProvider
        {
            get { return _output.FormatProvider; }
        }

        public override string NewLine
        {
            get { return _output.NewLine; }
            set { _output.NewLine = value; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((IDisposable)_output).Dispose();
            }
            _output = null;
        }

        public override void Flush()
        {
            _output.Flush();
        }

        public override void Write(char value)
        {
            if ('\n' == value)
            {
                _lineFeedCount++;
                if (!_hasCarriageReturn)
                {   // X'\n'Y -> X'\r\n'Y
                    _output.Write('\r');
                }
            }
            _hasCarriageReturn = '\r' == value;
            _output.Write(value);
        }
    }
}
