// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public enum CustomTypeMarshallerKind
    {
        Value,
        LinearCollection
    }
    public readonly record struct CustomTypeMarshallerData(CustomTypeMarshallerKind Kind, int? BufferSize, bool RequiresStackBuffer);
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
            public const string ElementIndirectionDepth = nameof(ElementIndirectionDepth);
            public const string CountElementName = nameof(CountElementName);
            public const string ConstantElementCount = nameof(ConstantElementCount);
        }

        public static (bool hasAttribute, ITypeSymbol? managedType, CustomTypeMarshallerData? kind) GetMarshallerShapeInfo(ITypeSymbol marshallerType)
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
            if (attr.ConstructorArguments.Length >= 1)
            {
                if (attr.ConstructorArguments[1].Value is not int i)
                {
                    return (true, managedType, null);
                }
                kind = (CustomTypeMarshallerKind)i;
            }
            var namedArguments = attr.NamedArguments.ToImmutableDictionary();
            int? bufferSize = null;
            if (namedArguments.TryGetValue(BufferSizeFieldName, out TypedConstant bufferSizeConstant))
            {
                bufferSize = bufferSizeConstant.Value as int?;
            }
            bool requiresStackBuffer = false;
            if (namedArguments.TryGetValue(RequiresStackBufferFieldName, out TypedConstant requiresStackBufferConstant))
            {
                requiresStackBuffer = bufferSizeConstant.Value as bool? ?? false;
            }
            return (true, managedType, new CustomTypeMarshallerData(kind, bufferSize, requiresStackBuffer));
        }

        /// <summary>
        /// Resolve a non-<see cref="INamedTypeSymbol"/> <paramref name="managedType"/> to the correct managed type if <paramref name="marshallerType"/> is generic and <paramref name="managedType"/> is using any placeholder types.
        /// </summary>
        /// <param name="managedType">The non-named managed type.</param>
        /// <param name="marshallerType">The marshaller type.</param>
        /// <param name="compilation">The compilation to use to make new type symbols.</param>
        /// <returns>The resolved managed type, or <paramref name="managedType"/> if the provided type did not have any placeholders.</returns>
        public static ITypeSymbol? ResolveManagedType(ITypeSymbol? managedType, INamedTypeSymbol marshallerType, Compilation compilation)
        {
            if (managedType is null or INamedTypeSymbol || !marshallerType.IsGenericType)
            {
                return managedType;
            }
            Stack<ITypeSymbol> typeStack = new();
            ITypeSymbol? innerType = managedType;
            while (innerType.TypeKind is TypeKind.Array or TypeKind.Pointer)
            {
                if (innerType is IArrayTypeSymbol array)
                {
                    typeStack.Push(innerType);
                    innerType = array.ElementType;
                }
                else if (innerType is IPointerTypeSymbol pointerType)
                {
                    typeStack.Push(innerType);
                    innerType = pointerType.PointedAtType;
                }
            }

            if (innerType.ToDisplayString() != TypeNames.CustomTypeMarshallerAttributeGenericPlaceholder)
            {
                return managedType;
            }

            ITypeSymbol resultType = marshallerType.TypeArguments[0];

            while (typeStack.Count > 0)
            {
                ITypeSymbol wrapperType = typeStack.Pop();
                if (wrapperType.TypeKind == TypeKind.Pointer)
                {
                    resultType = compilation.CreatePointerTypeSymbol(resultType);
                }
                else if (wrapperType.TypeKind == TypeKind.Array)
                {
                    IArrayTypeSymbol arrayType = (IArrayTypeSymbol)wrapperType;
                    if (arrayType.IsSZArray)
                    {
                        resultType = compilation.CreateArrayTypeSymbol(resultType, arrayType.Rank);
                    }
                }
            }
            return resultType;
        }

        public static (AttributeData? attribute, INamedTypeSymbol? marshallerType) GetDefaultMarshallerInfo(ITypeSymbol managedType)
        {
            AttributeData? attr = managedType.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.ToDisplayString() == TypeNames.NativeMarshallingAttribute);
            if (attr is null)
            {
                return (attr, null);
            }
            INamedTypeSymbol? marshallerType = null;
            if (attr.ConstructorArguments.Length == 0)
            {
                return (attr, null);
            }

            marshallerType = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
            if (managedType is not INamedTypeSymbol namedType || marshallerType is null)
            {
                return (attr, null);
            }
            if (namedType.TypeArguments.Length == 0)
            {
                return (attr, marshallerType);
            }
            else if (marshallerType.TypeArguments.Length != namedType.TypeArguments.Length)
            {
                return (attr, null);
            }
            else if (marshallerType.IsGenericType)
            {
                // Construct the marshaler type around the same type arguments as the managed type.
                return (attr, marshallerType.ConstructedFrom.Construct(namedType.TypeArguments, namedType.TypeArgumentNullableAnnotations));
            }

            return (attr, marshallerType);
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
            ITypeSymbol spanOfByte,
            CustomTypeMarshallerKind variant)
        {
            if (variant == CustomTypeMarshallerKind.LinearCollection)
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

        public static bool TryGetElementTypeFromLinearCollectionMarshaller(ITypeSymbol type, out ITypeSymbol elementType)
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
