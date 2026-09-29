// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using RoslynRefKind = Microsoft.CodeAnalysis.RefKind;
using RoslynSpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace Microsoft.Interop
{
    internal static class ErrorHandlingInfoParser
    {
        public static ErrorHandlingInfo? Parse(
            IMethodSymbol method,
            StubEnvironment environment,
            GeneratorDiagnosticsBag diagnostics)
        {
            AttributeData? attribute = null;
            foreach (AttributeData candidate in method.GetAttributes())
            {
                if (candidate.AttributeClass?.ToDisplayString() == TypeNames.ErrorHandlerAttribute)
                {
                    if (attribute is not null)
                    {
                        diagnostics.ReportConfigurationNotSupported(candidate, nameof(TypeNames.ErrorHandlerAttribute));
                        return null;
                    }

                    attribute = candidate;
                }
            }

            if (attribute is null)
            {
                return null;
            }

            if (attribute.AttributeConstructor is null
                || attribute.ConstructorArguments.Length != 2
                || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol marshallerType
                || attribute.ConstructorArguments[1].Value is not int locationValue
                || locationValue is < (int)ErrorHandlingLocation.ReturnValue or > (int)ErrorHandlingLocation.HiddenLastParameter)
            {
                diagnostics.ReportConfigurationNotSupported(attribute, nameof(TypeNames.ErrorHandlerAttribute));
                return null;
            }

            if (!ManualTypeMarshallingHelper.TryGetManagedTypeFromEntryType(marshallerType, environment.Compilation, out ITypeSymbol? managedType))
            {
                diagnostics.ReportConfigurationNotSupported(attribute, nameof(TypeNames.ErrorHandlerAttribute));
                return null;
            }

            ErrorHandlingLocation location = (ErrorHandlingLocation)locationValue;
            if (location == ErrorHandlingLocation.ReturnValue
                && method.ReturnType.SpecialType != RoslynSpecialType.System_Void
                && !SymbolEqualityComparer.Default.Equals(method.ReturnType, managedType))
            {
                diagnostics.ReportConfigurationNotSupported(attribute, nameof(TypeNames.ErrorHandlerAttribute));
                return null;
            }

            if (location == ErrorHandlingLocation.LastParameter
                && (method.Parameters.Length == 0
                    || method.Parameters[method.Parameters.Length - 1] is not { RefKind: RoslynRefKind.Out or RoslynRefKind.Ref } lastParameter
                    || !SymbolEqualityComparer.Default.Equals(lastParameter.Type, managedType)))
            {
                diagnostics.ReportConfigurationNotSupported(attribute, nameof(TypeNames.ErrorHandlerAttribute));
                return null;
            }

            MarshallingInfo marshallingInfo = CustomMarshallingInfoHelper.CreateNativeMarshallingInfoForNonSignatureElement(
                managedType,
                marshallerType,
                attribute,
                environment.Compilation,
                diagnostics);

            if (marshallingInfo == NoMarshallingInfo.Instance)
            {
                diagnostics.ReportConfigurationNotSupported(attribute, nameof(TypeNames.ErrorHandlerAttribute));
                return null;
            }

            return new ErrorHandlingInfo(
                ManagedTypeInfo.CreateTypeInfoForTypeSymbol(managedType),
                marshallingInfo,
                location);
        }
    }
}
