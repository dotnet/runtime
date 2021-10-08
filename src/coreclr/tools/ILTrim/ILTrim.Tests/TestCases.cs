using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace ILTrim.Tests
{
    public class TestCases
    {
        [Fact]
        public unsafe void CheckKept()
        {
            var testAssembly = GetTestAssembly();

            var trimmedStream = new MemoryStream();
            fixed (byte* asmPtr = testAssembly)
            {
                using var peReader = new PEReader(asmPtr, testAssembly.Length);
                ILTrim.Trimmer.TrimAssembly(peReader, trimmedStream);
            }

            var untrimmed = LoadTestAssembly(testAssembly);
            var trimmed = LoadTestAssembly(trimmedStream.GetBuffer());

            foreach (var t in untrimmed.GetTypes())
            {
                if (HasKeptAttribute(t.GetCustomAttributesData()))
                {
                    string name = t.FullName!;
                    Assert.True(trimmed.GetType(name) is not null, $"could not find Kept type '{name}'");
                }
            }

            static Assembly LoadTestAssembly(byte[] assembly)
            {
                var resolver = new CustomResolver(assembly);
                var loadContext = new MetadataLoadContext(resolver, "ILTrim.Tests.Cases");

                return Assert.Single(loadContext.GetAssemblies());
            }
        }

        private sealed class CustomResolver : MetadataAssemblyResolver
        {
            private byte[] _assembly;
            public CustomResolver(byte[] assembly)
            {
                _assembly = assembly;
            }

            public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
            {
                return context.LoadFromByteArray(_assembly);
            }
        }

        private static bool HasKeptAttribute(IEnumerable<CustomAttributeData> data)
        {
            foreach (var d in data)
            {
                if (d.AttributeType.Name == "KeptAttribute")
                {
                    return true;
                }
            }
            return false;
        }

        public static byte[] GetTestAssembly()
        {
            var srcFiles = Directory.GetFiles(
                Path.Combine(GetContainingDirectory(), "..", "ILTrim.Tests.Cases"),
                "*.cs",
                SearchOption.AllDirectories);

            var trees = srcFiles.Select(f => SyntaxFactory.ParseSyntaxTree(File.ReadAllText(f)));

            var comp = CSharpCompilation.Create(assemblyName: "ILTrim.Tests.Cases", trees);
            var peStream = new MemoryStream();
            var emitResult = comp.Emit(peStream, options: new EmitOptions().WithRuntimeMetadataVersion("5.0"));
            Assert.True(emitResult.Success);
            return peStream.GetBuffer();
        }

        private static string GetContainingDirectory([CallerFilePath]string path = "")
            => Path.GetDirectoryName(path)!;
    }
}