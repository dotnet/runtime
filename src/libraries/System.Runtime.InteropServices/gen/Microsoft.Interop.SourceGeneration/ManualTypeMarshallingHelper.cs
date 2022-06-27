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
    public readonly record struct CustomTypeMarshallerData(
        ManagedTypeInfo MarshallerType,
        ManagedTypeInfo NativeType,
        bool HasState,
        MarshallerShape Shape,
        bool IsStrictlyBlittable,
        ManagedTypeInfo? BufferElementType);

    public readonly record struct CustomTypeMarshallers(CustomTypeMarshallerData? In, CustomTypeMarshallerData? Ref, CustomTypeMarshallerData? Out);

    public static class ManualTypeMarshallingHelper
    {
        public static class MarshalUsingProperties
        {
            public const string ElementIndirectionDepth = nameof(ElementIndirectionDepth);
            public const string CountElementName = nameof(CountElementName);
            public const string ConstantElementCount = nameof(ConstantElementCount);
        }

        internal static class MarshallersProperties
        {
            public const string InMarshaller = nameof(InMarshaller);
            public const string RefMarshaller = nameof(RefMarshaller);
            public const string OutMarshaller = nameof(OutMarshaller);
        }

        [Flags]
        private enum MarshallingDirection
        {
            ManagedToUnmanaged = 0x1,
            UnmanagedToManaged = 0x2,
            Bidirectional = ManagedToUnmanaged | UnmanagedToManaged
        }

        public static bool IsLinearCollectionEntryPoint(ITypeSymbol entryPointType)
        {
            // TODO: Check for linear collection marshaller - ElementUnmanagedType attribute on last generic parameter
            return false;
        }

        public static bool TryGetMarshallers(ITypeSymbol entryPointType, ITypeSymbol managedType, bool isLinearCollectionMarshalling, Compilation compilation, out CustomTypeMarshallers? marshallers)
        {
            marshallers = null;
            var attr = entryPointType.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.ToDisplayString() == TypeNames.ManagedToUnmanagedMarshallersAttribute);
            if (attr is null || attr.ConstructorArguments.Length == 0)
                return false;

            ITypeSymbol? managedTypeOnAttr = attr.ConstructorArguments[0].Value as ITypeSymbol;
            if (!SymbolEqualityComparer.Default.Equals(managedType, managedTypeOnAttr)
                && !compilation.HasImplicitConversion(managedType, managedTypeOnAttr))
                return false;

            var namedArguments = attr.NamedArguments.ToImmutableDictionary();
            CustomTypeMarshallerData? inMarshaller = GetNamedArgumentAsMarshallerData(namedArguments, MarshallersProperties.InMarshaller, MarshallingDirection.ManagedToUnmanaged, managedTypeOnAttr, compilation);
            if (inMarshaller is null)
                inMarshaller = GetMarshallerDataForType(entryPointType, MarshallingDirection.ManagedToUnmanaged, managedTypeOnAttr, compilation);

            CustomTypeMarshallerData? refMarshaller = GetNamedArgumentAsMarshallerData(namedArguments, MarshallersProperties.RefMarshaller, MarshallingDirection.Bidirectional, managedTypeOnAttr, compilation);
            if (refMarshaller is null)
                refMarshaller = GetMarshallerDataForType(entryPointType, MarshallingDirection.Bidirectional, managedTypeOnAttr, compilation);

            CustomTypeMarshallerData? outMarshaller = GetNamedArgumentAsMarshallerData(namedArguments, MarshallersProperties.OutMarshaller, MarshallingDirection.UnmanagedToManaged, managedTypeOnAttr, compilation);
            if (outMarshaller is null)
                outMarshaller = GetMarshallerDataForType(entryPointType, MarshallingDirection.UnmanagedToManaged, managedTypeOnAttr, compilation);

            if (inMarshaller is null && refMarshaller is null && outMarshaller is null)
                return false;

            marshallers = new CustomTypeMarshallers()
            {
                In = inMarshaller,
                Ref = refMarshaller,
                Out = outMarshaller,
            };

            return true;
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
            if (managedType is null || !marshallerType.IsGenericType)
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

        public static IMethodSymbol? FindGetPinnableReference(ITypeSymbol type)
        {
            // Lookup a GetPinnableReference method based on the spec for the pattern-based
            // fixed statement. We aren't supporting a GetPinnableReference extension method
            // (which is apparently supported in the compiler).
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-7.3/pattern-based-fixed
            return type.GetMembers(ShapeMemberNames.GetPinnableReference)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { Parameters.Length: 0 } and
                    ({ ReturnsByRef: true } or { ReturnsByRefReadonly: true }));
        }

        private static CustomTypeMarshallerData? GetNamedArgumentAsMarshallerData(ImmutableDictionary<string, TypedConstant> namedArguments, string name, MarshallingDirection direction, ITypeSymbol managedType, Compilation compilation)
        {
            ITypeSymbol? marshallerType = namedArguments.TryGetValue(name, out TypedConstant typeMaybe) ? typeMaybe.Value as ITypeSymbol : null;
            if (marshallerType is null)
                return null;

            // TODO: Report invalid shape
            return GetMarshallerDataForType(marshallerType, direction, managedType, compilation);
        }

        private static CustomTypeMarshallerData? GetMarshallerDataForType(ITypeSymbol marshallerType, MarshallingDirection direction, ITypeSymbol managedType, Compilation compilation)
        {
            if (marshallerType is { IsStatic: true, TypeKind: TypeKind.Class })
            {
                return GetStatelessMarshallerDataForType(marshallerType, direction, managedType, compilation);
            }
            return null;
        }

        private static CustomTypeMarshallerData? GetStatelessMarshallerDataForType(ITypeSymbol marshallerType, MarshallingDirection direction, ITypeSymbol managedType, Compilation compilation)
        {
            (MarshallerShape shape, Dictionary<MarshallerShape, IMethodSymbol> methodsByShape) = StatelessMarshallerShapeHelper.GetShapeForType(marshallerType, managedType, compilation);

            ITypeSymbol? nativeType = null;
            if (direction.HasFlag(MarshallingDirection.ManagedToUnmanaged))
            {
                if (!shape.HasFlag(MarshallerShape.CallerAllocatedBuffer) && !shape.HasFlag(MarshallerShape.ToUnmanaged))
                    return null;

                IMethodSymbol method;
                if (methodsByShape.TryGetValue(MarshallerShape.CallerAllocatedBuffer, out method))
                {
                    nativeType = method.ReturnType;
                }
                else if (methodsByShape.TryGetValue(MarshallerShape.ToUnmanaged, out method))
                {
                    nativeType = method.ReturnType;
                }
            }

            if (direction.HasFlag(MarshallingDirection.UnmanagedToManaged))
            {
                if (!shape.HasFlag(MarshallerShape.GuaranteedUnmarshal) && !shape.HasFlag(MarshallerShape.ToManaged))
                    return null;

                IMethodSymbol method;
                if (methodsByShape.TryGetValue(MarshallerShape.GuaranteedUnmarshal, out method))
                {
                    nativeType = method.Parameters[0].Type;
                }
                else if (methodsByShape.TryGetValue(MarshallerShape.ToManaged, out method))
                {
                    nativeType = method.Parameters[0].Type;
                }
            }

            // Bidirectional requires ToUnmanaged without the caller-allocated buffer
            if (direction.HasFlag(MarshallingDirection.Bidirectional) && !shape.HasFlag(MarshallerShape.ToUnmanaged))
                return null;

            if (nativeType is null)
                return null;

            ManagedTypeInfo bufferElementType = null;
            if (methodsByShape.TryGetValue(MarshallerShape.CallerAllocatedBuffer, out IMethodSymbol methodWithBuffer))
            {
                bufferElementType = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(((INamedTypeSymbol)methodWithBuffer.Parameters[1].Type).TypeArguments[0]);
            }

            return new CustomTypeMarshallerData(
                ManagedTypeInfo.CreateTypeInfoForTypeSymbol(marshallerType),
                ManagedTypeInfo.CreateTypeInfoForTypeSymbol(nativeType),
                HasState: false,
                shape,
                nativeType.IsStrictlyBlittable(),
                bufferElementType);
        }

        private static CustomTypeMarshallerData? GetStatefulMarshallerDataForType(ITypeSymbol marshallerType, MarshallingDirection direction, ITypeSymbol managedType, Compilation compilation)
        {
            (MarshallerShape shape, StatefulMarshallerShapeHelper.MarshallerMethods methods) = StatefulMarshallerShapeHelper.GetShapeForType(marshallerType, managedType, compilation);

            ITypeSymbol? nativeType = null;
            if (direction.HasFlag(MarshallingDirection.ManagedToUnmanaged))
            {
                if (!shape.HasFlag(MarshallerShape.CallerAllocatedBuffer) && !shape.HasFlag(MarshallerShape.ToUnmanaged))
                    return null;

                if (methods.ToUnmanaged is not null)
                {
                    nativeType = methods.ToUnmanaged.ReturnType;
                }
            }

            if (direction.HasFlag(MarshallingDirection.UnmanagedToManaged))
            {
                if (!shape.HasFlag(MarshallerShape.GuaranteedUnmarshal) && !shape.HasFlag(MarshallerShape.ToManaged))
                    return null;

                if (methods.FromUnmanaged is not null)
                {
                    nativeType = methods.FromUnmanaged.Parameters[0].Type;
                }
            }

            // Bidirectional requires ToUnmanaged without the caller-allocated buffer
            if (direction.HasFlag(MarshallingDirection.Bidirectional) && !shape.HasFlag(MarshallerShape.ToUnmanaged))
                return null;

            if (nativeType is null)
                return null;

            ManagedTypeInfo bufferElementType = null;
            if (methods.FromManagedWithBuffer is not null)
            {
                bufferElementType = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(((INamedTypeSymbol)methods.FromManagedWithBuffer.Parameters[1].Type).TypeArguments[0]);
            }

            return new CustomTypeMarshallerData(
                ManagedTypeInfo.CreateTypeInfoForTypeSymbol(marshallerType),
                ManagedTypeInfo.CreateTypeInfoForTypeSymbol(nativeType),
                HasState: true,
                shape,
                nativeType.IsStrictlyBlittable(),
                bufferElementType);
        }
    }
}
