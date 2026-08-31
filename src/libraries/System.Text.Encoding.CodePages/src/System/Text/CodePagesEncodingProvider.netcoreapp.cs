// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text
{
    public sealed partial class CodePagesEncodingProvider : EncodingProvider
    {
        public override System.Collections.Generic.IEnumerable<System.Text.EncodingInfo> GetEncodings() => BaseCodePageEncoding.GetEncodings(this);
    }
}
