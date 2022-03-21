// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using ILCompiler.Reflection.ReadyToRun;
using Internal.ReadyToRunConstants;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    struct R2RSigProviderContext
    {

    }

    class R2RSignatureTypeProvider : IR2RSignatureTypeProvider<TypeDesc, MethodDesc, R2RSigProviderContext>
    {
        public R2RSignatureTypeProvider(TraceTypeSystemContext tsc)
        {
            _tsc = tsc;
        }

        TraceTypeSystemContext _tsc;

        TypeDesc IConstructedTypeProvider<TypeDesc>.GetArrayType(TypeDesc elementType, ArrayShape shape)
        {
            if (elementType == null)
                return null;
            return elementType.MakeArrayType(shape.Rank);
        }

        TypeDesc IConstructedTypeProvider<TypeDesc>.GetByReferenceType(TypeDesc elementType)
        {
            if (elementType == null)
                return null;
            return elementType.MakeByRefType();
        }

        TypeDesc IR2RSignatureTypeProvider<TypeDesc, MethodDesc, R2RSigProviderContext>.GetCanonType()
        {
            return _tsc.CanonType;
        }

        MethodDesc IR2RSignatureTypeProvider<TypeDesc, MethodDesc, R2RSigProviderContext>.GetConstrainedMethod(MethodDesc method, TypeDesc constraint)
        {
            // Cannot exist in entrypoint definition
            throw new System.NotImplementedException();
        }

        TypeDesc ISignatureTypeProvider<TypeDesc, R2RSigProviderContext>.GetFunctionPointerType(MethodSignature<TypeDesc> signature)
        {
            // Cannot exist in entrypoint definition
            throw new System.NotImplementedException();
        }

        TypeDesc IConstructedTypeProvider<TypeDesc>.GetGenericInstantiation(TypeDesc genericType, ImmutableArray<TypeDesc> typeArguments)
        {
            if (genericType == null)
                return null;

            foreach (var type in typeArguments)
            {
                if (type == null)
                    return null;
            }
            return _tsc.GetInstantiatedType((MetadataType)genericType, new Instantiation(typeArguments.ToArray()));
        }

        TypeDesc ISignatureTypeProvider<TypeDesc, R2RSigProviderContext>.GetGenericMethodParameter(R2RSigProviderContext genericContext, int index)
        {
            // Cannot exist in entrypoint definition
            throw new System.NotImplementedException();
        }

        TypeDesc ISignatureTypeProvider<TypeDesc, R2RSigProviderContext>.GetGenericTypeParameter(R2RSigProviderContext genericContext, int index)
        {
            // Cannot exist in entrypoint definition
            throw new System.NotImplementedException();
        }

        MethodDesc IR2RSignatureTypeProvider<TypeDesc, MethodDesc, R2RSigProviderContext>.GetInstantiatedMethod(MethodDesc uninstantiatedMethod, ImmutableArray<TypeDesc> instantiation)
        {
            if (uninstantiatedMethod == null)
                return null;

            foreach (var type in instantiation)
            {
                if (type == null)
                    return null;
            }
            return uninstantiatedMethod.MakeInstantiatedMethod(instantiation.ToArray());
        }

        MethodDesc IR2RSignatureTypeProvider<TypeDesc, MethodDesc, R2RSigProviderContext>.GetMethodFromMemberRef(MetadataReader reader, MemberReferenceHandle handle, TypeDesc owningTypeOverride)
        {
            var ecmaModule = (EcmaModule)_tsc.GetModuleForSimpleName(reader.GetString(reader.GetAssemblyDefinition().Name));
            var method = (MethodDesc)ecmaModule.GetObject(handle, NotFoundBehavior.ReturnNull);
            if (method == null)
            {
                return null;
            }
            if (owningTypeOverride != null)
            {
                return _tsc.GetMethodForInstantiatedType(method.GetTypicalMethodDefinition(), (InstantiatedType)owningTypeOverride);
            }
            return method;
        }

        protected MethodDesc GetMethodFromMethodDef(MetadataReader reader, MethodDefinitionHandle handle, TypeDesc owningTypeOverride)
        {
            var ecmaModule = (EcmaModule)_tsc.GetModuleForSimpleName(reader.GetString(reader.GetAssemblyDefinition().Name));
            var method = (MethodDesc)ecmaModule.GetObject(handle, NotFoundBehavior.ReturnNull);
            if (method == null)
            {
                return null;
            }
            if (owningTypeOverride != null)
            {
                if (owningTypeOverride != method.OwningType)
                {
                    return _tsc.GetMethodForInstantiatedType(method.GetTypicalMethodDefinition(), (InstantiatedType)owningTypeOverride);
                }
            }
            return method;
        }

        MethodDesc IR2RSignatureTypeProvider<TypeDesc, MethodDesc, R2RSigProviderContext>.GetMethodFromMethodDef(MetadataReader reader, MethodDefinitionHandle handle, TypeDesc owningTypeOverride)
        {
            return GetMethodFromMethodDef(reader, handle, owningTypeOverride);
        }

        MethodDesc IR2RSignatureTypeProvider<TypeDesc, MethodDesc, R2RSigProviderContext>.GetMethodWithFlags(ReadyToRunMethodSigFlags flags, MethodDesc method)
        {
            return method;
        }

        TypeDesc ISignatureTypeProvider<TypeDesc, R2RSigProviderContext>.GetModifiedType(TypeDesc modifier, TypeDesc unmodifiedType, bool isRequired)
        {
            // Cannot exist in entrypoint definition
            throw new System.NotImplementedException();
        }

        TypeDesc ISignatureTypeProvider<TypeDesc, R2RSigProviderContext>.GetPinnedType(TypeDesc elementType)
        {
            // Cannot exist in entrypoint definition
            throw new System.NotImplementedException();
        }

        TypeDesc IConstructedTypeProvider<TypeDesc>.GetPointerType(TypeDesc elementType)
        {
            // Cannot exist in entrypoint definition
            throw new System.NotImplementedException();
        }

        TypeDesc ISimpleTypeProvider<TypeDesc>.GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            WellKnownType wkt = 0;
            switch (typeCode)
            {
                case PrimitiveTypeCode.Void:
                    wkt = WellKnownType.Void;
                    break;
                case PrimitiveTypeCode.Boolean:
                    wkt = WellKnownType.Boolean;
                    break;
                case PrimitiveTypeCode.Char:
                    wkt = WellKnownType.Char;
                    break;
                case PrimitiveTypeCode.SByte:
                    wkt = WellKnownType.SByte;
                    break;
                case PrimitiveTypeCode.Byte:
                    wkt = WellKnownType.Byte;
                    break;
                case PrimitiveTypeCode.Int16:
                    wkt = WellKnownType.Int16;
                    break;
                case PrimitiveTypeCode.UInt16:
                    wkt = WellKnownType.UInt16;
                    break;
                case PrimitiveTypeCode.Int32:
                    wkt = WellKnownType.Int32;
                    break;
                case PrimitiveTypeCode.UInt32:
                    wkt = WellKnownType.UInt32;
                    break;
                case PrimitiveTypeCode.Int64:
                    wkt = WellKnownType.Int64;
                    break;
                case PrimitiveTypeCode.UInt64:
                    wkt = WellKnownType.UInt64;
                    break;
                case PrimitiveTypeCode.Single:
                    wkt = WellKnownType.Single;
                    break;
                case PrimitiveTypeCode.Double:
                    wkt = WellKnownType.Double;
                    break;
                case PrimitiveTypeCode.String:
                    wkt = WellKnownType.String;
                    break;
                case PrimitiveTypeCode.TypedReference:
                    wkt = WellKnownType.TypedReference;
                    break;
                case PrimitiveTypeCode.IntPtr:
                    wkt = WellKnownType.IntPtr;
                    break;
                case PrimitiveTypeCode.UIntPtr:
                    wkt = WellKnownType.UIntPtr;
                    break;
                case PrimitiveTypeCode.Object:
                    wkt = WellKnownType.Object;
                    break;
            }

            return _tsc.GetWellKnownType(wkt);
        }

        TypeDesc ISZArrayTypeProvider<TypeDesc>.GetSZArrayType(TypeDesc elementType)
        {
            if (elementType == null)
                return null;

            return elementType.MakeArrayType();
        }

        TypeDesc ISimpleTypeProvider<TypeDesc>.GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            var ecmaModule = (EcmaModule)_tsc.GetModuleForSimpleName(reader.GetString(reader.GetAssemblyDefinition().Name));
            return (TypeDesc)ecmaModule.GetObject(handle, NotFoundBehavior.ReturnNull);
        }

        TypeDesc ISimpleTypeProvider<TypeDesc>.GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var ecmaModule = (EcmaModule)_tsc.GetModuleForSimpleName(reader.GetString(reader.GetAssemblyDefinition().Name));
            return (TypeDesc)ecmaModule.GetObject(handle, NotFoundBehavior.ReturnNull);
        }

        TypeDesc ISignatureTypeProvider<TypeDesc, R2RSigProviderContext>.GetTypeFromSpecification(MetadataReader reader, R2RSigProviderContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            var ecmaModule = (EcmaModule)_tsc.GetModuleForSimpleName(reader.GetString(reader.GetAssemblyDefinition().Name));
            return (TypeDesc)ecmaModule.GetObject(handle, NotFoundBehavior.ReturnNull);
        }
    }

    class R2RSignatureTypeProviderForGlobalTables : R2RSignatureTypeProvider, IR2RSignatureTypeProvider<TypeDesc, MethodDesc, R2RSigProviderContext>
    {
        public R2RSignatureTypeProviderForGlobalTables(TraceTypeSystemContext tsc) : base(tsc)
        {
        }

        MethodDesc IR2RSignatureTypeProvider<TypeDesc, MethodDesc, R2RSigProviderContext>.GetMethodFromMethodDef(MetadataReader reader, MethodDefinitionHandle handle, TypeDesc owningTypeOverride)
        {
            if (owningTypeOverride != null)
            {
                reader = ((EcmaModule)((MetadataType)owningTypeOverride.GetTypeDefinition()).Module).MetadataReader;
            }
            Debug.Assert(reader != null);
            return GetMethodFromMethodDef(reader, handle, owningTypeOverride);
        }

        MethodDesc IR2RSignatureTypeProvider<TypeDesc, MethodDesc, R2RSigProviderContext>.GetMethodFromMemberRef(MetadataReader reader, MemberReferenceHandle handle, TypeDesc owningTypeOverride)
        {
            // Global signature cannot have MemberRef entries in them as such things aren't uniquely identifiable
            throw new NotSupportedException();
        }
    }
}
