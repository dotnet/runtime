
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DotNet.CoreSetup.Test;

namespace BundleTests
{
    public abstract class NoSdkTestBase
    {
        public static RepoDirectoriesProvider Provider => RepoDirectoriesProvider.Default;
        public static TempRoot TempRoot { get; } = new TempRoot(Provider.TestArtifacts);

        public NoSdkTestBase()
        {
            var sharedFxCopy = Provider.BuiltDotnet;
            if (Directory.Exists(sharedFxCopy))
            {
                Directory.Delete(sharedFxCopy, recursive: true);
            }
            TestArtifact.CopyRecursive(
                Provider.TestHostPath,
                sharedFxCopy,
                overwrite: false);
        }
    }
}