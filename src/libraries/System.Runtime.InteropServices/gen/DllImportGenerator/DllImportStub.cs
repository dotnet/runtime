using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal class DllImportStub
    {
        private DllImportStub()
        {
        }

        public string StubTypeNamespace { get; private set; }

        public IEnumerable<string> StubContainingTypesDecl { get; private set; }

        public IEnumerable<string> StubCode { get; private set; }

        public string DllImportReturnType { get; private set; }

        public string DllImportMethodName { get; private set; }

        public IEnumerable<(string Type, string Name)> DllImportParameters { get; private set; }

        public IEnumerable<Diagnostic> Diagnostics { get; private set; }

        public static DllImportStub Create(IMethodSymbol method, CancellationToken token = default)
        {
            // Cancel early if requested
            token.ThrowIfCancellationRequested();

            // Determine the namespace
            string stubTypeNamespace = null;
            if (!(method.ContainingNamespace is null)
                && !method.ContainingNamespace.IsGlobalNamespace)
            {
                stubTypeNamespace = method.ContainingNamespace.ToString();
            }

            // Determine type
            var stubContainingTypes = new List<string>();
            INamedTypeSymbol currType = method.ContainingType;
            while (!(currType is null))
            {
                var visibility = currType.DeclaredAccessibility switch
                {
                    Accessibility.Public => "public",
                    Accessibility.Private => "private",
                    Accessibility.Protected => "protected",
                    Accessibility.Internal => "internal",
                    _ => throw new NotSupportedException(), // [TODO] Proper error message
                };

                var typeKeyword = currType.TypeKind switch
                {
                    TypeKind.Class => "class",
                    TypeKind.Struct => "struct",
                    _ => throw new NotSupportedException(), // [TODO] Proper error message
                };

                stubContainingTypes.Add($"{visibility} partial {typeKeyword} {currType.Name}");
                currType = currType.ContainingType;
            }

            // Flip the order to that of how to declare the types
            stubContainingTypes.Reverse();

            // Determine parameter types
            var parameters = new List<(string Type, string Name)>();
            foreach (var namePair in method.Parameters)
            {
                parameters.Add((ComputeTypeForDllImport(namePair.Type), namePair.Name));
            }

            return new DllImportStub()
            {
                StubTypeNamespace = stubTypeNamespace,
                StubContainingTypesDecl = stubContainingTypes,
                StubCode = new[] { $"throw new System.{nameof(NotSupportedException)}();" },
                DllImportReturnType = ComputeTypeForDllImport(method.ReturnType),
                DllImportMethodName = method.Name + "__PInvoke__",
                DllImportParameters = parameters,
                Diagnostics = Enumerable.Empty<Diagnostic>(),
            };
        }

        private static string ComputeTypeForDllImport(ITypeSymbol type)
        {
            if (!type.IsUnmanagedType)
            {
                return "void*";
            }

            return type.SpecialType switch
            {
                SpecialType.System_Void => "void",
                SpecialType.System_SByte => "sbyte",
                SpecialType.System_Byte => "byte",
                SpecialType.System_Int16 => "short",
                SpecialType.System_UInt16 => "ushort",
                SpecialType.System_Int32 => "int",
                SpecialType.System_UInt32 => "uint",
                SpecialType.System_Int64 => "long",
                SpecialType.System_UInt64 => "ulong",
                SpecialType.System_Single => "float",
                SpecialType.System_Double => "double",
                SpecialType.System_String => "char*", // [TODO] Consider encoding here
                SpecialType.System_IntPtr => "void*",
                SpecialType.System_UIntPtr => "void*",
                _ => "void*",
            };
        }
    }
}
