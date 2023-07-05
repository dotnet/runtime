// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom.Compiler;

namespace Microsoft.CSharp
{
    internal sealed partial class CSharpCodeGenerator
    {
#pragma warning disable IDE0060
        private static CompilerResults FromFileBatch(CompilerParameters options, string[] fileNames)
        {
            throw new PlatformNotSupportedException();
        }
#pragma warning restore IDE0060
    }
}
