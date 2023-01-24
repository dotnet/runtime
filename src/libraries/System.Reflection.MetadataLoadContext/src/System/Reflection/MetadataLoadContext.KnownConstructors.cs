// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.TypeLoading;

namespace System.Reflection
{
    // Latch known constructors of pseudo-CustomAttribute types.
    public sealed partial class MetadataLoadContext
    {
        internal ConstructorInfo? TryGetFieldOffsetCtor() => _lazyFieldOffset ??= TryGetConstructor(CoreType.FieldOffsetAttribute, CoreType.Int32);
        private volatile ConstructorInfo? _lazyFieldOffset;

        internal ConstructorInfo? TryGetInCtor() => _lazyIn ??= TryGetConstructor(CoreType.InAttribute);
        private volatile ConstructorInfo? _lazyIn;

        internal ConstructorInfo? TryGetOutCtor() => _lazyOut ??= TryGetConstructor(CoreType.OutAttribute);
        private volatile ConstructorInfo? _lazyOut;

        internal ConstructorInfo? TryGetOptionalCtor() => _lazyOptional ??= TryGetConstructor(CoreType.OptionalAttribute);
        private volatile ConstructorInfo? _lazyOptional;

        internal ConstructorInfo? TryGetPreserveSigCtor() => _lazyPreserveSig ??= TryGetConstructor(CoreType.PreserveSigAttribute);
        private volatile ConstructorInfo? _lazyPreserveSig;

        internal ConstructorInfo? TryGetComImportCtor() => _lazyComImport ??= TryGetConstructor(CoreType.ComImportAttribute);
        private volatile ConstructorInfo? _lazyComImport;

        internal ConstructorInfo? TryGetDllImportCtor() => _lazyDllImport ??= TryGetConstructor(CoreType.DllImportAttribute, CoreType.String);
        private volatile ConstructorInfo? _lazyDllImport;

        internal ConstructorInfo? TryGetMarshalAsCtor() => _lazyMarshalAs ??= TryGetConstructor(CoreType.MarshalAsAttribute, CoreType.UnmanagedType);
        private volatile ConstructorInfo? _lazyMarshalAs;

        private ConstructorInfo? TryGetConstructor(CoreType attributeCoreType, params CoreType[] parameterCoreTypes)
        {
            int count = parameterCoreTypes.Length;
            Type? attributeType = TryGetCoreType(attributeCoreType);
            if (attributeType == null)
                return null;
            Type?[] parameterTypes = new Type[count];
            for (int i = 0; i < count; i++)
            {
                if ((parameterTypes[i] = TryGetCoreType(parameterCoreTypes[i])) == null)
                    return null;
            }

            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.ExactBinding;
            return attributeType.GetConstructor(bf, null, parameterTypes!, null);
        }
    }
}
