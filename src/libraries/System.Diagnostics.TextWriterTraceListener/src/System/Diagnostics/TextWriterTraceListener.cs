// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>Directs tracing or debugging output to
    ///       a <see cref='System.IO.TextWriter'/> or to a <see cref='System.IO.Stream'/>,
    ///       such as <see cref='System.Console.Out'/> or <see cref='System.IO.FileStream'/>.</para>
    /// </devdoc>
    public class TextWriterTraceListener : TraceListener
    {
        internal TextWriter? _writer;
        private string? _fileName;

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
        public TextWriterTraceListener(Stream stream, string? name)
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
        public TextWriterTraceListener(TextWriter writer, string? name)
            : base(name)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            _writer = writer;
        }

        /// <devdoc>
        ///    <para>Initializes a new instance of the <see cref='System.Diagnostics.TextWriterTraceListener'/> class with the
        ///    specified file name.</para>
        /// </devdoc>
        public TextWriterTraceListener(string? fileName)
        {
            _fileName = fileName;
        }

        /// <devdoc>
        ///    <para>Initializes a new instance of the <see cref='System.Diagnostics.TextWriterTraceListener'/> class with the
        ///    specified name and the specified file name.</para>
        /// </devdoc>
        public TextWriterTraceListener(string? fileName, string? name)
            : base(name)
        {
            _fileName = fileName;
        }

        /// <devdoc>
        ///    <para> Indicates the text writer that receives the tracing
        ///       or debugging output.</para>
        /// </devdoc>
        public TextWriter? Writer
        {
            get
            {
                EnsureWriter();
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

            // We need to set the _fileName to null so that we stop tracing output, if we don't set it
            // EnsureWriter will create the stream writer again if someone writes or traces output after closing.
            _fileName = null;
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
            EnsureWriter();
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
        public override void Write(string? message)
        {
            EnsureWriter();
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
        public override void WriteLine(string? message)
        {
            EnsureWriter();
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

        private static Encoding GetEncodingWithFallback(Encoding encoding)
        {
            // Clone it and set the "?" replacement fallback
            Encoding fallbackEncoding = (Encoding)encoding.Clone();
            fallbackEncoding.EncoderFallback = EncoderFallback.ReplacementFallback;
            fallbackEncoding.DecoderFallback = DecoderFallback.ReplacementFallback;

            return fallbackEncoding;
        }

        internal void EnsureWriter()
        {
            if (_writer == null)
            {
                bool success = false;

                if (_fileName == null)
                    return;

                // StreamWriter by default uses UTF8Encoding which will throw on invalid encoding errors.
                // This can cause the internal StreamWriter's state to be irrecoverable. It is bad for tracing
                // APIs to throw on encoding errors. Instead, we should provide a "?" replacement fallback
                // encoding to substitute illegal chars. For ex, In case of high surrogate character
                // D800-DBFF without a following low surrogate character DC00-DFFF
                // NOTE: We also need to use an encoding that does't emit BOM which is StreamWriter's default
                Encoding noBOMwithFallback = GetEncodingWithFallback(new System.Text.UTF8Encoding(false));


                // To support multiple appdomains/instances tracing to the same file,
                // we will try to open the given file for append but if we encounter
                // IO errors, we will prefix the file name with a unique GUID value
                // and try one more time
                string fullPath = Path.GetFullPath(_fileName);
                string dirPath = Path.GetDirectoryName(fullPath)!;
                string fileNameOnly = Path.GetFileName(fullPath);

                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        _writer = new StreamWriter(fullPath, true, noBOMwithFallback, 4096);
                        success = true;
                        break;
                    }
                    catch (IOException)
                    {
                        fileNameOnly = $"{Guid.NewGuid()}{fileNameOnly}";
                        fullPath = Path.Combine(dirPath, fileNameOnly);
                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        //ERROR_ACCESS_DENIED, mostly ACL issues
                        break;
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }

                if (!success)
                {
                    // Disable tracing to this listener. Every Write will be nop.
                    // We need to think of a central way to deal with the listener
                    // init errors in the future. The default should be that we eat
                    // up any errors from listener and optionally notify the user
                    _fileName = null;
                }
            }
        }

        internal bool IsEnabled(TraceOptions opts)
        {
            return (opts & TraceOutputOptions) != 0;
        }

    }
}
