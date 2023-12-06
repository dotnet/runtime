using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;
using Xunit.Abstractions;

namespace Regression.UnitTests
{
    public unsafe class ImportTests
    {
        private delegate* unmanaged<void*, int, TestResult> _importAPIs;
        private delegate* unmanaged<void*, int, TestResult> _longRunningAPIs;
        private delegate* unmanaged<void*, int, TestResult> _findAPIs;
        private delegate* unmanaged<void*, int, void**, int*, int, TestResult> _importAPIsIndirectionTables;

        public ImportTests(ITestOutputHelper outputHelper)
        {
            Log = outputHelper;

            nint mod = NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, Native.Path));
            var initialize = (delegate* unmanaged<void*, void*, int>)NativeLibrary.GetExport(mod, "UnitInitialize");
            int hr = initialize((void*)Dispensers.Baseline, (void*)Dispensers.DeltaImageBuilder);
            if (hr < 0)
            {
                throw new Exception($"Initialization failed: 0x{hr:x}");
            }

            _importAPIs = (delegate* unmanaged<void*, int, TestResult>)NativeLibrary.GetExport(mod, "UnitImportAPIs");
            _longRunningAPIs = (delegate* unmanaged<void*, int, TestResult>)NativeLibrary.GetExport(mod, "UnitLongRunningAPIs");
            _findAPIs = (delegate* unmanaged<void*, int, TestResult>)NativeLibrary.GetExport(mod, "UnitFindAPIs");
            _importAPIsIndirectionTables = (delegate* unmanaged<void*, int, void**, int*, int, TestResult>)NativeLibrary.GetExport(mod, "UnitImportAPIsIndirectionTables");
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

