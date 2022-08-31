// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class JSSignatureContext : IEquatable<JSSignatureContext>
    {
        private static SymbolDisplayFormat s_typeNameFormat { get; } = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes
        );

        internal static readonly string GeneratorName = typeof(JSImportGenerator).Assembly.GetName().Name;

        internal static readonly string GeneratorVersion = typeof(JSImportGenerator).Assembly.GetName().Version.ToString();

        public static JSSignatureContext Create(
            IMethodSymbol method,
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

            string stubTypeFullName = method.ContainingType.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

            (ImmutableArray<TypePositionInfo> typeInfos, IMarshallingGeneratorFactory generatorFactory) = GenerateTypeInformation(method, diagnostics, env);

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

            // there could be multiple method signatures with the same name, get unique signature name
            uint hash = 17;
            unchecked
            {
                foreach (var param in typeInfos)
                {
                    hash = hash * 31 + (uint)param.ManagedType.FullTypeName.GetHashCode();
                }
            };
            int typesHash = Math.Abs((int)hash);

            var fullName = $"{method.ContainingType.ToDisplayString()}.{method.Name}";
            string qualifiedName = GetFullyQualifiedMethodName(env, method);

            return new JSSignatureContext()
            {
                StubReturnType = method.ReturnType.AsTypeSyntax(),
                ElementTypeInformation = typeInfos,
                TypesHash = typesHash,
                StubTypeFullName = stubTypeFullName,
                AdditionalAttributes = additionalAttrs.ToImmutable(),
                MethodName = fullName,
                QualifiedMethodName = qualifiedName,
                BindingName = "__signature_" + method.Name + "_" + typesHash,
                GeneratorFactory = generatorFactory
            };
        }

        private static string GetFullyQualifiedMethodName(StubEnvironment env, IMethodSymbol method)
        {
            // Mono style nested class name format.
            string typeName = method.ContainingType.ToDisplayString(s_typeNameFormat).Replace(".", "/");

            if (!method.ContainingType.ContainingNamespace.IsGlobalNamespace)
                typeName = $"{method.ContainingType.ContainingNamespace.ToDisplayString()}.{typeName}";

            return $"[{env.Compilation.AssemblyName}]{typeName}:{method.Name}";
        }

        private static (ImmutableArray<TypePositionInfo>, IMarshallingGeneratorFactory) GenerateTypeInformation(IMethodSymbol method, GeneratorDiagnostics diagnostics, StubEnvironment env)
        {
            ImmutableArray<IUseSiteAttributeParser> useSiteAttributeParsers = ImmutableArray.Create<IUseSiteAttributeParser>(new JSMarshalAsAttributeParser(env.Compilation));
            var jsMarshallingAttributeParser = new MarshallingInfoParser(
                diagnostics,
                new MethodSignatureElementInfoProvider(env.Compilation, diagnostics, method, useSiteAttributeParsers),
                useSiteAttributeParsers,
                ImmutableArray.Create<IMarshallingInfoAttributeParser>(new JSMarshalAsAttributeParser(env.Compilation)),
                ImmutableArray.Create<ITypeBasedMarshallingInfoProvider>(new FallbackJSMarshallingInfoProvider()));

            // Determine parameter and return types
            ImmutableArray<TypePositionInfo>.Builder typeInfos = ImmutableArray.CreateBuilder<TypePositionInfo>();
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                IParameterSymbol param = method.Parameters[i];
                MarshallingInfo jsMarshallingInfo = jsMarshallingAttributeParser.ParseMarshallingInfo(param.Type, param.GetAttributes());

                var typeInfo = TypePositionInfo.CreateForParameter(param, jsMarshallingInfo, env.Compilation) with
                {
                    ManagedIndex = i,
                    NativeIndex = typeInfos.Count,
                };
                typeInfos.Add(typeInfo);
            }

            MarshallingInfo retJSMarshallingInfo = jsMarshallingAttributeParser.ParseMarshallingInfo(method.ReturnType, method.GetReturnTypeAttributes());

            var retTypeInfo = new TypePositionInfo(ManagedTypeInfo.CreateTypeInfoForTypeSymbol(method.ReturnType), retJSMarshallingInfo)
            {
                ManagedIndex = TypePositionInfo.ReturnIndex,
                NativeIndex = TypePositionInfo.ReturnIndex,
                MarshallingAttributeInfo = retJSMarshallingInfo,
            };

            typeInfos.Add(retTypeInfo);
            var jsGeneratorFactory = new JSGeneratorFactory();
            return (typeInfos.ToImmutable(), jsGeneratorFactory);
        }

        public ImmutableArray<TypePositionInfo> ElementTypeInformation { get; init; }
        public string? StubTypeFullName { get; init; }
        public int TypesHash { get; init; }

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

        public string MethodName { get; init; }
        public string QualifiedMethodName { get; init; }
        public string BindingName { get; init; }

        public IMarshallingGeneratorFactory GeneratorFactory { get; init; }

        public override bool Equals(object obj)
        {
            return obj is JSSignatureContext other && Equals(other);
        }

        public bool Equals(JSSignatureContext other)
        {
            // We don't check if the generator factories are equal since
            // the generator factory is deterministically created based on the ElementTypeInformation and Options.
            return other is not null
                && StubTypeFullName == other.StubTypeFullName
                && ElementTypeInformation.SequenceEqual(other.ElementTypeInformation)
                && StubReturnType.IsEquivalentTo(other.StubReturnType)
                && AdditionalAttributes.SequenceEqual(other.AdditionalAttributes, (IEqualityComparer<AttributeListSyntax>)SyntaxEquivalentComparer.Instance)
                ;
        }

        public override int GetHashCode()
        {
            throw new UnreachableException();
        }
    }
}
