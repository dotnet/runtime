// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public partial class FileInfo_MoveTo
    {
        public override void Move(string sourceFile, string destFile, bool overwrite)
        {
            new FileInfo(sourceFile).MoveTo(destFile, overwrite);
        }
    }
}
