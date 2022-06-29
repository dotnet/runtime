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
    [Flags]
    public enum MarshallerShape
    {
        None = 0x0,
        ToUnmanaged = 0x1,
        CallerAllocatedBuffer = 0x2,
        PinnableReference = 0x4,
        ToManaged = 0x8,
        GuaranteedUnmarshal = 0x10,
        Free = 0x20,
    }

    public static class ShapeMemberNames
    {
        public const string GetPinnableReference = nameof(GetPinnableReference);
        public const string BufferSize = nameof(BufferSize);
        public const string Free = nameof(Free);

        public static class Value
        {
            public static class Stateless
            {
                public const string ConvertToManaged = nameof(ConvertToManaged);
                public const string ConvertToManagedGuaranteed = nameof(ConvertToManagedGuaranteed);
                public const string ConvertToUnmanaged = nameof(ConvertToUnmanaged);
            }
        }

        public static class LinearCollection
        {
            public static class Stateless
            {
                // Managed to unmanaged
                public const string AllocateContainerForUnmanagedElements = nameof(AllocateContainerForUnmanagedElements);
                public const string GetManagedValuesSource = nameof(GetManagedValuesSource);
                public const string GetUnmanagedValuesDestination = nameof(GetUnmanagedValuesDestination);

                // Unmanaged to managed
                public const string AllocateContainerForManagedElements = nameof(AllocateContainerForManagedElements);
                public const string AllocateContainerForManagedElementsGuaranteed = nameof(AllocateContainerForManagedElementsGuaranteed);
                public const string GetManagedValuesDestination = nameof(GetManagedValuesDestination);
                public const string GetUnmanagedValuesSource = nameof(GetUnmanagedValuesSource);
            }
        }
    }

    public static class MarshallerShapeHelper
    {
        public static (MarshallerShape, Dictionary<MarshallerShape, IMethodSymbol>) GetShapeForType(ITypeSymbol marshallerType, ITypeSymbol managedType, Compilation compilation)
        {
            MarshallerShape shape = MarshallerShape.None;
            var methodsByShape = new Dictionary<MarshallerShape, IMethodSymbol>();

            IMethodSymbol? method = GetConvertToUnmanagedMethod(marshallerType, managedType);
            if (method is not null)
                AddMethod(MarshallerShape.ToUnmanaged, method);

            INamedTypeSymbol spanOfT = compilation.GetTypeByMetadataName(TypeNames.System_Span_Metadata)!;
            method = GetConvertToUnmanagedWithCallerAllocatedBufferMethod(marshallerType, managedType, spanOfT, out _);
            if (method is not null)
                AddMethod(MarshallerShape.CallerAllocatedBuffer, method);

            method = GetConvertToManagedMethod(marshallerType, managedType);
            if (method is not null)
                AddMethod(MarshallerShape.ToManaged, method);

            method = GetConvertToManagedGuaranteedMethod(marshallerType, managedType);
            if (method is not null)
                AddMethod(MarshallerShape.GuaranteedUnmarshal, method);

            method = GetStatelessGetPinnableReference(marshallerType);
            if (method is not null)
                AddMethod(MarshallerShape.PinnableReference, method);

            method = GetStatelessFree(marshallerType);
            if (method is not null)
                AddMethod(MarshallerShape.Free, method);

            return (shape, methodsByShape);

            void AddMethod(MarshallerShape shapeToAdd, IMethodSymbol methodToAdd)
            {
                methodsByShape.Add(shapeToAdd, methodToAdd);
                shape |= shapeToAdd;
            }
        }

        private static IMethodSymbol? GetStatelessFree(ITypeSymbol type)
        {
            return type.GetMembers(ShapeMemberNames.Free)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: true, Parameters.Length: 1, ReturnsVoid: true });
        }

        private static IMethodSymbol? GetStatelessGetPinnableReference(ITypeSymbol type)
        {
            return type.GetMembers(ShapeMemberNames.GetPinnableReference)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: true, Parameters.Length: 1 } and
                    ({ ReturnsByRef: true } or { ReturnsByRefReadonly: true }));
        }

        private static IMethodSymbol? GetConvertToUnmanagedMethod(ITypeSymbol type, ITypeSymbol managedType)
        {
            return type.GetMembers(ShapeMemberNames.Value.Stateless.ConvertToUnmanaged)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: true, Parameters.Length: 1, ReturnsVoid: false }
                    && SymbolEqualityComparer.Default.Equals(managedType, m.Parameters[0].Type));
        }

        private static IMethodSymbol? GetConvertToUnmanagedWithCallerAllocatedBufferMethod(
            ITypeSymbol type,
            ITypeSymbol managedType,
            ITypeSymbol spanOfT,
            out ITypeSymbol? spanElementType)
        {
            spanElementType = null;
            IEnumerable<IMethodSymbol> methods = type.GetMembers(ShapeMemberNames.Value.Stateless.ConvertToUnmanaged)
                .OfType<IMethodSymbol>()
                .Where(m => m is { IsStatic: true, Parameters.Length: 2, ReturnsVoid: false }
                    && SymbolEqualityComparer.Default.Equals(managedType, m.Parameters[0].Type));

            foreach (IMethodSymbol method in methods)
            {
                if (IsSpanOfUnmanagedType(method.Parameters[1].Type, spanOfT, out spanElementType))
                {
                    return method;
                }
            }

            return null;

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

        private static IMethodSymbol? GetConvertToManagedMethod(ITypeSymbol type, ITypeSymbol managedType)
        {
            return type.GetMembers(ShapeMemberNames.Value.Stateless.ConvertToManaged)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: true, Parameters.Length: 1, ReturnsVoid: false }
                    && SymbolEqualityComparer.Default.Equals(managedType, m.ReturnType));
        }

        private static IMethodSymbol? GetConvertToManagedGuaranteedMethod(ITypeSymbol type, ITypeSymbol managedType)
        {
            return type.GetMembers(ShapeMemberNames.Value.Stateless.ConvertToManagedGuaranteed)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: true, Parameters.Length: 1, ReturnsVoid: false }
                    && SymbolEqualityComparer.Default.Equals(managedType, m.ReturnType));
        }
    }
}
