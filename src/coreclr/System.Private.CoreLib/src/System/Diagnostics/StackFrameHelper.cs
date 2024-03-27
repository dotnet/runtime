// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;

namespace System.Diagnostics
{
    // READ ME:
    // Modifying the order or fields of this object may require other changes
    // to the unmanaged definition of the StackFrameHelper class, in
    // VM\DebugDebugger.h. The binder will catch some of these layout problems.
    internal sealed class StackFrameHelper
    {
        private Thread? targetThread;
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

        private delegate void GetSourceLineInfoDelegate(Assembly? assembly, string assemblyPath, IntPtr loadedPeAddress,
            int loadedPeSize, bool isFileLayout, IntPtr inMemoryPdbAddress, int inMemoryPdbSize, int methodToken, int ilOffset,
            out string? sourceFile, out int sourceLine, out int sourceColumn);

        private static GetSourceLineInfoDelegate? s_getSourceLineInfo;

        [ThreadStatic]
        private static int t_reentrancy;

        public StackFrameHelper(Thread? target)
        {
            targetThread = target;
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

        internal void InitializeSourceInfo(int iSkip, bool fNeedFileInfo, Exception? exception)
        {
            StackTrace.GetStackFramesInternal(this, iSkip, fNeedFileInfo, exception);

            if (!fNeedFileInfo)
                return;

            // Check if this function is being reentered because of an exception in the code below
            if (t_reentrancy > 0)
                return;

            t_reentrancy++;
            try
            {
                if (s_getSourceLineInfo == null)
                {
                    Type? symbolsType = Type.GetType(
                        "System.Diagnostics.StackTraceSymbols, System.Diagnostics.StackTrace, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                        throwOnError: false);

                    if (symbolsType == null)
                    {
                        return;
                    }

                    Type[] parameterTypes = new Type[]
                    {
                        typeof(Assembly), typeof(string), typeof(IntPtr), typeof(int), typeof(bool), typeof(IntPtr),
                        typeof(int), typeof(int), typeof(int),
                        typeof(string).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int).MakeByRefType()
                    };
                    MethodInfo? symbolsMethodInfo = symbolsType.GetMethod("GetSourceLineInfo", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, parameterTypes, null);
                    if (symbolsMethodInfo == null)
                    {
                        return;
                    }

                    // Create an instance of System.Diagnostics.Stacktrace.Symbols
                    object? target = Activator.CreateInstance(symbolsType);

                    // Create an instance delegate for the GetSourceLineInfo method
                    GetSourceLineInfoDelegate getSourceLineInfo = symbolsMethodInfo.CreateDelegate<GetSourceLineInfoDelegate>(target);

                    // We could race with another thread. It doesn't matter if we win or lose, the losing instance will be GC'ed and all threads including this one will
                    // use the winning instance
                    Interlocked.CompareExchange(ref s_getSourceLineInfo, getSourceLineInfo, null);
                }

                for (int index = 0; index < iFrameCount; index++)
                {
                    // If there was some reason not to try get the symbols from the portable PDB reader like the module was
                    // ENC or the source/line info was already retrieved, the method token is 0.
                    if (rgiMethodToken![index] != 0)
                    {
                        s_getSourceLineInfo!(rgAssembly![index], rgAssemblyPath![index]!, rgLoadedPeAddress![index], rgiLoadedPeSize![index], rgiIsFileLayout![index],
                            rgInMemoryPdbAddress![index], rgiInMemoryPdbSize![index], rgiMethodToken![index],
                            rgiILOffset![index], out rgFilename![index], out rgiLineNumber![index], out rgiColumnNumber![index]);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                t_reentrancy--;
            }
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

            IRuntimeMethodInfo? mhReal =
                LocalAppContextSwitches.ShowGenericInstantiations
                ? RuntimeMethodHandle.FromIntPtr(mh).GetMethodInfo()
                : RuntimeMethodHandle.GetTypicalMethodDefinition(new RuntimeMethodInfoStub(mh, this));

            return RuntimeType.GetMethodBase(mhReal);
        }

        public int GetOffset(int i) { return rgiOffset![i]; }
        public int GetILOffset(int i) { return rgiILOffset![i]; }
        public string? GetFilename(int i) { return rgFilename?[i]; }
        public int GetLineNumber(int i) { return rgiLineNumber == null ? 0 : rgiLineNumber[i]; }
        public int GetColumnNumber(int i) { return rgiColumnNumber == null ? 0 : rgiColumnNumber[i]; }

        public bool IsLastFrameFromForeignExceptionStackTrace(int i)
        {
            return (rgiLastFrameFromForeignExceptionStackTrace == null) ? false : rgiLastFrameFromForeignExceptionStackTrace[i];
        }

        public int GetNumberOfFrames() { return iFrameCount; }
    }
}
