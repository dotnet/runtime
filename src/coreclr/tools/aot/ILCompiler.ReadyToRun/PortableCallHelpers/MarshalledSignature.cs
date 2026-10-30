// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.TypeSystem.Interop;

namespace ILCompiler.PortableCallHelpers
{
    /// <summary>
    /// Computes the wasm signature string of the native call an interop IL stub makes, which differs
    /// from the managed signature wherever the runtime marshals an argument.
    /// </summary>
    /// <remarks>
    /// The runtime builds the thunk key from the IL stub's native signature (GetSignatureKey in
    /// vm/wasm/helpers.cpp). A non-blittable struct crosses as its native layout, so its size,
    /// alignment, and single-scalar classification are computed here with the same rules as the
    /// runtime's native layout (vm/classlayoutinfo.cpp, vm/fieldmarshaler.cpp). Tokens marshalling
    /// leaves unchanged are taken from <see cref="WasmLowering"/>, so blittable signatures keep their
    /// existing keys.
    /// </remarks>
    internal static class MarshalledSignature
    {
        // vm/fieldmarshaler.h DEFAULT_PACKING_SIZE
        private const int DefaultPackingSize = 64;


        /// <param name="isApproximate">
        /// Set when a marshalled struct's native layout could not be modeled, so the managed token
        /// was kept for it and the runtime may ask for a thunk this signature does not describe.
        /// </param>
        /// <param name="marshalledDelegates">
        /// Receives the delegate types the signature marshals back from native code, directly or through
        /// struct fields. Each is wrapped by an IL stub that needs its own thunk.
        /// </param>
        public static string GetPInvokeSignature(MethodDesc method, out bool isApproximate, ICollection<TypeDesc> marshalledDelegates = null)
        {
            isApproximate = false;
            string managedSignature = InteropSignature.GetMethodSignature(method, WasmLowering.LoweringFlags.IsUnmanagedCallersOnly);
            if (!MarshalHelpers.IsRuntimeMarshallingEnabled(((MetadataType)method.OwningType).Module))
                return managedSignature;

            return Lower(managedSignature,
                () => Marshaller.GetMarshallersForMethod(method),
                method.GetPInvokeMethodMetadata().Flags.PreserveSig,
                marshalledDelegates,
                out isApproximate);
        }

        public static string GetDelegateSignature(MethodDesc invokeMethod, out bool isApproximate, ICollection<TypeDesc> marshalledDelegates = null)
        {
            isApproximate = false;
            string managedSignature = InteropSignature.GetMethodSignature(invokeMethod, WasmLowering.LoweringFlags.IsUnmanagedCallersOnly);
            var delegateType = (EcmaType)invokeMethod.OwningType;
            if (!MarshalHelpers.IsRuntimeMarshallingEnabled(delegateType.Module))
                return managedSignature;

            PInvokeFlags flags = delegateType.GetDelegatePInvokeFlags();
            return Lower(managedSignature,
                () => Marshaller.GetMarshallersForSignature(invokeMethod.Signature, flags, invokeMethod.GetParameterMetadata(), delegateType.Module),
                flags.PreserveSig,
                marshalledDelegates,
                out isApproximate);
        }

        public static string GetCalliSignature(MethodSignature signature, ModuleDesc module, out bool isApproximate, ICollection<TypeDesc> marshalledDelegates = null)
        {
            isApproximate = false;
            string managedSignature = WasmLowering.GetSignature(signature, WasmLowering.LoweringFlags.None).SignatureString;
            if (!MarshalHelpers.IsRuntimeMarshallingEnabled(module))
                return managedSignature;

            return Lower(managedSignature,
                () => Marshaller.GetMarshallersForSignature(signature, Array.Empty<ParameterMetadata>(), module),
                preserveSig: true,
                marshalledDelegates,
                out isApproximate);
        }

