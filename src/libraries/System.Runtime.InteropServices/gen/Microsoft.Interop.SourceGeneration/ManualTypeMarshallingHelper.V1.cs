// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public readonly record struct CustomTypeMarshallerData_V1(CustomTypeMarshallerKind Kind, CustomTypeMarshallerDirection Direction, CustomTypeMarshallerFeatures Features, int? BufferSize);

    public static class ShapeMemberNames_V1
    {
        public abstract class Value
        {
            public const string ToNativeValue = nameof(ToNativeValue);
            public const string FromNativeValue = nameof(FromNativeValue);
            public const string GetPinnableReference = nameof(GetPinnableReference);
            public const string FreeNative = nameof(FreeNative);
            public const string ToManaged = nameof(ToManaged);
        }

        public abstract class LinearCollection : Value
        {
            public const string GetManagedValuesDestination = nameof(GetManagedValuesDestination);
            public const string GetManagedValuesSource = nameof(GetManagedValuesSource);
            public const string GetNativeValuesDestination = nameof(GetNativeValuesDestination);
            public const string GetNativeValuesSource = nameof(GetNativeValuesSource);
        }
    }
    public static class ManualTypeMarshallingHelper_V1
    {
        public static class CustomMarshallerAttributeFields
        {
            public const string BufferSize = nameof(BufferSize);
            public const string Direction = nameof(Direction);
            public const string Features = nameof(Features);
        }

        public static (bool hasAttribute, ITypeSymbol? managedType, CustomTypeMarshallerData_V1? kind) GetMarshallerShapeInfo(ITypeSymbol marshallerType)
        {
            var attr = marshallerType.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.ToDisplayString() == TypeNames.CustomTypeMarshallerAttribute);
            if (attr is null)
            {
                return (false, null, null);
            }
            if (attr.ConstructorArguments.Length == 0)
            {
                return (true, null, null);
            }
            CustomTypeMarshallerKind kind = CustomTypeMarshallerKind.Value;
            ITypeSymbol? managedType = attr.ConstructorArguments[0].Value as ITypeSymbol;
            if (attr.ConstructorArguments.Length > 1)
            {
                if (attr.ConstructorArguments[1].Value is not int i)
                {
                    return (true, managedType, null);
                }
                kind = (CustomTypeMarshallerKind)i;
            }
            var namedArguments = attr.NamedArguments.ToImmutableDictionary();
            int? bufferSize = namedArguments.TryGetValue(CustomMarshallerAttributeFields.BufferSize, out TypedConstant bufferSizeConstant) ? bufferSizeConstant.Value as int? : null;
            CustomTypeMarshallerDirection direction = namedArguments.TryGetValue(CustomMarshallerAttributeFields.Direction, out TypedConstant directionConstant) ? (CustomTypeMarshallerDirection)directionConstant.Value : CustomTypeMarshallerDirection.Ref;
            CustomTypeMarshallerFeatures features = namedArguments.TryGetValue(CustomMarshallerAttributeFields.Features, out TypedConstant featuresConstant) ? (CustomTypeMarshallerFeatures)featuresConstant.Value : CustomTypeMarshallerFeatures.None;
            return (true, managedType, new CustomTypeMarshallerData_V1(kind, direction, features, bufferSize));
        }

        /// <summary>
        /// Get the supported <see cref="CustomTypeMarshallerPinning"/> for a marshaller type
        /// </summary>
        /// <param name="marshallerType">The marshaller type.</param>
        /// <param name="managedType">The mananged type that would be marshalled.</param>
        /// <returns>Supported <see cref="CustomTypeMarshallerPinning"/></returns>
        public static CustomTypeMarshallerPinning GetMarshallerPinningFeatures(ITypeSymbol marshallerType, ITypeSymbol? managedType)
        {
            CustomTypeMarshallerPinning pinning = CustomTypeMarshallerPinning.None;

            if (ManualTypeMarshallingHelper.FindGetPinnableReference(marshallerType) is not null)
            {
                pinning |= CustomTypeMarshallerPinning.NativeType;
            }

            if (managedType is not null && ManualTypeMarshallingHelper.FindGetPinnableReference(managedType) is not null)
            {
                pinning |= CustomTypeMarshallerPinning.ManagedType;
            }

            return pinning;
        }

        public static bool HasToManagedMethod(ITypeSymbol nativeType, ITypeSymbol managedType)
        {
            return nativeType.GetMembers(ShapeMemberNames_V1.Value.ToManaged)
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
            CustomTypeMarshallerKind variant)
        {
            if (variant == CustomTypeMarshallerKind.LinearCollection)
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
            ITypeSymbol spanOfT,
            CustomTypeMarshallerKind variant,
            out ITypeSymbol? spanElementType)
        {
            spanElementType = null;
            if (variant == CustomTypeMarshallerKind.LinearCollection)
            {
                return ctor.Parameters.Length == 3
                    && SymbolEqualityComparer.Default.Equals(managedType, ctor.Parameters[0].Type)
                    && IsSpanOfUnmanagedType(ctor.Parameters[1].Type, spanOfT, out spanElementType)
                    && spanElementType.SpecialType == SpecialType.System_Byte
                    && ctor.Parameters[2].Type.SpecialType == SpecialType.System_Int32;
            }
            return ctor.Parameters.Length == 2
                && SymbolEqualityComparer.Default.Equals(managedType, ctor.Parameters[0].Type)
                && IsSpanOfUnmanagedType(ctor.Parameters[1].Type, spanOfT, out spanElementType);

            static bool IsSpanOfUnmanagedType(ITypeSymbol typeToCheck, ITypeSymbol spanOfT, out ITypeSymbol? typeArgument)
            {
                typeArgument = null;
                if (typeToCheck is INamedTypeSymbol namedType
                    && SymbolEqualityComparer.Default.Equals(spanOfT, namedType.ConstructedFrom)
                    && namedType.TypeArguments.Length == 1
                    && namedType.TypeArguments[0].IsUnmanagedType)
                {
                    typeArgument = namedType.TypeArguments[0];
                    return true;
                }

                return false;
            }
        }

        public static bool HasFreeNativeMethod(ITypeSymbol type)
        {
            return type.GetMembers(ShapeMemberNames_V1.Value.FreeNative)
                .OfType<IMethodSymbol>()
                .Any(m => m is { IsStatic: false, Parameters.Length: 0, ReturnType.SpecialType: SpecialType.System_Void });
        }

        public static IMethodSymbol? FindToNativeValueMethod(ITypeSymbol type)
        {
            return type.GetMembers(ShapeMemberNames_V1.Value.ToNativeValue)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, Parameters.Length: 0 });
        }

        public static IMethodSymbol? FindFromNativeValueMethod(ITypeSymbol type)
        {
            return type.GetMembers(ShapeMemberNames_V1.Value.FromNativeValue)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, Parameters.Length: 1, ReturnType.SpecialType: SpecialType.System_Void });
        }

        public static bool TryGetElementTypeFromLinearCollectionMarshaller(ITypeSymbol type, ITypeSymbol readOnlySpanOfT, out ITypeSymbol elementType)
        {
            if (FindGetManagedValuesSourceMethod(type, readOnlySpanOfT) is not IMethodSymbol managedValuesSourceMethod)
            {
                elementType = null!;
                return false;
            }

            elementType = ((INamedTypeSymbol)managedValuesSourceMethod.ReturnType).TypeArguments[0];
            return true;
        }

        public static IMethodSymbol? FindGetManagedValuesSourceMethod(ITypeSymbol type, ITypeSymbol readOnlySpanOfT)
        {
            return type
                .GetMembers(ShapeMemberNames_V1.LinearCollection.GetManagedValuesSource)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, ReturnsByRef: false, ReturnsByRefReadonly: false, Parameters.Length: 0, ReturnType: INamedTypeSymbol { ConstructedFrom: INamedTypeSymbol returnType } }
                    && SymbolEqualityComparer.Default.Equals(returnType, readOnlySpanOfT));
        }

        public static IMethodSymbol? FindGetManagedValuesDestinationMethod(ITypeSymbol type, ITypeSymbol spanOfT)
        {
            return type
                .GetMembers(ShapeMemberNames_V1.LinearCollection.GetManagedValuesDestination)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, ReturnsByRef: false, ReturnsByRefReadonly: false, Parameters.Length: 1, ReturnType: INamedTypeSymbol { ConstructedFrom: INamedTypeSymbol returnType } }
                    && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32
                    && SymbolEqualityComparer.Default.Equals(returnType, spanOfT));
        }

        public static IMethodSymbol? FindGetNativeValuesDestinationMethod(ITypeSymbol type, ITypeSymbol spanOfByte)
        {
            return type
                .GetMembers(ShapeMemberNames_V1.LinearCollection.GetNativeValuesDestination)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, ReturnsByRef: false, ReturnsByRefReadonly: false, Parameters.Length: 0, ReturnType: INamedTypeSymbol returnType }
                    && SymbolEqualityComparer.Default.Equals(returnType, spanOfByte));
        }

        public static IMethodSymbol? FindGetNativeValuesSourceMethod(ITypeSymbol type, ITypeSymbol readOnlySpanOfByte)
        {
            return type
                .GetMembers(ShapeMemberNames_V1.LinearCollection.GetNativeValuesSource)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, ReturnsByRef: false, ReturnsByRefReadonly: false, Parameters.Length: 1, ReturnType: INamedTypeSymbol returnType }
                    && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32
                    && SymbolEqualityComparer.Default.Equals(returnType, readOnlySpanOfByte));
        }
    }
}
