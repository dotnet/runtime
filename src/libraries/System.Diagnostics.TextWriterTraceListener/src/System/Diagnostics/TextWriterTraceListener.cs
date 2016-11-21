// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>Directs tracing or debugging output to
    ///       a <see cref='T:System.IO.TextWriter'/> or to a <see cref='T:System.IO.Stream'/>,
    ///       such as <see cref='F:System.Console.Out'/> or <see cref='T:System.IO.FileStream'/>.</para>
    /// </devdoc>
    public class TextWriterTraceListener : TraceListener
    {
        internal TextWriter _writer;
        private string _fileName;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.TextWriterTraceListener'/> class with
        /// <see cref='System.IO.TextWriter'/> 
        /// as the output recipient.</para>
        /// </devdoc>
        public TextWriterTraceListener()
        {
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.TextWriterTraceListener'/> class, using the 
        ///    stream as the recipient of the debugging and tracing output.</para>
        /// </devdoc>
        public TextWriterTraceListener(Stream stream)
            : this(stream, string.Empty)
        {
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.TextWriterTraceListener'/> class with the 
        ///    specified name and using the stream as the recipient of the debugging and tracing output.</para>
        /// </devdoc>
        public TextWriterTraceListener(Stream stream, string name)
            : base(name)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            _writer = new StreamWriter(stream);
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.TextWriterTraceListener'/> class using the 
        ///    specified writer as recipient of the tracing or debugging output.</para>
        /// </devdoc>
        public TextWriterTraceListener(TextWriter writer)
            : this(writer, string.Empty)
        {
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.TextWriterTraceListener'/> class with the 
        ///    specified name and using the specified writer as recipient of the tracing or
        ///    debugging
        ///    output.</para>
        /// </devdoc>
        public TextWriterTraceListener(TextWriter writer, string name)
            : base(name)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            _writer = writer;
        }

        /// <devdoc>
        ///    <para>Initializes a new instance of the <see cref='System.Diagnostics.TextWriterTraceListener'/> class with the 
        ///    specified file name.</para>
        /// </devdoc>
        public TextWriterTraceListener(string fileName)
        {
            _fileName = fileName;
        }

        /// <devdoc>
        ///    <para>Initializes a new instance of the <see cref='System.Diagnostics.TextWriterTraceListener'/> class with the 
        ///    specified name and the specified file name.</para>
        /// </devdoc>
        public TextWriterTraceListener(string fileName, string name)
            : base(name)
        {
            _fileName = fileName;
        }

        /// <devdoc>
        ///    <para> Indicates the text writer that receives the tracing
        ///       or debugging output.</para>
        /// </devdoc>
        public TextWriter Writer
        {
            get
            {
                return _writer;
            }

            set
            {
                _writer = value;
            }
        }

        /// <devdoc>
        /// <para>Closes the <see cref='System.Diagnostics.TextWriterTraceListener.Writer'/> so that it no longer
        ///    receives tracing or debugging output.</para>
        /// </devdoc>
        public override void Close()
        {
            if (_writer != null)
            {
                try 
                {
                    _writer.Close();
                }
                catch (ObjectDisposedException) { }
                _writer = null;
            }
        }

        /// <internalonly/>
        /// <devdoc>        
        /// </devdoc>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && _writer != null)
                {
                    _writer.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <devdoc>
        /// <para>Flushes the output buffer for the <see cref='System.Diagnostics.TextWriterTraceListener.Writer'/>.</para>
        /// </devdoc>
        public override void Flush()
        {
            try
            {
                _writer?.Flush();
            }
            catch (ObjectDisposedException) { }
        }

        /// <devdoc>
        ///    <para>Writes a message 
        ///       to this instance's <see cref='System.Diagnostics.TextWriterTraceListener.Writer'/>.</para>
        /// </devdoc>
        public override void Write(string message)
        {
            if (_writer != null)
            {
                if (NeedIndent) WriteIndent();
                try
                {
                    _writer.Write(message);
                }
                catch (ObjectDisposedException) { }
            }
        }

        /// <devdoc>
        ///    <para>Writes a message 
        ///       to this instance's <see cref='System.Diagnostics.TextWriterTraceListener.Writer'/> followed by a line terminator. The
        ///       default line terminator is a carriage return followed by a line feed (\r\n).</para>
        /// </devdoc>
        public override void WriteLine(string message)
        {
            if (_writer != null)
            {
                if (NeedIndent) WriteIndent();
                try
                {
                    _writer.WriteLine(message);
                    NeedIndent = true;
                }
                catch (ObjectDisposedException) { }
            }
        }
    }
}
