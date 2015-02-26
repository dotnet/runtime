// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    internal struct LineInfo
    {
        internal int lineNo;
        internal int linePos;

        public LineInfo(int lineNo, int linePos)
        {
            this.lineNo = lineNo;
            this.linePos = linePos;
        }

        public void Set(int lineNo, int linePos)
        {
            this.lineNo = lineNo;
            this.linePos = linePos;
        }
    }
}
