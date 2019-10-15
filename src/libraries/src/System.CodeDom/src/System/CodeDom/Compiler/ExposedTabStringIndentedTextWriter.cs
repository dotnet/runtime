// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.CodeDom.Compiler
{
    internal sealed class ExposedTabStringIndentedTextWriter : IndentedTextWriter
    {
        public ExposedTabStringIndentedTextWriter(TextWriter writer, string tabString) : base(writer, tabString)
        {
            TabString = tabString ?? IndentedTextWriter.DefaultTabString;
        }

        internal void InternalOutputTabs()
        {
            TextWriter inner = InnerWriter;
            for (int i = 0; i < Indent; i++)
            {
                inner.Write(TabString);
            }
        }

        internal string TabString { get; } // IndentedTextWriter doesn't expose this publicly
    }

    internal sealed class Indentation
    {
        private readonly ExposedTabStringIndentedTextWriter _writer;
        private readonly int _indent;
        private string _s;

        internal Indentation(ExposedTabStringIndentedTextWriter writer, int indent)
        {
            _writer = writer;
            _indent = indent;
        }

        internal string IndentationString
        {
            get
            {
                if (_s == null)
                {
                    string tabString = _writer.TabString;

                    switch (_indent)
                    {
                        case 0: _s = string.Empty; break;
                        case 1: _s = tabString; break;
                        case 2: _s = tabString + tabString; break;
                        case 3: _s = tabString + tabString + tabString; break;
                        case 4: _s = tabString + tabString + tabString + tabString; break;
                        default:
                            var args = new string[_indent];
                            for (int i = 0; i < args.Length; i++) args[i] = tabString;
                            return string.Concat(args);
                    }
                }

                return _s;
            }
        }
    }
}
