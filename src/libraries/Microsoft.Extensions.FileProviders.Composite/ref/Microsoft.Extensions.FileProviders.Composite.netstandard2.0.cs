// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.FileProviders
{
    public partial class CompositeFileProvider : Microsoft.Extensions.FileProviders.IFileProvider
    {
        public CompositeFileProvider(params Microsoft.Extensions.FileProviders.IFileProvider[] fileProviders) { }
        public CompositeFileProvider(System.Collections.Generic.IEnumerable<Microsoft.Extensions.FileProviders.IFileProvider> fileProviders) { }
        public System.Collections.Generic.IEnumerable<Microsoft.Extensions.FileProviders.IFileProvider> FileProviders { get { throw null; } }
        public Microsoft.Extensions.FileProviders.IDirectoryContents GetDirectoryContents(string subpath) { throw null; }
        public Microsoft.Extensions.FileProviders.IFileInfo GetFileInfo(string subpath) { throw null; }
        public Microsoft.Extensions.Primitives.IChangeToken Watch(string pattern) { throw null; }
    }
}
namespace Microsoft.Extensions.FileProviders.Composite
{
    public partial class CompositeDirectoryContents : Microsoft.Extensions.FileProviders.IDirectoryContents, System.Collections.Generic.IEnumerable<Microsoft.Extensions.FileProviders.IFileInfo>, System.Collections.IEnumerable
    {
        public CompositeDirectoryContents(System.Collections.Generic.IList<Microsoft.Extensions.FileProviders.IFileProvider> fileProviders, string subpath) { }
        public bool Exists { get { throw null; } }
        public System.Collections.Generic.IEnumerator<Microsoft.Extensions.FileProviders.IFileInfo> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
}
