
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
    public sealed class NoSdkOptionsProvider : RepoDirectoriesProvider
    {
        public static readonly NoSdkOptionsProvider Instance = new NoSdkOptionsProvider();

        private readonly TempRoot _outputRoot;

        private NoSdkOptionsProvider()
        {
            _outputRoot = new TempRoot(base.TestArtifacts);
            var sharedFxCopy = BuiltDotnet;
            if (Directory.Exists(sharedFxCopy))
            {
                Directory.Delete(sharedFxCopy, recursive: true);
            }
            TestArtifact.CopyRecursive(
                GetTestContextVariable("TESTHOST_PATH"),
                BuiltDotnet,
                overwrite: false);
        }

        public string Tfm => GetTestContextVariable("MNA_TFM");

        public string RuntimePackPath => GetTestContextVariable("RUNTIME_PACK_PATH");

        public string CoreClrPath => GetTestContextVariable("CORECLR_ARTIFACTS_PATH");

        private ImmutableArray<MetadataReference> _refAssemblies = default;
        public ImmutableArray<MetadataReference> RefAssemblies
        {
            get
            {
                if (_refAssemblies.IsDefault)
                {
                    var refDirPath = Path.Combine(
                        BaseArtifactsFolder,
                        "bin/microsoft.netcore.app.ref/ref/",
                        Tfm);
                    var references = Directory.GetFiles(refDirPath, "*.dll")
                        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                        .ToImmutableArray();
                    ImmutableInterlocked.InterlockedInitialize(
                        ref _refAssemblies,
                        references);
                }
                return _refAssemblies;
            }
        }

        public TempDirectory CreateTempDirectory() => _outputRoot.CreateDirectory();
    }
}