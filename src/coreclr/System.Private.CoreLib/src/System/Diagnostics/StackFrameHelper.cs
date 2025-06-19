// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics
{
    // READ ME:
    // Modifying the order or fields of this object may require other changes
    // to the unmanaged definition of the StackFrameHelper class, in
    // VM\DebugDebugger.h. The binder will catch some of these layout problems.
    internal sealed class StackFrameHelper
    {
        private int[]? rgiOffset;
        private int[]? rgiILOffset;

#pragma warning disable 414
        // dynamicMethods is an array of System.Resolver objects, used to keep
        // DynamicMethodDescs AND collectible LoaderAllocators alive for the lifetime of StackFrameHelper.
        private object? dynamicMethods; // Field is not used from managed.

        private IntPtr[]? rgMethodHandle;
        private string[]? rgAssemblyPath;
        private Assembly?[]? rgAssembly;
        private IntPtr[]? rgLoadedPeAddress;
        private int[]? rgiLoadedPeSize;
        private bool[]? rgiIsFileLayout;
        private IntPtr[]? rgInMemoryPdbAddress;
        private int[]? rgiInMemoryPdbSize;
        // if rgiMethodToken[i] == 0, then don't attempt to get the portable PDB source/info
        private int[]? rgiMethodToken;
        private string?[]? rgFilename;
        private int[]? rgiLineNumber;
        private int[]? rgiColumnNumber;
        private bool[]? rgiLastFrameFromForeignExceptionStackTrace;
        private int iFrameCount;
#pragma warning restore 414

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("System.Diagnostics.StackTraceSymbols, System.Diagnostics.StackTrace, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        private static extern object CreateStackTraceSymbols();

        [UnsafeAccessor(UnsafeAccessorKind.Method)]
        private static extern void GetSourceLineInfo(
            [UnsafeAccessorType("System.Diagnostics.StackTraceSymbols, System.Diagnostics.StackTrace, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")] object target,
            Assembly? assembly, string assemblyPath, IntPtr loadedPeAddress,
            int loadedPeSize, bool isFileLayout, IntPtr inMemoryPdbAddress, int inMemoryPdbSize, int methodToken, int ilOffset,
            out string? sourceFile, out int sourceLine, out int sourceColumn);

        private static object? s_stackTraceSymbolsCache;

        [ThreadStatic]
        private static int t_reentrancy;

        public StackFrameHelper()
        {
            rgMethodHandle = null;
            rgiMethodToken = null;
            rgiOffset = null;
            rgiILOffset = null;
            rgAssemblyPath = null;
            rgAssembly = null;
            rgLoadedPeAddress = null;
            rgiLoadedPeSize = null;
            rgiIsFileLayout = null;
            rgInMemoryPdbAddress = null;
            rgiInMemoryPdbSize = null;
            dynamicMethods = null;
            rgFilename = null;
            rgiLineNumber = null;
            rgiColumnNumber = null;

            rgiLastFrameFromForeignExceptionStackTrace = null;

            // 0 means capture all frames.  For StackTraces from an Exception, the EE always
            // captures all frames.  For other uses of StackTraces, we can abort stack walking after
            // some limit if we want to by setting this to a non-zero value.  In Whidbey this was
            // hard-coded to 512, but some customers complained.  There shouldn't be any need to limit
            // this as memory/CPU is no longer allocated up front.  If there is some reason to provide a
            // limit in the future, then we should expose it in the managed API so applications can
            // override it.
            iFrameCount = 0;
        }

        //
        // Initializes the stack trace helper. If fNeedFileInfo is true, initializes rgFilename,
        // rgiLineNumber and rgiColumnNumber fields using the portable PDB reader if not already
        // done by GetStackFramesInternal (on Windows for old PDB format).
        //

        internal void InitializeSourceInfo(bool fNeedFileInfo, Exception? exception)
        {
            StackTrace.GetStackFramesInternal(this, fNeedFileInfo, exception);
        }

        public MethodBase? GetMethodBase(int i)
        {
            // There may be a better way to do this.
            // we got RuntimeMethodHandles here and we need to go to MethodBase
            // but we don't know whether the reflection info has been initialized
            // or not. So we call GetMethods and GetConstructors on the type
            // and then we fetch the proper MethodBase!!
            IntPtr mh = rgMethodHandle![i];

            if (mh == IntPtr.Zero)
                return null;

            IRuntimeMethodInfo? mhReal = RuntimeMethodHandle.GetTypicalMethodDefinition(new RuntimeMethodInfoStub(new RuntimeMethodHandleInternal(mh), this));

            return RuntimeType.GetMethodBase(mhReal);
        }

        private void InitializeFrameSourceInfo(int index)
        {
            // rgiMethodToken is null if file info wasn't requested when collecting the stack trace.
            if (rgiMethodToken == null)
                return;

            // We use rgiMethodToken[i] to indicate whether we've initialized the info for this frame.
            // If the native code set it to zero, then information is already initialized from the native
            // symbol reader. If the native code set it to non-zero, then we're supposed to use the managed
            // symbol reader to try to resolve debug information.
            //
            // This is the only purpose the field has, however, so we can assign it to zero AFTER resolving
            // symbol information to indicate that it's already been resolved without causing any issues
            // elsewhere.
            if (rgiMethodToken[index] == 0)
                return;

            // Check if this function is being reentered because of an exception in the code below
            if (t_reentrancy > 0)
                return;

            t_reentrancy++;
            try
            {
                if (s_stackTraceSymbolsCache == null)
                {
                    // We could race with another thread. It doesn't matter if we win or lose, the losing instance will be GC'ed and all threads including this one will
                    // use the winning instance
                    Interlocked.CompareExchange(ref s_stackTraceSymbolsCache, CreateStackTraceSymbols(), null);
                }

                GetSourceLineInfo(s_stackTraceSymbolsCache!, rgAssembly![index], rgAssemblyPath![index]!, rgLoadedPeAddress![index], rgiLoadedPeSize![index], rgiIsFileLayout![index],
                    rgInMemoryPdbAddress![index], rgiInMemoryPdbSize![index], rgiMethodToken![index],
                    rgiILOffset![index], out rgFilename![index], out rgiLineNumber![index], out rgiColumnNumber![index]);

                // Make sure we mark down that debug information for this frame was resolved
                rgiMethodToken[index] = 0;
            }
            catch
            {
            }
            finally
            {
                t_reentrancy--;
            }
        }

        public int GetOffset(int i) { return rgiOffset![i]; }
        public int GetILOffset(int i) { return rgiILOffset![i]; }
        public string? GetFilename(int i)
        {
            InitializeFrameSourceInfo(i);
            return rgFilename?[i];
        }
        public int GetLineNumber(int i)
        {
            if (rgiLineNumber == null)
            {
                return 0;
            }
            else
            {
                InitializeFrameSourceInfo(i);
                return rgiLineNumber[i];
            }
        }
        public int GetColumnNumber(int i)
        {
            if (rgiColumnNumber == null)
            {
                return 0;
            }
            else
            {
                InitializeFrameSourceInfo(i);
                return rgiColumnNumber[i];
            }
        }

        public bool IsLastFrameFromForeignExceptionStackTrace(int i)
        {
            return rgiLastFrameFromForeignExceptionStackTrace != null && rgiLastFrameFromForeignExceptionStackTrace[i];
        }

        public int GetNumberOfFrames() { return iFrameCount; }
    }
}
