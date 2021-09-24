using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        bool ModuleSkipLocalsInit,
        DllImportGeneratorOptions Options);

    internal sealed class DllImportStubContext : IEquatable<DllImportStubContext>
    {
// We don't need the warnings around not setting the various
// non-nullable fields/properties on this type in the constructor
// since we always use a property initializer.
#pragma warning disable 8618
        private DllImportStubContext()
        {
        }
#pragma warning restore

        public ImmutableArray<TypePositionInfo> ElementTypeInformation { get; init; }

        public string? StubTypeNamespace { get; init; }

        public ImmutableArray<TypeDeclarationSyntax> StubContainingTypes { get; init; }

        public TypeSyntax StubReturnType { get; init; }

        public IEnumerable<ParameterSyntax> StubParameters
        {
            get
            {
                foreach (var typeInfo in ElementTypeInformation)
                {
                    if (typeInfo.ManagedIndex != TypePositionInfo.UnsetIndex
                        && typeInfo.ManagedIndex != TypePositionInfo.ReturnIndex)
                    {
                        yield return Parameter(Identifier(typeInfo.InstanceIdentifier))
                            .WithType(typeInfo.ManagedType.Syntax)
                            .WithModifiers(TokenList(Token(typeInfo.RefKindSyntax)));
                    }
                }
            }
        }

        public ImmutableArray<AttributeListSyntax> AdditionalAttributes { get; init; }

        public DllImportGeneratorOptions Options { get; init; }

        public IMarshallingGeneratorFactory GeneratorFactory { get; init; }

        public static DllImportStubContext Create(
            IMethodSymbol method,
            GeneratedDllImportData dllImportData,
            StubEnvironment env,
            GeneratorDiagnostics diagnostics,
            CancellationToken token)
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
            var containingTypes = ImmutableArray.CreateBuilder<TypeDeclarationSyntax>();
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

            var (typeInfos, generatorFactory) = GenerateTypeInformation(method, dllImportData, diagnostics, env);

            var additionalAttrs = ImmutableArray.CreateBuilder<AttributeListSyntax>();

            // Define additional attributes for the stub definition.
            if (env.TargetFrameworkVersion >= new Version(5, 0) && !MethodIsSkipLocalsInit(env, method))
            {
                additionalAttrs.Add(
                    AttributeList(
                        SeparatedList(new[]
                        {
                            // Adding the skip locals init indiscriminately since the source generator is
                            // targeted at non-blittable method signatures which typically will contain locals
                            // in the generated code.
                            Attribute(ParseName(TypeNames.System_Runtime_CompilerServices_SkipLocalsInitAttribute))
                        })));
            }

            return new DllImportStubContext()
            {
                StubReturnType = method.ReturnType.AsTypeSyntax(),
                ElementTypeInformation = typeInfos,
                StubTypeNamespace = stubTypeNamespace,
                StubContainingTypes = containingTypes.ToImmutable(),
                AdditionalAttributes = additionalAttrs.ToImmutable(),
                Options = env.Options,
                GeneratorFactory = generatorFactory
            };
        }

        private static (ImmutableArray<TypePositionInfo>, IMarshallingGeneratorFactory) GenerateTypeInformation(IMethodSymbol method, GeneratedDllImportData dllImportData, GeneratorDiagnostics diagnostics, StubEnvironment env)
        {
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
            var typeInfos = ImmutableArray.CreateBuilder<TypePositionInfo>();
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                var param = method.Parameters[i];
                MarshallingInfo marshallingInfo = marshallingAttributeParser.ParseMarshallingInfo(param.Type, param.GetAttributes());
                var typeInfo = TypePositionInfo.CreateForParameter(param, marshallingInfo, env.Compilation);
                typeInfo = typeInfo with
                {
                    ManagedIndex = i,
                    NativeIndex = typeInfos.Count
                };
                typeInfos.Add(typeInfo);

            }

            TypePositionInfo retTypeInfo = new(ManagedTypeInfo.CreateTypeInfoForTypeSymbol(method.ReturnType), marshallingAttributeParser.ParseMarshallingInfo(method.ReturnType, method.GetReturnTypeAttributes()));
            retTypeInfo = retTypeInfo with
            {
                ManagedIndex = TypePositionInfo.ReturnIndex,
                NativeIndex = TypePositionInfo.ReturnIndex
            };

            InteropGenerationOptions options = new(env.Options.UseMarshalType, env.Options.UseInternalUnsafeType);
            IMarshallingGeneratorFactory generatorFactory;

            if (env.Options.GenerateForwarders)
            {
                generatorFactory = new ForwarderMarshallingGeneratorFactory();
            }
            else
            {
                generatorFactory = new DefaultMarshallingGeneratorFactory(options);
                AttributedMarshallingModelGeneratorFactory attributedMarshallingFactory = new(generatorFactory, options);
                generatorFactory = attributedMarshallingFactory;
                if (!dllImportData.PreserveSig)
                {
                    // Create type info for native out param
                    if (!method.ReturnsVoid)
                    {
                        // Transform the managed return type info into an out parameter and add it as the last param
                        TypePositionInfo nativeOutInfo = retTypeInfo with
                        {
                            InstanceIdentifier = PInvokeStubCodeGenerator.ReturnIdentifier,
                            RefKind = RefKind.Out,
                            RefKindSyntax = SyntaxKind.OutKeyword,
                            ManagedIndex = TypePositionInfo.ReturnIndex,
                            NativeIndex = typeInfos.Count
                        };
                        typeInfos.Add(nativeOutInfo);
                    }

                    // Use a marshalling generator that supports the HRESULT return->exception marshalling.
                    generatorFactory = new NoPreserveSigMarshallingGeneratorFactory(generatorFactory);

                    // Create type info for native HRESULT return
                    retTypeInfo = new TypePositionInfo(SpecialTypeInfo.Int32, NoMarshallingInfo.Instance);
                    retTypeInfo = retTypeInfo with
                    {
                        NativeIndex = TypePositionInfo.ReturnIndex
                    };
                }

                generatorFactory = new ByValueContentsMarshalKindValidator(generatorFactory);
                attributedMarshallingFactory.ElementMarshallingGeneratorFactory = generatorFactory;
            }
            typeInfos.Add(retTypeInfo);

            return (typeInfos.ToImmutable(), generatorFactory);
        }

        public override bool Equals(object obj)
        {
            return obj is DllImportStubContext other && Equals(other);
        }

        public bool Equals(DllImportStubContext other)
        {
            // We don't check if the generator factories are equal since
            // the generator factory is deterministically created based on the ElementTypeInformation and Options.
            return other is not null
                && StubTypeNamespace == other.StubTypeNamespace
                && ElementTypeInformation.SequenceEqual(other.ElementTypeInformation)
                && StubContainingTypes.SequenceEqual(other.StubContainingTypes, (IEqualityComparer<TypeDeclarationSyntax>)new SyntaxEquivalentComparer())
                && StubReturnType.IsEquivalentTo(other.StubReturnType)
                && AdditionalAttributes.SequenceEqual(other.AdditionalAttributes, (IEqualityComparer<AttributeListSyntax>)new SyntaxEquivalentComparer())
                && Options.Equals(other.Options);
        }

        public override int GetHashCode()
        {
            throw new UnreachableException();
        }

        private static bool MethodIsSkipLocalsInit(StubEnvironment env, IMethodSymbol method)
        {
            if (env.ModuleSkipLocalsInit)
            {
                return true;
            }

            if (method.GetAttributes().Any(a => IsSkipLocalsInitAttribute(a)))
            {
                return true;
            }

            for (INamedTypeSymbol type = method.ContainingType; type is not null; type = type.ContainingType)
            {
                if (type.GetAttributes().Any(a => IsSkipLocalsInitAttribute(a)))
                {
                    return true;
                }
            }
            
            // We check the module case earlier, so we don't need to do it here.

            return false;

            static bool IsSkipLocalsInitAttribute(AttributeData a)
                => a.AttributeClass?.ToDisplayString() == TypeNames.System_Runtime_CompilerServices_SkipLocalsInitAttribute;
        }
    }
}