        private static string Lower(string managedSignature, Func<Marshaller[]> getMarshallers, bool preserveSig, ICollection<TypeDesc> marshalledDelegates, out bool isApproximate)
        {
            isApproximate = false;
            Marshaller[] marshallers;
            try
            {
                marshallers = getMarshallers();
            }
            catch (TypeSystemException)
            {
                // The IL stub fails the same way at run time, before it needs a thunk.
                return managedSignature;
            }

            List<string> tokens = InteropSignature.ParseSignatureTokens(managedSignature);
            Debug.Assert(tokens.Count == marshallers.Length, $"Unexpected tokens in unmanaged signature '{managedSignature}'");

            bool changed = false;
            for (int i = 0; i < marshallers.Length; i++)
            {
                string token = GetNativeToken(marshallers[i], tokens[i], isReturn: i == 0, marshalledDelegates, ref isApproximate);
                if (token != tokens[i])
                {
                    tokens[i] = token;
                    changed = true;
                }
            }

            if (!preserveSig)
            {
                // The native method returns an HRESULT, and a non-void return value moves to a
                // trailing pointer parameter.
                if (tokens[0] != "v")
                    tokens.Add("i");
                tokens[0] = "i";
                changed = true;
            }

            return changed ? string.Concat(tokens) : managedSignature;
        }

        private static string GetNativeToken(Marshaller marshaller, string managedToken, bool isReturn, ICollection<TypeDesc> marshalledDelegates, ref bool isApproximate)
        {
            // Only a delegate that comes back from native code is wrapped over a native function
            // pointer. One going the other way needs Marshal.GetFunctionPointerForDelegate instead.
            if (!marshaller.Return && !marshaller.Out && !marshaller.IsManagedByRef)
                marshalledDelegates = null;

            switch (marshaller.MarshallerKind)
            {
                case MarshallerKind.FunctionPointer when marshaller.ManagedType.IsDelegate:
                    marshalledDelegates?.Add(marshaller.ManagedType);
                    break;

                case MarshallerKind.Struct when marshaller.IsNativeByRef:
                    // Only the delegates its fields marshal matter; the struct itself crosses as a pointer.
                    TryGetStructShape((MetadataType)marshaller.ManagedType, depth: 0, marshalledDelegates, out _);
                    break;
            }

            if (marshaller.IsNativeByRef || IsPassedAsPointer(marshaller.ManagedType))
                return "i";

            switch (marshaller.MarshallerKind)
            {
                case MarshallerKind.Invalid:
                case MarshallerKind.Unknown:
                    // The IL stub throws a MarshalDirectiveException instead of making the call.
                    return managedToken;

                case MarshallerKind.BlittableStruct when !IsBlittableInRuntime((MetadataType)marshaller.ManagedType):
                case MarshallerKind.Struct:
                    if (TryGetStructShape((MetadataType)marshaller.ManagedType, depth: 0, marshalledDelegates, out NativeShape shape))
                        return shape.ToToken(isReturn);

                    isApproximate = true;
                    return managedToken;
            }

            TypeDesc nativeType;
            try
            {
                nativeType = marshaller.NativeType;
            }
            catch (NotSupportedException)
            {
                return managedToken;
            }

            return nativeType == marshaller.ManagedType ? managedToken : GetTypeShape(nativeType).ToToken(isReturn);
        }

        private readonly record struct NativeShape(int Size, int Alignment, string ScalarToken)
        {
            public string ToToken(bool isReturn)
            {
                // Struct returns always use the hidden buffer form, including multi-slot types.
                if (ScalarToken is not null && !(isReturn && InteropSignature.IsMultiSlotToken(ScalarToken)))
                    return ScalarToken;

                char kind = !isReturn && Alignment > 8 ? 'A' : 'S';
                return string.Create(CultureInfo.InvariantCulture, $"{kind}{Size}");
            }
        }

