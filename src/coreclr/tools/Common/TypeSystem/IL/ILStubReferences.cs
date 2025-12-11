// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Internal.IL
{
    public class ILStubReferences
    {
        /// <summary>
        /// Extracts all method and type references from the IL of a method.
        /// This is used to find references in synthetic IL (like async thunks) that may need
        /// to be added to the mutable module.
        /// </summary>
        public static List<TypeSystemEntity> GetNecessaryReferencesFromIL(MethodIL methodIL)
        {
            var references = new List<TypeSystemEntity>();
            byte[] ilBytes = methodIL.GetILBytes();
            ILReader reader = new ILReader(ilBytes);

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
                            references.Add(method);
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
                        if (obj is TypeDesc type
                            // These types can be represented by ELEMENT_TYPE values and don't need references.
                            && !type.IsPrimitive && !type.IsVoid && !type.IsObject && !type.IsString && !type.IsTypedReference)
                        {
                            references.Add(type);
                        }
                        break;
                    }

                    case ILOpcode.ldtoken:
                    {
                        int token = reader.ReadILToken();
                        object obj = methodIL.GetObject(token, NotFoundBehavior.ReturnNull);
                        if (obj is MethodDesc method)
                        {
                            references.Add(method);
                        }
                        else if (obj is TypeDesc type
                            // These types can be represented by ELEMENT_TYPE values and don't need references.
                            && !type.IsPrimitive && !type.IsVoid && !type.IsObject && !type.IsString && !type.IsTypedReference)
                        {
                            references.Add(type);
                        }
                        break;
                    }

                    default:
                        reader.Skip(opcode);
                        break;
                }
            }

            foreach (var region in methodIL.GetExceptionRegions())
            {
                if (region.Kind == ILExceptionRegionKind.Catch && region.ClassToken != 0)
                {
                    object obj = methodIL.GetObject(region.ClassToken, NotFoundBehavior.ReturnNull);
                    if (obj is TypeDesc type && !type.IsPrimitive && !type.IsVoid && !type.IsObject && !type.IsString && !type.IsTypedReference)
                        references.Add(type);
                }
            }

            foreach (var local in methodIL.GetLocals())
            {
                TypeDesc type = local.Type;
                // These types can be represented by ELEMENT_TYPE values and don't need references.
                if (!type.IsPrimitive && !type.IsVoid && !type.IsObject && !type.IsString && !type.IsTypedReference)
                    references.Add(type);
            }

            return references;
        }
    }
}
