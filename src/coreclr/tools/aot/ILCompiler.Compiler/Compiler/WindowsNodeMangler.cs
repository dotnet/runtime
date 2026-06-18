// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;
using System;
using System.Diagnostics;

namespace ILCompiler
{
    //
    // The naming format of these names is known to the debugger
    //
    public class WindowsNodeMangler : NodeMangler
    {
        private TargetDetails _target;

        public static Utf8String NonGCStaticMemberName = new Utf8String("__NONGCSTATICS"u8);
        public static Utf8String GCStaticMemberName = new Utf8String("__GCSTATICS"u8);
        public static Utf8String ThreadStaticMemberName = new Utf8String("__THREADSTATICS"u8);
        public static Utf8String ThreadStaticIndexName = new Utf8String("__THREADSTATICINDEX"u8);

        public WindowsNodeMangler(TargetDetails target)
        {
            _target = target;
        }

        // Mangled name of boxed version of a type
        public sealed override Utf8String MangledBoxedTypeName(TypeDesc type)
        {
            Debug.Assert(type.IsValueType);
            return Utf8String.Concat("Boxed_"u8, NameMangler.GetMangledTypeName(type).AsSpan());
        }

        public sealed override Utf8String MethodTable(TypeDesc type)
        {
            Utf8String mangledJustTypeName = type.IsValueType
                ? MangledBoxedTypeName(type)
                : NameMangler.GetMangledTypeName(type);

            // "??_7TypeName@@6B@" is the C++ mangling for "const TypeName::`vftable'"
            // This, along with LF_VTSHAPE debug records added by the object writer
            // is the debugger magic that allows debuggers to vcast types to their bases.
            return Utf8String.Concat("??_7"u8, mangledJustTypeName.AsSpan(), "@@6B@"u8);
        }

        private Utf8String CreateStaticFieldName(TypeDesc type, ReadOnlySpan<byte> fieldName)
        {
            return Utf8String.Concat("?"u8, fieldName, "@"u8, NameMangler.GetMangledTypeName(type).AsSpan(), "@@"u8);
        }

        public sealed override Utf8String GCStatics(TypeDesc type)
        {
            return CreateStaticFieldName(type, GCStaticMemberName.AsSpan());
        }

        public sealed override Utf8String NonGCStatics(TypeDesc type)
        {
            return CreateStaticFieldName(type, NonGCStaticMemberName.AsSpan());
        }

        public sealed override Utf8String ThreadStatics(TypeDesc type)
        {
            Utf8String name = NameMangler.CompilationUnitPrefix.Length > 0
                ? Utf8String.Concat(NameMangler.CompilationUnitPrefix, ThreadStaticMemberName)
                : ThreadStaticMemberName;

            return CreateStaticFieldName(type, name.AsSpan());
        }

        public sealed override Utf8String ThreadStaticsIndex(TypeDesc type)
        {
            return CreateStaticFieldName(type, ThreadStaticIndexName.AsSpan());
        }

        public sealed override Utf8String TypeGenericDictionary(TypeDesc type)
        {
            return Utf8String.Concat(GenericDictionaryNamePrefix, NameMangler.GetMangledTypeName(type));
        }

        public sealed override Utf8String MethodGenericDictionary(MethodDesc method)
        {
            return Utf8String.Concat(GenericDictionaryNamePrefix, NameMangler.GetMangledMethodName(method));
        }

        public sealed override Utf8String ExternMethod(Utf8String unmangledName, MethodDesc method)
        {
            if (_target.Architecture != TargetArchitecture.X86)
            {
                return unmangledName;
            }

            UnmanagedCallingConventions callConv;
            if (method.IsPInvoke)
            {
                callConv = method.GetPInvokeMethodCallingConventions() & UnmanagedCallingConventions.CallingConventionMask;
            }
            else if (method.IsUnmanagedCallersOnly)
            {
                if (method is not Internal.TypeSystem.Ecma.EcmaMethod)
                    callConv = method.Signature.GetStandaloneMethodSignatureCallingConventions();
                else
                    callConv = method.GetUnmanagedCallersOnlyMethodCallingConventions() & UnmanagedCallingConventions.CallingConventionMask;
            }
            else
            {
                Debug.Assert(method is Internal.TypeSystem.Ecma.EcmaMethod ecmaMethod && (ecmaMethod.GetRuntimeImportName() != null || ecmaMethod.GetRuntimeExportName() != null));
                return unmangledName;
            }

            int signatureBytes = 0;
            foreach (var p in method.Signature)
            {
                signatureBytes += AlignmentHelper.AlignUp(p.GetElementSize().AsInt, _target.PointerSize);
            }

            return callConv switch
            {
                UnmanagedCallingConventions.Stdcall => new Utf8StringBuilder().Append('_').Append(unmangledName).Append('@').Append(signatureBytes).ToUtf8String(),
                UnmanagedCallingConventions.Fastcall => new Utf8StringBuilder().Append('@').Append(unmangledName).Append('@').Append(signatureBytes).ToUtf8String(),
                UnmanagedCallingConventions.Cdecl => Utf8String.Concat("_"u8, unmangledName.AsSpan()),
                _ => throw new System.NotImplementedException()
            };
        }

        public sealed override Utf8String ExternVariable(Utf8String unmangledName)
        {
            if (_target.Architecture != TargetArchitecture.X86)
            {
                return unmangledName;
            }

            return Utf8String.Concat("_"u8, unmangledName.AsSpan());
        }
    }
}