        // The native layout of a non-blittable struct or class with layout, mirroring
        // EEClassNativeLayoutInfo::CollectNativeLayoutFieldMetadataThrowing.
        private static bool TryGetStructShape(MetadataType type, int depth, ICollection<TypeDesc> marshalledDelegates, out NativeShape shape)
        {
            shape = default;

            // Layout classes that inherit a layout are not modeled. A depth this large only comes
            // from a layout class that contains itself.
            if (depth > 32 || type.IsAutoLayout ||
                (!type.IsValueType && type.BaseType is not null && !type.BaseType.IsObject))
            {
                return false;
            }

            bool isAnsi = type.PInvokeStringFormat switch
            {
                PInvokeStringFormat.UnicodeClass => false,
                PInvokeStringFormat.AutoClass => !type.Context.Target.IsWindows,
                _ => true,
            };

            ClassLayoutMetadata layout = type.GetClassLayout();
            int packingSize = layout.PackingSize == 0 ? DefaultPackingSize : layout.PackingSize;

            int fieldCount = 0;
            int offset = 0;
            int lastFieldEnd = 0;
            int alignment = 1;
            NativeShape firstField = default;
            foreach (FieldDesc field in type.GetFields())
            {
                if (field.IsStatic)
                    continue;

                if (!TryGetFieldShape(field, isAnsi, depth, marshalledDelegates, out NativeShape fieldShape))
                    return false;

                int fieldAlignment = Math.Min(packingSize, fieldShape.Alignment);
                alignment = Math.Max(alignment, fieldAlignment);

                int fieldOffset;
                if (type.IsExplicitLayout)
                {
                    fieldOffset = field.MetadataOffset.AsInt;
                }
                else
                {
                    offset = AlignUp(offset, fieldAlignment);
                    fieldOffset = offset;
                    offset += fieldShape.Size;
                }

                lastFieldEnd = Math.Max(lastFieldEnd, fieldOffset + fieldShape.Size);

                if (fieldCount++ == 0)
                    firstField = fieldShape;
            }

            int size;
            if (layout.Size != 0)
                size = Math.Max(layout.Size, lastFieldEnd);
            else if (type.IsInlineArray)
                size = layout.InlineArrayLength * firstField.Size;
            else
                size = AlignUp(lastFieldEnd, alignment);

            if (size == 0)
                size = 1;

            // vm/wasm/helpers.cpp TryLowerNativeSingleScalar: one field that fills the struct and
            // holds a single scalar is passed as that scalar.
            string scalarToken = fieldCount == 1 && firstField.Size == size ? firstField.ScalarToken : null;

            shape = new NativeShape(size, alignment, scalarToken);
            return true;
        }

