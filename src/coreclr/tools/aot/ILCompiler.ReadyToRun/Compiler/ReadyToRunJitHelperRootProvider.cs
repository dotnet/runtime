// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.IL;
using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class ReadyToRunJitHelperRootProvider(ReadyToRunCompilerContext context) : ICompilationRootProvider
    {
        private static readonly ManagedHelperInfo[] s_managedHelpers =
        [
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_DIV, "System"u8, "Math"u8, "DivInt32"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_MOD, "System"u8, "Math"u8, "ModInt32"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_UDIV, "System"u8, "Math"u8, "DivUInt32"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_UMOD, "System"u8, "Math"u8, "ModUInt32"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_LMUL_OVF, "System"u8, "Math"u8, "MultiplyChecked"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_ULMUL_OVF, "System"u8, "Math"u8, "MultiplyChecked"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_LDIV, "System"u8, "Math"u8, "DivInt64"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_LMOD, "System"u8, "Math"u8, "ModInt64"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_ULDIV, "System"u8, "Math"u8, "DivUInt64"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_ULMOD, "System"u8, "Math"u8, "ModUInt64"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_DBL2INT_OVF, "System"u8, "Math"u8, "ConvertToInt32Checked"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_DBL2LNG_OVF, "System"u8, "Math"u8, "ConvertToInt64Checked"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_DBL2UINT_OVF, "System"u8, "Math"u8, "ConvertToUInt32Checked"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_DBL2ULNG_OVF, "System"u8, "Math"u8, "ConvertToUInt64Checked"u8),

            RootOnly(CorInfoHelpFunc.CORINFO_HELP_NEW_MDARR, "System"u8, "Array"u8, "Ctor"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_NEW_MDARR_RARE, "System"u8, "Array"u8, "Ctor"u8),

            Direct(CorInfoHelpFunc.CORINFO_HELP_INITCLASS, "System.Runtime.CompilerServices"u8, "InitHelpers"u8, "InitClass"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_INITINSTCLASS, "System.Runtime.CompilerServices"u8, "InitHelpers"u8, "InitInstantiatedClass"u8),

            RootOnly(CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFINTERFACE, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "IsInstanceOfInterface"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFARRAY, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "IsInstanceOfAny"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFCLASS, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "IsInstanceOfClass"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFANY, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "IsInstanceOfAny"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_CHKCASTINTERFACE, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "ChkCastInterface"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_CHKCASTARRAY, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "ChkCastAny"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_CHKCASTCLASS, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "ChkCastClass"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_CHKCASTANY, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "ChkCastAny"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_CHKCASTCLASS_SPECIAL, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "ChkCastClassSpecial"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_BOX, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "Box"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_BOX_NULLABLE, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "Box_Nullable"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_UNBOX, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "Unbox"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_UNBOX_TYPETEST, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "Unbox_TypeTest"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_UNBOX_NULLABLE, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "Unbox_Nullable"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETREFANY, "System"u8, "TypedReference"u8, "GetRefAny"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_ARRADDR_ST, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "StelemRef"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_LDELEMA_REF, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "LdelemaRef"u8),

            RootOnly(CorInfoHelpFunc.CORINFO_HELP_USER_BREAKPOINT, "System.Diagnostics"u8, "Debugger"u8, "UserBreakpoint"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_RNGCHKFAIL, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowIndexOutOfRangeException"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_OVERFLOW, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowOverflowException"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_THROWDIVZERO, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowDivideByZeroException"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_THROWNULLREF, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowNullReferenceException"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_VERIFICATION, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowVerificationException"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_METHOD_ACCESS_EXCEPTION, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowMethodAccessException"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_FIELD_ACCESS_EXCEPTION, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowFieldAccessException"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_CLASS_ACCESS_EXCEPTION, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowClassAccessException"u8),

            Direct(CorInfoHelpFunc.CORINFO_HELP_MON_ENTER, "System.Threading"u8, "Monitor"u8, "SynchronizedMethodEnter"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_MON_EXIT, "System.Threading"u8, "Monitor"u8, "SynchronizedMethodExit"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETSYNCFROMCLASSHANDLE, "System"u8, "RuntimeTypeHandle"u8, "GetRuntimeTypeFromHandle"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_POLL_GC, "System.Threading"u8, "Thread"u8, "PollGC"u8),

            Direct(CorInfoHelpFunc.CORINFO_HELP_BULK_WRITEBARRIER, "System"u8, "Buffer"u8, "BulkMoveWithWriteBarrier"u8),

            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETFIELDADDR, "System"u8, "RuntimeFieldHandle"u8, "GetFieldAddr"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETSTATICFIELDADDR, "System"u8, "RuntimeFieldHandle"u8, "GetStaticFieldAddr"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_GET_GCSTATIC_BASE, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetGCStaticBase"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_GET_NONGCSTATIC_BASE, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetNonGCStaticBase"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETDYNAMIC_GCSTATIC_BASE, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetDynamicGCStaticBase"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETDYNAMIC_NONGCSTATIC_BASE, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetDynamicNonGCStaticBase"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETPINNED_GCSTATIC_BASE, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetDynamicGCStaticBase"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETPINNED_NONGCSTATIC_BASE, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetDynamicNonGCStaticBase"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_GET_GCTHREADSTATIC_BASE, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetGCThreadStaticBase"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_GET_NONGCTHREADSTATIC_BASE, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetNonGCThreadStaticBase"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetDynamicGCThreadStaticBase"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETDYNAMIC_NONGCTHREADSTATIC_BASE, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetDynamicNonGCThreadStaticBase"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GET_GCTHREADSTATIC_BASE_NOCTOR, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetGCThreadStaticBase"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GET_NONGCTHREADSTATIC_BASE_NOCTOR, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetNonGCThreadStaticBase"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetDynamicGCThreadStaticBase"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETDYNAMIC_NONGCTHREADSTATIC_BASE_NOCTOR, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetDynamicNonGCThreadStaticBase"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetOptimizedGCThreadStaticBase"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_GETDYNAMIC_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetOptimizedNonGCThreadStaticBase"u8),

            Direct(CorInfoHelpFunc.CORINFO_HELP_GETCURRENTMANAGEDTHREADID, "System"u8, "Environment"u8, "get_CurrentManagedThreadId"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_MEMSET, "System"u8, "SpanHelpers"u8, "Fill"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_MEMZERO, "System"u8, "SpanHelpers"u8, "ClearWithoutReferences"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_MEMCPY, "System"u8, "SpanHelpers"u8, "Memmove"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_RUNTIMEHANDLE_METHOD, "System.Runtime.CompilerServices"u8, "GenericsHelpers"u8, "Method"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_RUNTIMEHANDLE_CLASS, "System.Runtime.CompilerServices"u8, "GenericsHelpers"u8, "Class"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE, "System"u8, "RuntimeTypeHandle"u8, "GetRuntimeTypeFromHandle"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD, "System"u8, "RuntimeMethodInfoStub"u8, "FromPtr"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD, "System"u8, "RuntimeFieldInfoStub"u8, "FromPtr"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE, "System"u8, "RuntimeTypeHandle"u8, "GetRuntimeTypeFromHandle"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_VIRTUAL_FUNC_PTR, "System.Runtime.CompilerServices"u8, "VirtualDispatchHelpers"u8, "VirtualFunctionPointer"u8),

            Direct(CorInfoHelpFunc.CORINFO_HELP_THROW_ARGUMENTEXCEPTION, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowArgumentException"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_THROW_ARGUMENTOUTOFRANGEEXCEPTION, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowArgumentOutOfRangeException"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_THROW_NOT_IMPLEMENTED, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowNotImplementedException"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowPlatformNotSupportedException"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_THROW_TYPE_NOT_SUPPORTED, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowTypeNotSupportedException"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_THROW_AMBIGUOUS_RESOLUTION_EXCEPTION, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowAmbiguousResolutionException"u8),
            RootOnly(CorInfoHelpFunc.CORINFO_HELP_THROW_ENTRYPOINT_NOT_FOUND_EXCEPTION, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowEntryPointNotFoundException"u8),

            Direct(CorInfoHelpFunc.CORINFO_HELP_ALLOC_CONTINUATION, "System.Runtime.CompilerServices"u8, "AsyncHelpers"u8, "AllocContinuation"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_ALLOC_CONTINUATION_METHOD, "System.Runtime.CompilerServices"u8, "AsyncHelpers"u8, "AllocContinuationMethod"u8),
            Direct(CorInfoHelpFunc.CORINFO_HELP_ALLOC_CONTINUATION_CLASS, "System.Runtime.CompilerServices"u8, "AsyncHelpers"u8, "AllocContinuationClass"u8),
        ];

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            var rootedMethods = new HashSet<MethodDesc>();

            foreach (ManagedHelperInfo helper in s_managedHelpers)
            {
                MetadataType type = context.SystemModule.GetType(
                    helper.NamespaceName.AsSpan(),
                    helper.TypeName.AsSpan(),
                    throwIfNotFound: false);
                if (type is null)
                {
                    continue;
                }

                foreach (MethodDesc method in type.GetMethods())
                {
                    if (method.Name == helper.MethodName.AsSpan() &&
                        !method.IsGenericMethodDefinition &&
                        rootedMethods.Add(method))
                    {
                        RootMethod(rootProvider, method);
                    }
                }
            }
        }

        internal static MethodDesc GetManagedHelper(TypeSystemContext context, CorInfoHelpFunc helper)
        {
            foreach (ManagedHelperInfo helperInfo in s_managedHelpers)
            {
                if (helperInfo.Helper == helper)
                {
                    if (helperInfo.Classification != ManagedHelperClassification.DirectCallCandidate)
                    {
                        return null;
                    }

                    MetadataType helperType = context.SystemModule.GetKnownType(
                        helperInfo.NamespaceName.AsSpan(),
                        helperInfo.TypeName.AsSpan());
                    return helperType
                        .GetKnownMethod(helperInfo.MethodName.AsSpan(), signature: null)
                        .GetCanonMethodTarget(CanonicalFormKind.Specific);
                }
            }

            return null;
        }

        private static ManagedHelperInfo Direct(
            CorInfoHelpFunc helper,
            ReadOnlySpan<byte> namespaceName,
            ReadOnlySpan<byte> typeName,
            ReadOnlySpan<byte> methodName)
        {
            return new ManagedHelperInfo(
                helper,
                namespaceName,
                typeName,
                methodName,
                ManagedHelperClassification.DirectCallCandidate);
        }

        private static ManagedHelperInfo RootOnly(
            CorInfoHelpFunc helper,
            ReadOnlySpan<byte> namespaceName,
            ReadOnlySpan<byte> typeName,
            ReadOnlySpan<byte> methodName)
        {
            return new ManagedHelperInfo(
                helper,
                namespaceName,
                typeName,
                methodName,
                ManagedHelperClassification.RootOnly);
        }

        private static void RootMethod(IRootingServiceProvider rootProvider, MethodDesc method)
        {
            rootProvider.AddCompilationRoot(
                method,
                rootMinimalDependencies: false,
                $"JIT helper {method}",
                isJitHelper: true);
        }

        private enum ManagedHelperClassification
        {
            RootOnly,
            DirectCallCandidate,
        }

        private readonly struct ManagedHelperInfo
        {
            public ManagedHelperInfo(
                CorInfoHelpFunc helper,
                ReadOnlySpan<byte> namespaceName,
                ReadOnlySpan<byte> typeName,
                ReadOnlySpan<byte> methodName,
                ManagedHelperClassification classification)
            {
                Helper = helper;
                NamespaceName = new Utf8String(namespaceName);
                TypeName = new Utf8String(typeName);
                MethodName = new Utf8String(methodName);
                Classification = classification;
            }

            public CorInfoHelpFunc Helper { get; }
            public Utf8String NamespaceName { get; }
            public Utf8String TypeName { get; }
            public Utf8String MethodName { get; }
            public ManagedHelperClassification Classification { get; }
        }
    }
}
