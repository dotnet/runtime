using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal record StubEnvironment(
        Compilation Compilation,
        bool SupportedTargetFramework,
        Version TargetFrameworkVersion,
        AnalyzerConfigOptions Options);

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

        public string? StubTypeNamespace { get; init; }

        public IEnumerable<TypeDeclarationSyntax> StubContainingTypes { get; init; }

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

        public BlockSyntax StubCode { get; init; }

        public AttributeListSyntax[] AdditionalAttributes { get; init; }

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
            All = ~None
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
            StubEnvironment env,
            GeneratorDiagnostics diagnostics,
            List<AttributeSyntax> forwardedAttributes,
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
                // Remove current members, attributes, and base list so we don't double declare them.
                typeDecl = typeDecl.WithMembers(List<MemberDeclarationSyntax>())
                                   .WithAttributeLists(List<AttributeListSyntax>())
                                   .WithBaseList(null);

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
                    CharSet.Ansi => CharEncoding.Ansi,
                    _ => CharEncoding.Undefined, // [Compat] Do not assume a specific value for None
                };
            }

            var defaultInfo = new DefaultMarshallingInfo(defaultEncoding);

            var marshallingAttributeParser = new MarshallingAttributeInfoParser(env.Compilation, diagnostics, defaultInfo, method);

            // Determine parameter and return types
            var paramsTypeInfo = new List<TypePositionInfo>();
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                var param = method.Parameters[i];
                MarshallingInfo marshallingInfo = marshallingAttributeParser.ParseMarshallingInfo(param.Type, param.GetAttributes());
                var typeInfo = TypePositionInfo.CreateForParameter(param, marshallingInfo, env.Compilation);
                typeInfo = typeInfo with 
                {
                    ManagedIndex = i,
                    NativeIndex = paramsTypeInfo.Count
                };
                paramsTypeInfo.Add(typeInfo);
            }

            TypePositionInfo retTypeInfo = TypePositionInfo.CreateForType(method.ReturnType, marshallingAttributeParser.ParseMarshallingInfo(method.ReturnType, method.GetReturnTypeAttributes()));
            retTypeInfo = retTypeInfo with
            {
                ManagedIndex = TypePositionInfo.ReturnIndex,
                NativeIndex = TypePositionInfo.ReturnIndex
            };

            var managedRetTypeInfo = retTypeInfo;
            // Do not manually handle PreserveSig when generating forwarders.
            // We want the runtime to handle everything.
            if (!dllImportData.PreserveSig && !env.Options.GenerateForwarders())
            {
                // Create type info for native HRESULT return
                retTypeInfo = TypePositionInfo.CreateForType(env.Compilation.GetSpecialType(SpecialType.System_Int32), NoMarshallingInfo.Instance);
                retTypeInfo = retTypeInfo with
                {
                    NativeIndex = TypePositionInfo.ReturnIndex
                };

                // Create type info for native out param
                if (!method.ReturnsVoid)
                {
                    // Transform the managed return type info into an out parameter and add it as the last param
                    TypePositionInfo nativeOutInfo = managedRetTypeInfo with
                    {
                        InstanceIdentifier = StubCodeGenerator.ReturnIdentifier,
                        RefKind = RefKind.Out,
                        RefKindSyntax = SyntaxKind.OutKeyword,
                        ManagedIndex = TypePositionInfo.ReturnIndex,
                        NativeIndex = paramsTypeInfo.Count
                    };
                    paramsTypeInfo.Add(nativeOutInfo);
                }
            }

            // Generate stub code
            var stubGenerator = new StubCodeGenerator(method, dllImportData, paramsTypeInfo, retTypeInfo, diagnostics, env.Options);
            var code = stubGenerator.GenerateSyntax(forwardedAttributes: forwardedAttributes.Count != 0 ? AttributeList(SeparatedList(forwardedAttributes)) : null);

            var additionalAttrs = new List<AttributeListSyntax>();

            // Define additional attributes for the stub definition.
            if (env.TargetFrameworkVersion >= new Version(5, 0))
            {
                additionalAttrs.Add(
                    AttributeList(
                        SeparatedList(new []
                        {
                            // Adding the skip locals init indiscriminately since the source generator is
                            // targeted at non-blittable method signatures which typically will contain locals
                            // in the generated code.
                            Attribute(ParseName(TypeNames.System_Runtime_CompilerServices_SkipLocalsInitAttribute))
                        })));
            }

            return new DllImportStub()
            {
                returnTypeInfo = managedRetTypeInfo,
                paramsTypeInfo = paramsTypeInfo,
                StubTypeNamespace = stubTypeNamespace,
                StubContainingTypes = containingTypes,
                StubCode = code,
                AdditionalAttributes = additionalAttrs.ToArray(),
            };
        }
    }
}
