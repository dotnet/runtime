// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Security;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    // READ ME:
    // Modifying the order or fields of this object may require other changes 
    // to the unmanaged definition of the StackFrameHelper class, in 
    // VM\DebugDebugger.h. The binder will catch some of these layout problems.
    internal class StackFrameHelper
    {
        private Thread targetThread;
        private int[] rgiOffset;
        private int[] rgiILOffset;

#pragma warning disable 414
        // dynamicMethods is an array of System.Resolver objects, used to keep
        // DynamicMethodDescs alive for the lifetime of StackFrameHelper.
        private Object dynamicMethods; // Field is not used from managed.        

        private IntPtr[] rgMethodHandle;
        private string[] rgAssemblyPath;
        private IntPtr[] rgLoadedPeAddress;
        private int[] rgiLoadedPeSize;
        private IntPtr[] rgInMemoryPdbAddress;
        private int[] rgiInMemoryPdbSize;
        // if rgiMethodToken[i] == 0, then don't attempt to get the portable PDB source/info
        private int[] rgiMethodToken;
        private string[] rgFilename;
        private int[] rgiLineNumber;
        private int[] rgiColumnNumber;
        private bool[] rgiLastFrameFromForeignExceptionStackTrace;
        private int iFrameCount;
#pragma warning restore 414

        private delegate void GetSourceLineInfoDelegate(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize,
            IntPtr inMemoryPdbAddress, int inMemoryPdbSize, int methodToken, int ilOffset,
            out string sourceFile, out int sourceLine, out int sourceColumn);

        private static GetSourceLineInfoDelegate s_getSourceLineInfo = null;

        [ThreadStatic]
        private static int t_reentrancy = 0;

        public StackFrameHelper(Thread target)
        {
            targetThread = target;
            rgMethodHandle = null;
            rgiMethodToken = null;
            rgiOffset = null;
            rgiILOffset = null;
            rgAssemblyPath = null;
            rgLoadedPeAddress = null;
            rgiLoadedPeSize = null;
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
        internal void InitializeSourceInfo(int iSkip, bool fNeedFileInfo, Exception exception)
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
                    Type symbolsType = Type.GetType(
                        "System.Diagnostics.StackTraceSymbols, System.Diagnostics.StackTrace, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                        throwOnError: false);

                    if (symbolsType == null)
                    {
                        return;
                    }

                    MethodInfo symbolsMethodInfo = symbolsType.GetMethod("GetSourceLineInfo", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (symbolsMethodInfo == null)
                    {
                        return;
                    }

                    // Create an instance of System.Diagnostics.Stacktrace.Symbols
                    object target = Activator.CreateInstance(symbolsType);

                    // Create an instance delegate for the GetSourceLineInfo method
                    GetSourceLineInfoDelegate getSourceLineInfo = (GetSourceLineInfoDelegate)symbolsMethodInfo.CreateDelegate(typeof(GetSourceLineInfoDelegate), target);

                    // We could race with another thread. It doesn't matter if we win or lose, the losing instance will be GC'ed and all threads including this one will
                    // use the winning instance
                    Interlocked.CompareExchange(ref s_getSourceLineInfo, getSourceLineInfo, null);
                }

                for (int index = 0; index < iFrameCount; index++)
                {
                    // If there was some reason not to try get the symbols from the portable PDB reader like the module was
                    // ENC or the source/line info was already retrieved, the method token is 0.
                    if (rgiMethodToken[index] != 0)
                    {
                        s_getSourceLineInfo(rgAssemblyPath[index], rgLoadedPeAddress[index], rgiLoadedPeSize[index],
                            rgInMemoryPdbAddress[index], rgiInMemoryPdbSize[index], rgiMethodToken[index],
                            rgiILOffset[index], out rgFilename[index], out rgiLineNumber[index], out rgiColumnNumber[index]);
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

        public virtual MethodBase GetMethodBase(int i)
        {
            // There may be a better way to do this.
            // we got RuntimeMethodHandles here and we need to go to MethodBase
            // but we don't know whether the reflection info has been initialized
            // or not. So we call GetMethods and GetConstructors on the type
            // and then we fetch the proper MethodBase!!
            IntPtr mh = rgMethodHandle[i];

            if (mh == IntPtr.Zero)
                return null;

            IRuntimeMethodInfo mhReal = RuntimeMethodHandle.GetTypicalMethodDefinition(new RuntimeMethodInfoStub(mh, this));

            return RuntimeType.GetMethodBase(mhReal);
        }

        public virtual int GetOffset(int i) { return rgiOffset[i]; }
        public virtual int GetILOffset(int i) { return rgiILOffset[i]; }
        public virtual string GetFilename(int i) { return rgFilename == null ? null : rgFilename[i]; }
        public virtual int GetLineNumber(int i) { return rgiLineNumber == null ? 0 : rgiLineNumber[i]; }
        public virtual int GetColumnNumber(int i) { return rgiColumnNumber == null ? 0 : rgiColumnNumber[i]; }

        public virtual bool IsLastFrameFromForeignExceptionStackTrace(int i)
        {
            return (rgiLastFrameFromForeignExceptionStackTrace == null) ? false : rgiLastFrameFromForeignExceptionStackTrace[i];
        }

        public virtual int GetNumberOfFrames() { return iFrameCount; }
    }


    // Class which represents a description of a stack trace
    // There is no good reason for the methods of this class to be virtual.  
    public class StackTrace
    {
        private StackFrame[] frames;
        private int m_iNumOfFrames;
        public const int METHODS_TO_SKIP = 0;
        private int m_iMethodsToSkip;

        // Constructs a stack trace from the current location.
        public StackTrace()
        {
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, false, null, null);
        }

        // Constructs a stack trace from the current location.
        //
        public StackTrace(bool fNeedFileInfo)
        {
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, fNeedFileInfo, null, null);
        }

        // Constructs a stack trace from the current location, in a caller's
        // frame
        //
        public StackTrace(int skipFrames)
        {
            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException(nameof(skipFrames),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;

            CaptureStackTrace(skipFrames + METHODS_TO_SKIP, false, null, null);
        }

        // Constructs a stack trace from the current location, in a caller's
        // frame
        //
        public StackTrace(int skipFrames, bool fNeedFileInfo)
        {
            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException(nameof(skipFrames),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;

            CaptureStackTrace(skipFrames + METHODS_TO_SKIP, fNeedFileInfo, null, null);
        }


        // Constructs a stack trace from the current location.
        public StackTrace(Exception e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, false, null, e);
        }

        // Constructs a stack trace from the current location.
        //
        public StackTrace(Exception e, bool fNeedFileInfo)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, fNeedFileInfo, null, e);
        }

        // Constructs a stack trace from the current location, in a caller's
        // frame
        //
        public StackTrace(Exception e, int skipFrames)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException(nameof(skipFrames),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;

            CaptureStackTrace(skipFrames + METHODS_TO_SKIP, false, null, e);
        }

        // Constructs a stack trace from the current location, in a caller's
        // frame
        //
        public StackTrace(Exception e, int skipFrames, bool fNeedFileInfo)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException(nameof(skipFrames),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;

            CaptureStackTrace(skipFrames + METHODS_TO_SKIP, fNeedFileInfo, null, e);
        }


        // Constructs a "fake" stack trace, just containing a single frame.  
        // Does not have the overhead of a full stack trace.
        //
        public StackTrace(StackFrame frame)
        {
            frames = new StackFrame[1];
            frames[0] = frame;
            m_iMethodsToSkip = 0;
            m_iNumOfFrames = 1;
        }


        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void GetStackFramesInternal(StackFrameHelper sfh, int iSkip, bool fNeedFileInfo, Exception e);

        internal static int CalculateFramesToSkip(StackFrameHelper StackF, int iNumFrames)
        {
            int iRetVal = 0;
            string PackageName = "System.Diagnostics";

            // Check if this method is part of the System.Diagnostics
            // package. If so, increment counter keeping track of 
            // System.Diagnostics functions
            for (int i = 0; i < iNumFrames; i++)
            {
                MethodBase mb = StackF.GetMethodBase(i);
                if (mb != null)
                {
                    Type t = mb.DeclaringType;
                    if (t == null)
                        break;
                    string ns = t.Namespace;
                    if (ns == null)
                        break;
                    if (string.Compare(ns, PackageName, StringComparison.Ordinal) != 0)
                        break;
                }
                iRetVal++;
            }

            return iRetVal;
        }

        // Retrieves an object with stack trace information encoded.
        // It leaves out the first "iSkip" lines of the stacktrace.
        //
        private void CaptureStackTrace(int iSkip, bool fNeedFileInfo, Thread targetThread, Exception e)
        {
            m_iMethodsToSkip += iSkip;

            StackFrameHelper StackF = new StackFrameHelper(targetThread);
            
            StackF.InitializeSourceInfo(0, fNeedFileInfo, e);

            m_iNumOfFrames = StackF.GetNumberOfFrames();

            if (m_iMethodsToSkip > m_iNumOfFrames)
                m_iMethodsToSkip = m_iNumOfFrames;

            if (m_iNumOfFrames != 0)
            {
                frames = new StackFrame[m_iNumOfFrames];

                for (int i = 0; i < m_iNumOfFrames; i++)
                {
                    bool fDummy1 = true;
                    bool fDummy2 = true;
                    StackFrame sfTemp = new StackFrame(fDummy1, fDummy2);

                    sfTemp.SetMethodBase(StackF.GetMethodBase(i));
                    sfTemp.SetOffset(StackF.GetOffset(i));
                    sfTemp.SetILOffset(StackF.GetILOffset(i));

                    sfTemp.SetIsLastFrameFromForeignExceptionStackTrace(StackF.IsLastFrameFromForeignExceptionStackTrace(i));

                    if (fNeedFileInfo)
                    {
                        sfTemp.SetFileName(StackF.GetFilename(i));
                        sfTemp.SetLineNumber(StackF.GetLineNumber(i));
                        sfTemp.SetColumnNumber(StackF.GetColumnNumber(i));
                    }

                    frames[i] = sfTemp;
                }

                // CalculateFramesToSkip skips all frames in the System.Diagnostics namespace,
                // but this is not desired if building a stack trace from an exception.
                if (e == null)
                    m_iMethodsToSkip += CalculateFramesToSkip(StackF, m_iNumOfFrames);

                m_iNumOfFrames -= m_iMethodsToSkip;
                if (m_iNumOfFrames < 0)
                {
                    m_iNumOfFrames = 0;
                }
            }

            // In case this is the same object being re-used, set frames to null
            else
            {
                frames = null;
            }
        }

        // Property to get the number of frames in the stack trace
        //
        public virtual int FrameCount
        {
            get { return m_iNumOfFrames; }
        }


        // Returns a given stack frame.  Stack frames are numbered starting at
        // zero, which is the last stack frame pushed.
        //
        public virtual StackFrame GetFrame(int index)
        {
            if ((frames != null) && (index < m_iNumOfFrames) && (index >= 0))
                return frames[index + m_iMethodsToSkip];

            return null;
        }

        // Returns an array of all stack frames for this stacktrace.
        // The array is ordered and sized such that GetFrames()[i] == GetFrame(i)
        // The nth element of this array is the same as GetFrame(n). 
        // The length of the array is the same as FrameCount.
        // 
        public virtual StackFrame[] GetFrames()
        {
            if (frames == null || m_iNumOfFrames <= 0)
                return null;

            // We have to return a subset of the array. Unfortunately this
            // means we have to allocate a new array and copy over.
            StackFrame[] array = new StackFrame[m_iNumOfFrames];
            Array.Copy(frames, m_iMethodsToSkip, array, 0, m_iNumOfFrames);
            return array;
        }

        // Builds a readable representation of the stack trace
        //
        public override string ToString()
        {
            // Include a trailing newline for backwards compatibility
            return ToString(TraceFormat.TrailingNewLine);
        }

        // TraceFormat is Used to specify options for how the 
        // string-representation of a StackTrace should be generated.
        internal enum TraceFormat
        {
            Normal,
            TrailingNewLine,        // include a trailing new line character
            NoResourceLookup    // to prevent infinite resource recusion
        }

        // Builds a readable representation of the stack trace, specifying 
        // the format for backwards compatibility.
        internal string ToString(TraceFormat traceFormat)
        {
            bool displayFilenames = true;   // we'll try, but demand may fail
            string word_At = "at";
            string inFileLineNum = "in {0}:line {1}";

            if (traceFormat != TraceFormat.NoResourceLookup)
            {
                word_At = SR.Word_At;
                inFileLineNum = SR.StackTrace_InFileLineNumber;
            }

            bool fFirstFrame = true;
            StringBuilder sb = new StringBuilder(255);
            for (int iFrameIndex = 0; iFrameIndex < m_iNumOfFrames; iFrameIndex++)
            {
                StackFrame sf = GetFrame(iFrameIndex);
                MethodBase mb = sf.GetMethod();
                if (mb != null && (ShowInStackTrace(mb) || 
                                   (iFrameIndex == m_iNumOfFrames - 1))) // Don't filter last frame
                {
                    // We want a newline at the end of every line except for the last
                    if (fFirstFrame)
                        fFirstFrame = false;
                    else
                        sb.Append(Environment.NewLine);

                    sb.AppendFormat(CultureInfo.InvariantCulture, "   {0} ", word_At);

                    bool isAsync = false;
                    Type declaringType = mb.DeclaringType;
                    string methodName = mb.Name;
                    bool methodChanged = false;
                    if (declaringType != null && declaringType.IsDefined(typeof(CompilerGeneratedAttribute)))
                    {
                        isAsync = typeof(IAsyncStateMachine).IsAssignableFrom(declaringType);
                        if (isAsync || typeof(IEnumerator).IsAssignableFrom(declaringType))
                        {
                            methodChanged = TryResolveStateMachineMethod(ref mb, out declaringType);
                        }
                    }

                    // if there is a type (non global method) print it
                    // ResolveStateMachineMethod may have set declaringType to null
                    if (declaringType != null)
                    {
                        // Append t.FullName, replacing '+' with '.'
                        string fullName = declaringType.FullName;
                        for (int i = 0; i < fullName.Length; i++)
                        {
                            char ch = fullName[i];
                            sb.Append(ch == '+' ? '.' : ch);
                        }
                        sb.Append('.');
                    }
                    sb.Append(mb.Name);

                    // deal with the generic portion of the method
                    if (mb is MethodInfo && ((MethodInfo)mb).IsGenericMethod)
                    {
                        Type[] typars = ((MethodInfo)mb).GetGenericArguments();
                        sb.Append('[');
                        int k = 0;
                        bool fFirstTyParam = true;
                        while (k < typars.Length)
                        {
                            if (fFirstTyParam == false)
                                sb.Append(',');
                            else
                                fFirstTyParam = false;

                            sb.Append(typars[k].Name);
                            k++;
                        }
                        sb.Append(']');
                    }

                    ParameterInfo[] pi = null;
                    try
                    {
                        pi = mb.GetParameters();
                    }
                    catch
                    {
                        // The parameter info cannot be loaded, so we don't
                        // append the parameter list.
                    }
                    if (pi != null)
                    {
                        // arguments printing
                        sb.Append('(');
                        bool fFirstParam = true;
                        for (int j = 0; j < pi.Length; j++)
                        {
                            if (fFirstParam == false)
                                sb.Append(", ");
                            else
                                fFirstParam = false;

                            string typeName = "<UnknownType>";
                            if (pi[j].ParameterType != null)
                                typeName = pi[j].ParameterType.Name;
                            sb.Append(typeName);
                            sb.Append(' ');
                            sb.Append(pi[j].Name);
                        }
                        sb.Append(')');
                    }

                    if (methodChanged)
                    {
                        // Append original method name e.g. +MoveNext()
                        sb.Append("+");
                        sb.Append(methodName);
                        sb.Append("()");
                    }

                    // source location printing
                    if (displayFilenames && (sf.GetILOffset() != -1))
                    {
                        // If we don't have a PDB or PDB-reading is disabled for the module,
                        // then the file name will be null.
                        string fileName = null;

                        // Getting the filename from a StackFrame is a privileged operation - we won't want
                        // to disclose full path names to arbitrarily untrusted code.  Rather than just omit
                        // this we could probably trim to just the filename so it's still mostly usefull.
                        try
                        {
                            fileName = sf.GetFileName();
                        }
                        catch (SecurityException)
                        {
                            // If the demand for displaying filenames fails, then it won't
                            // succeed later in the loop.  Avoid repeated exceptions by not trying again.
                            displayFilenames = false;
                        }

                        if (fileName != null)
                        {
                            // tack on " in c:\tmp\MyFile.cs:line 5"
                            sb.Append(' ');
                            sb.AppendFormat(CultureInfo.InvariantCulture, inFileLineNum, fileName, sf.GetFileLineNumber());
                        }
                    }

                    if (sf.GetIsLastFrameFromForeignExceptionStackTrace() &&
                        !isAsync) // Skip EDI boundary for async
                    {
                        sb.Append(Environment.NewLine);
                        sb.Append(SR.Exception_EndStackTraceFromPreviousThrow);
                    }
                }
            }

            if (traceFormat == TraceFormat.TrailingNewLine)
                sb.Append(Environment.NewLine);

            return sb.ToString();
        }

        private static bool ShowInStackTrace(MethodBase mb)
        {
            Debug.Assert(mb != null);
            return !(mb.IsDefined(typeof(StackTraceHiddenAttribute)) || (mb.DeclaringType?.IsDefined(typeof(StackTraceHiddenAttribute)) ?? false));
        }

        private static bool TryResolveStateMachineMethod(ref MethodBase method, out Type declaringType)
        {
            Debug.Assert(method != null);
            Debug.Assert(method.DeclaringType != null);

            declaringType = method.DeclaringType;

            Type parentType = declaringType.DeclaringType;
            if (parentType == null)
            {
                return false;
            }

            MethodInfo[] methods = parentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (methods == null)
            {
                return false;
            }

            foreach (MethodInfo candidateMethod in methods)
            {
                IEnumerable<StateMachineAttribute> attributes = candidateMethod.GetCustomAttributes<StateMachineAttribute>();
                if (attributes == null)
                {
                    continue;
                }

                foreach (StateMachineAttribute asma in attributes)
                {
                    if (asma.StateMachineType == declaringType)
                    {
                        method = candidateMethod;
                        declaringType = candidateMethod.DeclaringType;
                        // Mark the iterator as changed; so it gets the + annotation of the original method
                        // async statemachines resolve directly to their builder methods so aren't marked as changed
                        return asma is IteratorStateMachineAttribute;
                    }
                }
            }

            return false;
        }
    }
}
