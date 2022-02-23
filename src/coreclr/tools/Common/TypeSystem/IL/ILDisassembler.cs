// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL
{
    // Known shortcomings:
    // - Escaping identifier names is missing (special characters and ILASM identifier names)
    // - Array bounds in signatures missing
    // - Custom modifiers and PINNED constraint not decoded in signatures
    // - Calling conventions in signatures not decoded
    // - Vararg signatures
    // - Floating point numbers are not represented in roundtrippable format

    /// <summary>
    /// Helper struct to disassemble IL instructions into a textual representation.
    /// </summary>
    public struct ILDisassembler
    {
        private byte[] _ilBytes;
        private MethodIL _methodIL;
        private ILTypeNameFormatter _typeNameFormatter;
        private int _currentOffset;

        public ILDisassembler(MethodIL methodIL)
        {
            _methodIL = methodIL;
            _ilBytes = methodIL.GetILBytes();
            _currentOffset = 0;
            _typeNameFormatter = null;
        }

        #region Type/member/signature name formatting
        private ILTypeNameFormatter TypeNameFormatter
        {
            get
            {
                if (_typeNameFormatter == null)
                {
                    // Find the owning module so that the type name formatter can remove
                    // redundant assembly name qualifiers in type names.
                    TypeDesc owningTypeDefinition = _methodIL.OwningMethod.OwningType;
                    ModuleDesc owningModule = owningTypeDefinition is MetadataType ?
                        ((MetadataType)owningTypeDefinition).Module : null;

                    _typeNameFormatter = new ILTypeNameFormatter(owningModule);
                }
                return _typeNameFormatter;
            }
        }

        public void AppendType(StringBuilder sb, TypeDesc type, bool forceValueClassPrefix = true)
        {
            // Types referenced from the IL show as instantiated over generic parameter.
            // E.g. "initobj !0" becomes "initobj !T"
            TypeDesc typeInContext = type.InstantiateSignature(
                _methodIL.OwningMethod.OwningType.Instantiation, _methodIL.OwningMethod.Instantiation);
            if (typeInContext.HasInstantiation || forceValueClassPrefix)
                this.TypeNameFormatter.AppendNameWithValueClassPrefix(sb, typeInContext);
            else
                this.TypeNameFormatter.AppendName(sb, typeInContext);
        }

        private void AppendOwningType(StringBuilder sb, TypeDesc type)
        {
            // Special case primitive types: we don't want to use short names here
            if (type.IsPrimitive || type.IsString || type.IsObject)
                _typeNameFormatter.AppendNameForNamespaceTypeWithoutAliases(sb, (MetadataType)type);
            else
                AppendType(sb, type, false);
        }

        private void AppendMethodSignature(StringBuilder sb, MethodDesc method)
        {
            // If this is an instantiated generic method, the formatted signature should
            // be uninstantiated (e.g. "void Foo::Bar<int>(!!0 param)", not "void Foo::Bar<int>(int param)")
            MethodSignature signature = method.GetMethodDefinition().Signature;

            AppendSignaturePrefix(sb, signature);
            sb.Append(' ');
            AppendOwningType(sb, method.OwningType);
            sb.Append("::");
            sb.Append(method.Name);

            if (method.HasInstantiation)
            {
                sb.Append('<');

                for (int i = 0; i < method.Instantiation.Length; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    _typeNameFormatter.AppendNameWithValueClassPrefix(sb, method.Instantiation[i]);
                }

                sb.Append('>');
            }

            sb.Append('(');
            AppendSignatureArgumentList(sb, signature);
            sb.Append(')');
        }

        private void AppendMethodSignature(StringBuilder sb, MethodSignature signature)
        {
            AppendSignaturePrefix(sb, signature);
            sb.Append('(');
            AppendSignatureArgumentList(sb, signature);
            sb.Append(')');
        }

        private void AppendSignaturePrefix(StringBuilder sb, MethodSignature signature)
        {
            if (!signature.IsStatic)
                sb.Append("instance ");

            this.TypeNameFormatter.AppendNameWithValueClassPrefix(sb, signature.ReturnType);
        }

        private void AppendSignatureArgumentList(StringBuilder sb, MethodSignature signature)
        {
            for (int i = 0; i < signature.Length; i++)
            {
                if (i != 0)
                    sb.Append(", ");

                this.TypeNameFormatter.AppendNameWithValueClassPrefix(sb, signature[i]);
            }
        }

        private void AppendFieldSignature(StringBuilder sb, FieldDesc field)
        {
            this.TypeNameFormatter.AppendNameWithValueClassPrefix(sb, field.FieldType);
            sb.Append(' ');
            AppendOwningType(sb, field.OwningType);
            sb.Append("::");
            sb.Append(field.Name);
        }

        private void AppendStringLiteral(StringBuilder sb, string s)
        {
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\')
                    sb.Append("\\\\");
                else if (s[i] == '\t')
                    sb.Append("\\t");
                else if (s[i] == '"')
                    sb.Append("\\\"");
                else if (s[i] == '\n')
                    sb.Append("\\n");
                else
                    sb.Append(s[i]);
            }
            sb.Append('"');
        }

        private void AppendToken(StringBuilder sb, int token)
        {
            object obj = _methodIL.GetObject(token);
            if (obj is MethodDesc)
                AppendMethodSignature(sb, (MethodDesc)obj);
            else if (obj is FieldDesc)
                AppendFieldSignature(sb, (FieldDesc)obj);
            else if (obj is MethodSignature)
                AppendMethodSignature(sb, (MethodSignature)obj);
            else if (obj is TypeDesc)
                AppendType(sb, (TypeDesc)obj, false);
            else
            {
                Debug.Assert(obj is string, "NYI: " + obj.GetType());
                AppendStringLiteral(sb, (string)obj);
            }
        }
        #endregion

        #region Instruction decoding
        private byte ReadILByte()
        {
            return _ilBytes[_currentOffset++];
        }

        private UInt16 ReadILUInt16()
        {
            UInt16 val = (UInt16)(_ilBytes[_currentOffset] + (_ilBytes[_currentOffset + 1] << 8));
            _currentOffset += 2;
            return val;
        }

        private UInt32 ReadILUInt32()
        {
            UInt32 val = (UInt32)(_ilBytes[_currentOffset] + (_ilBytes[_currentOffset + 1] << 8) + (_ilBytes[_currentOffset + 2] << 16) + (_ilBytes[_currentOffset + 3] << 24));
            _currentOffset += 4;
            return val;
        }

        private int ReadILToken()
        {
            return (int)ReadILUInt32();
        }

        private ulong ReadILUInt64()
        {
            ulong value = ReadILUInt32();
            value |= (((ulong)ReadILUInt32()) << 32);
            return value;
        }

        private unsafe float ReadILFloat()
        {
            uint value = ReadILUInt32();
            return *(float*)(&value);
        }

        private unsafe double ReadILDouble()
        {
            ulong value = ReadILUInt64();
            return *(double*)(&value);
        }

        public static void AppendOffset(StringBuilder sb, int offset)
        {
            sb.Append($"IL_{offset:X4}");
        }

        private static void PadForInstructionArgument(StringBuilder sb)
        {
            if (sb.Length < 22)
                sb.Append(' ', 22 - sb.Length);
            else
                sb.Append(' ');
        }

        public bool HasNextInstruction
        {
            get
            {
                return _currentOffset < _ilBytes.Length;
            }
        }

        public int Offset
        {
            get
            {
                return _currentOffset;
            }
        }

        public int CodeSize
        {
            get
            {
                return _ilBytes.Length;
            }
        }

        public string GetNextInstruction()
        {
            StringBuilder decodedInstruction = new StringBuilder();
            AppendOffset(decodedInstruction, _currentOffset);
            decodedInstruction.Append(":  ");

        again:

            ILOpcode opCode = (ILOpcode)ReadILByte();
            if (opCode == ILOpcode.prefix1)
            {
                opCode = (ILOpcode)(0x100 + ReadILByte());
            }

            // Quick and dirty way to get the opcode name is to convert the enum value to string.
            // We need some adjustments though.
            string opCodeString = opCode.ToString().Replace("_", ".");
            if (opCodeString.EndsWith("."))
                opCodeString = opCodeString.Substring(0, opCodeString.Length - 1);

            decodedInstruction.Append(opCodeString);

            switch (opCode)
            {
                case ILOpcode.ldarg_s:
                case ILOpcode.ldarga_s:
                case ILOpcode.starg_s:
                case ILOpcode.ldloc_s:
                case ILOpcode.ldloca_s:
                case ILOpcode.stloc_s:
                case ILOpcode.ldc_i4_s:
                    PadForInstructionArgument(decodedInstruction);
                    decodedInstruction.Append(ReadILByte().ToStringInvariant());
                    return decodedInstruction.ToString();

                case ILOpcode.unaligned:
                    decodedInstruction.Append(' ');
                    decodedInstruction.Append(ReadILByte().ToStringInvariant());
                    decodedInstruction.Append(' ');
                    goto again;

                case ILOpcode.ldarg:
                case ILOpcode.ldarga:
                case ILOpcode.starg:
                case ILOpcode.ldloc:
                case ILOpcode.ldloca:
                case ILOpcode.stloc:
                    PadForInstructionArgument(decodedInstruction);
                    decodedInstruction.Append(ReadILUInt16().ToStringInvariant());
                    return decodedInstruction.ToString();

                case ILOpcode.ldc_i4:
                    PadForInstructionArgument(decodedInstruction);
                    decodedInstruction.Append(ReadILUInt32().ToStringInvariant());
                    return decodedInstruction.ToString();

                case ILOpcode.ldc_r4:
                    PadForInstructionArgument(decodedInstruction);
                    decodedInstruction.Append(ReadILFloat().ToStringInvariant());
                    return decodedInstruction.ToString();

                case ILOpcode.ldc_i8:
                    PadForInstructionArgument(decodedInstruction);
                    decodedInstruction.Append(ReadILUInt64().ToStringInvariant());
                    return decodedInstruction.ToString();

                case ILOpcode.ldc_r8:
                    PadForInstructionArgument(decodedInstruction);
                    decodedInstruction.Append(ReadILDouble().ToStringInvariant());
                    return decodedInstruction.ToString();

                case ILOpcode.jmp:
                case ILOpcode.call:
                case ILOpcode.calli:
                case ILOpcode.callvirt:
                case ILOpcode.cpobj:
                case ILOpcode.ldobj:
                case ILOpcode.ldstr:
                case ILOpcode.newobj:
                case ILOpcode.castclass:
                case ILOpcode.isinst:
                case ILOpcode.unbox:
                case ILOpcode.ldfld:
                case ILOpcode.ldflda:
                case ILOpcode.stfld:
                case ILOpcode.ldsfld:
                case ILOpcode.ldsflda:
                case ILOpcode.stsfld:
                case ILOpcode.stobj:
                case ILOpcode.box:
                case ILOpcode.newarr:
                case ILOpcode.ldelema:
                case ILOpcode.ldelem:
                case ILOpcode.stelem:
                case ILOpcode.unbox_any:
                case ILOpcode.refanyval:
                case ILOpcode.mkrefany:
                case ILOpcode.ldtoken:
                case ILOpcode.ldftn:
                case ILOpcode.ldvirtftn:
                case ILOpcode.initobj:
                case ILOpcode.constrained:
                case ILOpcode.sizeof_:
                    PadForInstructionArgument(decodedInstruction);
                    AppendToken(decodedInstruction, ReadILToken());
                    return decodedInstruction.ToString();

                case ILOpcode.br_s:
                case ILOpcode.leave_s:
                case ILOpcode.brfalse_s:
                case ILOpcode.brtrue_s:
                case ILOpcode.beq_s:
                case ILOpcode.bge_s:
                case ILOpcode.bgt_s:
                case ILOpcode.ble_s:
                case ILOpcode.blt_s:
                case ILOpcode.bne_un_s:
                case ILOpcode.bge_un_s:
                case ILOpcode.bgt_un_s:
                case ILOpcode.ble_un_s:
                case ILOpcode.blt_un_s:
                    PadForInstructionArgument(decodedInstruction);
                    AppendOffset(decodedInstruction, (sbyte)ReadILByte() + _currentOffset);
                    return decodedInstruction.ToString();

                case ILOpcode.br:
                case ILOpcode.leave:
                case ILOpcode.brfalse:
                case ILOpcode.brtrue:
                case ILOpcode.beq:
                case ILOpcode.bge:
                case ILOpcode.bgt:
                case ILOpcode.ble:
                case ILOpcode.blt:
                case ILOpcode.bne_un:
                case ILOpcode.bge_un:
                case ILOpcode.bgt_un:
                case ILOpcode.ble_un:
                case ILOpcode.blt_un:
                    PadForInstructionArgument(decodedInstruction);
                    AppendOffset(decodedInstruction, (int)ReadILUInt32() + _currentOffset);
                    return decodedInstruction.ToString();

                case ILOpcode.switch_:
                    {
                        decodedInstruction.Clear();
                        decodedInstruction.Append("switch (");
                        uint count = ReadILUInt32();
                        int jmpBase = _currentOffset + (int)(4 * count);
                        for (uint i = 0; i < count; i++)
                        {
                            if (i != 0)
                                decodedInstruction.Append(", ");
                            int delta = (int)ReadILUInt32();
                            AppendOffset(decodedInstruction, jmpBase + delta);
                        }
                        decodedInstruction.Append(")");
                        return decodedInstruction.ToString();
                    }

                default:
                    return decodedInstruction.ToString();
            }
        }
        #endregion

        #region Helpers
        public class ILTypeNameFormatter : TypeNameFormatter
        {
            private ModuleDesc _thisModule;

            public ILTypeNameFormatter(ModuleDesc thisModule)
            {
                _thisModule = thisModule;
            }

            public void AppendNameWithValueClassPrefix(StringBuilder sb, TypeDesc type)
            {
                if (!type.IsSignatureVariable
                    && type.IsDefType
                    && !type.IsPrimitive
                    && !type.IsObject
                    && !type.IsString)
                {
                    string prefix = type.IsValueType ? "valuetype " : "class ";
                    sb.Append(prefix);
                    AppendName(sb, type);
                }
                else
                {
                    AppendName(sb, type);
                }
            }

            public override void AppendName(StringBuilder sb, PointerType type)
            {
                AppendNameWithValueClassPrefix(sb, type.ParameterType);
                sb.Append('*');
            }

            public override void AppendName(StringBuilder sb, FunctionPointerType type)
            {
                MethodSignature signature = type.Signature;

                sb.Append("method ");

                if (!signature.IsStatic)
                    sb.Append("instance ");

                // TODO: rest of calling conventions

                AppendName(sb, signature.ReturnType);

                sb.Append(" *(");
                for (int i = 0; i < signature.Length; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    AppendName(sb, signature[i]);
                }
                sb.Append(')');
            }

            public override void AppendName(StringBuilder sb, SignatureMethodVariable type)
            {
                sb.Append("!!");
                sb.Append(type.Index.ToStringInvariant());
            }

            public override void AppendName(StringBuilder sb, SignatureTypeVariable type)
            {
                sb.Append("!");
                sb.Append(type.Index.ToStringInvariant());
            }

            public override void AppendName(StringBuilder sb, GenericParameterDesc type)
            {
                string prefix = type.Kind == GenericParameterKind.Type ? "!" : "!!";
                sb.Append(prefix);
                sb.Append(type.Name);
            }

            protected override void AppendNameForInstantiatedType(StringBuilder sb, DefType type)
            {
                AppendName(sb, type.GetTypeDefinition());
                sb.Append('<');

                for (int i = 0; i < type.Instantiation.Length; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    AppendNameWithValueClassPrefix(sb, type.Instantiation[i]);
                }   

                sb.Append('>');
            }

            public override void AppendName(StringBuilder sb, ByRefType type)
            {
                AppendNameWithValueClassPrefix(sb, type.ParameterType);
                sb.Append('&');
            }

            public override void AppendName(StringBuilder sb, ArrayType type)
            {
                AppendNameWithValueClassPrefix(sb, type.ElementType);
                sb.Append('[');
                sb.Append(',', type.Rank - 1);
                sb.Append(']');
            }

            protected override void AppendNameForNamespaceType(StringBuilder sb, DefType type)
            {
                switch (type.Category)
                {
                    case TypeFlags.Void:
                        sb.Append("void");
                        return;
                    case TypeFlags.Boolean:
                        sb.Append("bool");
                        return;
                    case TypeFlags.Char:
                        sb.Append("char");
                        return;
                    case TypeFlags.SByte:
                        sb.Append("int8");
                        return;
                    case TypeFlags.Byte:
                        sb.Append("uint8");
                        return;
                    case TypeFlags.Int16:
                        sb.Append("int16");
                        return;
                    case TypeFlags.UInt16:
                        sb.Append("uint16");
                        return;
                    case TypeFlags.Int32:
                        sb.Append("int32");
                        return;
                    case TypeFlags.UInt32:
                        sb.Append("uint32");
                        return;
                    case TypeFlags.Int64:
                        sb.Append("int64");
                        return;
                    case TypeFlags.UInt64:
                        sb.Append("uint64");
                        return;
                    case TypeFlags.IntPtr:
                        sb.Append("native int");
                        return;
                    case TypeFlags.UIntPtr:
                        sb.Append("native uint");
                        return;
                    case TypeFlags.Single:
                        sb.Append("float32");
                        return;
                    case TypeFlags.Double:
                        sb.Append("float64");
                        return;
                }

                if (type.IsString)
                {
                    sb.Append("string");
                    return;
                }

                if (type.IsObject)
                {
                    sb.Append("object");
                    return;
                }

                AppendNameForNamespaceTypeWithoutAliases(sb, type);
            }
            public void AppendNameForNamespaceTypeWithoutAliases(StringBuilder sb, DefType type)
            {
                ModuleDesc owningModule = (type as MetadataType)?.Module;
                if (owningModule != null && owningModule != _thisModule)
                {
                    Debug.Assert(owningModule is IAssemblyDesc);
                    string owningModuleName = ((IAssemblyDesc)owningModule).GetName().Name;
                    sb.Append('[');
                    sb.Append(owningModuleName);
                    sb.Append(']');
                }

                string ns = type.Namespace;
                if (ns.Length > 0)
                {
                    sb.Append(ns);
                    sb.Append('.');
                }
                sb.Append(type.Name);
            }

            protected override void AppendNameForNestedType(StringBuilder sb, DefType nestedType, DefType containingType)
            {
                AppendName(sb, containingType);
                sb.Append('/');
                sb.Append(nestedType.Name);
            }
        }
        #endregion
    }
}
