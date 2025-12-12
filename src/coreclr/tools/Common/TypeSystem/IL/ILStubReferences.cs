// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.TypeSystem;

namespace Internal.IL
{
    public static class ILStubReferences
    {
        /// <summary>
        /// Extracts all method, type, and field references from the IL of a method.
        /// This is used to find references in synthetic IL (like async thunks) that may need
        /// to be added to the mutable module.
        /// </summary>
        public static IEnumerable<TypeSystemEntity> GetNecessaryReferencesFromIL(MethodIL methodIL)
        {
            // Return references from the owning method's signature
            MethodDesc owningMethod = methodIL.OwningMethod;
            MethodSignature signature = owningMethod.Signature;


            // Technically we don't need tokens for types that can be represented with an ELEMENT_TYPE_* value in
            // the signature, but the source of a type is not tracked through compilation, so we can't easily
            // determine that when trying to resolve a ModuleToken for a TypeDesc in jit compilation.
            // We'll be conservative by returning all types in the signature.
            yield return signature.ReturnType;
            for (int i = 0; i < signature.Length; i++)
            {
                yield return signature[i];
            }
            yield return owningMethod.OwningType;
            if (owningMethod.HasInstantiation)
            {
                foreach (var typeArg in owningMethod.Instantiation)
                {
                    yield return typeArg;
                }
            }

            byte[] ilBytes = methodIL.GetILBytes();
            ILReader reader = new ILReader(ilBytes);
            List<TypeSystemEntity> entities = new();

            while (reader.HasNext)
            {
                ILOpcode opcode = reader.ReadILOpcode();

                switch (opcode)
                {
                    case ILOpcode.call:
                    case ILOpcode.callvirt:
                    case ILOpcode.newobj:
                    case ILOpcode.ldftn:
                    case ILOpcode.ldvirtftn:
                    case ILOpcode.jmp:
                    {
                        int token = reader.ReadILToken();
                        object obj = methodIL.GetObject(token, NotFoundBehavior.ReturnNull);
                        if (obj is MethodDesc method)
                        {
                            entities.Add(method);
                        }
                        break;
                    }

                    case ILOpcode.newarr:
                    case ILOpcode.castclass:
                    case ILOpcode.isinst:
                    case ILOpcode.box:
                    case ILOpcode.unbox:
                    case ILOpcode.unbox_any:
                    case ILOpcode.ldobj:
                    case ILOpcode.stobj:
                    case ILOpcode.cpobj:
                    case ILOpcode.initobj:
                    case ILOpcode.ldelem:
                    case ILOpcode.stelem:
                    case ILOpcode.ldelema:
                    case ILOpcode.constrained:
                    case ILOpcode.sizeof_:
                    case ILOpcode.mkrefany:
                    case ILOpcode.refanyval:
                    {
                        int token = reader.ReadILToken();
                        object obj = methodIL.GetObject(token, NotFoundBehavior.ReturnNull);
                        if (obj is TypeDesc type)
                        {
                            entities.Add(type);
                        }
                        break;
                    }

                    case ILOpcode.ldfld:
                    case ILOpcode.ldflda:
                    case ILOpcode.stfld:
                    case ILOpcode.ldsfld:
                    case ILOpcode.ldsflda:
                    case ILOpcode.stsfld:
                    {
                        int token = reader.ReadILToken();
                        object obj = methodIL.GetObject(token, NotFoundBehavior.ReturnNull);
                        if (obj is FieldDesc field)
                        {
                            entities.Add(field);
                        }
                        break;
                    }

                    case ILOpcode.ldtoken:
                    {

                        int token = reader.ReadILToken();
                        object obj = methodIL.GetObject(token, NotFoundBehavior.ReturnNull);
                        if (obj is MethodDesc method)
                        {
                            entities.Add(method);
                        }
                        else if (obj is TypeDesc type)
                        {
                            entities.Add(type);
                        }
                        else if (obj is FieldDesc field)
                        {
                            entities.Add(field);
                        }
                        break;
                    }

                    // calli has a signature token (StandAloneSig)
                    case ILOpcode.calli:
                    {
                        int token = reader.ReadILToken();
                        object obj = methodIL.GetObject(token, NotFoundBehavior.ReturnNull);
                        if (obj is MethodSignature sig)
                        {
                            // Return the return type
                            entities.Add(sig.ReturnType);
                            // Return parameter types
                            for (int i = 0; i < sig.Length; i++)
                            {
                                entities.Add(sig[i]);
                            }
                        }
                        break;
                    }
                    case ILOpcode.ldstr:
                    {
                        // Do we need to add string references?
                        reader.Skip(opcode);
                        break;
                    }

                    // Opcodes with no token operands - just skip
                    case ILOpcode.nop:
                    case ILOpcode.break_:
                    case ILOpcode.ldarg_0:
                    case ILOpcode.ldarg_1:
                    case ILOpcode.ldarg_2:
                    case ILOpcode.ldarg_3:
                    case ILOpcode.ldloc_0:
                    case ILOpcode.ldloc_1:
                    case ILOpcode.ldloc_2:
                    case ILOpcode.ldloc_3:
                    case ILOpcode.stloc_0:
                    case ILOpcode.stloc_1:
                    case ILOpcode.stloc_2:
                    case ILOpcode.stloc_3:
                    case ILOpcode.ldarg_s:
                    case ILOpcode.ldarga_s:
                    case ILOpcode.starg_s:
                    case ILOpcode.ldloc_s:
                    case ILOpcode.ldloca_s:
                    case ILOpcode.stloc_s:
                    case ILOpcode.ldnull:
                    case ILOpcode.ldc_i4_m1:
                    case ILOpcode.ldc_i4_0:
                    case ILOpcode.ldc_i4_1:
                    case ILOpcode.ldc_i4_2:
                    case ILOpcode.ldc_i4_3:
                    case ILOpcode.ldc_i4_4:
                    case ILOpcode.ldc_i4_5:
                    case ILOpcode.ldc_i4_6:
                    case ILOpcode.ldc_i4_7:
                    case ILOpcode.ldc_i4_8:
                    case ILOpcode.ldc_i4_s:
                    case ILOpcode.ldc_i4:
                    case ILOpcode.ldc_i8:
                    case ILOpcode.ldc_r4:
                    case ILOpcode.ldc_r8:
                    case ILOpcode.dup:
                    case ILOpcode.pop:
                    case ILOpcode.ret:
                    case ILOpcode.br_s:
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
                    case ILOpcode.br:
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
                    case ILOpcode.switch_:
                    case ILOpcode.ldind_i1:
                    case ILOpcode.ldind_u1:
                    case ILOpcode.ldind_i2:
                    case ILOpcode.ldind_u2:
                    case ILOpcode.ldind_i4:
                    case ILOpcode.ldind_u4:
                    case ILOpcode.ldind_i8:
                    case ILOpcode.ldind_i:
                    case ILOpcode.ldind_r4:
                    case ILOpcode.ldind_r8:
                    case ILOpcode.ldind_ref:
                    case ILOpcode.stind_ref:
                    case ILOpcode.stind_i1:
                    case ILOpcode.stind_i2:
                    case ILOpcode.stind_i4:
                    case ILOpcode.stind_i8:
                    case ILOpcode.stind_r4:
                    case ILOpcode.stind_r8:
                    case ILOpcode.add:
                    case ILOpcode.sub:
                    case ILOpcode.mul:
                    case ILOpcode.div:
                    case ILOpcode.div_un:
                    case ILOpcode.rem:
                    case ILOpcode.rem_un:
                    case ILOpcode.and:
                    case ILOpcode.or:
                    case ILOpcode.xor:
                    case ILOpcode.shl:
                    case ILOpcode.shr:
                    case ILOpcode.shr_un:
                    case ILOpcode.neg:
                    case ILOpcode.not:
                    case ILOpcode.conv_i1:
                    case ILOpcode.conv_i2:
                    case ILOpcode.conv_i4:
                    case ILOpcode.conv_i8:
                    case ILOpcode.conv_r4:
                    case ILOpcode.conv_r8:
                    case ILOpcode.conv_u4:
                    case ILOpcode.conv_u8:
                    case ILOpcode.conv_r_un:
                    case ILOpcode.throw_:
                    case ILOpcode.conv_ovf_i1_un:
                    case ILOpcode.conv_ovf_i2_un:
                    case ILOpcode.conv_ovf_i4_un:
                    case ILOpcode.conv_ovf_i8_un:
                    case ILOpcode.conv_ovf_u1_un:
                    case ILOpcode.conv_ovf_u2_un:
                    case ILOpcode.conv_ovf_u4_un:
                    case ILOpcode.conv_ovf_u8_un:
                    case ILOpcode.conv_ovf_i_un:
                    case ILOpcode.conv_ovf_u_un:
                    case ILOpcode.ldlen:
                    case ILOpcode.ldelem_i1:
                    case ILOpcode.ldelem_u1:
                    case ILOpcode.ldelem_i2:
                    case ILOpcode.ldelem_u2:
                    case ILOpcode.ldelem_i4:
                    case ILOpcode.ldelem_u4:
                    case ILOpcode.ldelem_i8:
                    case ILOpcode.ldelem_i:
                    case ILOpcode.ldelem_r4:
                    case ILOpcode.ldelem_r8:
                    case ILOpcode.ldelem_ref:
                    case ILOpcode.stelem_i:
                    case ILOpcode.stelem_i1:
                    case ILOpcode.stelem_i2:
                    case ILOpcode.stelem_i4:
                    case ILOpcode.stelem_i8:
                    case ILOpcode.stelem_r4:
                    case ILOpcode.stelem_r8:
                    case ILOpcode.stelem_ref:
                    case ILOpcode.conv_ovf_i1:
                    case ILOpcode.conv_ovf_u1:
                    case ILOpcode.conv_ovf_i2:
                    case ILOpcode.conv_ovf_u2:
                    case ILOpcode.conv_ovf_i4:
                    case ILOpcode.conv_ovf_u4:
                    case ILOpcode.conv_ovf_i8:
                    case ILOpcode.conv_ovf_u8:
                    case ILOpcode.ckfinite:
                    case ILOpcode.conv_u2:
                    case ILOpcode.conv_u1:
                    case ILOpcode.conv_i:
                    case ILOpcode.conv_ovf_i:
                    case ILOpcode.conv_ovf_u:
                    case ILOpcode.add_ovf:
                    case ILOpcode.add_ovf_un:
                    case ILOpcode.mul_ovf:
                    case ILOpcode.mul_ovf_un:
                    case ILOpcode.sub_ovf:
                    case ILOpcode.sub_ovf_un:
                    case ILOpcode.endfinally:
                    case ILOpcode.leave:
                    case ILOpcode.leave_s:
                    case ILOpcode.stind_i:
                    case ILOpcode.conv_u:
                    case ILOpcode.prefix1:
                    case ILOpcode.arglist:
                    case ILOpcode.ceq:
                    case ILOpcode.cgt:
                    case ILOpcode.cgt_un:
                    case ILOpcode.clt:
                    case ILOpcode.clt_un:
                    case ILOpcode.ldarg:
                    case ILOpcode.ldarga:
                    case ILOpcode.starg:
                    case ILOpcode.ldloc:
                    case ILOpcode.ldloca:
                    case ILOpcode.stloc:
                    case ILOpcode.localloc:
                    case ILOpcode.endfilter:
                    case ILOpcode.unaligned:
                    case ILOpcode.volatile_:
                    case ILOpcode.tail:
                    case ILOpcode.cpblk:
                    case ILOpcode.initblk:
                    case ILOpcode.no:
                    case ILOpcode.rethrow:
                    case ILOpcode.refanytype:
                    case ILOpcode.readonly_:
                        reader.Skip(opcode);
                        break;

                    default:
                        throw new NotImplementedException($"Unhandled opcode: {opcode}");
                }
            }
            foreach(var entity in entities)
                yield return entity;

            foreach (var region in methodIL.GetExceptionRegions())
            {
                if (region.Kind == ILExceptionRegionKind.Catch && region.ClassToken != 0)
                {
                    object obj = methodIL.GetObject(region.ClassToken, NotFoundBehavior.ReturnNull);
                    if (obj is TypeDesc type)
                        yield return type;
                }
            }

            foreach (var local in methodIL.GetLocals())
            {
                // Technically we don't need types that can be represented with an ELEMENT_TYPE_* value, but
                // but the source of a type is not tracked through compilation, so we can't easily determine that when
                // trying to resolve a ModuleToken for a TypeDesc in jit compilation.
                yield return local.Type;
            }
        }
    }
}
