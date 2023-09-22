// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator : IIncrementalGenerator
    {
        internal sealed partial class Parser
        {
            private readonly struct InvocationDiagnosticInfo
            {
                public InvocationDiagnosticInfo(DiagnosticDescriptor descriptor, object[]? messageArgs) =>
                    (Descriptor, MessageArgs) = (descriptor, messageArgs);

                public DiagnosticDescriptor Descriptor { get; }
                public object[]? MessageArgs { get; }
            }

            private readonly struct TypeParseInfo
            {
                public ITypeSymbol TypeSymbol { get; }
                public required MethodsToGen BindingOverload { get; init; }
                public required BinderInvocation BinderInvocation { get; init; }
                public required ContainingTypeDiagnosticInfo? ContainingTypeDiagnosticInfo { get; init; }

                public TypeParseInfo(ITypeSymbol type, Compilation compilation)
                {
                    Debug.Assert(compilation is not null);
                    // Trim compile-time erased metadata such as tuple labels and NRT annotations.
                    //TypeSymbol = compilation.EraseCompileTimeMetadata(type);
                    TypeSymbol = type;
                }

                public TypeParseInfo ToTransitiveTypeParseInfo(ITypeSymbol memberType, Compilation compilation, DiagnosticDescriptor? diagDescriptor = null, string? memberName = null)
                {
                    ContainingTypeDiagnosticInfo? diagnosticInfo = diagDescriptor is null
                        ? null
                        : new ContainingTypeDiagnosticInfo
                        {
                            TypeName = TypeSymbol.GetTypeName().Name,
                            Descriptor = diagDescriptor,
                            MemberName = memberName,
                        };

                    return new TypeParseInfo(memberType, compilation)
                    {
                        BindingOverload = BindingOverload,
                        BinderInvocation = BinderInvocation,
                        ContainingTypeDiagnosticInfo = diagnosticInfo,
                    };
                }
            }

            private readonly struct ContainingTypeDiagnosticInfo
            {
                public required string TypeName { get; init; }
                public required string? MemberName { get; init; }
                public required DiagnosticDescriptor Descriptor { get; init; }
            }

            private bool IsValidRootConfigType([NotNullWhen(true)] ITypeSymbol? type)
            {
                if (type is null ||
                    type.SpecialType is SpecialType.System_Object or SpecialType.System_Void ||
                    !_typeSymbols.Compilation.IsSymbolAccessibleWithin(type, _typeSymbols.Compilation.Assembly) ||
                    type.TypeKind is TypeKind.TypeParameter or TypeKind.Pointer or TypeKind.Error ||
                    type.IsRefLikeType ||
                    ContainsGenericParameters(type))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
