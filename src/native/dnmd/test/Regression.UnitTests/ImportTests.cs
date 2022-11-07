using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Regression.UnitTests
{
    public unsafe class ImportTests
    {
        private const int EnumBuffer = 4;

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

            var typedefs = AssertAndReturn(EnumTypeDefs(baselineImport), EnumTypeDefs(currentImport));
            foreach (var typedef in typedefs)
            {
                Assert.Equal(EnumInterfaceImpls(baselineImport, typedef), EnumInterfaceImpls(currentImport, typedef));
                var methods = AssertAndReturn(EnumMethods(baselineImport, typedef), EnumMethods(currentImport, typedef));
                foreach (var methoddef in methods)
                {
                    Assert.Equal(EnumParams(baselineImport, methoddef), EnumParams(currentImport, methoddef));
                }
                Assert.Equal(EnumEvents(baselineImport, typedef), EnumEvents(currentImport, typedef));
                Assert.Equal(EnumProperties(baselineImport, typedef), EnumProperties(currentImport, typedef));
                Assert.Equal(EnumFields(baselineImport, typedef), EnumFields(currentImport, typedef));
                Assert.Equal(GetClassLayout(baselineImport, typedef), GetClassLayout(currentImport, typedef));
            }

            Assert.Equal(EnumMembers(baselineImport), EnumMembers(currentImport));
            Assert.Equal(ResetEnum(baselineImport), ResetEnum(currentImport));
            Assert.Equal(EnumSignatures(baselineImport), EnumSignatures(currentImport));
            Assert.Equal(GetSigFromToken(baselineImport), GetSigFromToken(currentImport));
            Assert.Equal(EnumUserStrings(baselineImport), EnumUserStrings(currentImport));
            Assert.Equal(GetUserString(baselineImport), GetUserString(currentImport));
        }

        private static IEnumerable<T> AssertAndReturn<T>(IEnumerable<T> baseline, IEnumerable<T> current)
        {
            Assert.Equal(baseline, current);
            return baseline;
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> EnumInterfaceImpls(IMetaDataImport import, uint typedef)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> EnumMethods(IMetaDataImport import, uint typedef)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumMethods(ref hcorenum, typedef, tokensBuffer, tokensBuffer.Length, out uint returned)
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> EnumParams(IMetaDataImport import, uint methoddef)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumParams(ref hcorenum, methoddef, tokensBuffer, tokensBuffer.Length, out uint returned)
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> EnumProperties(IMetaDataImport import, uint typedef)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumProperties(ref hcorenum, typedef, tokensBuffer, tokensBuffer.Length, out uint returned)
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> EnumFields(IMetaDataImport import, uint typedef)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumFields(ref hcorenum, typedef, tokensBuffer, tokensBuffer.Length, out uint returned)
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> GetClassLayout(IMetaDataImport import, uint typedef)
        {
            List<uint> values = new();
            var offsets = new COR_FIELD_OFFSET[24];
            int hr = import.GetClassLayout(typedef,
                out uint pdwPackSize,
                offsets,
                offsets.Length,
                out int fieldOffsetCount,
                out uint pulClassSize);
            if (hr != 0)
            {
                values.Add((uint)hr);
            }
            else
            {
                values.Add(pdwPackSize);
                values.Add(pulClassSize);

                for (int i = 0; i < fieldOffsetCount; ++i)
                {
                    values.Add(offsets[i].ridOfField);
                    values.Add(offsets[i].ulOffset);
                }
            }
            return values;
        }

        private static List<uint> EnumEvents(IMetaDataImport import, uint typedef)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumEvents(ref hcorenum, typedef, tokensBuffer, tokensBuffer.Length, out uint returned)
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> EnumMembers(IMetaDataImport import)
        {
            List<uint> tokens = new();
            var typedefs = EnumTypeDefs(import);
            var tokensBuffer = new uint[EnumBuffer];
            foreach (uint typedef in typedefs)
            {
                tokens.Add(typedef);
                nint hcorenum = 0;
                try
                {
                    while (0 == import.EnumMembers(ref hcorenum, typedef, tokensBuffer, tokensBuffer.Length, out uint returned)
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

        private static List<uint> ResetEnum(IMetaDataImport import)
        {
            // We are going to test the ResetEnum() API using the
            // EnumMembers() API because it enumerates more than one table.
            List<uint> tokens = new();
            var typedefs = EnumTypeDefs(import);
            if (typedefs.Count == 0)
            {
                return tokens;
            }

            var tk = typedefs[0];
            nint hcorenum = 0;
            try
            {
                ReadInMembers(import, ref hcorenum, tk, ref tokens);

                // Determine how many we have and move to right before end
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                if (count != 0)
                {
                    Assert.Equal(0, import.ResetEnum(hcorenum, (uint)count - 1));
                    ReadInMembers(import, ref hcorenum, tk, ref tokens);

                    // Fully reset the enum
                    Assert.Equal(0, import.ResetEnum(hcorenum, 0));
                    ReadInMembers(import, ref hcorenum, tk, ref tokens);
                }
            }
            finally
            {
                import.CloseEnum(hcorenum);
            }
            return tokens;

            static void ReadInMembers(IMetaDataImport import, ref nint hcorenum, uint tk, ref List<uint> tokens)
            {
                var tokensBuffer = new uint[EnumBuffer];
                if (0 == import.EnumMembers(ref hcorenum, tk, tokensBuffer, tokensBuffer.Length, out uint returned)
                    && returned != 0)
                {
                    for (int i = 0; i < returned; ++i)
                    {
                        tokens.Add(tokensBuffer[i]);
                    }
                }
            }
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
                import.CloseEnum(hcorenum);
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

        private static List<uint> EnumUserStrings(IMetaDataImport import)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumUserStrings(ref hcorenum, tokensBuffer, tokensBuffer.Length, out uint returned)
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<string> GetUserString(IMetaDataImport import)
        {
            List<string> strings = new();
            var tokens = EnumUserStrings(import);
            var buffer = new char[1024];
            foreach (uint tk in tokens)
            {
                Assert.True(0 <= import.GetUserString(tk, buffer, buffer.Length, out int written));
                strings.Add(new string(buffer, 0, Math.Min(written, buffer.Length)));
            }
            return strings;
        }
    }
}