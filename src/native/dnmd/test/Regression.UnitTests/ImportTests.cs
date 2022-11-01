using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Regression.UnitTests
{
    public unsafe class ImportTests
    {
        private const int EnumBuffer = 16;

        public ImportTests(ITestOutputHelper outputHelper)
        {
            Log = outputHelper;
        }

        private ITestOutputHelper Log { get; }

        public static IEnumerable<object[]> FrameworkLibraries()
        {
            var spcl = typeof(object).Assembly.Location;
            var frameworkDir = Path.GetDirectoryName(spcl)!;
            foreach (var managedMaybe in Directory.EnumerateFiles(frameworkDir, "*.dll"))
            {
                PEReader pe = new(File.OpenRead(managedMaybe));
                if (!pe.HasMetadata)
                {
                    pe.Dispose();
                    continue;
                }

                yield return new object[] { Path.GetFileName(managedMaybe), pe };
            }
        }

        [Theory]
        [MemberData(nameof(FrameworkLibraries))]
        public void LoadMetadataAndEnumTokens(string filename, PEReader managedLibrary)
        {
            Debug.WriteLine($"Loading {filename}...");

            using var _ = managedLibrary;
            PEMemoryBlock block = managedLibrary.GetMetadata();

            // Load metadata
            IMetaDataImport baselineImport = GetIMetaDataImport(Dispensers.Baseline, ref block);
            IMetaDataImport currentImport = GetIMetaDataImport(Dispensers.Current, ref block);

            // Verify APIs
            Assert.Equal(EnumTypeDefs(baselineImport), EnumTypeDefs(currentImport));
            Assert.Equal(EnumTypeRefs(baselineImport), EnumTypeRefs(currentImport));
            Assert.Equal(EnumTypeSpecs(baselineImport), EnumTypeSpecs(currentImport));
            Assert.Equal(EnumModuleRefs(baselineImport), EnumModuleRefs(currentImport));
            Assert.Equal(EnumSignatures(baselineImport), EnumSignatures(currentImport));

            Assert.Equal(EnumInterfaceImpls(baselineImport), EnumInterfaceImpls(currentImport));
            Assert.Equal(GetSigFromToken(baselineImport), GetSigFromToken(currentImport));
        }

        private static IMetaDataImport GetIMetaDataImport(IMetaDataDispenser disp, ref PEMemoryBlock block)
        {
            var flags = CorOpenFlags.ReadOnly;
            var iid = typeof(IMetaDataImport).GUID;

            void* pUnk;
            int hr = disp.OpenScopeOnMemory(block.Pointer, block.Length, flags, &iid, &pUnk);
            Assert.Equal(0, hr);
            Assert.NotEqual(0, (nint)pUnk);
            var import = (IMetaDataImport)Marshal.GetObjectForIUnknown((nint)pUnk);
            Marshal.Release((nint)pUnk);
            return import;
        }

        private static List<uint> EnumTypeDefs(IMetaDataImport import)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumTypeDefs(ref hcorenum, tokensBuffer, tokensBuffer.Length, out uint returned)
                    && returned != 0)
                {
                    for (int i = 0; i < returned; ++i)
                    {
                        tokens.Add(tokensBuffer[i]);
                    }
                }
            }
            finally
            {
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> EnumTypeRefs(IMetaDataImport import)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumTypeRefs(ref hcorenum, tokensBuffer, tokensBuffer.Length, out uint returned)
                    && returned != 0)
                {
                    for (int i = 0; i < returned; ++i)
                    {
                        tokens.Add(tokensBuffer[i]);
                    }
                }
            }
            finally
            {
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> EnumTypeSpecs(IMetaDataImport import)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumTypeSpecs(ref hcorenum, tokensBuffer, tokensBuffer.Length, out uint returned)
                    && returned != 0)
                {
                    for (int i = 0; i < returned; ++i)
                    {
                        tokens.Add(tokensBuffer[i]);
                    }
                }
            }
            finally
            {
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> EnumModuleRefs(IMetaDataImport import)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumModuleRefs(ref hcorenum, tokensBuffer, tokensBuffer.Length, out uint returned)
                    && returned != 0)
                {
                    for (int i = 0; i < returned; ++i)
                    {
                        tokens.Add(tokensBuffer[i]);
                    }
                }
            }
            finally
            {
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> EnumSignatures(IMetaDataImport import)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumSignatures(ref hcorenum, tokensBuffer, tokensBuffer.Length, out uint returned)
                    && returned != 0)
                {
                    for (int i = 0; i < returned; ++i)
                    {
                        tokens.Add(tokensBuffer[i]);
                    }
                }
            }
            finally
            {
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> EnumInterfaceImpls(IMetaDataImport import)
        {
            List<uint> tokens = new();
            var typdefs = EnumTypeDefs(import);
            var tokensBuffer = new uint[EnumBuffer];
            foreach (uint typedef in typdefs)
            {
                tokens.Add(typedef);
                nint hcorenum = 0;
                try
                {
                    while (0 == import.EnumInterfaceImpls(ref hcorenum, typedef, tokensBuffer, tokensBuffer.Length, out uint returned)
                        && returned != 0)
                    {
                        for (int i = 0; i < returned; ++i)
                        {
                            tokens.Add(tokensBuffer[i]);
                        }
                    }
                }
                finally
                {
                    import.CloseEnum(hcorenum);
                }
            }
            return tokens;
        }

        private static List<(nint Ptr, uint Len)> GetSigFromToken(IMetaDataImport import)
        {
            List<(nint Ptr, uint Len)> sigs = new();
            var tokens = EnumSignatures(import);
            foreach (uint tk in tokens)
            {
                Assert.Equal(0, import.GetSigFromToken(tk, out nint sig, out uint len));
                sigs.Add((sig, len));
            }
            return sigs;
        }
    }
}