        // Mirrors the NativeFieldDescriptor that vm/fieldmarshaler.cpp creates for the field.
        private static bool TryGetFieldShape(FieldDesc field, bool isAnsi, int depth, ICollection<TypeDesc> marshalledDelegates, out NativeShape shape)
        {
            shape = default;
            TypeDesc fieldType = field.FieldType;
            MarshalAsDescriptor marshalAs = field.GetMarshalAsDescriptor();

            MarshallerKind kind = MarshalHelpers.GetMarshallerKind(fieldType, null, null, marshalAs,
                isReturn: false, isAnsi, MarshallerType.Field, out MarshallerKind elementKind);

            switch (kind)
            {
                case MarshallerKind.BlittableStruct when !IsBlittableInRuntime((MetadataType)fieldType):
                case MarshallerKind.Struct:
                case MarshallerKind.LayoutClass:
                    return TryGetStructShape((MetadataType)fieldType, depth + 1, marshalledDelegates, out shape);

                case MarshallerKind.FunctionPointer when fieldType.IsDelegate:
                    marshalledDelegates?.Add(fieldType);
                    break;

                case MarshallerKind.ByValArray:
                case MarshallerKind.ByValAnsiCharArray:
                {
                    if (marshalAs?.SizeConst is not uint count ||
                        !TryGetArrayElementShape(((ArrayType)fieldType).ElementType, elementKind, marshalAs, depth, marshalledDelegates, out NativeShape element))
                    {
                        return false;
                    }

                    shape = new NativeShape(element.Size * (int)count, element.Alignment, count == 1 ? element.ScalarToken : null);
                    return true;
                }

                case MarshallerKind.ByValUnicodeString:
                case MarshallerKind.ByValAnsiString:
                {
                    if (marshalAs?.SizeConst is not uint count)
                        return false;

                    int charSize = kind == MarshallerKind.ByValUnicodeString ? 2 : 1;
                    shape = new NativeShape(charSize * (int)count, charSize, count == 1 ? "i" : null);
                    return true;
                }

                case MarshallerKind.Invalid when marshalAs?.Type == NativeTypeKind.Error && fieldType.Category is TypeFlags.Int32 or TypeFlags.UInt32:
                    // CoreCLR marshals an HRESULT field as the 4-byte integer it already is.
                    shape = GetTypeShape(fieldType);
                    return true;

                case MarshallerKind.Invalid:
                    // The runtime lays out a field it cannot marshal as one byte and throws when the
                    // struct is marshalled, so the IL stub, and its thunk, still exist.
                    shape = new NativeShape(1, 1, null);
                    return true;

                case MarshallerKind.Unknown:
                case MarshallerKind.FailedTypeLoad:
                    return false;
            }

            try
            {
                shape = GetTypeShape(MarshalHelpers.GetNativeTypeFromMarshallerKind(fieldType, kind, elementKind, marshalAs));
                return true;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        private static bool TryGetArrayElementShape(TypeDesc elementType, MarshallerKind elementKind, MarshalAsDescriptor marshalAs, int depth, ICollection<TypeDesc> marshalledDelegates, out NativeShape shape)
        {
            if (elementKind is MarshallerKind.Struct or MarshallerKind.LayoutClass ||
                (elementKind == MarshallerKind.BlittableStruct && !IsBlittableInRuntime((MetadataType)elementType)))
            {
                return TryGetStructShape((MetadataType)elementType, depth + 1, marshalledDelegates, out shape);
            }

            try
            {
                shape = GetTypeShape(MarshalHelpers.GetNativeTypeFromMarshallerKind(elementType, elementKind, MarshallerKind.Unknown, marshalAs, isArrayElement: true));
                return true;
            }
            catch (NotSupportedException)
            {
                shape = default;
                return false;
            }
        }

        // Value types the runtime marshals by value as a pointer-sized handle.
        private static bool IsPassedAsPointer(TypeDesc type)
        {
            TypeSystemContext context = type.Context;
            return InteropTypes.IsSystemArrayWithOffset(context, type) ||
                   InteropTypes.IsSystemRuntimeTypeHandle(context, type) ||
                   InteropTypes.IsSystemRuntimeFieldHandle(context, type) ||
                   InteropTypes.IsSystemRuntimeMethodHandle(context, type);
        }

        // The runtime reads CharSet.Auto as Ansi off Windows, so a default-marshalled char field
        // makes such a struct non-blittable there even where MarshalUtils.IsBlittableType, which
        // reads it as Unicode, reports the struct blittable.
        private static bool IsBlittableInRuntime(MetadataType type)
        {
            if (type.Context.Target.IsWindows)
                return true;

            foreach (FieldDesc field in type.GetFields())
            {
                if (field.IsStatic)
                    continue;

                TypeDesc fieldType = field.FieldType;
                if (fieldType.IsWellKnownType(WellKnownType.Char) &&
                    type.PInvokeStringFormat == PInvokeStringFormat.AutoClass &&
                    field.GetMarshalAsDescriptor() is null)
                {
                    return false;
                }

                if (fieldType is MetadataType { IsValueType: true, IsPrimitive: false, IsEnum: false } fieldStruct &&
                    !IsBlittableInRuntime(fieldStruct))
                {
                    return false;
                }
            }

            return true;
        }

        // A type whose native and managed layouts are the same: a primitive, pointer, enum, or
        // blittable struct.
        private static NativeShape GetTypeShape(TypeDesc type)
        {
            if (type.IsPointer || type.IsFunctionPointer)
            {
                int pointerSize = type.Context.Target.PointerSize;
                return new NativeShape(pointerSize, pointerSize, "i");
            }

            string token = InteropSignature.GetAbiToken(type);
            bool isAggregate = token[0] is 'S' or 'A';
            return new NativeShape(type.GetElementSize().AsInt, ((DefType)type).InstanceFieldAlignment.AsInt, isAggregate ? null : token);
        }

        private static int AlignUp(int value, int alignment) => (value + alignment - 1) / alignment * alignment;
    }
}
