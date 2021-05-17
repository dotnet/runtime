// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(~TestPlatforms.Browser)]
    public partial class FileStream_ctor_options_as : FileStream_ctor_options_as_base
    {
        protected override long PreallocationSize => 10;

        protected override long InitialLength => 10;

        private long GetExpectedFileLength(long preallocationSize) => preallocationSize;

        private long GetActualPreallocationSize(FileStream fileStream) => fileStream.Length;
    }
}
