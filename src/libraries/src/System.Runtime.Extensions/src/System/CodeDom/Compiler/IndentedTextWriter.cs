// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.CodeDom.Compiler
{
    public class IndentedTextWriter : TextWriter
    {
        private readonly TextWriter _writer;
        private readonly string _tabString;
        private int _indentLevel;
        private bool _tabsPending;

        public const string DefaultTabString = "    ";

        public IndentedTextWriter(TextWriter writer) : this(writer, DefaultTabString) { }

        public IndentedTextWriter(TextWriter writer, string tabString) : base(CultureInfo.InvariantCulture)
        {
            _writer = writer;
            _tabString = tabString;
            _indentLevel = 0;
            _tabsPending = false;
        }

        public override Encoding Encoding => _writer.Encoding;

#if !uapaot // TODO-NULLABLE: Remove condition once ProjectNtfs Corelib is updated with nullable attributes
        [AllowNull]
#endif
        public override string NewLine
        {
            get { return _writer.NewLine!; } // TODO-NULLABLE: Remove ! when nullable attributes are respected
            set { _writer.NewLine = value; }
        }

        public int Indent
        {
            get { return _indentLevel; }
            set { _indentLevel = Math.Max(value, 0); }
        }

        public TextWriter InnerWriter => _writer;

        public override void Close() => _writer.Close();

        public override void Flush() => _writer.Flush();

        protected virtual void OutputTabs()
        {
            if (_tabsPending)
            {
                for (int i = 0; i < _indentLevel; i++)
                {
                    _writer.Write(_tabString);
                }
                _tabsPending = false;
            }
        }

        public override void Write(string? s)
        {
            OutputTabs();
            _writer.Write(s);
        }

        public override void Write(bool value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(char value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(char[]? buffer)
        {
            OutputTabs();
            _writer.Write(buffer);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            OutputTabs();
            _writer.Write(buffer, index, count);
        }

        public override void Write(double value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(float value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(int value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(long value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(object? value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(string format, object? arg0)
        {
            OutputTabs();
            _writer.Write(format, arg0);
        }

        public override void Write(string format, object? arg0, object? arg1)
        {
            OutputTabs();
            _writer.Write(format, arg0, arg1);
        }

        public override void Write(string format, params object?[] arg)
        {
            OutputTabs();
            _writer.Write(format, arg);
        }

        public void WriteLineNoTabs(string? s)
        {
            _writer.WriteLine(s);
        }

        public override void WriteLine(string? s)
        {
            OutputTabs();
            _writer.WriteLine(s);
            _tabsPending = true;
        }

        public override void WriteLine()
        {
            OutputTabs();
            _writer.WriteLine();
            _tabsPending = true;
        }

        public override void WriteLine(bool value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(char value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(char[]? buffer)
        {
            OutputTabs();
            _writer.WriteLine(buffer);
            _tabsPending = true;
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            OutputTabs();
            _writer.WriteLine(buffer, index, count);
            _tabsPending = true;
        }

        public override void WriteLine(double value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(float value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(int value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(long value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(object? value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(string format, object? arg0)
        {
            OutputTabs();
            _writer.WriteLine(format, arg0);
            _tabsPending = true;
        }

        public override void WriteLine(string format, object? arg0, object? arg1)
        {
            OutputTabs();
            _writer.WriteLine(format, arg0, arg1);
            _tabsPending = true;
        }

        public override void WriteLine(string format, params object?[] arg)
        {
            OutputTabs();
            _writer.WriteLine(format, arg);
            _tabsPending = true;
        }

        [CLSCompliant(false)]
        public override void WriteLine(uint value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }
    }
}
