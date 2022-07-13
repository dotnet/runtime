// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed record SignatureContext
    {
        // We don't need the warnings around not setting the various
        // non-nullable fields/properties on this type in the constructor
        // since we always use a property initializer.
#pragma warning disable 8618
        private SignatureContext()
        {
        }
#pragma warning restore

        public ImmutableArray<TypePositionInfo> ElementTypeInformation { get; init; }

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

        public static SignatureContext Create(
            IMethodSymbol method,
            InteropAttributeData interopAttributeData,
            StubEnvironment env,
            IGeneratorDiagnostics diagnostics,
            Assembly generatorInfoAssembly)
        {
            ImmutableArray<TypePositionInfo> typeInfos = GenerateTypeInformation(method, interopAttributeData, diagnostics, env);

            ImmutableArray<AttributeListSyntax>.Builder additionalAttrs = ImmutableArray.CreateBuilder<AttributeListSyntax>();

            if (env.TargetFramework != TargetFramework.Unknown)
            {
                string generatorName = generatorInfoAssembly.GetName().Name;
                string generatorVersion = generatorInfoAssembly.GetName().Version.ToString();
                // Define additional attributes for the stub definition.
                additionalAttrs.Add(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(ParseName(TypeNames.System_CodeDom_Compiler_GeneratedCodeAttribute),
                                AttributeArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
                                            AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(generatorName))),
                                            AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(generatorVersion)))
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

            return new SignatureContext()
            {
                StubReturnType = method.ReturnType.AsTypeSyntax(),
                ElementTypeInformation = typeInfos,
                AdditionalAttributes = additionalAttrs.ToImmutable(),
            };
        }

        private static ImmutableArray<TypePositionInfo> GenerateTypeInformation(IMethodSymbol method, InteropAttributeData interopAttributeData, IGeneratorDiagnostics diagnostics, StubEnvironment env)
        {
            // Compute the current default string encoding value.
            CharEncoding defaultEncoding = CharEncoding.Undefined;
            if (interopAttributeData.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling))
            {
                defaultEncoding = interopAttributeData.StringMarshalling switch
                {
                    StringMarshalling.Utf16 => CharEncoding.Utf16,
                    StringMarshalling.Utf8 => CharEncoding.Utf8,
                    StringMarshalling.Custom => CharEncoding.Custom,
                    _ => CharEncoding.Undefined, // [Compat] Do not assume a specific value
                };
            }
            else if (interopAttributeData.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshallingCustomType))
            {
                defaultEncoding = CharEncoding.Custom;
            }

            var defaultInfo = new DefaultMarshallingInfo(defaultEncoding, interopAttributeData.StringMarshallingCustomType);

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

            typeInfos.Add(retTypeInfo);

            return typeInfos.ToImmutable();
        }

        public bool Equals(SignatureContext other)
        {
            // We don't check if the generator factories are equal since
            // the generator factory is deterministically created based on the ElementTypeInformation and Options.
            return other is not null
                && ElementTypeInformation.SequenceEqual(other.ElementTypeInformation)
                && StubReturnType.IsEquivalentTo(other.StubReturnType)
                && AdditionalAttributes.SequenceEqual(other.AdditionalAttributes, (IEqualityComparer<AttributeListSyntax>)SyntaxEquivalentComparer.Instance);
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

            if (method.GetAttributes().Any(IsSkipLocalsInitAttribute))
            {
                return true;
            }

            for (INamedTypeSymbol type = method.ContainingType; type is not null; type = type.ContainingType)
            {
                if (type.GetAttributes().Any(IsSkipLocalsInitAttribute))
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
