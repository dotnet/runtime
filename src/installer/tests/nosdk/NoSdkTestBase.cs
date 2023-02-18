using System.IO;
using Microsoft.DotNet.CoreSetup.Test;

namespace BundleTests
{
    public abstract class NoSdkTestBase
    {
        public static RepoDirectoriesProvider Provider => RepoDirectoriesProvider.Default;

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
