// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

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
