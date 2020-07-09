// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

#if MS_IO_REDIST
namespace Microsoft.IO.Enumeration
#else
namespace System.IO.Enumeration
#endif
{
    public ref partial struct FileSystemEntry
    {
        /// <summary>
        /// Returns the full path for find results, based on the initially provided path.
        /// </summary>
        public string ToSpecifiedFullPath()
        {
            // We want to provide the enumerated segment of the path appended to the originally specified path. This is
            // the behavior of the various Directory APIs that return a list of strings.
            //
            // RootDirectory has the final separator trimmed, OriginalRootDirectory does not. Our legacy behavior would
            // effectively account for this by appending subdirectory names as it recursed. As such we need to trim one
            // separator when combining with the relative path (Directory.Slice(RootDirectory.Length)).
            //
            //   Original  =>  Root   => Directory    => FileName => relativePath => Specified
            //   C:\foo        C:\foo    C:\foo          bar         ""              C:\foo\bar
            //   C:\foo\       C:\foo    C:\foo          bar         ""              C:\foo\bar
            //   C:\foo/       C:\foo    C:\foo          bar         ""              C:\foo/bar
            //   C:\foo\\      C:\foo    C:\foo          bar         ""              C:\foo\\bar
            //   C:\foo        C:\foo    C:\foo\bar      jar         "bar"           C:\foo\bar\jar
            //   C:\foo\       C:\foo    C:\foo\bar      jar         "bar"           C:\foo\bar\jar
            //   C:\foo/       C:\foo    C:\foo\bar      jar         "bar"           C:\foo/bar\jar


            // If we're at the top level directory the Directory and RootDirectory will be identical. As there are no
            // trailing slashes in play, once we're in a subdirectory, slicing off the root will leave us with an
            // initial separator. We need to trim that off if it exists, but it isn't needed if the original root
            // didn't have a separator. Join() would handle it if we did trim it, not doing so is an optimization.

            ReadOnlySpan<char> relativePath = Directory.Slice(RootDirectory.Length);
            if (Path.EndsInDirectorySeparator(OriginalRootDirectory) && PathInternal.StartsWithDirectorySeparator(relativePath))
                relativePath = relativePath.Slice(1);

            return Path.Join(OriginalRootDirectory, relativePath, FileName);
        }
    }
}
