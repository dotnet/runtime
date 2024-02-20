// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using System.Diagnostics;

namespace ILCompiler
{
    //
    // The naming format of these names is known to the debugger
    //
    public class WindowsNodeMangler : NodeMangler
    {
        private TargetDetails _target;

        public const string NonGCStaticMemberName = "__NONGCSTATICS";
        public const string GCStaticMemberName = "__GCSTATICS";
        public const string ThreadStaticMemberName = "__THREADSTATICS";
        public const string ThreadStaticIndexName = "__THREADSTATICINDEX";

        public WindowsNodeMangler(TargetDetails target)
        {
            _target = target;
        }

        // Mangled name of boxed version of a type
        public sealed override string MangledBoxedTypeName(TypeDesc type)
        {
            Debug.Assert(type.IsValueType);
            return "Boxed_" + NameMangler.GetMangledTypeName(type);
        }

        public sealed override string MethodTable(TypeDesc type)
        {
            string mangledJustTypeName;

            if (type.IsValueType)
                mangledJustTypeName = MangledBoxedTypeName(type);
            else
                mangledJustTypeName = NameMangler.GetMangledTypeName(type);

            // "??_7TypeName@@6B@" is the C++ mangling for "const TypeName::`vftable'"
            // This, along with LF_VTSHAPE debug records added by the object writer
            // is the debugger magic that allows debuggers to vcast types to their bases.
            return "??_7" + mangledJustTypeName + "@@6B@";
        }

        private string CreateStaticFieldName(TypeDesc type, string fieldName)
        {
            return @$"?{fieldName}@{NameMangler.GetMangledTypeName(type)}@@";
        }

        public sealed override string GCStatics(TypeDesc type)
        {
            return CreateStaticFieldName(type, GCStaticMemberName);
        }

        public sealed override string NonGCStatics(TypeDesc type)
        {
            return CreateStaticFieldName(type, NonGCStaticMemberName);
        }

        public sealed override string ThreadStatics(TypeDesc type)
        {
            return CreateStaticFieldName(type, NameMangler.CompilationUnitPrefix + ThreadStaticMemberName);
        }

        public sealed override string ThreadStaticsIndex(TypeDesc type)
        {
            return CreateStaticFieldName(type, ThreadStaticIndexName);
        }

        public sealed override string TypeGenericDictionary(TypeDesc type)
        {
            return GenericDictionaryNamePrefix + NameMangler.GetMangledTypeName(type);
        }

        public sealed override string MethodGenericDictionary(MethodDesc method)
        {
            return GenericDictionaryNamePrefix + NameMangler.GetMangledMethodName(method);
        }

        public sealed override string ExternMethod(string unmangledName, MethodDesc method)
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
                // HACK: Work around GetUnmanagedCallersOnlyMethodCallingConventions crashing on non-Ecma methoddesc
                if (method is not Internal.TypeSystem.Ecma.EcmaMethod)
                    callConv = UnmanagedCallingConventions.Cdecl;
                else
                    callConv = method.GetUnmanagedCallersOnlyMethodCallingConventions() & UnmanagedCallingConventions.CallingConventionMask;
            }
            else
            {
                Debug.Assert(method is Internal.TypeSystem.Ecma.EcmaMethod ecmaMethod && (ecmaMethod.GetRuntimeExportName() != null || ecmaMethod.GetRuntimeImportName() != null));
                callConv = UnmanagedCallingConventions.Fastcall;
            }

            int signatureBytes = 0;
            foreach (var p in method.Signature)
            {
                signatureBytes += AlignmentHelper.AlignUp(p.GetElementSize().AsInt, _target.PointerSize);
            }

            return callConv switch
            {
                UnmanagedCallingConventions.Stdcall => string.Concat("_", unmangledName, "@", signatureBytes.ToString()),
                UnmanagedCallingConventions.Fastcall => string.Concat("@", unmangledName, "@", signatureBytes.ToString()),
                UnmanagedCallingConventions.Cdecl => string.Concat("_", unmangledName),
                _ => throw new System.NotImplementedException()
            };
        }

        public sealed override string ExternVariable(string unmangledName)
        {
            if (_target.Architecture != TargetArchitecture.X86)
            {
                return unmangledName;
            }

            return $"_{unmangledName}";
        }
    }
}
