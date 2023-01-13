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
            IMetaDataImport2 baselineImport = GetIMetaDataImport(Dispensers.Baseline, ref block);
            IMetaDataImport2 currentImport = GetIMetaDataImport(Dispensers.Current, ref block);

            // Verify APIs
            Assert.Equal(ResetEnum(baselineImport), ResetEnum(currentImport));

            Assert.Equal(GetScopeProps(baselineImport), GetScopeProps(currentImport));
            var modulerefs = AssertAndReturn(EnumModuleRefs(baselineImport), EnumModuleRefs(currentImport));
            foreach (var moduleref in modulerefs)
            {
                Assert.Equal(GetModuleRefProps(baselineImport, moduleref), GetModuleRefProps(currentImport, moduleref));
                Assert.Equal(GetNameFromToken(baselineImport, moduleref), GetNameFromToken(currentImport, moduleref));
            }

            Assert.Equal(FindTypeRef(baselineImport), FindTypeRef(currentImport));
            var typerefs = AssertAndReturn(EnumTypeRefs(baselineImport), EnumTypeRefs(currentImport));
            foreach (var typeref in typerefs)
            {
                Assert.Equal(GetTypeRefProps(baselineImport, typeref), GetTypeRefProps(currentImport, typeref));
                Assert.Equal(GetCustomAttribute_CompilerGenerated(baselineImport, typeref), GetCustomAttribute_CompilerGenerated(currentImport, typeref));
                Assert.Equal(GetNameFromToken(baselineImport, typeref), GetNameFromToken(currentImport, typeref));
            }

            var typespecs = AssertAndReturn(EnumTypeSpecs(baselineImport), EnumTypeSpecs(currentImport));
            foreach (var typespec in typespecs)
            {
                Assert.Equal(GetTypeSpecFromToken(baselineImport, typespec), GetTypeSpecFromToken(currentImport, typespec));
                Assert.Equal(GetCustomAttribute_CompilerGenerated(baselineImport, typespec), GetCustomAttribute_CompilerGenerated(currentImport, typespec));
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
                Assert.Equal(EnumMethodImpls(baselineImport, typedef), EnumMethodImpls(currentImport, typedef));
                Assert.Equal(GetCustomAttribute_CompilerGenerated(baselineImport, typedef), GetCustomAttribute_CompilerGenerated(currentImport, typedef));

                var methods = AssertAndReturn(EnumMethods(baselineImport, typedef), EnumMethods(currentImport, typedef));
                foreach (var methoddef in methods)
                {
                    Assert.Equal(IsGlobal(baselineImport, methoddef), IsGlobal(currentImport, methoddef));
                    var paramz = AssertAndReturn(EnumParams(baselineImport, methoddef), EnumParams(currentImport, methoddef));
                    foreach (var param in paramz)
                    {
                        Assert.Equal(GetFieldMarshal(baselineImport, param), GetFieldMarshal(currentImport, param));
                        Assert.Equal(GetParamProps(baselineImport, param), GetParamProps(currentImport, param));
                        Assert.Equal(GetNameFromToken(baselineImport, param), GetNameFromToken(currentImport, param));
                        Assert.Equal(GetCustomAttribute_Nullable(baselineImport, methoddef), GetCustomAttribute_Nullable(currentImport, methoddef));
                    }
                    Assert.Equal(GetCustomAttribute_CompilerGenerated(baselineImport, methoddef), GetCustomAttribute_CompilerGenerated(currentImport, methoddef));
                    Assert.Equal(GetParamForMethodIndex(baselineImport, methoddef), GetParamForMethodIndex(currentImport, methoddef));
                    Assert.Equal(EnumPermissionSetsAndGetProps(baselineImport, methoddef), EnumPermissionSetsAndGetProps(currentImport, methoddef));
                    Assert.Equal(GetPinvokeMap(baselineImport, methoddef), GetPinvokeMap(currentImport, methoddef));
                    Assert.Equal(GetMethodProps(baselineImport, methoddef, out nint _, out uint _), GetMethodProps(currentImport, methoddef, out nint sig, out uint sigLen));
                    Assert.Equal(GetNativeCallConvFromSig(baselineImport, sig, sigLen), GetNativeCallConvFromSig(currentImport, sig, sigLen));
                    Assert.Equal(GetNameFromToken(baselineImport, methoddef), GetNameFromToken(currentImport, methoddef));
                    Assert.Equal(GetRVA(baselineImport, methoddef), GetRVA(currentImport, methoddef));

                    var methodSpecs = AssertAndReturn(EnumMethodSpecs(baselineImport, methoddef), EnumMethodSpecs(currentImport, methoddef));
                    foreach (var methodSpec in methodSpecs)
                    {
                        Assert.Equal(GetMethodSpecProps(baselineImport, methodSpec), GetMethodSpecProps(currentImport, methodSpec));
                    }
                }

                var events = AssertAndReturn(EnumEvents(baselineImport, typedef), EnumEvents(currentImport, typedef));
                foreach (var eventdef in events)
                {
                    Assert.Equal(IsGlobal(baselineImport, eventdef), IsGlobal(currentImport, eventdef));
                    Assert.Equal(GetEventProps(baselineImport, eventdef, out var _), GetEventProps(currentImport, eventdef, out List<uint> mds));
                    foreach (var md in mds)
                    {
                        Assert.Equal(GetMethodSemantics(baselineImport, eventdef, md), GetMethodSemantics(currentImport, eventdef, md));
                    }
                    Assert.Equal(GetNameFromToken(baselineImport, eventdef), GetNameFromToken(currentImport, eventdef));
                }
                var props = AssertAndReturn(EnumProperties(baselineImport, typedef), EnumProperties(currentImport, typedef));
                foreach (var propdef in props)
                {
                    Assert.Equal(IsGlobal(baselineImport, propdef), IsGlobal(currentImport, propdef));
                    Assert.Equal(GetPropertyProps(baselineImport, propdef, out var _), GetPropertyProps(currentImport, propdef, out List<uint> mds));
                    foreach (var md in mds)
                    {
                        Assert.Equal(GetMethodSemantics(baselineImport, propdef, md), GetMethodSemantics(currentImport, propdef, md));
                    }
                    Assert.Equal(GetNameFromToken(baselineImport, propdef), GetNameFromToken(currentImport, propdef));
                }
                Assert.Equal(EnumFieldsWithName(baselineImport, typedef), EnumFieldsWithName(currentImport, typedef));
                var fields = AssertAndReturn(EnumFields(baselineImport, typedef), EnumFields(currentImport, typedef));
                foreach (var fielddef in fields)
                {
                    Assert.Equal(IsGlobal(baselineImport, fielddef), IsGlobal(currentImport, fielddef));
                    Assert.Equal(GetFieldProps(baselineImport, fielddef), GetFieldProps(currentImport, fielddef));
                    Assert.Equal(GetNameFromToken(baselineImport, fielddef), GetNameFromToken(currentImport, fielddef));
                    Assert.Equal(GetPinvokeMap(baselineImport, fielddef), GetPinvokeMap(currentImport, fielddef));
                    Assert.Equal(GetFieldMarshal(baselineImport, fielddef), GetFieldMarshal(currentImport, fielddef));
                    Assert.Equal(GetRVA(baselineImport, fielddef), GetRVA(currentImport, fielddef));
                    Assert.Equal(GetCustomAttribute_Nullable(baselineImport, fielddef), GetCustomAttribute_Nullable(currentImport, fielddef));
                }
                Assert.Equal(GetTypeDefProps(baselineImport, typedef), GetTypeDefProps(currentImport, typedef));
                Assert.Equal(GetNameFromToken(baselineImport, typedef), GetNameFromToken(currentImport, typedef));
                Assert.Equal(GetNestedClassProps(baselineImport, typedef), GetNestedClassProps(currentImport, typedef));
                Assert.Equal(GetClassLayout(baselineImport, typedef), GetClassLayout(currentImport, typedef));

                var genericParameters = AssertAndReturn(EnumGenericParameters(baselineImport, typedef), EnumGenericParameters(currentImport, typedef));
                foreach (var genericParam in genericParameters)
                {
                    Assert.Equal(GetGenericParameterProps(baselineImport, genericParam), GetGenericParameterProps(currentImport, genericParam));

                    var genericParameterConstraints = AssertAndReturn(EnumGenericParameterConstraints(baselineImport, genericParam), EnumGenericParameterConstraints(currentImport, genericParam));
                    foreach (var constraint in genericParameterConstraints)
                    {
                        Assert.Equal(GetGenericParameterConstraintProps(baselineImport, constraint), GetGenericParameterConstraintProps(currentImport, constraint));
                    }
                }
            }

            var sigs = AssertAndReturn(EnumSignatures(baselineImport), EnumSignatures(currentImport));
            foreach (var sig in sigs)
            {
                Assert.Equal(GetSigFromToken(baselineImport, sig), GetSigFromToken(currentImport, sig));
            }

            var userStrings = AssertAndReturn(EnumUserStrings(baselineImport), EnumUserStrings(currentImport));
            foreach (var us in userStrings)
            {
                Assert.Equal(GetUserString(baselineImport, us), GetUserString(currentImport, us));
            }

            var custAttrs = AssertAndReturn(EnumCustomAttributes(baselineImport), EnumCustomAttributes(currentImport));
            foreach (var custAttr in custAttrs)
            {
                Assert.Equal(GetCustomAttributeProps(baselineImport, custAttr), GetCustomAttributeProps(currentImport, custAttr));
            }

            Assert.Equal(GetVersionString(baselineImport), GetVersionString(currentImport));
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
            using var _lib = managedLibrary;
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
                        Assert.Equal(GetMemberRefProps(baselineImport, memberref, out _, out _), GetMemberRefProps(currentImport, memberref, out _, out _));
                    }
                    Assert.Equal(EnumMethodSemantics(baselineImport, methoddef), EnumMethodSemantics(currentImport, methoddef));
                }

                Assert.Equal(EnumCustomAttributes(baselineImport, typedef), EnumCustomAttributes(currentImport, typedef));
            }
        }

        [Fact]
        public void FindAPIs()
        {
            var dir = Path.GetDirectoryName(typeof(ImportTests).Assembly.Location)!;
            var tgtAssembly = Path.Combine(dir, "Regression.TargetAssembly.dll");
            using PEReader managedLibrary = new(File.OpenRead(tgtAssembly));
            PEMemoryBlock block = managedLibrary.GetMetadata();

            // Load metadata
            IMetaDataImport baselineImport = GetIMetaDataImport(Dispensers.Baseline, ref block);
            IMetaDataImport currentImport = GetIMetaDataImport(Dispensers.Current, ref block);

            var tgt = "C";

            var baseTypeDef = "B1";
            var tkB1 = AssertAndReturn(FindTokenByName(baselineImport, baseTypeDef), FindTokenByName(currentImport, baseTypeDef));
            var tkB1Base = AssertAndReturn(GetTypeDefBaseToken(baselineImport, tkB1), GetTypeDefBaseToken(currentImport, tkB1));
            Assert.Equal(FindTypeDefByName(baselineImport, tgt, tkB1Base), FindTypeDefByName(currentImport, tgt, tkB1Base));

            var baseTypeRef = "B2";
            var tkB2 = AssertAndReturn(FindTokenByName(baselineImport, baseTypeRef), FindTokenByName(currentImport, baseTypeRef));
            var tkB2Base = AssertAndReturn(GetTypeDefBaseToken(baselineImport, tkB2), GetTypeDefBaseToken(currentImport, tkB2));
            Assert.Equal(FindTypeDefByName(baselineImport, tgt, tkB2Base), FindTypeDefByName(currentImport, tgt, tkB2Base));

            var methodDefName = "MethodDef";
            var tkMethodDefBase = AssertAndReturn(FindMethodDef(baselineImport, tkB1Base, methodDefName), FindMethodDef(currentImport, tkB1Base, methodDefName));

            Assert.Equal(GetMethodProps(baselineImport, tkMethodDefBase, out _, out _), GetMethodProps(currentImport, tkMethodDefBase, out nint defSigBlob, out uint defSigBlobLength));
            Assert.Equal(FindMethod(baselineImport, tkB1Base, methodDefName, defSigBlob, defSigBlobLength), FindMethod(currentImport, tkB1Base, methodDefName, defSigBlob, defSigBlobLength));

            var methodRef1Name = "MethodRef1";
            var tkMemberRefNoVarArgsBase = AssertAndReturn(FindMemberRef(baselineImport, tkB1Base, methodRef1Name), FindMemberRef(currentImport, tkB1Base, methodRef1Name));

            // TODO: Baseline doesn't like the signature we're using. Let's mess with the signature in IL to figure out exactly what it will like
            // If baseline doesn't work, it doesn't matter if we work.
            Assert.Equal(GetMemberRefProps(baselineImport, tkMemberRefNoVarArgsBase, out _, out _), GetMemberRefProps(currentImport, tkMemberRefNoVarArgsBase, out nint ref1Blob, out uint ref1BlobLength));
            Assert.Equal(FindMethod(baselineImport, tkB1Base, methodRef1Name, ref1Blob, ref1BlobLength), FindMethod(currentImport, tkB1Base, methodRef1Name, ref1Blob, ref1BlobLength));

            var methodRef2Name = "MethodRef2";
            var tkMemberRefVarArgsBase = AssertAndReturn(FindMemberRef(baselineImport, tkB1Base, methodRef2Name), FindMemberRef(currentImport, tkB1Base, methodRef2Name));

            Assert.Equal(GetMemberRefProps(baselineImport, tkMemberRefVarArgsBase, out _, out _), GetMemberRefProps(currentImport, tkMemberRefVarArgsBase, out nint ref2Blob, out uint ref2BlobLength));
            Assert.Equal(FindMethod(baselineImport, tkB1Base, methodRef2Name, ref2Blob, ref2BlobLength), FindMethod(currentImport, tkB1Base, methodRef2Name, ref2Blob, ref2BlobLength));

            static uint FindTokenByName(IMetaDataImport import, string name)
            {
                int hr = import.FindTypeDefByName(name, 0, out uint ptd);
                Assert.Equal(0, hr);
                return ptd;
            }

            static uint GetTypeDefBaseToken(IMetaDataImport import, uint tk)
            {
                var name = new char[CharBuffer];
                int hr = import.GetTypeDefProps(tk,
                    name,
                    name.Length,
                    out int pchTypeDef,
                    out uint pdwTypeDefFlags,
                    out uint ptkExtends);
                Assert.Equal(0, hr);
                return ptkExtends;
            }

            static uint FindMethodDef(IMetaDataImport import, uint type, string methodName)
            {
                return EnumMembersWithName(import, type, methodName)[0];
            }

            static uint FindMemberRef(IMetaDataImport import, uint type, string methodName)
            {
                var methodDef = FindMethodDef(import, type, methodName);
                return EnumMemberRefs(import, methodDef)[0];
            }

            static unsafe uint FindMethod(IMetaDataImport import, uint td, string name, nint pvSigBlob, uint cbSigBlob)
            {
                byte[] sig = new Span<byte>((void*)pvSigBlob, (int)cbSigBlob).ToArray();
                int hr = import.FindMethod(td, name, sig, (uint)sig.Length, out uint methodToken);
                Assert.Equal(0, hr);
                return methodToken;
            }
        }

        private static T AssertAndReturn<T>(T baseline, T current)
        {
            Assert.Equal(baseline, current);
            return baseline;
        }

        private static IEnumerable<T> AssertAndReturn<T>(IEnumerable<T> baseline, IEnumerable<T> current)
        {
            Assert.Equal(baseline, current);
            return baseline;
        }

        private static IMetaDataImport2 GetIMetaDataImport(IMetaDataDispenser disp, ref PEMemoryBlock block)
        {
            var flags = CorOpenFlags.ReadOnly;
            var iid = typeof(IMetaDataImport2).GUID;

            void* pUnk;
            int hr = disp.OpenScopeOnMemory(block.Pointer, block.Length, flags, &iid, &pUnk);
            Assert.Equal(0, hr);
            Assert.NotEqual(0, (nint)pUnk);
            var import = (IMetaDataImport2)Marshal.GetObjectForIUnknown((nint)pUnk);
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

        private static List<uint> EnumMethodImpls(IMetaDataImport import, uint typedef)
        {
            List<uint> tokens = new();
            var tokensBuffer1 = new uint[EnumBuffer];
            var tokensBuffer2 = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumMethodImpls(ref hcorenum, typedef, tokensBuffer1, tokensBuffer2, tokensBuffer1.Length, out uint returned)
                    && returned != 0)
                {
                    for (int i = 0; i < returned; ++i)
                    {
                        tokens.Add(tokensBuffer1[i]);
                        tokens.Add(tokensBuffer2[i]);
                    }
                }
            }
            finally
            {
                Assert.Equal(0, import.CountEnum(hcorenum, out int count));
                Assert.Equal(count, tokens.Count / 2);
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

        private static List<uint> GetParamForMethodIndex(IMetaDataImport import, uint tk)
        {
            List<uint> values = new();

            for (uint i = 0; i < uint.MaxValue; ++i)
            {
                int hr = import.GetParamForMethodIndex(tk, i, out uint param);
                if (hr < 0)
                {
                    values.Add((uint)hr);
                    break;
                }
                else
                {
                    values.Add(param);
                }
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

        private static List<uint> FindTypeDefByName(IMetaDataImport import, string name, uint scope)
        {
            List<uint> values = new();

            int hr = import.FindTypeDefByName(name, scope, out uint ptd);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(ptd);
            }
            return values;
        }

        private static List<uint> FindTypeRef(IMetaDataImport import)
        {
            List<uint> values = new();
            int hr;
            uint tk;

            // The first assembly ref token typically contains System.Object and Enumerator.
            const uint assemblyRefToken = 0x23000001;
            hr = import.FindTypeRef(assemblyRefToken, "System.Object", out tk);
            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(tk);
            }

            // Look for a type that won't ever exist
            hr = import.FindTypeRef(assemblyRefToken, "DoesntExist", out tk);
            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(tk);
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

        private static List<nuint> GetMethodProps(IMetaDataImport import, uint methoddef, out nint ppvSigBlob, out uint pcbSigBlob)
        {
            List<nuint> values = new();

            var name = new char[CharBuffer];
            int hr = import.GetMethodProps(methoddef,
                out uint typeDef,
                name,
                name.Length,
                out int pchMethod,
                out uint pdwAttr,
                out ppvSigBlob,
                out pcbSigBlob,
                out uint pulCodeRVA,
                out uint pdwImplFlags);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(typeDef);
                uint hash = HashCharArray(name, pchMethod);
                values.Add(hash);
                values.Add((uint)pchMethod);
                values.Add(pdwAttr);
                values.Add((nuint)ppvSigBlob);
                values.Add(pcbSigBlob);
                values.Add(pulCodeRVA);
                values.Add(pdwImplFlags);

            }
            return values;
        }

        private static List<nuint> GetMemberRefProps(IMetaDataImport import, uint tk, out nint ppvSigBlob, out uint pcbSigBlob)
        {
            List<nuint> values = new();

            var name = new char[CharBuffer];
            int hr = import.GetMemberRefProps(tk,
                out uint parent,
                name,
                name.Length,
                out int pchMethod,
                out ppvSigBlob,
                out pcbSigBlob);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(parent);
                uint hash = HashCharArray(name, pchMethod);
                values.Add(hash);
                values.Add((uint)pchMethod);
                values.Add((nuint)ppvSigBlob);
                values.Add(pcbSigBlob);

            }
            return values;
        }

        private static List<uint> GetNestedClassProps(IMetaDataImport import, uint typedef)
        {
            List<uint> values = new();

            int hr = import.GetNestedClassProps(typedef,
                out uint ptkEnclosingClass);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(ptkEnclosingClass);
            }
            return values;
        }

        private static List<nuint> GetCustomAttributeProps(IMetaDataImport import, uint tk)
        {
            List<nuint> values = new();

            int hr = import.GetCustomAttributeProps(tk,
                out uint ptkObj,
                out uint ptkType,
                out nint ppBlob,
                out uint pcbSize);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(ptkObj);
                values.Add(ptkType);
                values.Add((nuint)ppBlob);
                values.Add(pcbSize);
            }
            return values;
        }

        private static List<uint> GetEventProps(IMetaDataImport import, uint tk, out List<uint> methoddefs)
        {
            List<uint> values = new();
            methoddefs = new List<uint>();

            var name = new char[CharBuffer];
            var others = new uint[CharBuffer];
            int hr = import.GetEventProps(tk,
                out uint pClass,
                name,
                name.Length,
                out int pchEvent,
                out uint pdwEventFlags,
                out uint ptkEventType,
                out uint pmdAddOn,
                out uint pmdRemoveOn,
                out uint pmdFire,
                others,
                others.Length,
                out int pcOtherMethod);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(pClass);
                uint hash = HashCharArray(name, pchEvent);
                values.Add(hash);
                values.Add((uint)pchEvent);
                values.Add(pdwEventFlags);
                values.Add(ptkEventType);
                values.Add(pmdAddOn);
                values.Add(pmdRemoveOn);
                values.Add(pmdFire);
                for (int i = 0; i < Math.Min(others.Length, pcOtherMethod); ++i)
                {
                    values.Add(others[i]);
                    methoddefs.Add(others[i]);
                }

                methoddefs.Add(pmdAddOn);
                methoddefs.Add(pmdRemoveOn);
                methoddefs.Add(pmdFire);
            }
            return values;
        }

        private static List<nuint> GetPropertyProps(IMetaDataImport import, uint tk, out List<uint> methoddefs)
        {
            List<nuint> values = new();
            methoddefs = new List<uint>();

            var name = new char[CharBuffer];
            var others = new uint[CharBuffer];
            int hr = import.GetPropertyProps(tk,
                out uint pClass,
                name,
                name.Length,
                out int pchProperty,
                out uint pdwPropFlags,
                out nint ppvSig,
                out uint pbSig,
                out uint pdwCPlusTypeFlag,
                out nint ppDefaultValue,
                out uint pcchDefaultValue,
                out uint pmdSetter,
                out uint pmdGetter,
                others,
                others.Length,
                out int pcOtherMethod);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(pClass);
                uint hash = HashCharArray(name, pchProperty);
                values.Add(hash);
                values.Add((uint)pchProperty);
                values.Add(pdwPropFlags);
                values.Add((nuint)ppvSig);
                values.Add(pbSig);
                values.Add(pdwCPlusTypeFlag);
                values.Add((nuint)ppDefaultValue);
                values.Add(pcchDefaultValue);
                values.Add(pmdSetter);
                values.Add(pmdGetter);
                for (int i = 0; i < Math.Min(others.Length, pcOtherMethod); ++i)
                {
                    values.Add(others[i]);
                    methoddefs.Add(others[i]);
                }

                methoddefs.Add(pmdSetter);
                methoddefs.Add(pmdGetter);
            }
            return values;
        }

        private static List<nuint> GetFieldProps(IMetaDataImport import, uint tk)
        {
            List<nuint> values = new();

            var name = new char[CharBuffer];
            int hr = import.GetFieldProps(tk,
                out uint pClass,
                name,
                name.Length,
                out int pchField,
                out uint pdwAttr,
                out nint ppvSigBlob,
                out uint pcbSigBlob,
                out uint pdwCPlusTypeFlag,
                out nint ppValue,
                out uint pcchValue);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(pClass);
                uint hash = HashCharArray(name, pchField);
                values.Add(hash);
                values.Add((uint)pchField);
                values.Add(pdwAttr);
                values.Add((nuint)ppvSigBlob);
                values.Add(pcbSigBlob);
                values.Add(pdwCPlusTypeFlag);
                // Due to how the "null" pointer is computed, only add when non-zero
                if (pcchValue != 0)
                {
                    values.Add((nuint)ppValue);
                }
                values.Add(pcchValue);
            }
            return values;
        }

        private static List<nuint> GetParamProps(IMetaDataImport import, uint tk)
        {
            List<nuint> values = new();

            var name = new char[CharBuffer];
            int hr = import.GetParamProps(tk,
                out uint pmd,
                out uint pulSequence,
                name,
                name.Length,
                out int pchName,
                out uint pdwAttr,
                out uint pdwCPlusTypeFlag,
                out nint ppValue,
                out uint pcchValue);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(pmd);
                values.Add(pulSequence);
                uint hash = HashCharArray(name, pchName);
                values.Add(hash);
                values.Add((uint)pchName);
                values.Add(pdwAttr);
                values.Add(pdwCPlusTypeFlag);
                // Due to how the "null" pointer is computed, only add when non-zero
                if (pcchValue != 0)
                {
                    values.Add((nuint)ppValue);
                }
                values.Add(pcchValue);
            }
            return values;
        }

        private static List<uint> GetMethodSemantics(IMetaDataImport import, uint tkEventProp, uint methodDef)
        {
            List<uint> values = new();

            int hr = import.GetMethodSemantics(methodDef, tkEventProp, out uint pdwSemanticsFlags);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(pdwSemanticsFlags);
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

        private static List<nuint> GetCustomAttribute_Nullable(IMetaDataImport import, uint tkObj)
        {
            const string NullableAttrName = "System.Runtime.CompilerServices.NullableAttribute";
            return GetCustomAttributeByName(import, NullableAttrName, tkObj);
        }

        private static List<nuint> GetCustomAttribute_CompilerGenerated(IMetaDataImport import, uint tkObj)
        {
            const string CompilerGeneratedAttrName = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
            return GetCustomAttributeByName(import, CompilerGeneratedAttrName, tkObj);
        }

        private static List<nuint> GetCustomAttributeByName(IMetaDataImport import, string customAttr, uint tkObj)
        {
            List<nuint> values = new();

            int hr = import.GetCustomAttributeByName(tkObj,
                customAttr,
                out nint ppData,
                out uint pcbData);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add((nuint)ppData);
                values.Add(pcbData);
            }
            return values;
        }

        private static List<nint> GetNameFromToken(IMetaDataImport import, uint tkObj)
        {
            List<nint> values = new();

            int hr = import.GetNameFromToken(tkObj, out nint pszUtf8NamePtr);

            values.Add(hr);
            if (hr >= 0)
            {
                values.Add(pszUtf8NamePtr);
            }
            return values;
        }

        private static List<uint> GetNativeCallConvFromSig(IMetaDataImport import, nint sig, uint sigLen)
        {
            List<uint> values = new();

            int hr = import.GetNativeCallConvFromSig(sig, sigLen, out uint pCallConv);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(pCallConv);
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

        private static List<uint> EnumMembersWithName(IMetaDataImport import, uint typedef, string memberName = ".ctor")
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumMembersWithName(ref hcorenum, typedef, memberName, tokensBuffer, tokensBuffer.Length, out uint returned)
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

        private static List<uint> EnumCustomAttributes(IMetaDataImport import, uint tk = 0, uint tkType = 0)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumCustomAttributes(ref hcorenum, tk, tkType, tokensBuffer, tokensBuffer.Length, out uint returned)
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

        private static List<uint> EnumGenericParameters(IMetaDataImport2 import, uint tk = 0)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumGenericParameters(ref hcorenum, tk, tokensBuffer, tokensBuffer.Length, out uint returned)
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

        private static List<nuint> GetGenericParameterProps(IMetaDataImport2 import, uint tk = 0)
        {
            List<nuint> values = new();
            var name = new char[CharBuffer];
            int hr = import.GetGenericParamProps(tk, out uint sequenceNumber, out uint flags, out uint owner, out _, name, name.Length, out int nameWritten);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(sequenceNumber);
                values.Add(flags);
                values.Add(owner);
                uint hash = HashCharArray(name, nameWritten);
                values.Add(hash);
            }
            return values;
        }

        private static List<uint> EnumGenericParameterConstraints(IMetaDataImport2 import, uint tk = 0)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumGenericParamConstraints(ref hcorenum, tk, tokensBuffer, tokensBuffer.Length, out uint returned)
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

        private static List<nuint> GetGenericParameterConstraintProps(IMetaDataImport2 import, uint tk = 0)
        {
            List<nuint> values = new();
            var name = new char[CharBuffer];
            int hr = import.GetGenericParamConstraintProps(tk, out uint tkParam, out uint tkContraintType);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(tkParam);
                values.Add(tkContraintType);
            }
            return values;
        }

        private static List<uint> EnumMethodSpecs(IMetaDataImport2 import, uint tk = 0)
        {
            List<uint> tokens = new();
            var tokensBuffer = new uint[EnumBuffer];
            nint hcorenum = 0;
            try
            {
                while (0 == import.EnumMethodSpecs(ref hcorenum, tk, tokensBuffer, tokensBuffer.Length, out uint returned)
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

        private static List<nuint> GetMethodSpecProps(IMetaDataImport2 import, uint tk = 0)
        {
            List<nuint> values = new();
            int hr = import.GetMethodSpecProps(tk, out uint tkParent, out nint ppvSigBlob, out uint pcbSigBlob);

            values.Add((uint)hr);
            if (hr >= 0)
            {
                values.Add(tkParent);
                values.Add((nuint)ppvSigBlob);
                values.Add(pcbSigBlob);
            }
            return values;
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

        private static List<nint> GetSigFromToken(IMetaDataImport import, uint tk)
        {
            List<nint> values = new();

            int hr = import.GetSigFromToken(tk, out nint sig, out uint len);

            values.Add(hr);
            if (hr >= 0)
            {
                values.Add(sig);
                values.Add((int)len);
            }
            return values;
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

        private static string GetVersionString(IMetaDataImport2 import)
        {
            var buffer = new char[CharBuffer];
            Assert.True(0 <= import.GetVersionString(buffer, buffer.Length, out int written));
            return new string(buffer, 0, Math.Min(written, buffer.Length));
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