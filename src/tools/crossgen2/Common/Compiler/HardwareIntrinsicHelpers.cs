// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Internal.TypeSystem;
using Internal.IL;
using Internal.IL.Stubs;
using System.Diagnostics;

namespace ILCompiler
{
    public static class HardwareIntrinsicHelpers
    {
        /// <summary>
        /// Gets a value indicating whether this is a hardware intrinsic on the platform that we're compiling for.
        /// </summary>
        public static bool IsHardwareIntrinsic(MethodDesc method)
        {
            TypeDesc owningType = method.OwningType;

            if (owningType.IsIntrinsic && owningType is MetadataType mdType)
            {
                TargetArchitecture targetArch = owningType.Context.Target.Architecture;

                if (targetArch == TargetArchitecture.X64 || targetArch == TargetArchitecture.X86)
                {
                    mdType = (MetadataType)mdType.ContainingType ?? mdType;
                    if (mdType.Namespace == "System.Runtime.Intrinsics.X86")
                        return true;
                }
                else if (targetArch == TargetArchitecture.ARM64)
                {
                    if (mdType.Namespace == "System.Runtime.Intrinsics.Arm.Arm64")
                        return true;
                }
            }

            return false;
        }

        public static bool IsIsSupportedMethod(MethodDesc method)
        {
            return method.Name == "get_IsSupported";
        }

        public static MethodIL GetUnsupportedImplementationIL(MethodDesc method)
        {
            // The implementation of IsSupported for codegen backends that don't support hardware intrinsics
            // at all is to return 0.
            if (IsIsSupportedMethod(method))
            {
                return new ILStubMethodIL(method,
                    new byte[] {
                        (byte)ILOpcode.ldc_i4_0,
                        (byte)ILOpcode.ret
                    },
                    Array.Empty<LocalVariableDefinition>(), null);
            }

            // Other methods throw PlatformNotSupportedException
            MethodDesc throwPnse = method.Context.GetHelperEntryPoint("ThrowHelpers", "ThrowPlatformNotSupportedException");

            return new ILStubMethodIL(method,
                    new byte[] {
                        (byte)ILOpcode.call, 1, 0, 0, 0,
                        (byte)ILOpcode.br_s, unchecked((byte)-7),
                    },
                    Array.Empty<LocalVariableDefinition>(),
                    new object[] { throwPnse });
        }

        /// <summary>
        /// Generates IL for the IsSupported property that reads this information from a field initialized by the runtime
        /// at startup. Returns null for hardware intrinsics whose support level is known at compile time
        /// (i.e. they're known to be always supported or always unsupported).
        /// </summary>
        public static MethodIL EmitIsSupportedIL(MethodDesc method, FieldDesc isSupportedField)
        {
            Debug.Assert(IsIsSupportedMethod(method));
            Debug.Assert(isSupportedField.IsStatic && isSupportedField.FieldType.IsWellKnownType(WellKnownType.Int32));

            TargetDetails target = method.Context.Target;
            MetadataType owningType = (MetadataType)method.OwningType;

            // Check for case of nested "X64" types
            if (owningType.Name == "X64")
            {
                if (target.Architecture != TargetArchitecture.X64)
                    return null;

                // Un-nest the type so that we can do a name match
                owningType = (MetadataType)owningType.ContainingType;
            }

            int flag;
            if ((target.Architecture == TargetArchitecture.X64 || target.Architecture == TargetArchitecture.X86)
                && owningType.Namespace == "System.Runtime.Intrinsics.X86")
            {
                switch (owningType.Name)
                {
                    case "Aes":
                        flag = XArchIntrinsicConstants.Aes;
                        break;
                    case "Pclmulqdq":
                        flag = XArchIntrinsicConstants.Pclmulqdq;
                        break;
                    case "Sse3":
                        flag = XArchIntrinsicConstants.Sse3;
                        break;
                    case "Ssse3":
                        flag = XArchIntrinsicConstants.Ssse3;
                        break;
                    case "Lzcnt":
                        flag = XArchIntrinsicConstants.Lzcnt;
                        break;
                    // NOTE: this switch is complemented by IsKnownSupportedIntrinsicAtCompileTime
                    // in the method below.
                    default:
                        return null;
                }
            }
            else
            {
                return null;
            }

            var emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            codeStream.Emit(ILOpcode.ldsfld, emit.NewToken(isSupportedField));
            codeStream.EmitLdc(flag);
            codeStream.Emit(ILOpcode.and);
            codeStream.EmitLdc(0);
            codeStream.Emit(ILOpcode.cgt_un);
            codeStream.Emit(ILOpcode.ret);

            return emit.Link(method);
        }

        /// <summary>
        /// Gets a value indicating whether the support for a given intrinsic is known at compile time.
        /// </summary>
        public static bool IsKnownSupportedIntrinsicAtCompileTime(MethodDesc method)
        {
            TargetDetails target = method.Context.Target;

            if (target.Architecture == TargetArchitecture.X64
                || target.Architecture == TargetArchitecture.X86)
            {
                var owningType = (MetadataType)method.OwningType;
                if (owningType.Name == "X64")
                {
                    if (target.Architecture != TargetArchitecture.X64)
                        return true;
                    owningType = (MetadataType)owningType.ContainingType;
                }

                if (owningType.Namespace != "System.Runtime.Intrinsics.X86")
                    return true;

                // Sse and Sse2 are baseline required intrinsics.
                // RyuJIT also uses Sse41/Sse42 with the general purpose Vector APIs.
                // RyuJIT only respects Popcnt if Sse41/Sse42 is also enabled.
                // Avx/Avx2/Bmi1/Bmi2 require VEX encoding and RyuJIT currently can't enable them
                // without enabling VEX encoding everywhere. We don't support them.
                // This list complements EmitIsSupportedIL above.
                return owningType.Name == "Sse" || owningType.Name == "Sse2"
                    || owningType.Name == "Sse41" || owningType.Name == "Sse42"
                    || owningType.Name == "Popcnt"
                    || owningType.Name == "Bmi1" || owningType.Name == "Bmi2"
                    || owningType.Name == "Avx" || owningType.Name == "Avx2";
            }

            return false;
        }

        // Keep this enumeration in sync with startup.cpp in the native runtime.
        private static class XArchIntrinsicConstants
        {
            public const int Aes = 0x0001;
            public const int Pclmulqdq = 0x0002;
            public const int Sse3 = 0x0004;
            public const int Ssse3 = 0x0008;
            public const int Sse41 = 0x0010;
            public const int Sse42 = 0x0020;
            public const int Popcnt = 0x0040;
            public const int Lzcnt = 0x0080;
        }
    }
}
