// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Provides method bodies for System.Runtime.CompilerServices.RuntimeHelpers intrinsics.
    /// </summary>
    public static class RuntimeHelpersIntrinsics
    {
        public static MethodIL EmitIL(MethodDesc method)
        {
            Debug.Assert(((MetadataType)method.OwningType).Name == "RuntimeHelpers"u8);

            // All the methods handled below are per-instantiation generic methods
            if (method.Instantiation.Length != 1 || method.IsTypicalMethodDefinition)
                return null;

            TypeDesc elementType = method.Instantiation[0];

            // Fallback to non-intrinsic implementation for universal generics
            if (elementType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                return null;

            bool result;
            if (method.Name == "IsBitwiseEquatable"u8)
            {
                bool? equatable = ComparerIntrinsics.ImplementsIEquatable(elementType);
                if (equatable == true)
                {
                    TypeDesc iEquatable = elementType.Context.SystemModule
                        .GetKnownType("System"u8, "IEquatable`1"u8)
                        .MakeInstantiatedType(elementType);
                    MethodDesc getIsBitwiseEquatable = iEquatable.GetKnownMethod("get_IsBitwiseEquatable"u8, null);

                    ILEmitter emitter = new ILEmitter();
                    ILCodeStream codeStream = emitter.NewCodeStream();
                    codeStream.Emit(ILOpcode.constrained, emitter.NewToken(elementType));
                    codeStream.Emit(ILOpcode.call, emitter.NewToken(getIsBitwiseEquatable));
                    codeStream.Emit(ILOpcode.ret);
                    return emitter.Link(method);
                }

                result = elementType.IsEnum;
                if (!result && equatable == false && elementType is MetadataType mdType && mdType.IsValueType)
                {
                    // Value type that can use memcmp and that doesn't override object.Equals or implement IEquatable<T>.Equals.
                    MethodDesc objectEquals = mdType.Context.GetWellKnownType(WellKnownType.Object).GetMethod("Equals"u8, null);
                    result =
                        mdType.FindVirtualFunctionTargetMethodOnObjectType(objectEquals).OwningType != mdType &&
                        ComparerIntrinsics.CanCompareValueTypeBits(mdType, objectEquals);
                }
            }
            else
            {
                return null;
            }

            ILOpcode opcode = result ? ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0;

            return new ILStubMethodIL(method, new byte[] { (byte)opcode, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), Array.Empty<object>());
        }
    }
}
