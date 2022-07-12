// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
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
        ManagedTypeInfo? BufferElementType,
        ManagedTypeInfo? CollectionElementType,
        MarshallingInfo? CollectionElementMarshallingInfo);

    public readonly record struct CustomTypeMarshallers(
        ImmutableDictionary<MarshalMode, CustomTypeMarshallerData> Modes)
    {
        public CustomTypeMarshallerData GetModeOrDefault(MarshalMode mode)
        {
            CustomTypeMarshallerData data;
            if (Modes.TryGetValue(mode, out data))
                return data;

            if (Modes.TryGetValue(MarshalMode.Default, out data))
                return data;

            // TODO: Hard failure based on previous implementation
            throw new InvalidOperationException();
        }

        public bool TryGetModeOrDefault(MarshalMode mode, out CustomTypeMarshallerData data)
        {
            if (Modes.TryGetValue(mode, out data))
                return true;

            return Modes.TryGetValue(MarshalMode.Default, out data);
        }

        public bool IsDefinedOrDefault(MarshalMode mode)
        {
            return Modes.ContainsKey(mode) || Modes.ContainsKey(MarshalMode.Default);
        }
    }

    public static class ManualTypeMarshallingHelper
    {
        public static class MarshalUsingProperties
        {
            public const string ElementIndirectionDepth = nameof(ElementIndirectionDepth);
            public const string CountElementName = nameof(CountElementName);
            public const string ConstantElementCount = nameof(ConstantElementCount);
        }

        public static bool IsLinearCollectionEntryPoint(INamedTypeSymbol entryPointType)
        {
            return entryPointType.IsGenericType
                && entryPointType.GetAttributes().Any(attr => attr.AttributeClass.ToDisplayString() == TypeNames.ContiguousCollectionMarshallerAttribute);
        }

        public static bool HasEntryPointMarshallerAttribute(ITypeSymbol entryPointType)
        {
            return entryPointType.GetAttributes().Any(attr => attr.AttributeClass.ToDisplayString() == TypeNames.CustomMarshallerAttribute);
        }

        public static bool TryGetValueMarshallersFromEntryType(
            INamedTypeSymbol entryPointType,
            ITypeSymbol managedType,
            Compilation compilation,
            out CustomTypeMarshallers? marshallers)
        {
            return TryGetMarshallersFromEntryType(entryPointType, managedType, isLinearCollectionMarshalling: false, compilation, getMarshallingInfoForElement: null, out marshallers);
        }

        public static bool TryGetLinearCollectionMarshallersFromEntryType(
            INamedTypeSymbol entryPointType,
            ITypeSymbol managedType,
            Compilation compilation,
            Func<ITypeSymbol, MarshallingInfo> getMarshallingInfo,
            out CustomTypeMarshallers? marshallers)
        {
            return TryGetMarshallersFromEntryType(entryPointType, managedType, isLinearCollectionMarshalling: true, compilation, getMarshallingInfo, out marshallers);
        }

        private static bool TryGetMarshallersFromEntryType(
            INamedTypeSymbol entryPointType,
            ITypeSymbol managedType,
            bool isLinearCollectionMarshalling,
            Compilation compilation,
            Func<ITypeSymbol, MarshallingInfo> getMarshallingInfoForElement,
            out CustomTypeMarshallers? marshallers)
        {
            marshallers = null;
            var attrs = entryPointType.GetAttributes().Where(attr => attr.AttributeClass.ToDisplayString() == TypeNames.CustomMarshallerAttribute).ToArray();
            if (attrs is null || attrs.Length == 0)
                return false;

            // We expect a callback for getting the element marshalling info when handling linear collection marshalling
            if (isLinearCollectionMarshalling && getMarshallingInfoForElement is null)
                return false;

            Dictionary<MarshalMode, CustomTypeMarshallerData> modes = new();

            foreach (AttributeData attr in attrs)
            {
                if (attr.AttributeConstructor is null)
                {
                    // If the attribute constructor couldn't be bound by the compiler, then we shouldn't try to extract the constructor arguments.
                    // Roslyn doesn't provide them if it can't bind the constructor.
                    // We don't report a diagnostic here since Roslyn will report a diagnostic anyway.
                    continue;
                }

                if (attr.ConstructorArguments.Length != 3)
                {
                    Debug.WriteLine($"{attr} has {attr.ConstructorArguments.Length} constructor arguments - expected 3");
                    continue;
                }

                // Verify the defined marshaller is for the managed type.
                ITypeSymbol? managedTypeOnAttr = attr.ConstructorArguments[0].Value as ITypeSymbol;

                // Verify any instantiation of Generic parameters is provided by entry point.
                // TODO: Hard failure based on previous implementation
                ITypeSymbol? managedTypeInst = ResolveManagedType(managedTypeOnAttr, entryPointType, compilation);
                if (managedTypeInst is null)
                    return false;

                // Verify any instantiated managed types are derived properly.
                // TODO: Hard failure based on previous implementation
                if (!managedType.IsConstructedFromEqualTypes(managedTypeInst)
                    && !compilation.HasImplicitConversion(managedType, managedTypeInst))
                    return false;

                var marshalMode = (MarshalMode)attr.ConstructorArguments[1].Value!;

                ITypeSymbol? marshallerTypeOnAttr = attr.ConstructorArguments[2].Value as ITypeSymbol;
                if (marshallerTypeOnAttr is null)
                    continue;

                ITypeSymbol marshallerType = marshallerTypeOnAttr;
                if (isLinearCollectionMarshalling && marshallerTypeOnAttr is INamedTypeSymbol namedMarshallerType)
                {
                    // Update the marshaller type with resolved type arguments based on the entry point type
                    // We expect the entry point to already have its type arguments updated based on the managed type
                    Stack<string> nestedTypeNames = new Stack<string>();
                    INamedTypeSymbol currentType = namedMarshallerType;
                    while (currentType is not null)
                    {
                        if (currentType.IsConstructedFromEqualTypes(entryPointType))
                            break;

                        nestedTypeNames.Push(currentType.Name);
                        currentType = currentType.ContainingType;
                    }

                    currentType = entryPointType;
                    foreach (string name in nestedTypeNames)
                    {
                        currentType = currentType.GetTypeMembers(name).First();
                    }

                    marshallerType = currentType;
                }

                // TODO: Report invalid shape for mode
                //       Skip checking for bidirectional support for Default mode - always take / store marshaller data
                CustomTypeMarshallerData? data = GetMarshallerDataForType(marshallerType, marshalMode, managedType, isLinearCollectionMarshalling, compilation, getMarshallingInfoForElement);

                // TODO: Should we fire a diagnostic for duplicated modes or just take the last one?
                if (data is null
                    || modes.ContainsKey(marshalMode))
                {
                    continue;
                }

                modes.Add(marshalMode, data.Value);
            }

            if (modes.Count == 0)
                return false;

            marshallers = new CustomTypeMarshallers()
            {
                Modes = modes.ToImmutableDictionary()
            };

            return true;
        }

        /// <summary>
        /// Resolve a non-<see cref="INamedTypeSymbol"/> <paramref name="managedType"/> to the correct
        /// managed type if <paramref name="entryType"/> is generic and <paramref name="managedType"/>
        /// is using any placeholder types.
        /// </summary>
        /// <param name="managedType">The non-named managed type.</param>
        /// <param name="entryType">The marshaller type.</param>
        /// <param name="compilation">The compilation to use to make new type symbols.</param>
        /// <returns>The resolved managed type, or <paramref name="managedType"/> if the provided type did not have any placeholders.</returns>
        public static ITypeSymbol? ResolveManagedType(ITypeSymbol? managedType, INamedTypeSymbol entryType, Compilation compilation)
        {
            if (managedType is null || !entryType.IsGenericType)
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

            if (innerType.ToDisplayString() != TypeNames.CustomTypeMarshallerAttributeGenericPlaceholder
                && innerType.ToDisplayString() != TypeNames.CustomMarshallerAttributeGenericPlaceholder)
            {
                return managedType;
            }

            ITypeSymbol resultType = entryType.TypeArguments[0];

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

        /// <summary>
        /// Get the managed type's defined marshaller entry type.
        /// </summary>
        /// <param name="managedType">The managed type.</param>
        /// <returns>The attribute data and entry type for marshalling.</returns>
        public static (AttributeData? attribute, INamedTypeSymbol? entryType) GetDefaultMarshallerEntryType(ITypeSymbol managedType)
        {
            AttributeData? attr = managedType.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.ToDisplayString() == TypeNames.NativeMarshallingAttribute);
            if (attr is null || attr.ConstructorArguments.Length == 0)
            {
                return (attr, null);
            }

            INamedTypeSymbol? entryType = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
            if (managedType is not INamedTypeSymbol namedType || entryType is null)
            {
                return (attr, null);
            }

            // Non-generic types involved, return the entry defined in the attribute.
            if (namedType.TypeArguments.Length == 0)
            {
                return (attr, entryType);
            }

            // Mismatch of generic type arguments between the type and entry.
            if (namedType.TypeArguments.Length != entryType.TypeArguments.Length)
            {
                return (attr, null);
            }

            // If the marshaller is generic, instantiate it based on the type.
            if (entryType.IsGenericType)
            {
                // Construct the marshaler type around the same type arguments as the managed type.
                return (attr, entryType.ConstructedFrom.Construct(namedType.TypeArguments, namedType.TypeArgumentNullableAnnotations));
            }

            // Entry isn't generic, just return it.
            return (attr, entryType);
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

        private static CustomTypeMarshallerData? GetMarshallerDataForType(
            ITypeSymbol marshallerType,
            MarshalMode mode,
            ITypeSymbol managedType,
            bool isLinearCollectionMarshaller,
            Compilation compilation,
            Func<ITypeSymbol, MarshallingInfo> getMarshallingInfo)
        {
            if (marshallerType is { IsStatic: true, TypeKind: TypeKind.Class })
            {
                return GetStatelessMarshallerDataForType(marshallerType, mode, managedType, isLinearCollectionMarshaller, compilation, getMarshallingInfo);
            }
            if (marshallerType.IsValueType)
            {
                return GetStatefulMarshallerDataForType(marshallerType, mode, managedType, isLinearCollectionMarshaller, compilation, getMarshallingInfo);
            }
            return null;
        }

        private static bool ModeUsesManagedToUnmanagedShape(MarshalMode mode)
            => mode is MarshalMode.Default
                or MarshalMode.ManagedToUnmanagedIn
                or MarshalMode.UnmanagedToManagedOut
                or MarshalMode.ElementIn
                or MarshalMode.ManagedToUnmanagedRef
                or MarshalMode.UnmanagedToManagedRef
                or MarshalMode.ElementRef;

        private static bool ModeUsesUnmanagedToManagedShape(MarshalMode mode)
            => mode is MarshalMode.Default
                or MarshalMode.ManagedToUnmanagedOut
                or MarshalMode.UnmanagedToManagedIn
                or MarshalMode.ElementOut
                or MarshalMode.ManagedToUnmanagedRef
                or MarshalMode.UnmanagedToManagedRef
                or MarshalMode.ElementRef;

        private static CustomTypeMarshallerData? GetStatelessMarshallerDataForType(ITypeSymbol marshallerType, MarshalMode mode, ITypeSymbol managedType, bool isLinearCollectionMarshaller, Compilation compilation, Func<ITypeSymbol, MarshallingInfo>? getMarshallingInfo)
        {
            (MarshallerShape shape, StatelessMarshallerShapeHelper.MarshallerMethods methods) = StatelessMarshallerShapeHelper.GetShapeForType(marshallerType, managedType, isLinearCollectionMarshaller, compilation);

            ITypeSymbol? collectionElementType = null;
            ITypeSymbol? nativeType = null;
            if (ModeUsesManagedToUnmanagedShape(mode))
            {
                if (mode != MarshalMode.Default && !shape.HasFlag(MarshallerShape.CallerAllocatedBuffer) && !shape.HasFlag(MarshallerShape.ToUnmanaged))
                    return null;

                if (isLinearCollectionMarshaller)
                {
                    // Element type is the type parameter of the ReadOnlySpan returned by GetManagedValuesSource
                    collectionElementType = ((INamedTypeSymbol)methods.ManagedValuesSource.ReturnType).TypeArguments[0];
                }

                // Native type is the return type of ConvertToUnmanaged / AllocateContainerForUnmanagedElement
                if (methods.ToUnmanagedWithBuffer is not null)
                {
                    nativeType = methods.ToUnmanagedWithBuffer.ReturnType;
                }
                else if (methods.ToUnmanaged is not null)
                {
                    nativeType = methods.ToUnmanaged.ReturnType;
                }
            }

            if (ModeUsesUnmanagedToManagedShape(mode))
            {
                // Unmanaged to managed requires ToManaged either with or without guaranteed unmarshal
                if (mode != MarshalMode.Default && !shape.HasFlag(MarshallerShape.GuaranteedUnmarshal) && !shape.HasFlag(MarshallerShape.ToManaged))
                    return null;

                if (isLinearCollectionMarshaller)
                {
                    // Native type is the first parameter of GetUnmanagedValuesSource
                    nativeType = methods.UnmanagedValuesSource.Parameters[0].Type;

                    // Element type is the type parameter of the Span returned by GetManagedValuesDestination
                    collectionElementType = ((INamedTypeSymbol)methods.ManagedValuesDestination.ReturnType).TypeArguments[0];
                }
                else
                {
                    // Native type is the first parameter of ConvertToManaged or ConvertToManagedFinally
                    if (methods.ToManagedFinally is not null)
                    {
                        nativeType = methods.ToManagedFinally.Parameters[0].Type;
                    }
                    else if (methods.ToManaged is not null)
                    {
                        nativeType = methods.ToManaged.Parameters[0].Type;
                    }
                }
            }

            // Bidirectional requires ToUnmanaged without the caller-allocated buffer
            if (mode != MarshalMode.Default && ModeUsesManagedToUnmanagedShape(mode) && ModeUsesUnmanagedToManagedShape(mode) && !shape.HasFlag(MarshallerShape.ToUnmanaged))
                return null;

            if (nativeType is null)
                return null;

            ManagedTypeInfo bufferElementType = null;
            if (methods.ToUnmanagedWithBuffer is not null)
            {
                bufferElementType = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(((INamedTypeSymbol)methods.ToUnmanagedWithBuffer.Parameters[1].Type).TypeArguments[0]);
            }

            ManagedTypeInfo? collectionElementTypeInfo = null;
            MarshallingInfo? collectionElementMarshallingInfo = null;
            if (collectionElementType is not null)
            {
                collectionElementTypeInfo = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(collectionElementType);
                collectionElementMarshallingInfo = getMarshallingInfo(collectionElementType);
            }

            return new CustomTypeMarshallerData(
                ManagedTypeInfo.CreateTypeInfoForTypeSymbol(marshallerType),
                ManagedTypeInfo.CreateTypeInfoForTypeSymbol(nativeType),
                HasState: false,
                shape,
                nativeType.IsStrictlyBlittable(),
                bufferElementType,
                collectionElementTypeInfo,
                collectionElementMarshallingInfo);
        }

        private static CustomTypeMarshallerData? GetStatefulMarshallerDataForType(
            ITypeSymbol marshallerType,
            MarshalMode mode,
            ITypeSymbol managedType,
            bool isLinearCollectionMarshaller,
            Compilation compilation,
            Func<ITypeSymbol, MarshallingInfo>? getMarshallingInfo)
        {
            (MarshallerShape shape, StatefulMarshallerShapeHelper.MarshallerMethods methods) = StatefulMarshallerShapeHelper.GetShapeForType(marshallerType, managedType, isLinearCollectionMarshaller, compilation);

            ITypeSymbol? nativeType = null;
            ITypeSymbol? collectionElementType = null;
            if (ModeUsesManagedToUnmanagedShape(mode))
            {
                // Managed to unmanaged requires ToUnmanaged either with or without the caller-allocated buffer
                if (mode != MarshalMode.Default && !shape.HasFlag(MarshallerShape.CallerAllocatedBuffer) && !shape.HasFlag(MarshallerShape.ToUnmanaged))
                    return null;

                if (methods.ToUnmanaged is not null)
                {
                    nativeType = methods.ToUnmanaged.ReturnType;
                }

                if (isLinearCollectionMarshaller)
                {
                    // Element type is the type parameter of the ReadOnlySpan returned by GetManagedValuesSource
                    collectionElementType = ((INamedTypeSymbol)methods.ManagedValuesSource.ReturnType).TypeArguments[0];
                }
            }

            if (ModeUsesUnmanagedToManagedShape(mode))
            {
                // Unmanaged to managed requires ToManaged either with or without guaranteed unmarshal
                if (mode != MarshalMode.Default && !shape.HasFlag(MarshallerShape.GuaranteedUnmarshal) && !shape.HasFlag(MarshallerShape.ToManaged))
                    return null;

                if (methods.FromUnmanaged is not null && nativeType is null)
                {
                    nativeType = methods.FromUnmanaged.Parameters[0].Type;
                }

                if (isLinearCollectionMarshaller && collectionElementType is null)
                {
                    // Element type is the type parameter of the Span returned by GetManagedValuesDestination
                    collectionElementType = ((INamedTypeSymbol)methods.ManagedValuesDestination.ReturnType).TypeArguments[0];
                }
            }

            // Bidirectional requires ToUnmanaged without the caller-allocated buffer
            if (mode != MarshalMode.Default && ModeUsesManagedToUnmanagedShape(mode) && ModeUsesUnmanagedToManagedShape(mode) && !shape.HasFlag(MarshallerShape.ToUnmanaged))
                return null;

            if (nativeType is null)
                return null;

            ManagedTypeInfo bufferElementType = null;
            if (methods.FromManagedWithBuffer is not null)
            {
                bufferElementType = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(((INamedTypeSymbol)methods.FromManagedWithBuffer.Parameters[1].Type).TypeArguments[0]);
            }

            ManagedTypeInfo? collectionElementTypeInfo = null;
            MarshallingInfo? collectionElementMarshallingInfo = null;
            if (collectionElementType is not null)
            {
                collectionElementTypeInfo = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(collectionElementType);
                collectionElementMarshallingInfo = getMarshallingInfo(collectionElementType);
            }

            return new CustomTypeMarshallerData(
                ManagedTypeInfo.CreateTypeInfoForTypeSymbol(marshallerType),
                ManagedTypeInfo.CreateTypeInfoForTypeSymbol(nativeType),
                HasState: true,
                shape,
                nativeType.IsStrictlyBlittable(),
                bufferElementType,
                CollectionElementType: collectionElementTypeInfo,
                CollectionElementMarshallingInfo: collectionElementMarshallingInfo);
        }
    }
}
