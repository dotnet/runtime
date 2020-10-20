using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal class DllImportStub
    {
        private TypePositionInfo returnTypeInfo;
        private IEnumerable<TypePositionInfo> paramsTypeInfo;

// We don't need the warnings around not setting the various
// non-nullable fields/properties on this type in the constructor
// since we always use a property initializer.
#pragma warning disable 8618
        private DllImportStub()
        {
        }
#pragma warning restore

        public string? StubTypeNamespace { get; private set; }

        public IEnumerable<TypeDeclarationSyntax> StubContainingTypes { get; private set; }

        public TypeSyntax StubReturnType { get => this.returnTypeInfo.ManagedType.AsTypeSyntax(); }

        public IEnumerable<ParameterSyntax> StubParameters
        {
            get
            {
                foreach (var typeinfo in paramsTypeInfo)
                {
                    if (typeinfo.ManagedIndex != TypePositionInfo.UnsetIndex
                        && typeinfo.ManagedIndex != TypePositionInfo.ReturnIndex)
                    {
                        yield return Parameter(Identifier(typeinfo.InstanceIdentifier))
                            .WithType(typeinfo.ManagedType.AsTypeSyntax())
                            .WithModifiers(TokenList(Token(typeinfo.RefKindSyntax)));
                    }
                }
            }
        }

        public BlockSyntax StubCode { get; private set; }

        public MethodDeclarationSyntax DllImportDeclaration { get; private set; }

        /// <summary>
        /// Flags used to indicate members on GeneratedDllImport attribute.
        /// </summary>
        [Flags]
        public enum DllImportMember
        {
            None = 0,
            BestFitMapping = 1 << 0,
            CallingConvention = 1 << 1,
            CharSet = 1 << 2,
            EntryPoint = 1 << 3,
            ExactSpelling = 1 << 4,
            PreserveSig = 1 << 5,
            SetLastError = 1 << 6,
            ThrowOnUnmappableChar = 1 << 7,
        }

        /// <summary>
        /// GeneratedDllImportAttribute data
        /// </summary>
        /// <remarks>
        /// The names of these members map directly to those on the
        /// DllImportAttribute and should not be changed.
        /// </remarks>
        public class GeneratedDllImportData
        {
            public string ModuleName { get; set; } = null!;

            /// <summary>
            /// Value set by the user on the original declaration.
            /// </summary>
            public DllImportMember IsUserDefined = DllImportMember.None;

            // Default values for the below fields are based on the
            // documented semanatics of DllImportAttribute:
            //   - https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute
            public bool BestFitMapping { get; set; } = true;
            public CallingConvention CallingConvention { get; set; } = CallingConvention.Winapi;
            public CharSet CharSet { get; set; } = CharSet.Ansi;
            public string EntryPoint { get; set; } = null!;
            public bool ExactSpelling { get; set; } = false; // VB has different and unusual default behavior here.
            public bool PreserveSig { get; set; } = true;
            public bool SetLastError { get; set; } = false;
            public bool ThrowOnUnmappableChar { get; set; } = false;
        }

        public static DllImportStub Create(
            IMethodSymbol method,
            GeneratedDllImportData dllImportData,
            Compilation compilation,
            GeneratorDiagnostics diagnostics,
            CancellationToken token = default)
        {
            // Cancel early if requested
            token.ThrowIfCancellationRequested();

            // Determine the namespace
            string? stubTypeNamespace = null;
            if (!(method.ContainingNamespace is null)
                && !method.ContainingNamespace.IsGlobalNamespace)
            {
                stubTypeNamespace = method.ContainingNamespace.ToString();
            }

            // Determine containing type(s)
            var containingTypes = new List<TypeDeclarationSyntax>();
            INamedTypeSymbol currType = method.ContainingType;
            while (!(currType is null))
            {
                // Use the declaring syntax as a basis for this type declaration.
                // Since we're generating source for the method, we know that the current type
                // has to be declared in source.
                TypeDeclarationSyntax typeDecl = (TypeDeclarationSyntax)currType.DeclaringSyntaxReferences[0].GetSyntax();
                // Remove current members and attributes so we don't double declare them.
                typeDecl = typeDecl.WithMembers(List<MemberDeclarationSyntax>())
                                   .WithAttributeLists(List<AttributeListSyntax>());

                containingTypes.Add(typeDecl);

                currType = currType.ContainingType;
            }

            // Compute the current default string encoding value.
            var defaultEncoding = CharEncoding.Undefined;
            if (dllImportData.IsUserDefined.HasFlag(DllImportMember.CharSet))
            {
                defaultEncoding = dllImportData.CharSet switch
                {
                    CharSet.Unicode => CharEncoding.Utf16,
                    CharSet.Auto => CharEncoding.PlatformDefined,
                    _ => CharEncoding.Utf8, // [Compat] ANSI is no longer ANSI code pages on Windows and UTF-8, on non-Windows.
                };
            }

            var defaultInfo = new DefaultMarshallingInfo(defaultEncoding);

            // Determine parameter and return types
            var paramsTypeInfo = new List<TypePositionInfo>();
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                var param = method.Parameters[i];
                var typeInfo = TypePositionInfo.CreateForParameter(param, defaultInfo, compilation, diagnostics);
                typeInfo.ManagedIndex = i;
                typeInfo.NativeIndex = paramsTypeInfo.Count;
                paramsTypeInfo.Add(typeInfo);
            }

            TypePositionInfo retTypeInfo = TypePositionInfo.CreateForType(method.ReturnType, method.GetReturnTypeAttributes(), defaultInfo, compilation, diagnostics);
            retTypeInfo.ManagedIndex = TypePositionInfo.ReturnIndex;
            retTypeInfo.NativeIndex = TypePositionInfo.ReturnIndex;
            if (!dllImportData.PreserveSig)
            {
                // [TODO] Create type info for native HRESULT return
                // retTypeInfo = ...

                // [TODO] Create type info for native out param
                // if (!method.ReturnsVoid)
                // {
                //     TypePositionInfo nativeOutInfo = ...;
                //     nativeOutInfo.ManagedIndex = TypePositionInfo.ReturnIndex;
                //     nativeOutInfo.NativeIndex = paramsTypeInfo.Count;
                //     paramsTypeInfo.Add(nativeOutInfo);
                // }
            }

            // Generate stub code
            var stubGenerator = new StubCodeGenerator(method, paramsTypeInfo, retTypeInfo, diagnostics);
            var (code, dllImport) = stubGenerator.GenerateSyntax();

            return new DllImportStub()
            {
                returnTypeInfo = retTypeInfo,
                paramsTypeInfo = paramsTypeInfo,
                StubTypeNamespace = stubTypeNamespace,
                StubContainingTypes = containingTypes,
                StubCode = code,
                DllImportDeclaration = dllImport,
            };
        }
    }
}
