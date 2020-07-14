// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders.Composite
{
    public class MockFileInfo : IFileInfo
    {
        public MockFileInfo(string name)
        {
            Name = name;
        }

        public bool Exists
        {
            get { return true; }
        }

        public bool IsDirectory { get; set; }

        public DateTimeOffset LastModified { get; set; }

        public long Length { get; set; }

        public string Name { get; }

        public string PhysicalPath { get; set; }

        public Stream CreateReadStream()
        {
            throw new NotImplementedException();
        }
    }
}
