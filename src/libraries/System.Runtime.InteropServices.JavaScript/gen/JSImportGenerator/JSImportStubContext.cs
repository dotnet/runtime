// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class JSSignatureContext : IEquatable<JSSignatureContext>
    {
        internal static readonly string GeneratorName = typeof(JSImportGenerator).Assembly.GetName().Name;

        internal static readonly string GeneratorVersion = typeof(JSImportGenerator).Assembly.GetName().Version.ToString();
        internal static bool MethodIsSkipLocalsInit(StubEnvironment env, IMethodSymbol method)
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

            string stubTypeFullName = "";

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

                stubTypeFullName = currType.Name + (string.IsNullOrEmpty(stubTypeFullName) ? "" : ".") + stubTypeFullName;
                currType = currType.ContainingType;
            }
            stubTypeFullName = stubTypeNamespace == null ? stubTypeFullName : (stubTypeNamespace + "." + stubTypeFullName);

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
            string qualifiedName;
            var ns = string.Join(".", method.ContainingType.ToDisplayParts().Where(p => p.Kind == SymbolDisplayPartKind.NamespaceName).Select(x => x.ToString()).ToArray());
            var cn = string.Join("/", method.ContainingType.ToDisplayParts().Where(p => p.Kind == SymbolDisplayPartKind.ClassName).Select(x => x.ToString()).ToArray());
            var qclasses = method.ContainingType.ContainingNamespace == null ? ns : ns + "." + cn;
            qualifiedName = $"[{env.Compilation.AssemblyName}]{qclasses}:{method.Name}";

            return new JSSignatureContext()
            {
                StubReturnType = method.ReturnType.AsTypeSyntax(),
                ElementTypeInformation = typeInfos,
                StubTypeNamespace = stubTypeNamespace,
                TypesHash = typesHash,
                StubTypeFullName = stubTypeFullName,
                StubContainingTypes = containingTypes.ToImmutable(),
                AdditionalAttributes = additionalAttrs.ToImmutable(),
                MethodName = fullName,
                QualifiedMethodName = qualifiedName,
                BindingName = "__signature_" + method.Name + "_" + typesHash,
                GeneratorFactory = generatorFactory
            };
        }

        private static (ImmutableArray<TypePositionInfo>, IMarshallingGeneratorFactory) GenerateTypeInformation(IMethodSymbol method, GeneratorDiagnostics diagnostics, StubEnvironment env)
        {
            var jsMarshallingAttributeParser = new JSMarshallingAttributeInfoParser(env.Compilation, diagnostics, method);

            // Determine parameter and return types
            ImmutableArray<TypePositionInfo>.Builder typeInfos = ImmutableArray.CreateBuilder<TypePositionInfo>();
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                IParameterSymbol param = method.Parameters[i];
                MarshallingInfo marshallingInfo = NoMarshallingInfo.Instance;
                MarshallingInfo jsMarshallingInfo = jsMarshallingAttributeParser.ParseMarshallingInfo(param.Type, param.GetAttributes(), marshallingInfo);

                var typeInfo = TypePositionInfo.CreateForParameter(param, marshallingInfo, env.Compilation);
                typeInfo = JSTypeInfo.CreateForType(typeInfo, param.Type, jsMarshallingInfo, env.Compilation);
                typeInfo = typeInfo with
                {
                    ManagedIndex = i,
                    NativeIndex = typeInfos.Count,
                };
                typeInfos.Add(typeInfo);
            }

            MarshallingInfo retMarshallingInfo = NoMarshallingInfo.Instance;
            MarshallingInfo retJSMarshallingInfo = jsMarshallingAttributeParser.ParseMarshallingInfo(method.ReturnType, method.GetReturnTypeAttributes(), retMarshallingInfo);

            var retTypeInfo = new TypePositionInfo(ManagedTypeInfo.CreateTypeInfoForTypeSymbol(method.ReturnType), retMarshallingInfo);
            retTypeInfo = JSTypeInfo.CreateForType(retTypeInfo, method.ReturnType, retJSMarshallingInfo, env.Compilation);
            retTypeInfo = retTypeInfo with
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

        public string? StubTypeNamespace { get; init; }
        public string? StubTypeFullName { get; init; }
        public int TypesHash { get; init; }

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
                && StubTypeNamespace == other.StubTypeNamespace
                && StubTypeFullName == other.StubTypeFullName
                && ElementTypeInformation.SequenceEqual(other.ElementTypeInformation)
                && StubContainingTypes.SequenceEqual(other.StubContainingTypes, (IEqualityComparer<TypeDeclarationSyntax>)SyntaxEquivalentComparer.Instance)
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
