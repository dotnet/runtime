// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.JitInterface;
using Internal.CorConstants;
using Internal.ReadyToRunConstants;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public abstract class SignatureBuilder
    {
        public abstract void EmitByte(byte data);

        public void EmitBytes(byte[] data)
        {
            foreach (byte b in data)
            {
                EmitByte(b);
            }
        }

        public void EmitUInt(uint data)
        {
            if (data <= 0x7F)
            {
                EmitByte((byte)data);
                return;
            }

            if (data <= 0x3FFF)
            {
                EmitByte((byte)((data >> 8) | 0x80));
                EmitByte((byte)(data & 0xFF));
                return;
            }

            if (data <= 0x1FFFFFFF)
            {
                EmitByte((byte)((data >> 24) | 0xC0));
                EmitByte((byte)((data >> 16) & 0xff));
                EmitByte((byte)((data >> 8) & 0xff));
                EmitByte((byte)(data & 0xff));
                return;
            }

            throw new NotImplementedException();
        }

        public static uint RidFromToken(mdToken token)
        {
            return unchecked((uint)token) & 0x00FFFFFFu;
        }

        public static CorTokenType TypeFromToken(int token)
        {
            return (CorTokenType)(unchecked((uint)token) & 0xFF000000u);
        }

        public static CorTokenType TypeFromToken(mdToken token)
        {
            return TypeFromToken((int)token);
        }

        public void EmitTokenRid(mdToken token)
        {
            EmitUInt((uint)RidFromToken(token));
        }

        // compress a token
        // The least significant bit of the first compress byte will indicate the token type.
        //
        public void EmitToken(mdToken token)
        {
            uint rid = RidFromToken(token);
            CorTokenType type = (CorTokenType)TypeFromToken(token);

            if (rid > 0x3FFFFFF)
            {
                // token is too big to be compressed
                throw new NotImplementedException();
            }

            rid = (rid << 2);

            // TypeDef is encoded with low bits 00
            // TypeRef is encoded with low bits 01
            // TypeSpec is encoded with low bits 10
            // BaseType is encoded with low bit 11
            switch (type)
            {
                case CorTokenType.mdtTypeDef:
                    break;

                case CorTokenType.mdtTypeRef:
                    // make the last two bits 01
                    rid |= 0x1;
                    break;

                case CorTokenType.mdtTypeSpec:
                    // make last two bits 0
                    rid |= 0x2;
                    break;

                case CorTokenType.mdtBaseType:
                    rid |= 0x3;
                    break;

                default:
                    throw new NotImplementedException();
            }

            EmitUInt(rid);
        }

        private static class SignMask
        {
            public const uint ONEBYTE = 0xffffffc0; // Mask the same size as the missing bits.
            public const uint TWOBYTE = 0xffffe000; // Mask the same size as the missing bits.
            public const uint FOURBYTE = 0xf0000000; // Mask the same size as the missing bits.
        }

        /// <summary>
        /// Compress a signed integer. The least significant bit of the first compressed byte will be the sign bit.
        /// </summary>
        public void EmitInt(int data)
        {
            uint isSigned = (data < 0 ? 1u : 0u);
            uint udata = unchecked((uint)data);

            // Note that we cannot use CompressData to pack the data value, because of negative values
            // like: 0xffffe000 (-8192) which has to be encoded as 1 in 2 bytes, i.e. 0x81 0x00
            // However CompressData would store value 1 as 1 byte: 0x01
            if ((udata & SignMask.ONEBYTE) == 0 || (udata & SignMask.ONEBYTE) == SignMask.ONEBYTE)
            {
                udata = ((udata & ~SignMask.ONEBYTE) << 1 | isSigned);
                Debug.Assert(udata <= 0x7f);
                EmitByte((byte)udata);
                return;
            }

            if ((udata & SignMask.TWOBYTE) == 0 || (udata & SignMask.TWOBYTE) == SignMask.TWOBYTE)
            {
                udata = ((udata & ~SignMask.TWOBYTE) << 1 | isSigned);
                Debug.Assert(udata <= 0x3fff);
                EmitByte((byte)((udata >> 8) | 0x80));
                EmitByte((byte)(udata & 0xff));
                return;
            }

            if ((udata & SignMask.FOURBYTE) == 0 || (udata & SignMask.FOURBYTE) == SignMask.FOURBYTE)
            {
                udata = ((udata & ~SignMask.FOURBYTE) << 1 | isSigned);
                Debug.Assert(udata <= 0x1FFFFFFF);
                EmitByte((byte)((udata >> 24) | 0xC0));
                EmitByte((byte)((udata >> 16) & 0xff));
                EmitByte((byte)((udata >> 8) & 0xff));
                EmitByte((byte)(udata & 0xff));
                return;
            }

            // Out of compressible range
            throw new NotImplementedException();
        }

        /// <summary>
        /// Compress a CorElementType into a single byte.
        /// </summary>
        /// <param name="elementType">COR element type to compress</param>
        internal void EmitElementType(CorElementType elementType)
        {
            EmitByte((byte)elementType);
        }

        public void EmitTypeSignature(TypeDesc typeDesc, SignatureContext context)
        {
            if (typeDesc is RuntimeDeterminedType runtimeDeterminedType)
            {
                switch (runtimeDeterminedType.RuntimeDeterminedDetailsType.Kind)
                {
                    case GenericParameterKind.Type:
                        EmitElementType(CorElementType.ELEMENT_TYPE_VAR);
                        break;

                    case GenericParameterKind.Method:
                        EmitElementType(CorElementType.ELEMENT_TYPE_MVAR);
                        break;

                    default:
                        throw new NotImplementedException();
                }
                EmitUInt((uint)runtimeDeterminedType.RuntimeDeterminedDetailsType.Index);
                return;
            }

            if (typeDesc.HasInstantiation && !typeDesc.IsGenericDefinition)
            {
                EmitInstantiatedTypeSignature((InstantiatedType)typeDesc, context);
                return;
            }

            switch (typeDesc.Category)
            {
                case TypeFlags.Array:
                    EmitArrayTypeSignature((ArrayType)typeDesc, context);
                    return;

                case TypeFlags.SzArray:
                    EmitSzArrayTypeSignature((ArrayType)typeDesc, context);
                    return;

                case TypeFlags.Pointer:
                    EmitPointerTypeSignature((PointerType)typeDesc, context);
                    return;

                case TypeFlags.FunctionPointer:
                    EmitFunctionPointerTypeSignature((FunctionPointerType)typeDesc, context);
                    return;

                case TypeFlags.ByRef:
                    EmitByRefTypeSignature((ByRefType)typeDesc, context);
                    break;

                case TypeFlags.Void:
                    EmitElementType(CorElementType.ELEMENT_TYPE_VOID);
                    return;

                case TypeFlags.Boolean:
                    EmitElementType(CorElementType.ELEMENT_TYPE_BOOLEAN);
                    return;

                case TypeFlags.Char:
                    EmitElementType(CorElementType.ELEMENT_TYPE_CHAR);
                    return;

                case TypeFlags.SByte:
                    EmitElementType(CorElementType.ELEMENT_TYPE_I1);
                    return;

                case TypeFlags.Byte:
                    EmitElementType(CorElementType.ELEMENT_TYPE_U1);
                    return;

                case TypeFlags.Int16:
                    EmitElementType(CorElementType.ELEMENT_TYPE_I2);
                    return;

                case TypeFlags.UInt16:
                    EmitElementType(CorElementType.ELEMENT_TYPE_U2);
                    return;

                case TypeFlags.Int32:
                    EmitElementType(CorElementType.ELEMENT_TYPE_I4);
                    return;

                case TypeFlags.UInt32:
                    EmitElementType(CorElementType.ELEMENT_TYPE_U4);
                    return;

                case TypeFlags.Int64:
                    EmitElementType(CorElementType.ELEMENT_TYPE_I8);
                    return;

                case TypeFlags.UInt64:
                    EmitElementType(CorElementType.ELEMENT_TYPE_U8);
                    return;

                case TypeFlags.IntPtr:
                    EmitElementType(CorElementType.ELEMENT_TYPE_I);
                    return;

                case TypeFlags.UIntPtr:
                    EmitElementType(CorElementType.ELEMENT_TYPE_U);
                    return;

                case TypeFlags.Single:
                    EmitElementType(CorElementType.ELEMENT_TYPE_R4);
                    return;

                case TypeFlags.Double:
                    EmitElementType(CorElementType.ELEMENT_TYPE_R8);
                    return;

                case TypeFlags.Interface:
                case TypeFlags.Class:
                    if (typeDesc.IsString)
                    {
                        EmitElementType(CorElementType.ELEMENT_TYPE_STRING);
                    }
                    else if (typeDesc.IsObject)
                    {
                        EmitElementType(CorElementType.ELEMENT_TYPE_OBJECT);
                    }
                    else if (typeDesc.IsCanonicalDefinitionType(CanonicalFormKind.Specific))
                    {
                        EmitElementType(CorElementType.ELEMENT_TYPE_CANON_ZAPSIG);
                    }
                    else
                    {
                        ModuleToken token = context.GetModuleTokenForType((EcmaType)typeDesc);
                        EmitModuleOverride(token.Module, context);
                        EmitElementType(CorElementType.ELEMENT_TYPE_CLASS);
                        EmitToken(token.Token);
                    }
                    return;

                case TypeFlags.ValueType:
                case TypeFlags.Nullable:
                case TypeFlags.Enum:
                    if (typeDesc.IsWellKnownType(WellKnownType.TypedReference))
                    {
                        EmitElementType(CorElementType.ELEMENT_TYPE_TYPEDBYREF);
                        return;
                    }

                    {
                        ModuleToken token = context.GetModuleTokenForType((EcmaType)typeDesc);
                        EmitModuleOverride(token.Module, context);
                        EmitElementType(CorElementType.ELEMENT_TYPE_VALUETYPE);
                        EmitToken(token.Token);
                        return;
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        private void EmitModuleOverride(IEcmaModule module, SignatureContext context)
        {
            if (module != context.LocalContext)
            {
                EmitElementType(CorElementType.ELEMENT_TYPE_MODULE_ZAPSIG);
                uint moduleIndex = (uint)context.Resolver.GetModuleIndex(module);
                EmitUInt(moduleIndex);
            }
        }

        private void EmitTypeToken(EcmaType type, SignatureContext context)
        {
            ModuleToken token = context.GetModuleTokenForType(type);
            EmitToken(token.Token);
        }

        private void EmitInstantiatedTypeSignature(InstantiatedType type, SignatureContext context)
        {
            IEcmaModule targetModule = context.GetTargetModule(type);
            EmitModuleOverride(targetModule, context);
            EmitElementType(CorElementType.ELEMENT_TYPE_GENERICINST);
            EmitTypeSignature(type.GetTypeDefinition(), context.InnerContext(targetModule));
            SignatureContext outerContext = context.OuterContext;
            EmitUInt((uint)type.Instantiation.Length);
            for (int paramIndex = 0; paramIndex < type.Instantiation.Length; paramIndex++)
            {
                EmitTypeSignature(type.Instantiation[paramIndex], outerContext);
            }
        }

        private void EmitPointerTypeSignature(PointerType type, SignatureContext context)
        {
            EmitElementType(CorElementType.ELEMENT_TYPE_PTR);
            EmitTypeSignature(type.ParameterType, context);
        }

        private void EmitFunctionPointerTypeSignature(FunctionPointerType type, SignatureContext context)
        {
            SignatureCallingConvention callingConvention = (SignatureCallingConvention)(type.Signature.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask);
            SignatureAttributes callingConventionAttributes = ((type.Signature.Flags & MethodSignatureFlags.Static) != 0 ? SignatureAttributes.None : SignatureAttributes.Instance);

            EmitElementType(CorElementType.ELEMENT_TYPE_FNPTR);
            EmitUInt((uint)((byte)callingConvention | (byte)callingConventionAttributes));
            EmitUInt((uint)type.Signature.Length);

            EmitTypeSignature(type.Signature.ReturnType, context);
            for (int argIndex = 0; argIndex < type.Signature.Length; argIndex++)
            {
                EmitTypeSignature(type.Signature[argIndex], context);
            }
        }

        private void EmitByRefTypeSignature(ByRefType type, SignatureContext context)
        {
            EmitElementType(CorElementType.ELEMENT_TYPE_BYREF);
            EmitTypeSignature(type.ParameterType, context);
        }

        private void EmitSzArrayTypeSignature(ArrayType type, SignatureContext context)
        {
            Debug.Assert(type.IsSzArray);
            EmitElementType(CorElementType.ELEMENT_TYPE_SZARRAY);
            EmitTypeSignature(type.ElementType, context);
        }

        private void EmitArrayTypeSignature(ArrayType type, SignatureContext context)
        {
            Debug.Assert(type.IsArray && !type.IsSzArray);
            EmitElementType(CorElementType.ELEMENT_TYPE_ARRAY);
            EmitTypeSignature(type.ElementType, context);
            EmitUInt((uint)type.Rank);
            if (type.Rank != 0)
            {
                EmitUInt(0); // Number of sizes
                EmitUInt(0); // Number of lower bounds
            }
        }

        public void EmitMethodSignature(
            MethodWithToken method,
            bool enforceDefEncoding,
            bool enforceOwningType,
            SignatureContext context,
            bool isInstantiatingStub)
        {
            uint flags = 0;
            if (method.Unboxing)
            {
                flags |= (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_UnboxingStub;
            }
            if (isInstantiatingStub)
            {
                flags |= (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_InstantiatingStub;
            }
            if (method.ConstrainedType != null)
            {
                flags |= (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_Constrained;
            }
            if (enforceOwningType || method.OwningTypeNotDerivedFromToken)
            {
                flags |= (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_OwnerType;
            }

            EmitMethodSpecificationSignature(method, flags, enforceDefEncoding, enforceOwningType, context);

            if (method.ConstrainedType != null)
            {
                EmitTypeSignature(method.ConstrainedType, context);
            }
        }

        public void EmitMethodDefToken(ModuleToken methodDefToken)
        {
            Debug.Assert(methodDefToken.TokenType == CorTokenType.mdtMethodDef);
            EmitUInt(methodDefToken.TokenRid);
        }

        public void EmitMethodRefToken(ModuleToken memberRefToken)
        {
            Debug.Assert(memberRefToken.TokenType == CorTokenType.mdtMemberRef);
            EmitUInt(RidFromToken(memberRefToken.Token));
        }

        private void EmitMethodSpecificationSignature(MethodWithToken method,
            uint flags, bool enforceDefEncoding, bool enforceOwningType, SignatureContext context)
        {
            ModuleToken methodToken = method.Token;

            if (method.Method.HasInstantiation && !method.Method.IsGenericMethodDefinition)
            {
                flags |= (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_MethodInstantiation;
                if (!method.Token.IsNull)
                {
                    if (method.Token.TokenType == CorTokenType.mdtMethodSpec)
                    {
                        MethodSpecification methodSpecification = methodToken.MetadataReader.GetMethodSpecification((MethodSpecificationHandle)methodToken.Handle);
                        methodToken = new ModuleToken(methodToken.Module, methodSpecification.Method);
                    }
                }
            }

            Debug.Assert(!methodToken.IsNull);

            switch (methodToken.TokenType)
            {
                case CorTokenType.mdtMethodDef:
                    break;

                case CorTokenType.mdtMemberRef:
                    flags |= (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_MemberRefToken;
                    break;

                default:
                    throw new NotImplementedException();
            }

            if ((method.Token.Module != context.LocalContext) && (!enforceOwningType || (enforceDefEncoding && methodToken.TokenType == CorTokenType.mdtMemberRef)))
            {
                // If enforeOwningType is set, this is an entry for the InstanceEntryPoint or InstrumentationDataTable nodes
                // which are not used in quite the same way, and for which the MethodDef is always matched to the module
                // which defines the type
                flags |= (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_UpdateContext;
            }

            EmitUInt(flags);

            if ((flags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_UpdateContext) != 0)
            {
                uint moduleIndex = (uint)context.Resolver.GetModuleIndex(method.Token.Module);
                EmitUInt(moduleIndex);
                context = context.InnerContext(method.Token.Module);
            }

            if ((flags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_OwnerType) != 0)
            {
                // The type here should be the type referred to by the memberref (if this is one, not the type where the method was eventually found!
                EmitTypeSignature(method.OwningType, context);
            }
            EmitTokenRid(methodToken.Token);
            if ((flags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_MethodInstantiation) != 0)
            {
                Instantiation instantiation = method.Method.Instantiation;
                EmitUInt((uint)instantiation.Length);
                SignatureContext methodInstantiationsContext;
                if ((flags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_UpdateContext) != 0)
                    methodInstantiationsContext = context;
                else
                    methodInstantiationsContext = context.OuterContext;

                for (int typeParamIndex = 0; typeParamIndex < instantiation.Length; typeParamIndex++)
                {
                    EmitTypeSignature(instantiation[typeParamIndex], methodInstantiationsContext);
                }
            }
        }

        public void EmitFieldSignature(FieldDesc field, SignatureContext context)
        {
            uint fieldSigFlags = 0;
            TypeDesc canonOwnerType = field.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific);
            TypeDesc ownerType = null;
            if (canonOwnerType.HasInstantiation)
            {
                ownerType = field.OwningType;
                fieldSigFlags |= (uint)ReadyToRunFieldSigFlags.READYTORUN_FIELD_SIG_OwnerType;
            }
            if (canonOwnerType != field.OwningType)
            {
                // Convert field to canonical form as this is what the field - module token lookup stores
                field = field.Context.GetFieldForInstantiatedType(field.GetTypicalFieldDefinition(), (InstantiatedType)canonOwnerType);
            }

            ModuleToken fieldToken = context.GetModuleTokenForField(field);
            switch (fieldToken.TokenType)
            {
                case CorTokenType.mdtMemberRef:
                    fieldSigFlags |= (uint)ReadyToRunFieldSigFlags.READYTORUN_FIELD_SIG_MemberRefToken;
                    break;

                case CorTokenType.mdtFieldDef:
                    break;

                default:
                    throw new NotImplementedException();
            }

            EmitUInt(fieldSigFlags);
            if (ownerType != null)
            {
                EmitTypeSignature(ownerType, context);
            }
            EmitTokenRid(fieldToken.Token);
        }
    }

    public class ObjectDataSignatureBuilder : SignatureBuilder
    {
        private ObjectDataBuilder _builder;

        public ObjectDataSignatureBuilder()
        {
            _builder = new ObjectDataBuilder();
        }

        public void AddSymbol(ISymbolDefinitionNode symbol)
        {
            _builder.AddSymbol(symbol);
        }

        public override void EmitByte(byte data)
        {
            _builder.EmitByte(data);
        }

        public void EmitReloc(ISymbolNode symbol, RelocType relocType, int delta = 0)
        {
            _builder.EmitReloc(symbol, relocType, delta);
        }

        public ObjectNode.ObjectData ToObjectData()
        {
            return _builder.ToObjectData();
        }

        public SignatureContext EmitFixup(NodeFactory factory, ReadyToRunFixupKind fixupKind, IEcmaModule targetModule, SignatureContext outerContext)
        {
            if (targetModule == outerContext.LocalContext)
            {
                EmitByte((byte)fixupKind);
                return outerContext;
            }
            else
            {
                EmitByte((byte)(fixupKind | ReadyToRunFixupKind.ModuleOverride));
                EmitUInt((uint)factory.ManifestMetadataTable.ModuleToIndex(targetModule));
                return new SignatureContext(targetModule, outerContext.Resolver);
            }
        }
    }

    internal class ArraySignatureBuilder : SignatureBuilder
    {
        private ArrayBuilder<byte> _builder;

        public ArraySignatureBuilder()
        {
            _builder = new ArrayBuilder<byte>();
        }

        public override void EmitByte(byte data)
        {
            _builder.Add(data);
        }

        public byte[] ToArray()
        {
            return _builder.ToArray();
        }
    }
}
