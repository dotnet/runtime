// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace System.IO
{
    internal sealed class LowLevelStringWriter : LowLevelTextWriter
    {
        public LowLevelStringWriter()
        {
        }

        public override void Write(char c)
        {
            _sb.Append(c);
        }

        public override void Write(String s)
        {
            if (s != null)
                _sb.Append(s);
        }

        public override String ToString()
        {
            return _sb.ToString();
        }

        private readonly StringBuilder _sb = new StringBuilder();
    }
}

