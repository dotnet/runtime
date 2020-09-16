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

        private DllImportStub()
        {
        }

        public string StubTypeNamespace { get; private set; }

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

        public IEnumerable<Diagnostic> Diagnostics { get; private set; }

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
            public string ModuleName { get; set; }

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
            public string EntryPoint { get; set; } = null;
            public bool ExactSpelling { get; set; } = false; // VB has different and unusual default behavior here.
            public bool PreserveSig { get; set; } = true;
            public bool SetLastError { get; set; } = false;
            public bool ThrowOnUnmappableChar { get; set; } = false;
        }

        public static DllImportStub Create(
            IMethodSymbol method,
            GeneratedDllImportData dllImportData,
            Compilation compilation,
            CancellationToken token = default)
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

            // Determine containing type(s)
            var containingTypes = new List<TypeDeclarationSyntax>();
            INamedTypeSymbol currType = method.ContainingType;
            while (!(currType is null))
            {
                var visibility = currType.DeclaredAccessibility switch
                {
                    Accessibility.Public => SyntaxKind.PublicKeyword,
                    Accessibility.Private => SyntaxKind.PrivateKeyword,
                    Accessibility.Protected => SyntaxKind.ProtectedKeyword,
                    Accessibility.Internal => SyntaxKind.InternalKeyword,
                    _ => throw new NotSupportedException(), // [TODO] Proper error message
                };

                TypeDeclarationSyntax typeDecl = currType.TypeKind switch
                {
                    TypeKind.Class => ClassDeclaration(currType.Name),
                    TypeKind.Struct => StructDeclaration(currType.Name),
                    _ => throw new NotSupportedException(), // [TODO] Proper error message
                };

                typeDecl = typeDecl.AddModifiers(
                    Token(visibility),
                    Token(SyntaxKind.PartialKeyword));
                containingTypes.Add(typeDecl);

                currType = currType.ContainingType;
            }

            // Determine parameter and return types
            var paramsTypeInfo = new List<TypePositionInfo>();
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                var param = method.Parameters[i];
                var typeInfo = TypePositionInfo.CreateForParameter(param, compilation);
                typeInfo.ManagedIndex = i;
                typeInfo.NativeIndex = paramsTypeInfo.Count;
                paramsTypeInfo.Add(typeInfo);
            }

            TypePositionInfo retTypeInfo = TypePositionInfo.CreateForType(method.ReturnType, method.GetReturnTypeAttributes(), compilation);
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
            var (code, dllImport) = StubCodeGenerator.GenerateSyntax(method, paramsTypeInfo, retTypeInfo);

            return new DllImportStub()
            {
                returnTypeInfo = retTypeInfo,
                paramsTypeInfo = paramsTypeInfo,
                StubTypeNamespace = stubTypeNamespace,
                StubContainingTypes = containingTypes,
                StubCode = code,
                DllImportDeclaration = dllImport,
                Diagnostics = Enumerable.Empty<Diagnostic>(),
            };
        }
    }
}
