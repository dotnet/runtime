// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Internal.IL
{
    public sealed class UnsafeAccessors
    {
        public static MethodIL TryGetIL(EcmaMethod method)
        {
            Debug.Assert(method != null);
            CustomAttributeValue<TypeDesc>? decodedAttribute = method.GetDecodedCustomAttribute("System.Runtime.CompilerServices", "UnsafeAccessorAttribute");
            if (!decodedAttribute.HasValue)
            {
                return null;
            }

            // UnsafeAccessor must be on a static method
            if (!method.Signature.IsStatic)
            {
                return GenerateAccessorBadImageFailure(method);
            }

            // Block generic support early
            if (method.HasInstantiation || method.OwningType.HasInstantiation)
            {
                return GenerateAccessorBadImageFailure(method);
            }

            if (!TryParseUnsafeAccessorAttribute(method, decodedAttribute.Value, out UnsafeAccessorKind kind, out string name))
            {
                return GenerateAccessorBadImageFailure(method);
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

            bool isAmbiguous = false;

            // Using the kind type, perform the following:
            //  1) Validate the basic type information from the signature.
            //  2) Resolve the name to the appropriate member.
            switch (kind)
            {
                case UnsafeAccessorKind.Constructor:
                    // A return type is required for a constructor, otherwise
                    // we don't know the type to construct.
                    // Types should not be parameterized (that is, byref).
                    // The name is defined by the runtime and should be empty.
                    if (retType.IsVoid || retType.IsByRef || !string.IsNullOrEmpty(name))
                    {
                        return GenerateAccessorBadImageFailure(method);
                    }

                    if (!ValidateTargetType(retType, out context.TargetType))
                    {
                        return GenerateAccessorBadImageFailure(method);
                    }

                    const string ctorName = ".ctor";
                    if (!TrySetTargetMethod(ref context, ctorName, out isAmbiguous))
                    {
                        return GenerateAccessorSpecificFailure(ref context, ctorName, isAmbiguous);
                    }
                    break;
                case UnsafeAccessorKind.Method:
                case UnsafeAccessorKind.StaticMethod:
                    // Method access requires a target type.
                    if (firstArgType == null)
                    {
                        return GenerateAccessorBadImageFailure(method);
                    }

                    // If the non-static method access is for a
                    // value type, the instance must be byref.
                    if (kind == UnsafeAccessorKind.Method
                        && firstArgType.IsValueType
                        && !firstArgType.IsByRef)
                    {
                        return GenerateAccessorBadImageFailure(method);
                    }

                    if (!ValidateTargetType(firstArgType, out context.TargetType))
                    {
                        return GenerateAccessorBadImageFailure(method);
                    }

                    context.IsTargetStatic = kind == UnsafeAccessorKind.StaticMethod;
                    if (!TrySetTargetMethod(ref context, name, out isAmbiguous))
                    {
                        return GenerateAccessorSpecificFailure(ref context, name, isAmbiguous);
                    }
                    break;

                case UnsafeAccessorKind.Field:
                case UnsafeAccessorKind.StaticField:
                    // Field access requires a single argument for target type and a return type.
                    if (sig.Length != 1 || retType.IsVoid)
                    {
                        return GenerateAccessorBadImageFailure(method);
                    }

                    // The return type must be byref.
                    // If the non-static field access is for a
                    // value type, the instance must be byref.
                    if (!retType.IsByRef
                        || (kind == UnsafeAccessorKind.Field
                            && firstArgType.IsValueType
                            && !firstArgType.IsByRef))
                    {
                        return GenerateAccessorBadImageFailure(method);
                    }

                    if (!ValidateTargetType(firstArgType, out context.TargetType))
                    {
                        return GenerateAccessorBadImageFailure(method);
                    }

                    context.IsTargetStatic = kind == UnsafeAccessorKind.StaticField;
                    if (!TrySetTargetField(ref context, name, ((ParameterizedType)retType).GetParameterType()))
                    {
                        return GenerateAccessorSpecificFailure(ref context, name, isAmbiguous);
                    }
                    break;

                default:
                    return GenerateAccessorBadImageFailure(method);
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
                    name = method.Name;
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

        private static bool ValidateTargetType(TypeDesc targetTypeMaybe, out TypeDesc validated)
        {
            TypeDesc targetType = targetTypeMaybe.IsByRef
                ? ((ParameterizedType)targetTypeMaybe).ParameterType
                : targetTypeMaybe;

            // Due to how some types degrade, we block on parameterized
            // types. For example ref or pointer.
            if ((targetType.IsParameterizedType && !targetType.IsArray)
                || targetType.IsFunctionPointer)
            {
                targetType = null;
            }

            validated = targetType;
            return validated != null;
        }

        private static bool DoesMethodMatchUnsafeAccessorDeclaration(ref GenerationContext context, MethodDesc method, bool ignoreCustomModifiers)
        {
            MethodSignature declSig = context.Declaration.Signature;
            MethodSignature maybeSig = method.Signature;

            // Check if we need to also validate custom modifiers.
            // If we are, do it first.
            if (!ignoreCustomModifiers)
            {
                // Compare any unmanaged callconv and custom modifiers on the signatures.
                // We treat unmanaged calling conventions at the same level of precedance
                // as custom modifiers, eventhough they are normally bits in a signature.
                ReadOnlySpan<EmbeddedSignatureDataKind> kinds = new EmbeddedSignatureDataKind[]
                {
                    EmbeddedSignatureDataKind.UnmanagedCallConv,
                    EmbeddedSignatureDataKind.RequiredCustomModifier,
                    EmbeddedSignatureDataKind.OptionalCustomModifier
                };

                var declData = declSig.GetEmbeddedSignatureData(kinds) ?? Array.Empty<EmbeddedSignatureData>();
                var maybeData = maybeSig.GetEmbeddedSignatureData(kinds) ?? Array.Empty<EmbeddedSignatureData>();
                if (declData.Length != maybeData.Length)
                {
                    return false;
                }

                // Validate the custom modifiers match precisely.
                for (int i = 0; i < declData.Length; ++i)
                {
                    EmbeddedSignatureData dd = declData[i];
                    EmbeddedSignatureData md = maybeData[i];
                    if (dd.kind != md.kind || dd.type != md.type)
                    {
                        return false;
                    }

                    // The indices on non-constructor declarations require
                    // some slight modification since there is always an extra
                    // argument in the declaration compared to the target.
                    string declIndex = dd.index;
                    if (context.Kind != UnsafeAccessorKind.Constructor)
                    {
                        string unmanagedCallConvMaybe = string.Empty;

                        // Check for and drop the unmanaged calling convention
                        // value suffix to add it back after updating below.
                        if (declIndex.Contains('|'))
                        {
                            Debug.Assert(dd.kind == EmbeddedSignatureDataKind.UnmanagedCallConv);
                            var tmp = declIndex.Split('|');
                            Debug.Assert(tmp.Length == 2);
                            declIndex = tmp[0];
                            unmanagedCallConvMaybe = "|" + tmp[1];
                        }

                        // Decrement the second to last index by one to
                        // account for the difference in declarations.
                        string[] lvls = declIndex.Split('.');
                        int toUpdate = lvls.Length < 2 ? 0 : lvls.Length - 2;
                        int idx = int.Parse(lvls[toUpdate], CultureInfo.InvariantCulture);
                        idx--;
                        lvls[toUpdate] = idx.ToString();
                        declIndex = string.Join(".", lvls) + unmanagedCallConvMaybe;
                    }

                    if (declIndex != md.index)
                    {
                        return false;
                    }
                }
            }

            // Validate calling convention of declaration.
            if ((declSig.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask)
                != (maybeSig.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask))
            {
                return false;
            }

            // Validate argument count and return type
            if (context.Kind == UnsafeAccessorKind.Constructor)
            {
                // Declarations for constructor scenarios have
                // matching argument counts with the target.
                if (declSig.Length != maybeSig.Length)
                {
                    return false;
                }

                // Validate the return value for target constructor
                // candidate is void.
                if (!maybeSig.ReturnType.IsVoid)
                {
                    return false;
                }
            }
            else
            {
                // Declarations of non-constructor scenarios have
                // an additional argument to indicate target type
                // and to pass an instance for non-static methods.
                if (declSig.Length != (maybeSig.Length + 1))
                {
                    return false;
                }

                if (declSig.ReturnType != maybeSig.ReturnType)
                {
                    return false;
                }
            }

            // Validate argument types
            for (int i = 0; i < maybeSig.Length; ++i)
            {
                // Skip over first argument (index 0) on non-constructor accessors.
                // See argument count validation above.
                TypeDesc declType = context.Kind == UnsafeAccessorKind.Constructor ? declSig[i] : declSig[i + 1];
                TypeDesc maybeType = maybeSig[i];

                // Compare the types
                if (declType != maybeType)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TrySetTargetMethod(ref GenerationContext context, string name, out bool isAmbiguous, bool ignoreCustomModifiers = true)
        {
            TypeDesc targetType = context.TargetType;

            MethodDesc targetMaybe = null;
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

                // Check signature
                if (!DoesMethodMatchUnsafeAccessorDeclaration(ref context, md, ignoreCustomModifiers))
                {
                    continue;
                }

                // Check if there is some ambiguity.
                if (targetMaybe != null)
                {
                    if (ignoreCustomModifiers)
                    {
                        // We have detected ambiguity when ignoring custom modifiers.
                        // Start over, but look for a match requiring custom modifiers
                        // to match precisely.
                        if (TrySetTargetMethod(ref context, name, out isAmbiguous, ignoreCustomModifiers: false))
                            return true;
                    }

                    isAmbiguous = true;
                    return false;
                }

                targetMaybe = md;
            }

            isAmbiguous = false;
            context.TargetMethod = targetMaybe;
            return context.TargetMethod != null;
        }

        private static bool TrySetTargetField(ref GenerationContext context, string name, TypeDesc fieldType)
        {
            TypeDesc targetType = context.TargetType;

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
            return emit.Link(context.Declaration);
        }

        private static MethodIL GenerateAccessorSpecificFailure(ref GenerationContext context, string name, bool ambiguous)
        {
            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            ILCodeLabel label = emit.NewCodeLabel();
            codeStream.EmitLabel(label);

            MethodDesc thrower;
            TypeSystemContext typeSysContext = context.Declaration.Context;
            if (ambiguous)
            {
                codeStream.EmitLdc((int)ExceptionStringID.AmbiguousMatchUnsafeAccessor);
                thrower = typeSysContext.GetHelperEntryPoint("ThrowHelpers", "ThrowAmbiguousMatchException");
            }
            else
            {

                ExceptionStringID id;
                if (context.Kind == UnsafeAccessorKind.Field || context.Kind == UnsafeAccessorKind.StaticField)
                {
                    id = ExceptionStringID.MissingField;
                    thrower = typeSysContext.GetHelperEntryPoint("ThrowHelpers", "ThrowMissingFieldException");
                }
                else
                {
                    id = ExceptionStringID.MissingMethod;
                    thrower = typeSysContext.GetHelperEntryPoint("ThrowHelpers", "ThrowMissingMethodException");
                }

                codeStream.EmitLdc((int)id);
                codeStream.Emit(ILOpcode.ldstr, emit.NewToken(name));
            }

            Debug.Assert(thrower != null);
            codeStream.Emit(ILOpcode.call, emit.NewToken(thrower));
            codeStream.Emit(ILOpcode.br, label);
            return emit.Link(context.Declaration);
        }

        private static MethodIL GenerateAccessorBadImageFailure(MethodDesc method)
        {
            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            ILCodeLabel label = emit.NewCodeLabel();
            codeStream.EmitLabel(label);
            codeStream.EmitLdc((int)ExceptionStringID.BadImageFormatGeneric);
            MethodDesc thrower = method.Context.GetHelperEntryPoint("ThrowHelpers", "ThrowBadImageFormatException");
            codeStream.Emit(ILOpcode.call, emit.NewToken(thrower));
            codeStream.Emit(ILOpcode.br, label);

            return emit.Link(method);
        }
    }
}
