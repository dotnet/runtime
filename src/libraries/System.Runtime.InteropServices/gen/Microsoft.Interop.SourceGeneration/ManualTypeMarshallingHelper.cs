// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public static class ManualTypeMarshallingHelper
    {
        public const string ValuePropertyName = "Value";
        public const string GetPinnableReferenceName = "GetPinnableReference";
        public const string BufferSizeFieldName = "BufferSize";
        public const string RequiresStackBufferFieldName = "RequiresStackBuffer";
        public const string ToManagedMethodName = "ToManaged";
        public const string FreeNativeMethodName = "FreeNative";
        public const string ManagedValuesPropertyName = "ManagedValues";
        public const string NativeValueStoragePropertyName = "NativeValueStorage";
        public const string SetUnmarshalledCollectionLengthMethodName = "SetUnmarshalledCollectionLength";

        public static class MarshalUsingProperties
        {
            public const string ElementIndirectionLevel = nameof(ElementIndirectionLevel);
            public const string CountElementName = nameof(CountElementName);
            public const string ConstantElementCount = nameof(ConstantElementCount);
        }

        public enum NativeTypeMarshallingVariant
        {
            Standard,
            ContiguousCollection
        }

        public static bool HasToManagedMethod(ITypeSymbol nativeType, ITypeSymbol managedType)
        {
            return nativeType.GetMembers(ToManagedMethodName)
                    .OfType<IMethodSymbol>()
                    .Any(m => m.Parameters.IsEmpty
                        && !m.ReturnsByRef
                        && !m.ReturnsByRefReadonly
                        && SymbolEqualityComparer.Default.Equals(m.ReturnType, managedType)
                        && !m.IsStatic);
        }

        public static bool IsManagedToNativeConstructor(
            IMethodSymbol ctor,
            ITypeSymbol managedType,
            NativeTypeMarshallingVariant variant)
        {
            if (variant == NativeTypeMarshallingVariant.ContiguousCollection)
            {
                return ctor.Parameters.Length == 2
                && SymbolEqualityComparer.Default.Equals(managedType, ctor.Parameters[0].Type)
                && ctor.Parameters[1].Type.SpecialType == SpecialType.System_Int32;
            }
            return ctor.Parameters.Length == 1
                && SymbolEqualityComparer.Default.Equals(managedType, ctor.Parameters[0].Type);
        }

        public static bool IsCallerAllocatedSpanConstructor(
            IMethodSymbol ctor,
            ITypeSymbol managedType,
            ITypeSymbol spanOfByte,
            NativeTypeMarshallingVariant variant)
        {
            if (variant == NativeTypeMarshallingVariant.ContiguousCollection)
            {
                return ctor.Parameters.Length == 3
                && SymbolEqualityComparer.Default.Equals(managedType, ctor.Parameters[0].Type)
                && SymbolEqualityComparer.Default.Equals(spanOfByte, ctor.Parameters[1].Type)
                && ctor.Parameters[2].Type.SpecialType == SpecialType.System_Int32;
            }
            return ctor.Parameters.Length == 2
                && SymbolEqualityComparer.Default.Equals(managedType, ctor.Parameters[0].Type)
                && SymbolEqualityComparer.Default.Equals(spanOfByte, ctor.Parameters[1].Type);
        }

        public static IMethodSymbol? FindGetPinnableReference(ITypeSymbol type)
        {
            // Lookup a GetPinnableReference method based on the spec for the pattern-based
            // fixed statement. We aren't supporting a GetPinnableReference extension method
            // (which is apparently supported in the compiler).
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-7.3/pattern-based-fixed
            return type.GetMembers(GetPinnableReferenceName)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { Parameters: { Length: 0 } } and
                    ({ ReturnsByRef: true } or { ReturnsByRefReadonly: true }));
        }

        public static IPropertySymbol? FindValueProperty(ITypeSymbol type)
        {
            return type.GetMembers(ValuePropertyName)
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => !p.IsStatic);
        }

        public static bool HasFreeNativeMethod(ITypeSymbol type)
        {
            return type.GetMembers(FreeNativeMethodName)
                .OfType<IMethodSymbol>()
                .Any(m => m is { IsStatic: false, Parameters: { Length: 0 }, ReturnType: { SpecialType: SpecialType.System_Void } });
        }

        public static bool TryGetManagedValuesProperty(ITypeSymbol type, out IPropertySymbol managedValuesProperty)
        {
            managedValuesProperty = type
                .GetMembers(ManagedValuesPropertyName)
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => p is { IsStatic: false, GetMethod: not null, ReturnsByRef: false, ReturnsByRefReadonly: false });
            return managedValuesProperty is not null;
        }

        public static bool TryGetElementTypeFromContiguousCollectionMarshaller(ITypeSymbol type, out ITypeSymbol elementType)
        {
            if (!TryGetManagedValuesProperty(type, out IPropertySymbol managedValuesProperty))
            {
                elementType = null!;
                return false;
            }

            elementType = ((INamedTypeSymbol)managedValuesProperty.Type).TypeArguments[0];
            return true;
        }

        public static bool HasSetUnmarshalledCollectionLengthMethod(ITypeSymbol type)
        {
            return type.GetMembers(SetUnmarshalledCollectionLengthMethodName)
                .OfType<IMethodSymbol>()
                .Any(m => m is
                {
                    IsStatic: false,
                    Parameters: { Length: 1 },
                    ReturnType: { SpecialType: SpecialType.System_Void }
                } && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32);
        }

        public static bool HasNativeValueStorageProperty(ITypeSymbol type, ITypeSymbol spanOfByte)
        {
            return type
                .GetMembers(NativeValueStoragePropertyName)
                .OfType<IPropertySymbol>()
                .Any(p => p is { IsStatic: false, GetMethod: not null, ReturnsByRef: false, ReturnsByRefReadonly: false }
                    && SymbolEqualityComparer.Default.Equals(p.Type, spanOfByte));
        }
    }
}
