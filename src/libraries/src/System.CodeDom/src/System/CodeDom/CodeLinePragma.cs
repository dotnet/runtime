// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeLinePragma
    {
        private string _fileName;

        public CodeLinePragma() { }

        public CodeLinePragma(string fileName, int lineNumber)
        {
            FileName = fileName;
            LineNumber = lineNumber;
        }

        public string FileName
        {
            get { return _fileName ?? string.Empty; }
            set { _fileName = value; }
        }

        public int LineNumber { get; set; }
    }
}
