// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Internal.IL.Stubs;

namespace Internal.IL
{
    public sealed class ReadyToRunILProvider : ILProvider
    {
        private MethodIL TryGetIntrinsicMethodILForActivator(MethodDesc method)
        {
            if (method.Instantiation.Length == 1
                && method.Signature.Length == 0
                && method.Name == "CreateInstance")
            {
                TypeDesc type = method.Instantiation[0];
                if (type.IsValueType && type.GetParameterlessConstructor() == null)
                {
                    // Replace the body with implementation that just returns "default"
                    MethodDesc createDefaultInstance = method.OwningType.GetKnownMethod("CreateDefaultInstance", method.GetTypicalMethodDefinition().Signature);
                    return GetMethodIL(createDefaultInstance.MakeInstantiatedMethod(type));
                }
            }

            return null;
        }

        /// <summary>
        /// Provides method bodies for intrinsics recognized by the compiler.
        /// It can return null if it's not an intrinsic recognized by the compiler,
        /// but an intrinsic e.g. recognized by codegen.
        /// </summary>
        private MethodIL TryGetIntrinsicMethodIL(MethodDesc method)
        {
            var mdType = method.OwningType as MetadataType;
            if (mdType == null)
                return null;

            if (mdType.Name == "RuntimeHelpers" && mdType.Namespace == "System.Runtime.CompilerServices")
            {
                return RuntimeHelpersIntrinsics.EmitIL(method);
            }

            if (mdType.Name == "Unsafe" && mdType.Namespace == "Internal.Runtime.CompilerServices")
            {
                return UnsafeIntrinsics.EmitIL(method);
            }

            if (mdType.Name == "MemoryMarshal" && mdType.Namespace == "System.Runtime.InteropServices")
            {
                return MemoryMarshalIntrinsics.EmitIL(method);
            }

            if (mdType.Name == "Volatile" && mdType.Namespace == "System.Threading")
            {
                return VolatileIntrinsics.EmitIL(method);
            }

            if (mdType.Name == "Interlocked" && mdType.Namespace == "System.Threading")
            {
                return InterlockedIntrinsics.EmitIL(method);
            }

            return null;
        }

        /// <summary>
        /// Provides method bodies for intrinsics recognized by the compiler that
        /// are specialized per instantiation. It can return null if the intrinsic
        /// is not recognized.
        /// </summary>
        private MethodIL TryGetPerInstantiationIntrinsicMethodIL(MethodDesc method)
        {
            var mdType = method.OwningType as MetadataType;
            if (mdType == null)
                return null;

            if (mdType.Name == "RuntimeHelpers" && mdType.Namespace == "System.Runtime.CompilerServices")
            {
                return RuntimeHelpersIntrinsics.EmitIL(method);
            }

            if (mdType.Name == "Activator" && mdType.Namespace == "System")
            {
                return TryGetIntrinsicMethodILForActivator(method);
            }

            return null;
        }

        public override MethodIL GetMethodIL(MethodDesc method)
        {
            if (method is EcmaMethod ecmaMethod)
            {
                if (method.IsIntrinsic)
                {
                    MethodIL result = TryGetIntrinsicMethodIL(method);
                    if (result != null)
                        return result;
                }

                MethodIL methodIL = EcmaMethodIL.Create(ecmaMethod);
                if (methodIL != null)
                    return methodIL;

                return null;
            }
            else if (method is MethodForInstantiatedType || method is InstantiatedMethod)
            {
                // Intrinsics specialized per instantiation
                if (method.IsIntrinsic)
                {
                    MethodIL methodIL = TryGetPerInstantiationIntrinsicMethodIL(method);
                    if (methodIL != null)
                        return methodIL;
                }

                var methodDefinitionIL = GetMethodIL(method.GetTypicalMethodDefinition());
                if (methodDefinitionIL == null)
                    return null;
                return new InstantiatedMethodIL(method, methodDefinitionIL);
            }
            else
            {
                return null;
            }
        }
    }
}
