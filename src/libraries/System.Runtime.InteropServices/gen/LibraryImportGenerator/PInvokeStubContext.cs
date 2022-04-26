// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal sealed record StubEnvironment(
        Compilation Compilation,
        TargetFramework TargetFramework,
        Version TargetFrameworkVersion,
        bool ModuleSkipLocalsInit,
        LibraryImportGeneratorOptions Options)
    {
        /// <summary>
        /// Override for determining if two StubEnvironment instances are
        /// equal. This intentionally excludes the Compilation instance
        /// since that represents the actual compilation and not just the settings.
        /// </summary>
        /// <param name="env1">The first StubEnvironment</param>
        /// <param name="env2">The second StubEnvironment</param>
        /// <returns>True if the settings are equal, otherwise false.</returns>
        public static bool AreCompilationSettingsEqual(StubEnvironment env1, StubEnvironment env2)
        {
            return env1.TargetFramework == env2.TargetFramework
                && env1.TargetFrameworkVersion == env2.TargetFrameworkVersion
                && env1.ModuleSkipLocalsInit == env2.ModuleSkipLocalsInit
                && env1.Options.Equals(env2.Options);
        }
    }

    internal sealed class PInvokeStubContext : IEquatable<PInvokeStubContext>
    {
        private static readonly string GeneratorName = typeof(LibraryImportGenerator).Assembly.GetName().Name;

        private static readonly string GeneratorVersion = typeof(LibraryImportGenerator).Assembly.GetName().Version.ToString();

        // We don't need the warnings around not setting the various
        // non-nullable fields/properties on this type in the constructor
        // since we always use a property initializer.
#pragma warning disable 8618
        private PInvokeStubContext()
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
                foreach (TypePositionInfo typeInfo in ElementTypeInformation)
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

        public LibraryImportGeneratorOptions Options { get; init; }

        public IMarshallingGeneratorFactory GeneratorFactory { get; init; }

        public static PInvokeStubContext Create(
            IMethodSymbol method,
            LibraryImportData libraryImportData,
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
            ImmutableArray<TypeDeclarationSyntax>.Builder containingTypes = ImmutableArray.CreateBuilder<TypeDeclarationSyntax>();
            INamedTypeSymbol currType = method.ContainingType;
            while (!(currType is null))
            {
                // Use the declaring syntax as a basis for this type declaration.
                // Since we're generating source for the method, we know that the current type
                // has to be declared in source.
                TypeDeclarationSyntax typeDecl = (TypeDeclarationSyntax)currType.DeclaringSyntaxReferences[0].GetSyntax(token);
                // Remove current members, attributes, and base list so we don't double declare them.
                typeDecl = typeDecl.WithMembers(List<MemberDeclarationSyntax>())
                                   .WithAttributeLists(List<AttributeListSyntax>())
                                   .WithBaseList(null);

                containingTypes.Add(typeDecl);

                currType = currType.ContainingType;
            }

            (ImmutableArray<TypePositionInfo> typeInfos, IMarshallingGeneratorFactory generatorFactory) = GenerateTypeInformation(method, libraryImportData, diagnostics, env);

            ImmutableArray<AttributeListSyntax>.Builder additionalAttrs = ImmutableArray.CreateBuilder<AttributeListSyntax>();

            if (env.TargetFramework != TargetFramework.Unknown)
            {
                // Define additional attributes for the stub definition.
                additionalAttrs.Add(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(ParseName(TypeNames.System_CodeDom_Compiler_GeneratedCodeAttribute),
                                AttributeArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
                                            AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(GeneratorName))),
                                            AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(GeneratorVersion)))
                                        }))))));
            }

            if (env.TargetFrameworkVersion >= new Version(5, 0) && !MethodIsSkipLocalsInit(env, method))
            {
                additionalAttrs.Add(
                    AttributeList(
                        SingletonSeparatedList(
                            // Adding the skip locals init indiscriminately since the source generator is
                            // targeted at non-blittable method signatures which typically will contain locals
                            // in the generated code.
                            Attribute(ParseName(TypeNames.System_Runtime_CompilerServices_SkipLocalsInitAttribute)))));
            }

            return new PInvokeStubContext()
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

        private static (ImmutableArray<TypePositionInfo>, IMarshallingGeneratorFactory) GenerateTypeInformation(IMethodSymbol method, LibraryImportData libraryImportData, GeneratorDiagnostics diagnostics, StubEnvironment env)
        {
            // Compute the current default string encoding value.
            CharEncoding defaultEncoding = CharEncoding.Undefined;
            if (libraryImportData.IsUserDefined.HasFlag(LibraryImportMember.StringMarshalling))
            {
                defaultEncoding = libraryImportData.StringMarshalling switch
                {
                    StringMarshalling.Utf16 => CharEncoding.Utf16,
                    StringMarshalling.Utf8 => CharEncoding.Utf8,
                    StringMarshalling.Custom => CharEncoding.Custom,
                    _ => CharEncoding.Undefined, // [Compat] Do not assume a specific value
                };
            }
            else if (libraryImportData.IsUserDefined.HasFlag(LibraryImportMember.StringMarshallingCustomType))
            {
                defaultEncoding = CharEncoding.Custom;
            }

            var defaultInfo = new DefaultMarshallingInfo(defaultEncoding, libraryImportData.StringMarshallingCustomType);

            var marshallingAttributeParser = new MarshallingAttributeInfoParser(env.Compilation, diagnostics, defaultInfo, method);

            // Determine parameter and return types
            ImmutableArray<TypePositionInfo>.Builder typeInfos = ImmutableArray.CreateBuilder<TypePositionInfo>();
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                IParameterSymbol param = method.Parameters[i];
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

            InteropGenerationOptions options = new(env.Options.UseMarshalType);
            IMarshallingGeneratorFactory generatorFactory;

            if (env.Options.GenerateForwarders)
            {
                generatorFactory = new ForwarderMarshallingGeneratorFactory();
            }
            else
            {
                if (env.TargetFramework != TargetFramework.Net || env.TargetFrameworkVersion.Major < 7)
                {
                    // If we're using our downstream support, fall back to the Forwarder marshaller when the TypePositionInfo is unhandled.
                    generatorFactory = new ForwarderMarshallingGeneratorFactory();
                }
                else
                {
                    // If we're in a "supported" scenario, then emit a diagnostic as our final fallback.
                    generatorFactory = new UnsupportedMarshallingFactory();
                }

                generatorFactory = new MarshalAsMarshallingGeneratorFactory(options, generatorFactory);

                IAssemblySymbol coreLibraryAssembly = env.Compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly;
                ITypeSymbol? disabledRuntimeMarshallingAttributeType = coreLibraryAssembly.GetTypeByMetadataName(TypeNames.System_Runtime_CompilerServices_DisableRuntimeMarshallingAttribute);
                bool runtimeMarshallingDisabled = disabledRuntimeMarshallingAttributeType is not null
                    && env.Compilation.Assembly.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, disabledRuntimeMarshallingAttributeType));

                IMarshallingGeneratorFactory elementFactory = new AttributedMarshallingModelGeneratorFactory(generatorFactory, new AttributedMarshallingModelOptions(runtimeMarshallingDisabled));
                // We don't need to include the later generator factories for collection elements
                // as the later generator factories only apply to parameters.
                generatorFactory = new AttributedMarshallingModelGeneratorFactory(generatorFactory, elementFactory, new AttributedMarshallingModelOptions(runtimeMarshallingDisabled));

                generatorFactory = new ByValueContentsMarshalKindValidator(generatorFactory);
            }
            typeInfos.Add(retTypeInfo);

            return (typeInfos.ToImmutable(), generatorFactory);
        }

        public override bool Equals(object obj)
        {
            return obj is PInvokeStubContext other && Equals(other);
        }

        public bool Equals(PInvokeStubContext other)
        {
            // We don't check if the generator factories are equal since
            // the generator factory is deterministically created based on the ElementTypeInformation and Options.
            return other is not null
                && StubTypeNamespace == other.StubTypeNamespace
                && ElementTypeInformation.SequenceEqual(other.ElementTypeInformation)
                && StubContainingTypes.SequenceEqual(other.StubContainingTypes, (IEqualityComparer<TypeDeclarationSyntax>)SyntaxEquivalentComparer.Instance)
                && StubReturnType.IsEquivalentTo(other.StubReturnType)
                && AdditionalAttributes.SequenceEqual(other.AdditionalAttributes, (IEqualityComparer<AttributeListSyntax>)SyntaxEquivalentComparer.Instance)
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
