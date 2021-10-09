// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Xml.Xsl.Xslt
{
    internal sealed class CompilerError
    {
        public CompilerError(string fileName, int line, int column, string errorNumber, string errorText)
        {
            Line = line;
            Column = column;
            ErrorNumber = errorNumber;
            ErrorText = errorText;
            FileName = fileName;
        }

        public int Line { get; set; }

        public int Column { get; set; }

        public string ErrorNumber { get; set; }

        public string ErrorText { get; set; }

        public bool IsWarning { get; set; }

        public string FileName { get; set; }
    }

    internal sealed class CompilerErrorCollection : CollectionBase
    {
        public CompilerErrorCollection() { }

        public int Add(CompilerError value) => List.Add(value);

        public void AddRange(CompilerError[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            for (int i = 0; i < value.Length; i++)
            {
                this.Add(value[i]);
            }
        }

        public void CopyTo(CompilerError[] array, int index) => List.CopyTo(array, index);
    }
}
