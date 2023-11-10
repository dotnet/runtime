// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    [Flags]
    public enum EnvironmentFlags
    {
        None = 0,
        SkipLocalsInit = 0x1,
        DisableRuntimeMarshalling = 0x2,
    }

    public sealed record StubEnvironment(
        Compilation Compilation,
        EnvironmentFlags EnvironmentFlags)
    {
        private Optional<INamedTypeSymbol?> _lcidConversionAttrType;
        public INamedTypeSymbol? LcidConversionAttrType
        {
            get
            {
                if (_lcidConversionAttrType.HasValue)
                {
                    return _lcidConversionAttrType.Value;
                }
                _lcidConversionAttrType = new Optional<INamedTypeSymbol?>(Compilation.GetTypeByMetadataName(TypeNames.LCIDConversionAttribute));
                return _lcidConversionAttrType.Value;
            }
        }

        private Optional<INamedTypeSymbol?> _suppressGCTransitionAttrType;
        public INamedTypeSymbol? SuppressGCTransitionAttrType
        {
            get
            {
                if (_suppressGCTransitionAttrType.HasValue)
                {
                    return _suppressGCTransitionAttrType.Value;
                }
                _suppressGCTransitionAttrType = new Optional<INamedTypeSymbol?>(Compilation.GetTypeByMetadataName(TypeNames.SuppressGCTransitionAttribute));
                return _suppressGCTransitionAttrType.Value;
            }
        }

        private Optional<INamedTypeSymbol?> _unmanagedCallConvAttrType;
        public INamedTypeSymbol? UnmanagedCallConvAttrType
        {
            get
            {
                if (_unmanagedCallConvAttrType.HasValue)
                {
                    return _unmanagedCallConvAttrType.Value;
                }
                _unmanagedCallConvAttrType = new Optional<INamedTypeSymbol?>(Compilation.GetTypeByMetadataName(TypeNames.UnmanagedCallConvAttribute));
                return _unmanagedCallConvAttrType.Value;
            }
        }

        private Optional<INamedTypeSymbol?> _defaultDllImportSearchPathsAttrType;
        public INamedTypeSymbol? DefaultDllImportSearchPathsAttrType
        {
            get
            {
                if (_defaultDllImportSearchPathsAttrType.HasValue)
                {
                    return _defaultDllImportSearchPathsAttrType.Value;
                }
                _defaultDllImportSearchPathsAttrType = new Optional<INamedTypeSymbol?>(Compilation.GetTypeByMetadataName(TypeNames.DefaultDllImportSearchPathsAttribute));
                return _defaultDllImportSearchPathsAttrType.Value;
            }
        }
    }
}
