// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Text
{
    public sealed partial class CodePagesEncodingProvider : System.Text.EncodingProvider
    {
        internal CodePagesEncodingProvider() { }
        public static System.Text.EncodingProvider Instance { get { throw null; } }
        public override System.Text.Encoding? GetEncoding(int codepage) { throw null; }
        public override System.Text.Encoding? GetEncoding(string name) { throw null; }
    }
}
