// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class ReadyToRunJitHelperRootProvider(ReadyToRunCompilerContext context) : ICompilationRootProvider
    {
        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            RootMethod(rootProvider, "System"u8, "Math"u8, "ConvertToInt32Checked");
            RootMethod(rootProvider, "System"u8, "Math"u8, "ConvertToUInt32Checked");
            RootMethod(rootProvider, "System"u8, "Math"u8, "ConvertToInt64Checked");
            RootMethod(rootProvider, "System"u8, "Math"u8, "ConvertToUInt64Checked");

            RootMethod(rootProvider, "System"u8, "Math"u8, "MultiplyChecked");
            RootMethod(rootProvider, "System"u8, "Math"u8, "DivInt32");
            RootMethod(rootProvider, "System"u8, "Math"u8, "DivUInt32");
            RootMethod(rootProvider, "System"u8, "Math"u8, "DivInt64");
            RootMethod(rootProvider, "System"u8, "Math"u8, "DivUInt64");
            RootMethod(rootProvider, "System"u8, "Math"u8, "ModInt32");
            RootMethod(rootProvider, "System"u8, "Math"u8, "ModUInt32");
            RootMethod(rootProvider, "System"u8, "Math"u8, "ModInt64");
            RootMethod(rootProvider, "System"u8, "Math"u8, "ModUInt64");
            RootMethod(rootProvider, "System"u8, "Array"u8, "Ctor");
            RootMethod(rootProvider, "System"u8, "TypedReference"u8, "GetRefAny");
            RootMethod(rootProvider, "System"u8, "RuntimeTypeHandle"u8, "GetRuntimeTypeFromHandle");
            RootMethod(rootProvider, "System"u8, "RuntimeMethodInfoStub"u8, "FromPtr");
            RootMethod(rootProvider, "System"u8, "RuntimeFieldInfoStub"u8, "FromPtr");
            RootMethod(rootProvider, "System"u8, "RuntimeFieldHandle"u8, "GetFieldAddr");
            RootMethod(rootProvider, "System"u8, "RuntimeFieldHandle"u8, "GetStaticFieldAddr");
            RootMethod(rootProvider, "System"u8, "Environment"u8, "get_CurrentManagedThreadId");
            RootMethod(rootProvider, "System"u8, "Buffer"u8, "BulkMoveWithWriteBarrier");
            RootMethod(rootProvider, "System"u8, "SpanHelpers"u8, "Fill");
            RootMethod(rootProvider, "System"u8, "SpanHelpers"u8, "ClearWithoutReferences");
            RootMethod(rootProvider, "System"u8, "SpanHelpers"u8, "Memmove");

            RootMethod(rootProvider, "System.Diagnostics"u8, "Debugger"u8, "UserBreakpoint");

            RootMethod(rootProvider, "System.Threading"u8, "Monitor"u8, "SynchronizedMethodEnter");
            RootMethod(rootProvider, "System.Threading"u8, "Monitor"u8, "SynchronizedMethodExit");
            RootMethod(rootProvider, "System.Threading"u8, "Thread"u8, "PollGC");

            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "AsyncHelpers"u8, "AllocContinuation");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "AsyncHelpers"u8, "AllocContinuationMethod");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "AsyncHelpers"u8, "AllocContinuationClass");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "IsInstanceOfAny");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "IsInstanceOfClass");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "IsInstanceOfInterface");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "ChkCastAny");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "ChkCastInterface");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "ChkCastClass");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "ChkCastClassSpecial");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "Box");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "Box_Nullable");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "Unbox");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "Unbox_TypeTest");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "Unbox_Nullable");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "StelemRef");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "CastHelpers"u8, "LdelemaRef");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "GenericsHelpers"u8, "Method");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "GenericsHelpers"u8, "Class");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "InitHelpers"u8, "InitClass");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "InitHelpers"u8, "InitInstantiatedClass");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetNonGCStaticBase");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetGCStaticBase");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetDynamicNonGCStaticBase");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetDynamicGCStaticBase");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetNonGCThreadStaticBase");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetGCThreadStaticBase");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetDynamicNonGCThreadStaticBase");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetDynamicGCThreadStaticBase");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetOptimizedNonGCThreadStaticBase");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "StaticsHelpers"u8, "GetOptimizedGCThreadStaticBase");
            RootMethod(rootProvider, "System.Runtime.CompilerServices"u8, "VirtualDispatchHelpers"u8, "VirtualFunctionPointer");

            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowArgumentException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowArgumentOutOfRangeException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowIndexOutOfRangeException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowNotImplementedException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowPlatformNotSupportedException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowTypeNotSupportedException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowOverflowException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowDivideByZeroException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowNullReferenceException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowVerificationException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowAmbiguousResolutionException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowEntryPointNotFoundException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowMethodAccessException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowFieldAccessException");
            RootMethod(rootProvider, "Internal.Runtime.CompilerHelpers"u8, "ThrowHelpers"u8, "ThrowClassAccessException");
        }

        private void RootMethod(IRootingServiceProvider rootProvider, ReadOnlySpan<byte> namespaceName, ReadOnlySpan<byte> typeName, string methodName)
        {
            MetadataType type = context.SystemModule.GetType(namespaceName, typeName, throwIfNotFound: false);
            if (type is null)
            {
                return;
            }

            foreach (MethodDesc method in type.GetMethods())
            {
                if (method.GetName() == methodName && !method.IsGenericMethodDefinition)
                {
                    rootProvider.AddCompilationRoot(method, rootMinimalDependencies: false, $"JIT helper {type}.{methodName}");
                }
            }
        }
    }
}
