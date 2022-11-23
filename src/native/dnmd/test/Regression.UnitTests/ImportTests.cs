using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Xunit;
using Xunit.Abstractions;

using Common;

namespace Regression.UnitTests
{
    public unsafe class ImportTests
    {
        private const int EnumBuffer = 32;
        private const int CharBuffer = 64;

        public ImportTests(ITestOutputHelper outputHelper)
        {
            Log = outputHelper;
        }

        private ITestOutputHelper Log { get; }

        public static IEnumerable<object[]> CoreFrameworkLibraries()
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

        [SupportedOSPlatform("windows")]
        public static IEnumerable<object[]> Net20FrameworkLibraries()
        {
            foreach (var managedMaybe in Directory.EnumerateFiles(Dispensers.NetFx20Dir, "*.dll"))
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

        [SupportedOSPlatform("windows")]
        public static IEnumerable<object[]> Net40FrameworkLibraries()
        {
            foreach (var managedMaybe in Directory.EnumerateFiles(Dispensers.NetFx40Dir, "*.dll"))
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

        public static IEnumerable<object[]> AllCoreLibs()
        {
            List<string> corelibs = new() { typeof(object).Assembly.Location };

            if (OperatingSystem.IsWindows())
            {
                corelibs.Add(Path.Combine(Dispensers.NetFx20Dir, "mscorlib.dll"));
                corelibs.Add(Path.Combine(Dispensers.NetFx40Dir, "mscorlib.dll"));
            }

            foreach (var corelibMaybe in corelibs)
            {
                if (!File.Exists(corelibMaybe))
                {
                    continue;
                }

                PEReader pe = new(File.OpenRead(corelibMaybe));
                yield return new object[] { Path.GetFileName(corelibMaybe), pe };
            }
        }

        [Theory]
        [MemberData(nameof(CoreFrameworkLibraries))]
        public void ImportAPIs_Core(string filename, PEReader managedLibrary) => ImportAPIs(filename, managedLibrary);

        [Theory]
        [MemberData(nameof(Net20FrameworkLibraries))]
        public void ImportAPIs_Net20(string filename, PEReader managedLibrary) => ImportAPIs(filename, managedLibrary);

        [Theory]
        [MemberData(nameof(Net40FrameworkLibraries))]
        public void ImportAPIs_Net40(string filename, PEReader managedLibrary) => ImportAPIs(filename, managedLibrary);

        private void ImportAPIs(string filename, PEReader managedLibrary)
        {
            Debug.WriteLine($"{nameof(ImportAPIs)} - {filename}");
            using var _ = managedLibrary;
            PEMemoryBlock block = managedLibrary.GetMetadata();

            // Load metadata
            IMetaDataImport baselineImport = GetIMetaDataImport(Dispensers.Baseline, ref block);
            IMetaDataImport currentImport = GetIMetaDataImport(Dispensers.Current, ref block);

            // Verify APIs
            Assert.Equal(GetScopeProps(baselineImport), GetScopeProps(currentImport));
            var modulerefs = AssertAndReturn(EnumModuleRefs(baselineImport), EnumModuleRefs(currentImport));
            foreach (var moduleref in modulerefs)
            {
                Assert.Equal(GetModuleRefProps(baselineImport, moduleref), GetModuleRefProps(currentImport, moduleref));
            }

            var typerefs = AssertAndReturn(EnumTypeRefs(baselineImport), EnumTypeRefs(currentImport));
            foreach (var typeref in typerefs)
            {
                Assert.Equal(GetTypeRefProps(baselineImport, typeref), GetTypeRefProps(currentImport, typeref));
            }

            var typespecs = AssertAndReturn(EnumTypeSpecs(baselineImport), EnumTypeSpecs(currentImport));
            foreach (var typespec in typespecs)
            {
                Assert.Equal(GetTypeSpecFromToken(baselineImport, typespec), GetTypeSpecFromToken(currentImport, typespec));
            }

            var typedefs = AssertAndReturn(EnumTypeDefs(baselineImport), EnumTypeDefs(currentImport));
            foreach (var typedef in typedefs)
            {
                Assert.Equal(IsGlobal(baselineImport, typedef), IsGlobal(currentImport, typedef));
                Assert.Equal(EnumInterfaceImpls(baselineImport, typedef), EnumInterfaceImpls(currentImport, typedef));
                Assert.Equal(EnumPermissionSetsAndGetProps(baselineImport, typedef), EnumPermissionSetsAndGetProps(currentImport, typedef));
                Assert.Equal(EnumMembers(baselineImport, typedef), EnumMembers(currentImport, typedef));
                Assert.Equal(EnumMembersWithName(baselineImport, typedef), EnumMembersWithName(currentImport, typedef));
                Assert.Equal(EnumMethodsWithName(baselineImport, typedef), EnumMethodsWithName(currentImport, typedef));

                var methods = AssertAndReturn(EnumMethods(baselineImport, typedef), EnumMethods(currentImport, typedef));
                foreach (var methoddef in methods)
                {
                    Assert.Equal(IsGlobal(baselineImport, methoddef), IsGlobal(currentImport, methoddef));
                    var paramz = AssertAndReturn(EnumParams(baselineImport, methoddef), EnumParams(currentImport, methoddef));
                    foreach (var param in paramz)
                    {
                        Assert.Equal(GetFieldMarshal(baselineImport, param), GetFieldMarshal(currentImport, param));
                    }
                    Assert.Equal(EnumPermissionSetsAndGetProps(baselineImport, methoddef), EnumPermissionSetsAndGetProps(currentImport, methoddef));
                    Assert.Equal(GetPinvokeMap(baselineImport, methoddef), GetPinvokeMap(currentImport, methoddef));
                    Assert.Equal(GetMethodProps(baselineImport, methoddef), GetMethodProps(currentImport, methoddef));
                    Assert.Equal(GetRVA(baselineImport, methoddef), GetRVA(currentImport, methoddef));
                }

                var events = AssertAndReturn(EnumEvents(baselineImport, typedef), EnumEvents(currentImport, typedef));
                foreach (var eventdef in events)
                {
                    Assert.Equal(IsGlobal(baselineImport, eventdef), IsGlobal(currentImport, eventdef));
                }
                var props = AssertAndReturn(EnumProperties(baselineImport, typedef), EnumProperties(currentImport, typedef));
                foreach (var propdef in props)
                {
                    Assert.Equal(IsGlobal(baselineImport, propdef), IsGlobal(currentImport, propdef));
                }
                Assert.Equal(EnumFieldsWithName(baselineImport, typedef), EnumFieldsWithName(currentImport, typedef));
                var fields = AssertAndReturn(EnumFields(baselineImport, typedef), EnumFields(currentImport, typedef));
                foreach (var fielddef in fields)
                {
                    Assert.Equal(IsGlobal(baselineImport, fielddef), IsGlobal(currentImport, fielddef));
                    Assert.Equal(GetPinvokeMap(baselineImport, fielddef), GetPinvokeMap(currentImport, fielddef));
                    Assert.Equal(GetFieldMarshal(baselineImport, fielddef), GetFieldMarshal(currentImport, fielddef));
                    Assert.Equal(GetRVA(baselineImport, fielddef), GetRVA(currentImport, fielddef));
                }
                Assert.Equal(GetTypeDefProps(baselineImport, typedef), GetTypeDefProps(currentImport, typedef));
                Assert.Equal(GetClassLayout(baselineImport, typedef), GetClassLayout(currentImport, typedef));
            }

            Assert.Equal(ResetEnum(baselineImport), ResetEnum(currentImport));
            Assert.Equal(EnumSignatures(baselineImport), EnumSignatures(currentImport));
            Assert.Equal(GetSigFromToken(baselineImport), GetSigFromToken(currentImport));
            var userStrings = AssertAndReturn(EnumUserStrings(baselineImport), EnumUserStrings(currentImport));
            foreach (var us in userStrings)
            {
                Assert.Equal(GetUserString(baselineImport, us), GetUserString(currentImport, us));
            }
        }

        /// <summary>
        /// These APIs are very expensive to run on all managed libraries. This library only runs
        /// them on the system corelibs and only on a reduced selection of the tokens.
        /// </summary>
        [Theory]
        [MemberData(nameof(AllCoreLibs))]
        public void LongRunningAPIs(string filename, PEReader managedLibrary)
        {
            Debug.WriteLine($"{nameof(LongRunningAPIs)} - {filename}");
            using var _ = managedLibrary;
            PEMemoryBlock block = managedLibrary.GetMetadata();

            // Load metadata
            IMetaDataImport baselineImport = GetIMetaDataImport(Dispensers.Baseline, ref block);
            IMetaDataImport currentImport = GetIMetaDataImport(Dispensers.Current, ref block);

            int stride;
            int count;

            var typedefs = AssertAndReturn(EnumTypeDefs(baselineImport), EnumTypeDefs(currentImport));
            count = 0;
            stride = Math.Max(typedefs.Count() / 128, 16);
            foreach (var typedef in typedefs)
            {
                if (count++ % stride != 0)
                {
                    continue;
                }

                Assert.Equal(EnumMemberRefs(baselineImport, typedef), EnumMemberRefs(currentImport, typedef));

                var methods = AssertAndReturn(EnumMethods(baselineImport, typedef), EnumMethods(currentImport, typedef));
                foreach (var methoddef in methods)
                {
                    var memberrefs = AssertAndReturn(EnumMemberRefs(baselineImport, methoddef), EnumMemberRefs(currentImport, methoddef));
                    foreach (var memberref in memberrefs)
                    {
                        Assert.Equal(GetMemberRefProps(baselineImport, memberref), GetMemberRefProps(currentImport, memberref));
                    }
                    Assert.Equal(EnumMethodSemantics(baselineImport, methoddef), EnumMethodSemantics(currentImport, methoddef));
                }
            }
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

        private static List<uint> GetScopeProps(IMetaDataImport import)
        {
            List<uint> values = new();

            var name = new char[CharBuffer];
            int hr = import.GetScopeProps(
                name,
                name.Length,
                out int pchName,
                out Guid mvid);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                uint hash = HashCharArray(name, pchName);
                values.Add(hash);
                values.Add((uint)pchName);
                foreach (uint i in new ReadOnlySpan<uint>(&mvid, sizeof(Guid) / sizeof(uint)))
                {
                    values.Add(i);
                }
            }
            return values;
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

        private static List<nuint> GetTypeSpecFromToken(IMetaDataImport import, uint typespec)
        {
            List<nuint> values = new();

            int hr = import.GetTypeSpecFromToken(typespec, out nint sig, out uint len);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add((nuint)sig);
                values.Add(len);
            }
            return values;
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

        private static List<uint> EnumMethodsWithName(IMetaDataImport import, uint typedef)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumMethodsWithName(ref hcorenum, typedef, ".ctor", tokensBuffer, tokensBuffer.Length, out uint returned)
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

        private static List<uint> EnumMethodSemantics(IMetaDataImport import, uint methoddef)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumMethodSemantics(ref hcorenum, methoddef, tokensBuffer, tokensBuffer.Length, out uint returned)
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

        private static List<uint> EnumMemberRefs(IMetaDataImport import, uint tk)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumMemberRefs(ref hcorenum, tk, tokensBuffer, tokensBuffer.Length, out uint returned)
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

        private static List<uint> EnumFieldsWithName(IMetaDataImport import, uint typedef)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumFieldsWithName(ref hcorenum, typedef, "_name", tokensBuffer, tokensBuffer.Length, out uint returned)
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

        private static List<nuint> EnumPermissionSetsAndGetProps(IMetaDataImport import, uint permTk)
        {
            List<nuint> values = new();
            var tokensBuffer = new uint[EnumBuffer];

            // See CorDeclSecurity for actions definitions
            for (uint action = 0; action <= 0xf; ++action)
            {
                List<uint> tokens = new();
                nint hcorenum = 0;
                try
                {
                    while (0 == import.EnumPermissionSets(ref hcorenum, permTk, action, tokensBuffer, tokensBuffer.Length, out uint returned)
                        && returned != 0)
                    {
                        for (int j = 0; j < returned; ++j)
                        {
                            tokens.Add(tokensBuffer[j]);
                        }
                    }
                }
                finally
                {
                    Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                    Assert.Equal(count, tokens.Count);
                    import.CloseEnum(hcorenum);
                }

                foreach (var pk in tokens)
                {
                    int hr = import.GetPermissionSetProps(pk, out uint a, out nint ppvPermission, out uint pcbPermission);
                    if (hr != 0)
                    {
                        values.Add((uint)hr);
                    }
                    else
                    {
                        values.Add(a);
                        values.Add((nuint)ppvPermission);
                        values.Add(pcbPermission);
                    }
                }
            }
            return values;
        }

        private static List<uint> GetRVA(IMetaDataImport import, uint tk)
        {
            List<uint> values = new();
            int hr = import.GetRVA(tk,
                out uint pulCodeRVA,
                out uint pdwImplFlags);
            if (hr != 0)
            {
                values.Add((uint)hr);
            }
            else
            {
                values.Add(pulCodeRVA);
                values.Add(pdwImplFlags);
            }
            return values;
        }

        private static List<uint> GetPinvokeMap(IMetaDataImport import, uint tk)
        {
            List<uint> values = new();

            var name = new char[CharBuffer];
            int hr = import.GetPinvokeMap(tk,
                out uint pdwMappingFlags,
                name,
                name.Length,
                out int pchImportName,
                out uint pmrImportDLL);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(pdwMappingFlags);
                uint hash = HashCharArray(name, pchImportName);
                values.Add(hash);
                values.Add((uint)pchImportName);
                values.Add(pmrImportDLL);
            }
            return values;
        }

        private static List<nint> GetFieldMarshal(IMetaDataImport import, uint tk)
        {
            List<nint> values = new();

            int hr = import.GetFieldMarshal(tk,
                out nint ppvNativeType,
                out uint pcbNativeType);

            values.Add(hr);
            if (hr >= 0)
            {
                values.Add(ppvNativeType);
                values.Add((int)pcbNativeType);
            }
            return values;
        }

        private static uint IsGlobal(IMetaDataImport import, uint tk)
        {
            int hr = import.IsGlobal(tk, out uint pbGlobal);
            if (hr != 0)
            {
                return (uint)hr;
            }

            return pbGlobal;
        }

        private static List<uint> GetTypeDefProps(IMetaDataImport import, uint typedef)
        {
            List<uint> values = new();

            var name = new char[CharBuffer];
            int hr = import.GetTypeDefProps(typedef,
                name,
                name.Length,
                out int pchTypeDef,
                out uint pdwTypeDefFlags,
                out uint ptkExtends);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                uint hash = HashCharArray(name, pchTypeDef);
                values.Add(hash);
                values.Add((uint)pchTypeDef);
                values.Add(pdwTypeDefFlags);
                values.Add(ptkExtends);
            }
            return values;
        }

        private static List<uint> GetTypeRefProps(IMetaDataImport import, uint typeref)
        {
            List<uint> values = new();

            var name = new char[CharBuffer];
            int hr = import.GetTypeRefProps(typeref,
                out uint ptkResolutionScope,
                name,
                name.Length,
                out int pchTypeRef);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(ptkResolutionScope);
                uint hash = HashCharArray(name, pchTypeRef);
                values.Add(hash);
                values.Add((uint)pchTypeRef);
            }
            return values;
        }

