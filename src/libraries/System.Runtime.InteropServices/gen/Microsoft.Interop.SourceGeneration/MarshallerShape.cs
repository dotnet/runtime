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
        StatelessPinnableReference = 0x4,
        StatefulPinnableReference = 0x8,
        ToManaged = 0x10,
        GuaranteedUnmarshal = 0x20,
        Free = 0x40,
        NotifyInvokeSucceeded = 0x80,
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

            public static class Stateful
            {
                // Managed to Unmanaged
                public const string FromManaged = nameof(FromManaged);
                public const string ToUnmanaged = nameof(ToUnmanaged);
                // Unmanaged to managed
                public const string ToManaged = nameof(ToManaged);
                public const string ToManagedGuaranteed = nameof(ToManagedGuaranteed);
                public const string FromUnmanaged = nameof(FromUnmanaged);
                // Optional features
                public const string Free = nameof(Free);
                public const string NotifyInvokeSucceeded = nameof(NotifyInvokeSucceeded);
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

            public static class Stateful
            {
                // Managed to Unmanaged
                public const string FromManaged = nameof(FromManaged);
                public const string ToUnmanaged = nameof(ToUnmanaged);
                public const string GetManagedValuesSource = nameof(GetManagedValuesSource);
                public const string GetUnmanagedValuesDestination = nameof(GetUnmanagedValuesDestination);
                // Unmanaged to managed
                public const string GetManagedValuesDestination = nameof(GetManagedValuesDestination);
                public const string GetUnmanagedValuesSource = nameof(GetUnmanagedValuesSource);
                public const string ToManaged = nameof(ToManaged);
                public const string ToManagedGuaranteed = nameof(ToManagedGuaranteed);
                public const string FromUnmanaged = nameof(FromUnmanaged);
                // Optional features
                public const string Free = nameof(Free);
                public const string NotifyInvokeSucceeded = nameof(NotifyInvokeSucceeded);
            }
        }
    }

    public static class StatelessMarshallerShapeHelper
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

            method = GetStatelessGetPinnableReference(marshallerType, managedType);
            if (method is not null)
                AddMethod(MarshallerShape.StatelessPinnableReference, method);

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

        private static IMethodSymbol? GetStatelessGetPinnableReference(ITypeSymbol type, ITypeSymbol managedType)
        {
            return type.GetMembers(ShapeMemberNames.GetPinnableReference)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: true, Parameters.Length: 1 } and
                    ({ ReturnsByRef: true } or { ReturnsByRefReadonly: true })
                    && SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, managedType));
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

    public static class StatefulMarshallerShapeHelper
    {
        public record MarshallerMethods
        {
            public IMethodSymbol? FromManaged { get; init; }
            public IMethodSymbol? FromManagedWithBuffer { get; init; }
            public IMethodSymbol? ToManaged { get; init; }
            public IMethodSymbol? ToManagedGuranteed { get; init; }
            public IMethodSymbol? FromUnmanaged { get; init; }
            public IMethodSymbol? ToUnmanaged { get; init; }
            public IMethodSymbol? Free { get; init; }
            public IMethodSymbol? NotifyInvokeSucceeded { get; init; }
        }

        public static (MarshallerShape shape, MarshallerMethods methods) GetShapeForType(ITypeSymbol marshallerType, ITypeSymbol managedType, Compilation compilation)
        {
            MarshallerShape shape = MarshallerShape.None;
            MarshallerMethods methods = new();

            ITypeSymbol? unmanagedType = null;

            IMethodSymbol? fromManaged = GetFromManagedMethod(marshallerType, managedType);
            INamedTypeSymbol spanOfT = compilation.GetTypeByMetadataName(TypeNames.System_Span_Metadata)!;
            IMethodSymbol? fromManagedWithCallerAllocatedBuffer = GetFromManagedWithCallerAllocatedBufferMethod(marshallerType, managedType, spanOfT, out _);

            IMethodSymbol? toUnmanaged = GetToUnmanagedMethod(marshallerType);

            if ((fromManaged, fromManagedWithCallerAllocatedBuffer) is not (null, null) && toUnmanaged is not null)
            {
                unmanagedType = toUnmanaged.ReturnType;
                if (unmanagedType.IsUnmanagedType)
                {
                    if (fromManagedWithCallerAllocatedBuffer is not null)
                    {
                        shape |= MarshallerShape.CallerAllocatedBuffer;
                    }
                    if (fromManaged is not null)
                    {
                        shape |= MarshallerShape.ToUnmanaged;
                    }
                    methods = methods with
                    {
                        FromManaged = fromManaged,
                        FromManagedWithBuffer = fromManagedWithCallerAllocatedBuffer,
                        ToUnmanaged = toUnmanaged
                    };
                }
            }

            IMethodSymbol toManaged = GetToManagedMethod(marshallerType, managedType);
            IMethodSymbol toManagedGuaranteed = GetToManagedGuaranteedMethod(marshallerType, managedType);
            ImmutableArray<IMethodSymbol> fromUnmanagedCandidates = ImmutableArray.CreateRange(GetFromUnmanagedMethodCandidates(marshallerType));
            if ((toManaged, toManagedGuaranteed) is not (null, null)
                && fromUnmanagedCandidates.Length == 1
                && (unmanagedType is null || SymbolEqualityComparer.Default.Equals(fromUnmanagedCandidates[0].Parameters[0].Type, unmanagedType)))
            {
                if (toManagedGuaranteed is not null)
                {
                    shape |= MarshallerShape.GuaranteedUnmarshal;
                }
                if (toManaged is not null)
                {
                    shape |= MarshallerShape.ToManaged;
                }
                methods = methods with
                {
                    FromUnmanaged = fromUnmanagedCandidates[0],
                    ToManaged = toManaged,
                    ToManagedGuranteed = toManagedGuaranteed
                };
            }

            IMethodSymbol free = GetStatefulFreeMethod(marshallerType);
            if (free is not null)
            {
                shape |= MarshallerShape.Free;
                methods = methods with { Free = free };
            }

            IMethodSymbol notifyInvokeSucceeded = GetNotifyInvokeSucceededMethod(marshallerType);
            if (notifyInvokeSucceeded is not null)
            {
                shape |= MarshallerShape.NotifyInvokeSucceeded;
                methods = methods with { NotifyInvokeSucceeded = notifyInvokeSucceeded };
            }

            if (GetStatelessGetPinnableReference(marshallerType, managedType) is not null)
            {
                shape |= MarshallerShape.StatelessPinnableReference;
            }
            if (GetStatefulGetPinnableReference(marshallerType) is not null)
            {
                shape |= MarshallerShape.StatefulPinnableReference;
            }

            return (shape, methods);
        }

        private static IMethodSymbol? GetFromManagedMethod(ITypeSymbol type, ITypeSymbol managedType)
        {
            return type.GetMembers(ShapeMemberNames.Value.Stateful.FromManaged)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, Parameters.Length: 1, ReturnsVoid: true }
                    && SymbolEqualityComparer.Default.Equals(managedType, m.Parameters[0].Type));
        }

        private static IMethodSymbol? GetFromManagedWithCallerAllocatedBufferMethod(
            ITypeSymbol type,
            ITypeSymbol managedType,
            ITypeSymbol spanOfT,
            out ITypeSymbol? spanElementType)
        {
            spanElementType = null;
            IEnumerable<IMethodSymbol> methods = type.GetMembers(ShapeMemberNames.Value.Stateful.FromManaged)
                .OfType<IMethodSymbol>()
                .Where(m => m is { IsStatic: false, Parameters.Length: 2, ReturnsVoid: true }
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

        private static IMethodSymbol? GetToManagedMethod(ITypeSymbol type, ITypeSymbol managedType)
        {
            return type.GetMembers(ShapeMemberNames.Value.Stateful.ToManaged)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, Parameters.Length: 0, ReturnsVoid: false, ReturnsByRef: false, ReturnsByRefReadonly: false }
                    && SymbolEqualityComparer.Default.Equals(managedType, m.ReturnType));
        }

        private static IMethodSymbol? GetToManagedGuaranteedMethod(ITypeSymbol type, ITypeSymbol managedType)
        {
            return type.GetMembers(ShapeMemberNames.Value.Stateful.ToManagedGuaranteed)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, Parameters.Length: 0, ReturnsVoid: false, ReturnsByRef: false, ReturnsByRefReadonly: false }
                    && SymbolEqualityComparer.Default.Equals(managedType, m.ReturnType));
        }

        private static IMethodSymbol? GetToUnmanagedMethod(ITypeSymbol type)
        {
            return type.GetMembers(ShapeMemberNames.Value.Stateful.ToUnmanaged)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, Parameters.Length: 0, ReturnsVoid: false, ReturnsByRef: false, ReturnsByRefReadonly: false });
        }

        private static IEnumerable<IMethodSymbol> GetFromUnmanagedMethodCandidates(ITypeSymbol type)
        {
            return type.GetMembers(ShapeMemberNames.Value.Stateful.FromUnmanaged)
                .OfType<IMethodSymbol>()
                .Where(m => m is { IsStatic: false, Parameters.Length: 1, ReturnsVoid: true });
        }

        private static IMethodSymbol? GetStatefulFreeMethod(ITypeSymbol type)
        {
            return type.GetMembers(ShapeMemberNames.Value.Stateful.Free)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, Parameters.Length: 0, ReturnsVoid: true });
        }

        private static IMethodSymbol? GetNotifyInvokeSucceededMethod(ITypeSymbol type)
        {
            return type.GetMembers(ShapeMemberNames.Value.Stateful.Free)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, Parameters.Length: 0, ReturnsVoid: true });
        }

        private static IMethodSymbol? GetStatelessGetPinnableReference(ITypeSymbol type, ITypeSymbol managedType)
        {
            return type.GetMembers(ShapeMemberNames.GetPinnableReference)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: true, Parameters.Length: 1 } and
                    ({ ReturnsByRef: true } or { ReturnsByRefReadonly: true })
                    && SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, managedType));
        }

        private static IMethodSymbol? GetStatefulGetPinnableReference(ITypeSymbol type)
        {
            return type.GetMembers(ShapeMemberNames.GetPinnableReference)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { IsStatic: false, Parameters.Length: 0 } and
                    ({ ReturnsByRef: true } or { ReturnsByRefReadonly: true }));
        }
    }
}
