﻿// Licensed to the .NET Foundation under one or more agreements.
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

        public IEnumerable<TypePositionInfo> ManagedParameters => ElementTypeInformation.Where(tpi => !TypePositionInfo.IsSpecialIndex(tpi.ManagedIndex));

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
                        SyntaxTokenList tokens = TokenList();

                        // "out" parameters are implicitly scoped, so we can't put the "scoped" keyword on them.
                        // All other cases of explicit parameters are only scoped when the "scoped" keyword is present.
                        // When the "scoped" keyword is present, it must be present on all declarations.
                        if (typeInfo.ScopedKind != ScopedKind.None && typeInfo.RefKind != RefKind.Out)
                        {
                            tokens = tokens.Add(Token(SyntaxKind.ScopedKeyword));
                        }

                        if (typeInfo.IsByRef)
                        {
                            tokens = tokens.Add(Token(typeInfo.RefKindSyntax));
                        }

                        yield return Parameter(Identifier(typeInfo.InstanceIdentifier))
                            .WithType(typeInfo.ManagedType.Syntax)
                            .WithModifiers(tokens);
                    }
                }
            }
        }

        public ImmutableArray<AttributeListSyntax> AdditionalAttributes { get; init; }

        public static SignatureContext Create(
            IMethodSymbol method,
            MarshallingInfoParser marshallingInfoParser,
            StubEnvironment env,
            Assembly generatorInfoAssembly)
        {
            ImmutableArray<TypePositionInfo> typeInfos = GenerateTypeInformation(method, marshallingInfoParser, env);

            ImmutableArray<AttributeListSyntax>.Builder additionalAttrs = ImmutableArray.CreateBuilder<AttributeListSyntax>();

            if (env.TargetFramework != TargetFramework.Unknown)
            {
                string generatorName = generatorInfoAssembly.GetName().Name;
                string generatorVersion = generatorInfoAssembly.GetName().Version.ToString();
                // Define additional attributes for the stub definition.
                additionalAttrs.Add(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(
                                NameSyntaxes.System_CodeDom_Compiler_GeneratedCodeAttribute,
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
                            Attribute(NameSyntaxes.System_Runtime_CompilerServices_SkipLocalsInitAttribute))));
            }

            return new SignatureContext()
            {
                StubReturnType = method.ReturnType.AsTypeSyntax(),
                ElementTypeInformation = typeInfos,
                AdditionalAttributes = additionalAttrs.ToImmutable(),
            };
        }

        private static ImmutableArray<TypePositionInfo> GenerateTypeInformation(
            IMethodSymbol method,
            MarshallingInfoParser marshallingInfoParser,
            StubEnvironment env)
        {

            // Determine parameter and return types
            ImmutableArray<TypePositionInfo>.Builder typeInfos = ImmutableArray.CreateBuilder<TypePositionInfo>();
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                IParameterSymbol param = method.Parameters[i];
                MarshallingInfo marshallingInfo = marshallingInfoParser.ParseMarshallingInfo(param.Type, param.GetAttributes());
                var typeInfo = TypePositionInfo.CreateForParameter(param, marshallingInfo, env.Compilation);
                typeInfo = typeInfo with
                {
                    ManagedIndex = i,
                    NativeIndex = typeInfos.Count
                };
                typeInfos.Add(typeInfo);
            }

            TypePositionInfo retTypeInfo = new(ManagedTypeInfo.CreateTypeInfoForTypeSymbol(method.ReturnType), marshallingInfoParser.ParseMarshallingInfo(method.ReturnType, method.GetReturnTypeAttributes()));
            retTypeInfo = retTypeInfo with
            {
                ManagedIndex = TypePositionInfo.ReturnIndex,
                NativeIndex = TypePositionInfo.ReturnIndex,
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
