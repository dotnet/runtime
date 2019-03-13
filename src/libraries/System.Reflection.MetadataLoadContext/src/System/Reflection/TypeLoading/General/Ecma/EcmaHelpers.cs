// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    internal static class EcmaHelpers
    {
        /// <summary>
        /// Returns a RoAssemblyName corresponding to the assembly reference.
        /// </summary>
        public static RoAssemblyName ToRoAssemblyName(this AssemblyReferenceHandle h, MetadataReader reader)
        {
            AssemblyReference a = h.GetAssemblyReference(reader);
            string name = a.Name.GetString(reader);
            Version version = a.Version.AdjustForUnspecifiedVersionComponents();
            string culture = a.Culture.GetStringOrNull(reader);
            byte[] pkOrPkt = a.PublicKeyOrToken.GetBlobBytes(reader);
            AssemblyFlags flags = a.Flags;
            if ((flags & AssemblyFlags.PublicKey) != 0)
            {
                pkOrPkt = pkOrPkt.ComputePublicKeyToken();
            }

            return new RoAssemblyName(name, version, culture, pkOrPkt);
        }

        public static CoreType ToCoreType(this PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Boolean: return CoreType.Boolean;
                case PrimitiveTypeCode.Byte: return CoreType.Byte;
                case PrimitiveTypeCode.Char: return CoreType.Char;
                case PrimitiveTypeCode.Double: return CoreType.Double;
                case PrimitiveTypeCode.Int16: return CoreType.Int16;
                case PrimitiveTypeCode.Int32: return CoreType.Int32;
                case PrimitiveTypeCode.Int64: return CoreType.Int64;
                case PrimitiveTypeCode.IntPtr: return CoreType.IntPtr;
                case PrimitiveTypeCode.Object: return CoreType.Object;
                case PrimitiveTypeCode.SByte: return CoreType.SByte;
                case PrimitiveTypeCode.Single: return CoreType.Single;
                case PrimitiveTypeCode.String: return CoreType.String;
                case PrimitiveTypeCode.TypedReference: return CoreType.TypedReference;
                case PrimitiveTypeCode.UInt16: return CoreType.UInt16;
                case PrimitiveTypeCode.UInt32: return CoreType.UInt32;
                case PrimitiveTypeCode.UInt64: return CoreType.UInt64;
                case PrimitiveTypeCode.UIntPtr: return CoreType.UIntPtr;
                case PrimitiveTypeCode.Void: return CoreType.Void;
                default:
                    Debug.Fail("Unexpected PrimitiveTypeCode: " + typeCode);
                    return CoreType.Void;
            }
        }

        public static PrimitiveTypeCode GetEnumUnderlyingPrimitiveTypeCode(this Type enumType, MetadataLoadContext loader)
        {
            Type type = enumType.GetEnumUnderlyingType();
            CoreTypes coreTypes = loader.GetAllFoundCoreTypes();
            // Be careful how you compare - one or more elements of "coreTypes" can be null!
            if (type == coreTypes[CoreType.Boolean]) return PrimitiveTypeCode.Boolean;
            if (type == coreTypes[CoreType.Char]) return PrimitiveTypeCode.Char;
            if (type == coreTypes[CoreType.Byte]) return PrimitiveTypeCode.Byte;
            if (type == coreTypes[CoreType.Int16]) return PrimitiveTypeCode.Int16;
            if (type == coreTypes[CoreType.Int32]) return PrimitiveTypeCode.Int32;
            if (type == coreTypes[CoreType.Int64]) return PrimitiveTypeCode.Int64;
            if (type == coreTypes[CoreType.IntPtr]) return PrimitiveTypeCode.IntPtr;
            if (type == coreTypes[CoreType.SByte]) return PrimitiveTypeCode.SByte;
            if (type == coreTypes[CoreType.UInt16]) return PrimitiveTypeCode.UInt16;
            if (type == coreTypes[CoreType.UInt32]) return PrimitiveTypeCode.UInt32;
            if (type == coreTypes[CoreType.UInt64]) return PrimitiveTypeCode.UInt64;
            if (type == coreTypes[CoreType.UIntPtr]) return PrimitiveTypeCode.UIntPtr;

            throw new BadImageFormatException(SR.Format(SR.UnexpectedUnderlyingEnumType, enumType, type));
        }

        // Okay, read this closely.
        //
        //   System.Reflection.AssemblyHashAlgorithm is an enum defined by System.Reflection.Metadata.dll.
        //
        //   System.Configuration.Assemblies.AssemblyHashAlgorithm is an identical enum defined
        //     by System.Runtime.dll.
        //
        // The values line up exactly so it's safe to cast from one to the other but we'll encapsulate that 
        // observation here rather stick casts (and this painfully awkward pair of colliding type names) around.
        //
        public static System.Configuration.Assemblies.AssemblyHashAlgorithm ToConfigurationAssemblyHashAlgorithm(this System.Reflection.AssemblyHashAlgorithm srmHash)
        {
            return (System.Configuration.Assemblies.AssemblyHashAlgorithm)srmHash;
        }

        // Another case of System.Reflection and System.Reflection.Metadata defining the exact same enum.
        public static ExceptionHandlingClauseOptions ToExceptionHandlingClauseOptions(this ExceptionRegionKind kind) => (ExceptionHandlingClauseOptions)kind;

        // Yet another case of System.Reflection and System.Reflection.Metadata defining the exact same enum.
        public static AssemblyNameFlags ToAssemblyNameFlags(this AssemblyFlags flags) => (AssemblyNameFlags)flags;

        public static bool IsConstructor(this in MethodDefinition method, MetadataReader reader)
        {
            if ((method.Attributes & (MethodAttributes.RTSpecialName | MethodAttributes.SpecialName)) != (MethodAttributes.RTSpecialName | MethodAttributes.SpecialName))
                return false;

            MetadataStringComparer stringComparer = reader.StringComparer;

            StringHandle nameHandle = method.Name;
            return stringComparer.Equals(nameHandle, ConstructorInfo.ConstructorName) || stringComparer.Equals(nameHandle, ConstructorInfo.TypeConstructorName);
        }

        public static unsafe ReadOnlySpan<byte> AsReadOnlySpan(this StringHandle handle, MetadataReader reader)
        {
            BlobReader br = handle.GetBlobReader(reader);
            return new ReadOnlySpan<byte>(br.CurrentPointer, br.Length);
        }

        public static RoMethod ToMethodOrNull(this MethodDefinitionHandle handle, RoInstantiationProviderType declaringType, Type reflectedType)
        {
            if (handle.IsNil)
                return null;

            return handle.ToMethod(declaringType, reflectedType);
        }

        public static RoMethod ToMethod(this MethodDefinitionHandle handle, RoInstantiationProviderType declaringType, Type reflectedType)
        {
            return new RoDefinitionMethod<EcmaMethodDecoder>(declaringType, reflectedType, new EcmaMethodDecoder(handle, (EcmaModule)(declaringType.Module)));
        }
    }
}
