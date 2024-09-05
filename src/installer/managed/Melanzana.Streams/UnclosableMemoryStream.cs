// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Melanzana.Streams
{
    public class UnclosableMemoryStream : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
        }
    }
}
