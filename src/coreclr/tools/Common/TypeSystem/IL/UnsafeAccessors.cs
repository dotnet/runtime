// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
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

            if (!TryParseUnsafeAccessorAttribute(method, decodedAttribute.Value, out UnsafeAccessorKind kind, out string name))
            {
                return GenerateAccessorBadImageFailure(method);
            }

            GenerationContext context = new()
            {
                Kind = kind,
                Declaration = method
            };

            SetTargetResult result;

            result = TrySetTargetMethodSignature(ref context);
            if (result is not SetTargetResult.Success)
            {
                return GenerateAccessorSpecificFailure(ref context, name, result);
            }

            TypeDesc retType = context.DeclarationSignature.ReturnType;

            TypeDesc firstArgType = null;
            if (context.DeclarationSignature.Length > 0)
            {
                firstArgType = context.DeclarationSignature[0];
            }

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
                    result = TrySetTargetMethod(ref context, ctorName);
                    if (result is not SetTargetResult.Success)
                    {
                        return GenerateAccessorSpecificFailure(ref context, ctorName, result);
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
                    result = TrySetTargetMethod(ref context, name);
                    if (result is not SetTargetResult.Success)
                    {
                        return GenerateAccessorSpecificFailure(ref context, name, result);
                    }
                    break;

                case UnsafeAccessorKind.Field:
                case UnsafeAccessorKind.StaticField:
                    // Field access requires a single argument for target type and a return type.
                    if (context.DeclarationSignature.Length != 1 || retType.IsVoid)
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
                    result = TrySetTargetField(ref context, name, ((ParameterizedType)retType).GetParameterType());
                    if (result is not SetTargetResult.Success)
                    {
                        return GenerateAccessorSpecificFailure(ref context, name, result);
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
                    name = method.GetName();
                }
            }

            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GenerationContext
        {
            public UnsafeAccessorKind Kind;
            public EcmaMethod Declaration;
            public MethodSignature DeclarationSignature;
            public BitArray ReplacedSignatureElements;
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

            // We do not support signature variables as a target (for example, VAR and MVAR).
            if (targetType is SignatureVariable)
            {
                targetType = null;
            }

            validated = targetType;
            return validated != null;
        }

        private static bool DoesMethodMatchUnsafeAccessorDeclaration(ref GenerationContext context, MethodDesc method, bool ignoreCustomModifiers)
        {
            MethodSignature declSig = context.DeclarationSignature;
            MethodSignature maybeSig = method.Signature;

            // Check if we need to also validate custom modifiers.
            // If we are, do it first.
            if (!ignoreCustomModifiers)
            {
                // Compare any unmanaged callconv and custom modifiers on the signatures.
                // We treat unmanaged calling conventions at the same level of precedence
                // as custom modifiers, eventhough they are normally bits in a signature.
                ReadOnlySpan<EmbeddedSignatureDataKind> kinds =
                [
                    EmbeddedSignatureDataKind.UnmanagedCallConv,
                    EmbeddedSignatureDataKind.RequiredCustomModifier,
                    EmbeddedSignatureDataKind.OptionalCustomModifier
                ];

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

        private static bool VerifyDeclarationSatisfiesTargetConstraints(MethodDesc declaration, TypeDesc targetType, MethodDesc targetMethod)
        {
            Debug.Assert(declaration != null);
            Debug.Assert(targetType != null);
            Debug.Assert(targetMethod != null);

            if (targetType.HasInstantiation)
            {
                Instantiation declClassInst = declaration.OwningType.Instantiation;
                var instType = targetType.Context.GetInstantiatedType((MetadataType)targetType.GetTypeDefinition(), declClassInst);
                if (!instType.CheckConstraints())
                {
                    return false;
                }

                targetMethod = instType.FindMethodOnExactTypeWithMatchingTypicalMethod(targetMethod);
            }

            if (targetMethod.HasInstantiation)
            {
                Instantiation declMethodInst = declaration.Instantiation;
                var instMethod = targetType.Context.GetInstantiatedMethod(targetMethod, declMethodInst);
                if (!instMethod.CheckConstraints())
                {
                    return false;
                }
            }
            return true;
        }

        private enum SetTargetResult
        {
            Success,
            Missing,
            MissingType,
            Ambiguous,
            Invalid,
            NotSupported
        }

        private static SetTargetResult TrySetTargetMethod(ref GenerationContext context, string name, bool ignoreCustomModifiers = true)
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
                if (!md.GetName().Equals(name))
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
                        if (SetTargetResult.Success == TrySetTargetMethod(ref context, name, ignoreCustomModifiers: false))
                            return SetTargetResult.Success;
                    }
                    return SetTargetResult.Ambiguous;
                }

                targetMaybe = md;
            }

            if (targetMaybe != null)
            {
                if (!VerifyDeclarationSatisfiesTargetConstraints(context.Declaration, targetType, targetMaybe))
                {
                    return SetTargetResult.Invalid;
                }

                if (targetMaybe.HasInstantiation)
                {
                    TypeDesc[] methodInstantiation = new TypeDesc[targetMaybe.Instantiation.Length];
                    for (int i = 0; i < methodInstantiation.Length; ++i)
                    {
                        methodInstantiation[i] = targetMaybe.Context.GetSignatureVariable(i, true);
                    }
                    targetMaybe = targetMaybe.Context.GetInstantiatedMethod(targetMaybe, new Instantiation(methodInstantiation));
                }
                Debug.Assert(targetMaybe is not null);
            }

            context.TargetMethod = targetMaybe;
            return context.TargetMethod != null ? SetTargetResult.Success : SetTargetResult.Missing;
        }

        private static SetTargetResult TrySetTargetField(ref GenerationContext context, string name, TypeDesc fieldType)
        {
            TypeDesc targetType = context.TargetType;

            foreach (FieldDesc fd in targetType.GetFields())
            {
                if (context.IsTargetStatic != fd.IsStatic)
                {
                    continue;
                }

                // Validate the name and target type match.
                if (fd.GetName().Equals(name)
                    && fieldType == fd.FieldType)
                {
                    context.TargetField = fd;
                    return SetTargetResult.Success;
                }
            }
            return SetTargetResult.Missing;
        }

        private static bool IsValidInitialTypeForReplacementType(TypeDesc initialType, TypeDesc replacementType)
        {
            if (replacementType.IsByRef)
            {
                if (!initialType.IsByRef)
                {
                    // We can't replace a non-byref with a byref.
                    return false;
                }

                return IsValidInitialTypeForReplacementType(((ByRefType)initialType).ParameterType, ((ByRefType)replacementType).ParameterType);
            }
            else if (initialType.IsByRef)
            {
                // We can't replace a byref with a non-byref.
                return false;
            }

            if (replacementType.IsPointer)
            {
                return initialType is PointerType { ParameterType.IsVoid: true };
            }

            Debug.Assert(!replacementType.IsValueType);

            return initialType.IsObject;
        }

        private static SetTargetResult TrySetTargetMethodSignature(ref GenerationContext context)
        {
            EcmaMethod method = context.Declaration;
            MetadataReader reader = method.MetadataReader;
            MethodDefinition methodDef = reader.GetMethodDefinition(method.Handle);
            ParameterHandleCollection parameters = methodDef.GetParameters();

            MethodSignature originalSignature = method.Signature;

            MethodSignatureBuilder updatedSignature = new MethodSignatureBuilder(originalSignature);

            foreach (ParameterHandle parameterHandle in parameters)
            {
                Parameter parameter = reader.GetParameter(parameterHandle);

                if (parameter.SequenceNumber > originalSignature.Length)
                {
                    // This is invalid metadata (parameter metadata for a parameter that doesn't exist in the signature).
                    return SetTargetResult.Invalid;
                }

                CustomAttributeHandle unsafeAccessorTypeAttributeHandle = FindUnsafeAccessorTypeAttribute(reader, parameter);

                if (unsafeAccessorTypeAttributeHandle.IsNil)
                {
                    continue;
                }

                bool isReturnValue = parameter.SequenceNumber == 0;

                TypeDesc initialType = isReturnValue ? originalSignature.ReturnType : originalSignature[parameter.SequenceNumber - 1];

                if (isReturnValue && initialType.IsByRef)
                {
                    // We can't support UnsafeAccessorTypeAttribute on by-ref returns
                    // today as it would create a type-safety hole.
                    return SetTargetResult.NotSupported;
                }

                SetTargetResult decodeResult = DecodeUnsafeAccessorType(method, reader.GetCustomAttribute(unsafeAccessorTypeAttributeHandle), out TypeDesc replacementType);
                if (decodeResult != SetTargetResult.Success)
                {
                    return decodeResult;
                }

                // Future versions of the runtime may support
                // UnsafeAccessorTypeAttribute on value types.
                if (replacementType.IsValueType)
                {
                    return SetTargetResult.NotSupported;
                }

                if (!IsValidInitialTypeForReplacementType(initialType, replacementType))
                {
                    return SetTargetResult.Invalid;
                }

                context.ReplacedSignatureElements ??= new BitArray(originalSignature.Length + 1, false);
                context.ReplacedSignatureElements[parameter.SequenceNumber] = true;

                if (isReturnValue)
                {
                    updatedSignature.ReturnType = replacementType;
                }
                else
                {
                    updatedSignature[parameter.SequenceNumber - 1] = replacementType;
                }
            }

            context.DeclarationSignature = updatedSignature.ToSignature();
            return SetTargetResult.Success;
        }

        private static SetTargetResult DecodeUnsafeAccessorType(EcmaMethod method, CustomAttribute unsafeAccessorTypeAttribute, out TypeDesc replacementType)
        {
            replacementType = null;
            CustomAttributeValue<TypeDesc> decoded = unsafeAccessorTypeAttribute.DecodeValue(
                new CustomAttributeTypeProvider(method.Module));

            if (decoded.FixedArguments[0].Value is not string replacementTypeName)
            {
                return SetTargetResult.Invalid;
            }

            replacementType = method.Module.GetTypeByCustomAttributeTypeName(
                replacementTypeName,
                throwIfNotFound: false,
                canonGenericResolver: (module, name) =>
                {
                    if (!name.StartsWith('!'))
                    {
                        return null;
                    }

                    bool isMethodParameter = name.StartsWith("!!", StringComparison.Ordinal);

                    if (!int.TryParse(name.AsSpan(isMethodParameter ? 2 : 1), NumberStyles.None, CultureInfo.InvariantCulture, out int index))
                    {
                        return null;
                    }

                    if (isMethodParameter)
                    {
                        if (index >= method.Instantiation.Length)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        if (index >= method.OwningType.Instantiation.Length)
                        {
                            return null;
                        }
                    }

                    return module.Context.GetSignatureVariable(index, isMethodParameter);
                });

            return replacementType is null
                ? SetTargetResult.MissingType
                : SetTargetResult.Success;
        }

        private static CustomAttributeHandle FindUnsafeAccessorTypeAttribute(MetadataReader reader, Parameter parameter)
        {
            foreach (CustomAttributeHandle customAttributeHandle in parameter.GetCustomAttributes())
            {
                reader.GetAttributeNamespaceAndName(customAttributeHandle, out StringHandle namespaceName, out StringHandle name);
                if (reader.StringComparer.Equals(namespaceName, "System.Runtime.CompilerServices")
                    && reader.StringComparer.Equals(name, "UnsafeAccessorTypeAttribute"))
                {
                    return customAttributeHandle;
                }
            }

            return default;
        }

        private static ParameterHandle FindParameterForSequenceNumber(MetadataReader reader, ref ParameterHandleCollection.Enumerator parameterEnumerator, int sequenceNumber)
        {
            Parameter currentParameter = reader.GetParameter(parameterEnumerator.Current);
            if (currentParameter.SequenceNumber == sequenceNumber)
            {
                return parameterEnumerator.Current;
            }

            // Scan until we are either at this parameter or at the first one after it (if there is no Parameter row in the table)
            while (parameterEnumerator.MoveNext())
            {
                Parameter thisParameterMaybe = reader.GetParameter(parameterEnumerator.Current);
                if (thisParameterMaybe.SequenceNumber > sequenceNumber)
                {
                    // We've passed where it should be.
                    return default;
                }

                if (thisParameterMaybe.SequenceNumber == sequenceNumber)
                {
                    // We found it.
                    return parameterEnumerator.Current;
                }
            }

            return default;
        }

        private static MethodIL GenerateAccessor(ref GenerationContext context)
        {
            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            MetadataReader reader = context.Declaration.MetadataReader;
            ParameterHandleCollection.Enumerator parameterEnumerator = reader.GetMethodDefinition(context.Declaration.Handle).GetParameters().GetEnumerator();
            parameterEnumerator.MoveNext();

            // Load stub arguments.
            // When the target is static, the first argument is only
            // used to look up the target member to access and ignored
            // during dispatch.
            int beginIndex = context.IsTargetStatic ? 1 : 0;
            int stubArgCount = context.DeclarationSignature.Length;
            Stubs.ILLocalVariable?[] localsToRestore = null;

            for (int i = beginIndex; i < stubArgCount; ++i)
            {
                codeStream.EmitLdArg(i);
                if (context.ReplacedSignatureElements?[i + 1] == true)
                {
                    if (context.DeclarationSignature[i] is { Category: TypeFlags.Class } classType)
                    {
                        codeStream.Emit(ILOpcode.unbox_any, emit.NewToken(classType));
                    }
                    else if (context.DeclarationSignature[i] is ByRefType { ParameterType.Category: TypeFlags.Class } byrefType)
                    {
                        localsToRestore ??= new Stubs.ILLocalVariable?[stubArgCount];

                        TypeDesc targetType = byrefType.ParameterType;
                        Stubs.ILLocalVariable local = emit.NewLocal(targetType);
                        codeStream.EmitLdInd(targetType);
                        codeStream.Emit(ILOpcode.unbox_any, emit.NewToken(targetType));
                        codeStream.EmitStLoc(local);
                        codeStream.EmitLdLoca(local);

                        // Only mark the local to be restored after the call
                        // if the parameter is not marked as "in".
                        // The "sequence number" for parameters is 1-based, whereas the parameter index is 0-based.
                        ParameterHandle paramHandle = FindParameterForSequenceNumber(reader, ref parameterEnumerator, i + 1);
                        if (paramHandle.IsNil
                            || !reader.GetParameter(paramHandle).Attributes.HasFlag(ParameterAttributes.In))
                        {
                            localsToRestore[i] = local;
                        }
                    }
                }
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

            if (localsToRestore is not null)
            {
                for (int i = beginIndex; i < stubArgCount; ++i)
                {
                    if (localsToRestore[i] != null)
                    {
                        codeStream.EmitLdArg(i);
                        codeStream.EmitLdLoc(localsToRestore[i].Value);
                        codeStream.EmitStInd(((ParameterizedType)context.Declaration.Signature[i]).ParameterType);
                    }
                }
            }

            // Return from the generated stub
            codeStream.Emit(ILOpcode.ret);
            return emit.Link(context.Declaration);
        }

        private static MethodIL GenerateAccessorSpecificFailure(ref GenerationContext context, string name, SetTargetResult result)
        {
            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            ILCodeLabel label = emit.NewCodeLabel();
            codeStream.EmitLabel(label);

            MethodDesc thrower;
            TypeSystemContext typeSysContext = context.Declaration.Context;
            if (result is SetTargetResult.Ambiguous)
            {
                codeStream.EmitLdc((int)ExceptionStringID.AmbiguousMatchUnsafeAccessor);
                thrower = typeSysContext.GetHelperEntryPoint("ThrowHelpers", "ThrowAmbiguousMatchException");
            }
            else if (result is SetTargetResult.Invalid)
            {
                codeStream.EmitLdc((int)ExceptionStringID.InvalidProgramDefault);
                thrower = typeSysContext.GetHelperEntryPoint("ThrowHelpers", "ThrowInvalidProgramException");
            }
            else if (result is SetTargetResult.NotSupported)
            {
                thrower = typeSysContext.GetHelperEntryPoint("ThrowHelpers", "ThrowNotSupportedException");
            }
            else if (result is SetTargetResult.MissingType)
            {
                thrower = typeSysContext.GetHelperEntryPoint("ThrowHelpers", "ThrowUnavailableType");
            }
            else
            {
                Debug.Assert(result is SetTargetResult.Missing);
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