        private static List<uint> GetModuleRefProps(IMetaDataImport import, uint moduleref)
        {
            List<uint> values = new();

            var name = new char[CharBuffer];
            int hr = import.GetModuleRefProps(moduleref,
                name,
                name.Length,
                out int pchModuleRef);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                uint hash = HashCharArray(name, pchModuleRef);
                values.Add(hash);
                values.Add((uint)pchModuleRef);
            }
            return values;
        }

        private static List<nuint> GetMethodProps(IMetaDataImport import, uint methoddef)
        {
            List<nuint> values = new();

            var name = new char[CharBuffer];
            int hr = import.GetMethodProps(methoddef,
                out uint typeDef,
                name,
                name.Length,
                out int pchMethod,
                out uint pdwAttr,
                out nint ppvSigBlob,
                out uint pcbSigBlob,
                out uint pulCodeRVA,
                out uint pdwImplFlags);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                uint hash = HashCharArray(name, pchMethod);
                values.Add(hash);
                values.Add(pdwAttr);
                values.Add((nuint)ppvSigBlob);
                values.Add(pcbSigBlob);
                values.Add(pulCodeRVA);
                values.Add(pdwImplFlags);

            }
            return values;
        }

        private static List<nuint> GetMemberRefProps(IMetaDataImport import, uint tk)
        {
            List<nuint> values = new();

            var name = new char[CharBuffer];
            int hr = import.GetMemberRefProps(tk,
                out uint parent,
                name,
                name.Length,
                out int pchMethod,
                out nint ppvSigBlob,
                out uint pcbSigBlob);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(parent);
                uint hash = HashCharArray(name, pchMethod);
                values.Add(hash);
                values.Add((nuint)ppvSigBlob);
                values.Add(pcbSigBlob);

            }
            return values;
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

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(pdwPackSize);
                values.Add(pulClassSize);

                var retLen = Math.Min(offsets.Length, fieldOffsetCount);
                for (int i = 0; i < retLen; ++i)
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

        private static List<uint> EnumMembers(IMetaDataImport import, uint typedef)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
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
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count);
                import.CloseEnum(hcorenum);
            }
            return tokens;
        }

        private static List<uint> EnumMembersWithName(IMetaDataImport import, uint typedef)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumMembersWithName(ref hcorenum, typedef, ".ctor", tokensBuffer, tokensBuffer.Length, out uint returned)
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

        private static string GetUserString(IMetaDataImport import, uint tk)
        {
            var buffer = new char[CharBuffer];
            Assert.True(0 <= import.GetUserString(tk, buffer, buffer.Length, out int written));
            return new string(buffer, 0, Math.Min(written, buffer.Length));
        }

        private static uint HashCharArray(char[] arr, int written)
        {
            return (uint)new string(arr, 0, Math.Min(written, arr.Length)).GetHashCode();
        }
    }
}