        public static IEnumerable<object[]> AssembliesWithDelta()
        {
            yield return DeltaAssembly1();
            static unsafe object[] DeltaAssembly1()
            {
                Compilation baselineCompilation = CSharpCompilation.Create("DeltaAssembly1")
                    .WithReferences(Basic.Reference.Assemblies.NetStandard20.ReferenceInfos.netstandard.Reference)
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                SyntaxTree sourceBase = CSharpSyntaxTree.ParseText("""
                        using System;
                        public class Class1
                        {
                            private int field;
                            public void Method(int x)
                            {
                            }

                            public int Property { get; set; }

                            public event EventHandler? Event;
                        }
                        """);
                baselineCompilation = baselineCompilation.AddSyntaxTrees(
                    sourceBase,
                    CSharpSyntaxTree.ParseText("""
                        using System;
                        public class Class2
                        {
                            private int field;
                            public void Method(int x)
                            {
                            }

                            public int Property { get; set; }

                            public event EventHandler? Event;
                        }
                        """));

                Compilation diffCompilation = baselineCompilation.ReplaceSyntaxTree(
                    sourceBase,
                    CSharpSyntaxTree.ParseText("""
                        using System;
                        public class Class1
                        {
                            private class Attr : Attribute { }

                            private short field2;
                            private int field;

                            [return:Attr]
                            public void Method(int x)
                            {
                            }

                            public int Property { get; set; }

                            public short Property2 { get; set; }

                            public event EventHandler? Event;

                            public event EventHandler? Event2;
                        }
                        """));

                var diagnostics = baselineCompilation.GetDiagnostics();
                MemoryStream baselineImage = new();
                baselineCompilation.Emit(baselineImage, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
                baselineImage.Seek(0, SeekOrigin.Begin);

                ModuleMetadata metadata = ModuleMetadata.CreateFromStream(baselineImage);
                EmitBaseline baseline = EmitBaseline.CreateInitialBaseline(metadata, _ => default, _ => default, true);

                MemoryStream mddiffStream = new();

                diffCompilation.EmitDifference(
                    baseline,
                    new[]
                    {
                        CreateSemanticEdit(SemanticEditKind.Update, baselineCompilation, diffCompilation, c => c.GetTypeByMetadataName("Class1")),
                        CreateSemanticEdit(SemanticEditKind.Insert, baselineCompilation, diffCompilation, c => c.GetTypeByMetadataName("Class1")!.GetMembers("field2").FirstOrDefault()),
                        CreateSemanticEdit(SemanticEditKind.Insert, baselineCompilation, diffCompilation, c => c.GetTypeByMetadataName("Class1")!.GetMembers("Property2").FirstOrDefault()),
                        CreateSemanticEdit(SemanticEditKind.Insert, baselineCompilation, diffCompilation, c => c.GetTypeByMetadataName("Class1")!.GetMembers("Event2").FirstOrDefault()),
                        CreateSemanticEdit(SemanticEditKind.Update, baselineCompilation, diffCompilation, c => c.GetTypeByMetadataName("Class1")!.GetMembers("Method").FirstOrDefault()),
                        CreateSemanticEdit(SemanticEditKind.Insert, baselineCompilation, diffCompilation, c => c.GetTypeByMetadataName("Class1")!.GetTypeMembers("Attr").FirstOrDefault()),
                    },
                    s =>
                    {
                        return false;
                    },
                    mddiffStream,
                    new MemoryStream(), // il stream
                    new MemoryStream() // pdb diff stream
                );

                baselineImage.Seek(0, SeekOrigin.Begin);
                PEReader baselineReader = new PEReader(baselineImage);
                return new object[]
                {
                    nameof(DeltaAssembly1),
                    baselineReader,
                    new Memory<byte>[]
                    {
                        mddiffStream.ToArray()
                    }
                };
            }

            static SemanticEdit CreateSemanticEdit(SemanticEditKind editKind, Compilation baseline, Compilation diff, Func<Compilation, ISymbol?> findSymbol)
            {
                return new SemanticEdit(editKind, findSymbol(baseline), findSymbol(diff));
            }
        }

        [Theory]
        [MemberData(nameof(CoreFrameworkLibraries))]
        public void ImportAPIs_Core(string filename, PEReader managedLibrary) => ImportAPIs(filename, managedLibrary);

        [WindowsOnlyTheory]
        [MemberData(nameof(Net20FrameworkLibraries))]
        public void ImportAPIs_Net20(string filename, PEReader managedLibrary) => ImportAPIs(filename, managedLibrary);

        [WindowsOnlyTheory]
        [MemberData(nameof(Net40FrameworkLibraries))]
        public void ImportAPIs_Net40(string filename, PEReader managedLibrary) => ImportAPIs(filename, managedLibrary);

        [Theory]
        [MemberData(nameof(AssembliesWithDelta))]
        public unsafe void ImportAPIs_AssembliesWithAppliedDeltas(string filename, PEReader deltaBaseline, IList<Memory<byte>> diffs)
        {
            Debug.WriteLine($"{nameof(ImportAPIs_AssembliesWithAppliedDeltas)} - {filename}");
            using var _ = deltaBaseline;
            PEMemoryBlock block = deltaBaseline.GetMetadata();

            void*[] deltaImagePointers = new void*[diffs.Count];
            int[] deltaImageLengths = new int[diffs.Count];
            MemoryHandle[] handles = new MemoryHandle[diffs.Count];

            for (int i = 0; i < diffs.Count; i++)
            {
                handles[i] = diffs[i].Pin();
                deltaImagePointers[i] = handles[i].Pointer;
                deltaImageLengths[i] = diffs[i].Length;
            }

            try
            {
                fixed (void** deltaImagePointersPtr = deltaImagePointers)
                fixed (int* deltaImageLengthsPtr = deltaImageLengths)
                {
                    _importAPIsIndirectionTables(
                        block.Pointer,
                        block.Length,
                        deltaImagePointersPtr,
                        deltaImageLengthsPtr,
                        diffs.Count).Check();
                }
            }
            finally
            {
                for (int i = 0; i < diffs.Count; i++)
                {
                    handles[i].Dispose();
                }
            }
        }

        private void ImportAPIs(string filename, PEReader managedLibrary)
        {
            Debug.WriteLine($"{nameof(ImportAPIs)} - {filename}");
            using var _ = managedLibrary;
            PEMemoryBlock block = managedLibrary.GetMetadata();
            _importAPIs(block.Pointer, block.Length).Check();
        }

        private void ImportAPIs(string filename, MetadataReader managedLibrary)
        {
            Debug.WriteLine($"{nameof(ImportAPIs)} - {filename}");
            _importAPIs(managedLibrary.MetadataPointer, managedLibrary.MetadataLength).Check();
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

            _longRunningAPIs(block.Pointer, block.Length).Check();
        }

        [Fact]
        public void FindAPIs()
        {
            var dir = Path.GetDirectoryName(typeof(ImportTests).Assembly.Location)!;
            var tgtAssembly = Path.Combine(dir, "Regression.TargetAssembly.dll");
            using PEReader managedLibrary = new(File.OpenRead(tgtAssembly));
            PEMemoryBlock block = managedLibrary.GetMetadata();

            _findAPIs(block.Pointer, block.Length).Check();
        }
    }
}