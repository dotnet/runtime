// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Internal.IL
{
    public sealed class UnsafeAccessors
    {
        private const string InvalidUnsafeAccessorUsage = "Invalid usage of UnsafeAccessorAttribute.";

        public static MethodIL TryGetIL(EcmaMethod method)
        {
            Debug.Assert(method != null);

            CustomAttributeValue<TypeDesc>? decodedAttribute = method.GetDecodedCustomAttribute("System.Runtime.CompilerServices", "UnsafeAccessorAttribute");
            if (!decodedAttribute.HasValue)
            {
                return null;
            }

            if (!TryParseUnsafeAccessorAttribute(method, decodedAttribute.Value, out UnsafeAccessorKind kind, out string name))
            {
                ThrowHelper.ThrowBadImageFormatException(InvalidUnsafeAccessorUsage);
            }

            GenerationContext context = new()
            {
                Kind = kind,
                Declaration = method
            };

            MethodSignature sig = method.Signature;
            TypeDesc retType = sig.ReturnType;
            TypeDesc firstArgType = null;
            if (sig.Length > 0)
            {
                firstArgType = sig[0];
            }

            // Using the kind type, perform the following:
            //  1) Validate the basic type information from the signature.
            //  2) Resolve the name to the appropriate member.
            switch (kind)
            {
                case UnsafeAccessorKind.Constructor:
                    // A return type is required for a constructor, otherwise
                    // we don't know the type to construct.
                    // The name is defined by the runtime and should be empty.
                    if (sig.ReturnType.IsVoid || !string.IsNullOrEmpty(name))
                    {
                        ThrowHelper.ThrowBadImageFormatException(InvalidUnsafeAccessorUsage);
                    }

                    context.TargetType = retType;
                    if (!TrySetTargetMethodCtor(ref context))
                    {
                        ThrowHelper.ThrowMissingMethodException(retType, ".ctor", null);
                    }
                    break;
                case UnsafeAccessorKind.Method:
                case UnsafeAccessorKind.StaticMethod:
                    // Method access requires a target type.
                    if (firstArgType == null)
                    {
                        ThrowHelper.ThrowBadImageFormatException(InvalidUnsafeAccessorUsage);
                    }

                    context.TargetType = firstArgType;
                    context.IsTargetStatic = kind == UnsafeAccessorKind.StaticMethod;
                    if (!TrySetTargetMethod(ref context, name))
                    {
                        ThrowHelper.ThrowMissingMethodException(firstArgType, name, null);
                    }
                    break;

                case UnsafeAccessorKind.Field:
                case UnsafeAccessorKind.StaticField:
                    // Field access requires a single argument for target type and a return type.
                    if (sig.Length != 1 || sig.ReturnType.IsVoid)
                    {
                        ThrowHelper.ThrowBadImageFormatException(InvalidUnsafeAccessorUsage);
                    }

                    // The return type must be byref.
                    // If the non-static field access is for a
                    // value type, the instance must be byref.
                    if (!sig.ReturnType.IsByRef
                        || (kind == UnsafeAccessorKind.Field
                            && firstArgType.IsValueType
                            && !firstArgType.IsByRef))
                    {
                        ThrowHelper.ThrowBadImageFormatException(InvalidUnsafeAccessorUsage);
                    }

                    context.TargetType = firstArgType;
                    context.IsTargetStatic = kind == UnsafeAccessorKind.StaticField;
                    if (!TrySetTargetField(ref context, name, retType))
                    {
                        ThrowHelper.ThrowMissingFieldException(sig.ReturnType, name);
                    }
                    break;

                default:
                    ThrowHelper.ThrowBadImageFormatException(InvalidUnsafeAccessorUsage);
                    break;
            }

            // Generate the IL for the accessor.
            return GenerateAccessor(ref context);
        }

        // This is a redeclaration of the new type in the .NET 8 TFM.
        private enum UnsafeAccessorKind
        {
            Constructor,
            Method,
            StaticMethod,
            Field,
            StaticField
        };

        private static bool TryParseUnsafeAccessorAttribute(MethodDesc method, CustomAttributeValue<TypeDesc> decodedValue, out UnsafeAccessorKind kind, out string name)
        {
            kind = default;
            name = default;

            var context = method.Context;

            // Get the kind of accessor
            if (decodedValue.FixedArguments.Length != 1
                || decodedValue.FixedArguments[0].Type.UnderlyingType != context.GetWellKnownType(WellKnownType.Int32))
            {
                return false;
            }

            kind = (UnsafeAccessorKind)decodedValue.FixedArguments[0].Value!;

            // Check the name of the target to access. This is the name we
            // use to look up the intended token in metadata.
            string nameMaybe = null;
            foreach (var argument in decodedValue.NamedArguments)
            {
                if (argument.Name == "Name")
                {
                    nameMaybe = (string)argument.Value;
                }
            }

            // If the Name isn't defined, then use the name of the method.
            if (nameMaybe != null)
            {
                name = nameMaybe;
            }
            else
            {
                // The Constructor case has an implied value provided by
                // the runtime. We are going to enforce this during consumption
                // so we avoid the setting of the value. We validate the name
                // as empty at the use site.
                if (kind is not UnsafeAccessorKind.Constructor)
                {
                    name = nameMaybe;
                }
            }

            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GenerationContext
        {
            public UnsafeAccessorKind Kind;
            public EcmaMethod Declaration;
            public TypeDesc TargetType;
            public bool IsTargetStatic;
            public MethodDesc TargetMethod;
            public FieldDesc TargetField;
        }

        private static bool TrySetTargetMethod(ref GenerationContext context, string name)
        {
            TypeDesc targetType = context.TargetType.IsByRef
                ? ((ParameterizedType)context.TargetType).ParameterType
                : context.TargetType;

            // Due to how some types degrade, we block on parameterized
            // types that are represented as TypeDesc. For example ref or pointer.
            if ((targetType.IsParameterizedType && !targetType.IsArray)
                || targetType.IsFunctionPointerType)
            {
                ThrowHelper.ThrowBadImageFormatException(InvalidUnsafeAccessorUsage);
            }

            TypeDesc declTypeDesc;
            TypeDesc maybeTypeDesc;
            foreach (MethodDesc md in targetType.GetMethods())
            {
                // Check the target and current method match static/instance state.
                if (context.IsTargetStatic != md.Signature.IsStatic)
                {
                    continue;
                }

                // Check for matching name
                if (!md.Name.Equals(name))
                {
                    continue;
                }

                // Validate calling convention.
                if ((MethodSignatureFlags.UnmanagedCallingConventionMask & md.Signature.Flags)
                    != (MethodSignatureFlags.UnmanagedCallingConventionMask & context.Declaration.Signature.Flags))
                {
                    continue;
                }

                // Validate the return type and prepare for validating
                // the current signature's argument list.
                int sigCount = context.Declaration.Signature.Length;
                if (context.Kind == UnsafeAccessorKind.Constructor)
                {
                    if (!md.Signature.ReturnType.IsVoid)
                    {
                        continue;
                    }
                }
                else
                {
                    declTypeDesc = context.Declaration.Signature.ReturnType;
                    maybeTypeDesc = md.Signature.ReturnType;
                    if (declTypeDesc != maybeTypeDesc)
                    {
                        continue;
                    }

                    // Non-constructor accessors skip the first argument
                    // when validating the target argument list.
                    sigCount--;
                }

                // Validate argument count matches.
                if (sigCount != md.Signature.Length)
                {
                    continue;
                }

                // Validate arguments match - reverse order
                for (; sigCount > 0; --sigCount)
                {
                    declTypeDesc = context.Declaration.Signature[sigCount];
                    maybeTypeDesc = md.Signature[sigCount];
                    if (declTypeDesc != maybeTypeDesc)
                    {
                        break;
                    }
                }

                // If we validated all arguments, we have a match.
                if (sigCount != 0)
                {
                    continue;
                }

                context.TargetMethod = md;
                return true;
            }
            return false;
        }

        private static bool TrySetTargetMethodCtor(ref GenerationContext context)
        {
            // Special case the default constructor case.
            if (context.Declaration.Signature.Length == 0
                && context.TargetType.HasExplicitOrImplicitDefaultConstructor())
            {
                context.TargetMethod = context.TargetType.GetDefaultConstructor();
                return true;
            }

            // Defer to the normal method look up for
            // cases beyond the default constructor.
            return TrySetTargetMethod(ref context, ".ctor");
        }

        private static bool TrySetTargetField(ref GenerationContext context, string name, TypeDesc fieldType)
        {
            TypeDesc targetType = context.TargetType.IsByRef
                ? ((ParameterizedType)context.TargetType).ParameterType
                : context.TargetType;

            foreach (FieldDesc fd in targetType.GetFields())
            {
                if (context.IsTargetStatic != fd.IsStatic)
                {
                    continue;
                }

                // Validate the name and target type match.
                if (fd.Name.Equals(name)
                    && fieldType == fd.FieldType)
                {
                    context.TargetField = fd;
                    return true;
                }
            }
            return false;
        }

        private static MethodIL GenerateAccessor(ref GenerationContext context)
        {
            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            // Load stub arguments.
            // When the target is static, the first argument is only
            // used to look up the target member to access and ignored
            // during dispatch.
            int beginIndex = context.IsTargetStatic ? 1 : 0;
            int stubArgCount = context.Declaration.Signature.Length;
            for (int i = beginIndex; i < stubArgCount; ++i)
            {
                codeStream.EmitLdArg(i);
            }

            // Provide access to the target member
            switch (context.Kind)
            {
                case UnsafeAccessorKind.Constructor:
                    Debug.Assert(context.TargetMethod != null);
                    codeStream.Emit(ILOpcode.newobj, emit.NewToken(context.TargetMethod));
                    break;
                case UnsafeAccessorKind.Method:
                    Debug.Assert(context.TargetMethod != null);
                    codeStream.Emit(ILOpcode.callvirt, emit.NewToken(context.TargetMethod));
                    break;
                case UnsafeAccessorKind.StaticMethod:
                    Debug.Assert(context.TargetMethod != null);
                    codeStream.Emit(ILOpcode.call, emit.NewToken(context.TargetMethod));
                    break;
                case UnsafeAccessorKind.Field:
                    Debug.Assert(context.TargetField != null);
                    codeStream.Emit(ILOpcode.ldflda, emit.NewToken(context.TargetField));
                    break;
                case UnsafeAccessorKind.StaticField:
                    Debug.Assert(context.TargetField != null);
                    codeStream.Emit(ILOpcode.ldsflda, emit.NewToken(context.TargetField));
                    break;
                default:
                    Debug.Fail("Unknown UnsafeAccessorKind");
                    break;
            }

            // Return from the generated stub
            codeStream.Emit(ILOpcode.ret);
            return emit.Link(context.TargetMethod);
        }
    }
}
