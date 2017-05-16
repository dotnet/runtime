// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeChecksumPragma : CodeDirective
    {
        private string _fileName;

        public CodeChecksumPragma()
        {
        }

        public CodeChecksumPragma(string fileName, Guid checksumAlgorithmId, byte[] checksumData)
        {
            _fileName = fileName;
            ChecksumAlgorithmId = checksumAlgorithmId;
            ChecksumData = checksumData;
        }

        public string FileName
        {
            get { return _fileName ?? string.Empty; }
            set { _fileName = value; }
        }

        public Guid ChecksumAlgorithmId { get; set; }

        public byte[] ChecksumData { get; set; }
    }
}
