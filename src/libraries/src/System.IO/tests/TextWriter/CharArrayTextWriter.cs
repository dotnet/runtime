// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.IO.Tests
{
    public class CharArrayTextWriter : TextWriter
    {
        private StringBuilder _sb;

        public override Encoding Encoding => Encoding.Unicode;

        public CharArrayTextWriter()
        {
            _sb = new StringBuilder();
        }

        public override void Write(char value)
        {
            _sb.Append(value);
        }

        public string Text => _sb.ToString();

        public void Clear() => _sb.Clear();
    }
}
