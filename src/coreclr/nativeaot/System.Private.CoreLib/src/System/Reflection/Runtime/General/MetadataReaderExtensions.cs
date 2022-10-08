// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;

using Debug = System.Diagnostics.Debug;
using AssemblyFlags = Internal.Metadata.NativeFormat.AssemblyFlags;

namespace System.Reflection.Runtime.General
{
    [ReflectionBlocked]
    [CLSCompliant(false)]
    public static partial class MetadataReaderExtensions
    {
        /// <summary>
        /// Convert raw token to a typed metadata handle.
        /// </summary>
        /// <param name="token">Token - raw integral handle representation</param>
        /// <returns>Token converted to handle</returns>
        public static unsafe Handle AsHandle(this int token)
        {
            return *(Handle*)&token;
        }

        /// <summary>
        /// Convert raw token to a typed metadata handle.
        /// </summary>
        /// <param name="token">Token - raw integral handle representation</param>
        /// <returns>Token converted to handle</returns>
        public static unsafe Handle AsHandle(this uint token)
        {
            return *(Handle*)&token;
        }

        /// <summary>
        /// Convert typed metadata handle to the raw token value.
        /// </summary>
        /// <param name="handle">Typed metadata handle</param>
        /// <returns>Token - raw integral handle represented as signed int</returns>
        public static unsafe int AsInt(this Handle handle)
        {
            return *(int*)&handle;
        }

        /// <summary>
        /// Convert typed metadata handle to the raw token value.
        /// </summary>
        /// <param name="handle">Typed metadata handle</param>
        /// <returns>Token - raw integral handle represented as unsigned int</returns>
        public static unsafe uint AsUInt(this Handle handle)
        {
            return *(uint*)&handle;
        }

        public static string GetString(this ConstantStringValueHandle handle, MetadataReader reader)
        {
            return reader.GetConstantStringValue(handle).Value;
        }

        // Useful for namespace Name string which can be a null handle.
        public static string GetStringOrNull(this ConstantStringValueHandle handle, MetadataReader reader)
        {
            if (reader.IsNull(handle))
                return null;
            return reader.GetConstantStringValue(handle).Value;
        }

        public static bool IsTypeDefRefOrSpecHandle(this Handle handle, MetadataReader reader)
        {
            HandleType handleType = handle.HandleType;
            return handleType == HandleType.TypeDefinition ||
                handleType == HandleType.TypeReference ||
                handleType == HandleType.TypeSpecification;
        }

        public static bool IsTypeDefRefSpecOrModifiedTypeHandle(this Handle handle, MetadataReader reader)
        {
            HandleType handleType = handle.HandleType;
            return handleType == HandleType.TypeDefinition ||
                handleType == HandleType.TypeReference ||
                handleType == HandleType.TypeSpecification ||
                handleType == HandleType.ModifiedType;
        }

        public static RuntimeAssemblyName ToRuntimeAssemblyName(this ScopeDefinitionHandle scopeDefinitionHandle, MetadataReader reader)
        {
            ScopeDefinition scopeDefinition = scopeDefinitionHandle.GetScopeDefinition(reader);
            return CreateRuntimeAssemblyNameFromMetadata(
                reader,
                scopeDefinition.Name,
                scopeDefinition.MajorVersion,
                scopeDefinition.MinorVersion,
                scopeDefinition.BuildNumber,
                scopeDefinition.RevisionNumber,
                scopeDefinition.Culture,
                scopeDefinition.PublicKey,
                scopeDefinition.Flags
                );
        }

        public static RuntimeAssemblyName ToRuntimeAssemblyName(this ScopeReferenceHandle scopeReferenceHandle, MetadataReader reader)
        {
            ScopeReference scopeReference = scopeReferenceHandle.GetScopeReference(reader);
            return CreateRuntimeAssemblyNameFromMetadata(
                reader,
                scopeReference.Name,
                scopeReference.MajorVersion,
                scopeReference.MinorVersion,
                scopeReference.BuildNumber,
                scopeReference.RevisionNumber,
                scopeReference.Culture,
                scopeReference.PublicKeyOrToken,
                scopeReference.Flags
                );
        }

        private static RuntimeAssemblyName CreateRuntimeAssemblyNameFromMetadata(
            MetadataReader reader,
            ConstantStringValueHandle name,
            ushort majorVersion,
            ushort minorVersion,
            ushort buildNumber,
            ushort revisionNumber,
            ConstantStringValueHandle culture,
            ByteCollection publicKeyOrToken,
            global::Internal.Metadata.NativeFormat.AssemblyFlags assemblyFlags)
        {
            AssemblyNameFlags assemblyNameFlags = AssemblyNameFlags.None;
            if (0 != (assemblyFlags & global::Internal.Metadata.NativeFormat.AssemblyFlags.PublicKey))
                assemblyNameFlags |= AssemblyNameFlags.PublicKey;
            if (0 != (assemblyFlags & global::Internal.Metadata.NativeFormat.AssemblyFlags.Retargetable))
                assemblyNameFlags |= AssemblyNameFlags.Retargetable;
            int contentType = ((int)assemblyFlags) & 0x00000E00;
            assemblyNameFlags |= (AssemblyNameFlags)contentType;

            ArrayBuilder<byte> keyOrTokenArrayBuilder = new ArrayBuilder<byte>();
            foreach (byte b in publicKeyOrToken)
                keyOrTokenArrayBuilder.Add(b);

            return new RuntimeAssemblyName(
                name.GetString(reader),
                new Version(majorVersion, minorVersion, buildNumber, revisionNumber),
                culture.GetStringOrNull(reader),
                assemblyNameFlags,
                keyOrTokenArrayBuilder.ToArray()
                );
        }
    }
}
