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
                        yield return Parameter(Identifier(typeInfo.InstanceIdentifier))
                            .WithType(typeInfo.ManagedType.Syntax)
                            .WithModifiers(MarshallerHelpers.GetManagedParameterModifiers(typeInfo));
                    }
                }
            }
        }

        public ImmutableArray<AttributeListSyntax> AdditionalAttributes { get; init; }

        public static SignatureContext Create(
            IMethodSymbol method,
            MarshallingInfoParser marshallingInfoParser,
            StubEnvironment env,
            CodeEmitOptions options,
            Assembly generatorInfoAssembly)
        {
            ImmutableArray<TypePositionInfo> typeInfos = GenerateTypeInformation(method, marshallingInfoParser, env);

            ImmutableArray<AttributeListSyntax>.Builder additionalAttrs = ImmutableArray.CreateBuilder<AttributeListSyntax>();

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

            if (options.SkipInit && !MethodIsSkipLocalsInit(env, method))
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
            // When the underlying method is a property accessor, bare attributes on the property declaration
            // (e.g. `[MarshalUsing(typeof(X))] string Prop { get; set; }`) land on the property symbol and
            // are not otherwise visible to the marshalling pipeline. Fall them through to the accessor's
            // value surface only -- the getter's return, or the setter's value parameter (the last
            // parameter, after any indexer index parameters). Index parameters on indexer accessors and the
            // setter's `void` return are not value surfaces and do not inherit property-level attributes.
            // Accessor-level attributes win over property-level ones on a per-type basis. Target-scoped
            // attributes (`[return:]`, `[param:]`, `[get:]`, `[set:]`) are routed by Roslyn onto the
            // accessor directly and so are already in the accessor's attribute set.
            ImmutableArray<AttributeData> associatedPropertyAttributes = method.AssociatedSymbol is IPropertySymbol property
                ? property.GetAttributes()
                : ImmutableArray<AttributeData>.Empty;

            // The value parameter on a setter is the last parameter (index parameters precede it on
            // indexer setters). Getters have no value parameter -- their value surface is the return.
            int valueParameterIndex = method.MethodKind == MethodKind.PropertySet
                ? method.Parameters.Length - 1
                : -1;

            ImmutableArray<TypePositionInfo>.Builder typeInfos = ImmutableArray.CreateBuilder<TypePositionInfo>();
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                IParameterSymbol param = method.Parameters[i];
                ImmutableArray<AttributeData> paramAttributes = i == valueParameterIndex
                    ? MergeAccessorAndPropertyAttributes(param.GetAttributes(), associatedPropertyAttributes)
                    : param.GetAttributes();
                MarshallingInfo marshallingInfo = marshallingInfoParser.ParseMarshallingInfo(param.Type, paramAttributes);
                var typeInfo = TypePositionInfo.CreateForParameter(param, marshallingInfo, env.Compilation);
                typeInfo = typeInfo with
                {
                    ManagedIndex = i,
                    NativeIndex = typeInfos.Count
                };
                typeInfos.Add(typeInfo);
            }

            ImmutableArray<AttributeData> returnAttributes = method.MethodKind == MethodKind.PropertyGet
                ? MergeAccessorAndPropertyAttributes(method.GetReturnTypeAttributes(), associatedPropertyAttributes)
                : method.GetReturnTypeAttributes();
            TypePositionInfo retTypeInfo = new(ManagedTypeInfo.CreateTypeInfoForTypeSymbol(method.ReturnType), marshallingInfoParser.ParseMarshallingInfo(method.ReturnType, returnAttributes));
            retTypeInfo = retTypeInfo with
            {
                ManagedIndex = TypePositionInfo.ReturnIndex,
                NativeIndex = TypePositionInfo.ReturnIndex,
            };

            typeInfos.Add(retTypeInfo);

            return typeInfos.ToImmutable();
        }

        private static ImmutableArray<AttributeData> MergeAccessorAndPropertyAttributes(
            ImmutableArray<AttributeData> accessorAttributes,
            ImmutableArray<AttributeData> associatedPropertyAttributes)
        {
            if (associatedPropertyAttributes.IsEmpty)
            {
                return accessorAttributes;
            }

            // Accessor-level attributes win over property-level ones at the same dedup key
            // (attribute type + ElementIndirectionDepth for [MarshalUsing], attribute type alone
            // otherwise). [MarshalUsing] is the only AllowMultiple = true attribute that flows
            // through this merge: it can repeat on a single value surface with distinct
            // ElementIndirectionDepth values to describe marshalling at successive levels of
            // indirection (the value itself at depth 0, its elements at depth 1, and so on). The
            // public contract on MarshalUsingAttribute.ElementIndirectionDepth states only one
            // [MarshalUsing] with a given depth may be provided on a given parameter or return
            // value, so dedup keys for [MarshalUsing] include the depth -- an accessor-level
            // [MarshalUsing] overrides only the property-level [MarshalUsing] at the matching
            // depth, and property-level [MarshalUsing]s at other depths flow through.
            //
            // To keep this dedup unambiguous, the COM generator additionally rejects accessor-level
            // [MarshalUsing] attributes that omit the marshaller type (see
            // MarshalUsingOnPropertyAccessorMustSpecifyType in GeneratorDiagnostics). That keeps the
            // partial-split case (e.g., marshaller type on the property and count-only on the
            // accessor) from silently dropping one side; the user combines the information on a
            // single attribute or attaches the count-only [MarshalUsing] to the property.
            HashSet<(string?, int)> accessorAttributeKeys = new();
            foreach (AttributeData attr in accessorAttributes)
            {
                accessorAttributeKeys.Add(GetMergeKey(attr));
            }

            ImmutableArray<AttributeData>.Builder merged = ImmutableArray.CreateBuilder<AttributeData>(accessorAttributes.Length + associatedPropertyAttributes.Length);
            merged.AddRange(accessorAttributes);
            foreach (AttributeData attr in associatedPropertyAttributes)
            {
                if (!accessorAttributeKeys.Contains(GetMergeKey(attr)))
                {
                    merged.Add(attr);
                }
            }
            return merged.ToImmutable();

            static (string?, int) GetMergeKey(AttributeData attr)
            {
                string? attributeName = attr.AttributeClass?.ToDisplayString();
                int depth = 0;
                if (attributeName == TypeNames.MarshalUsingAttribute)
                {
                    foreach (KeyValuePair<string, TypedConstant> named in attr.NamedArguments)
                    {
                        if (named.Key == ManualTypeMarshallingHelper.MarshalUsingProperties.ElementIndirectionDepth)
                        {
                            depth = (int)named.Value.Value!;
                            break;
                        }
                    }
                }
                return (attributeName, depth);
            }
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
            if (env.EnvironmentFlags.HasFlag(EnvironmentFlags.SkipLocalsInit))
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
