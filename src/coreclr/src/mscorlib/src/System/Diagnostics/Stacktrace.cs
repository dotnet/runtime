// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics {
    using System;
    using System.Collections;
    using System.Text;
    using System.Threading;
    using System.Security;
    using System.Security.Permissions;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    // READ ME:
    // Modifying the order or fields of this object may require other changes 
    // to the unmanaged definition of the StackFrameHelper class, in 
    // VM\DebugDebugger.h. The binder will catch some of these layout problems.
    [Serializable]
    internal class StackFrameHelper : IDisposable
    {
        [NonSerialized]
        private Thread targetThread;
        private int[] rgiOffset;
        private int[] rgiILOffset;
        // this field is here only for backwards compatibility of serialization format
        private MethodBase[] rgMethodBase;

#pragma warning disable 414
        // dynamicMethods is an array of System.Resolver objects, used to keep
        // DynamicMethodDescs alive for the lifetime of StackFrameHelper.
        private Object dynamicMethods; // Field is not used from managed.        

        [NonSerialized]
        private IntPtr[] rgMethodHandle;
        private String[] rgAssemblyPath;
        private IntPtr[] rgLoadedPeAddress;
        private int[] rgiLoadedPeSize;
        private IntPtr[] rgInMemoryPdbAddress;
        private int[] rgiInMemoryPdbSize;
        // if rgiMethodToken[i] == 0, then don't attempt to get the portable PDB source/info
        private int[] rgiMethodToken;
        private String[] rgFilename;
        private int[] rgiLineNumber;
        private int[] rgiColumnNumber;
#if FEATURE_EXCEPTIONDISPATCHINFO
        [OptionalField]
        private bool[] rgiLastFrameFromForeignExceptionStackTrace;
#endif // FEATURE_EXCEPTIONDISPATCHINFO
        private GetSourceLineInfoDelegate getSourceLineInfo;
        private int iFrameCount;
#pragma warning restore 414

        private delegate void GetSourceLineInfoDelegate(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize,
            IntPtr inMemoryPdbAddress, int inMemoryPdbSize, int methodToken, int ilOffset, 
            out string sourceFile, out int sourceLine, out int sourceColumn);

#if FEATURE_CORECLR
        private static Type s_symbolsType = null;
        private static MethodInfo s_symbolsMethodInfo = null;

        [ThreadStatic]
        private static int t_reentrancy = 0;
#endif
        
        public StackFrameHelper(Thread target)
        {
            targetThread = target;
            rgMethodBase = null;
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
            getSourceLineInfo = null;

#if FEATURE_EXCEPTIONDISPATCHINFO
            rgiLastFrameFromForeignExceptionStackTrace = null;
#endif // FEATURE_EXCEPTIONDISPATCHINFO

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

#if FEATURE_CORECLR
            if (!fNeedFileInfo)
                return;

            // Check if this function is being reentered because of an exception in the code below
            if (t_reentrancy > 0)
                return;

            t_reentrancy++;
            try
            {
                if (s_symbolsMethodInfo == null)
                {
                    s_symbolsType = Type.GetType(
                        "System.Diagnostics.StackTraceSymbols, System.Diagnostics.StackTrace, Version=4.0.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                        throwOnError: false);

                    if (s_symbolsType == null)
                        return;

                    s_symbolsMethodInfo = s_symbolsType.GetMethod("GetSourceLineInfo", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (s_symbolsMethodInfo == null)
                        return;
                }

                if (getSourceLineInfo == null)
                {
                    // Create an instance of System.Diagnostics.Stacktrace.Symbols
                    object target = Activator.CreateInstance(s_symbolsType);

                    // Create an instance delegate for the GetSourceLineInfo method
                    getSourceLineInfo = (GetSourceLineInfoDelegate)s_symbolsMethodInfo.CreateDelegate(typeof(GetSourceLineInfoDelegate), target);
                }

                for (int index = 0; index < iFrameCount; index++)
                {
                    // If there was some reason not to try get the symbols from the portable PDB reader like the module was
                    // ENC or the source/line info was already retrieved, the method token is 0.
                    if (rgiMethodToken[index] != 0)
                    {
                        getSourceLineInfo(rgAssemblyPath[index], rgLoadedPeAddress[index], rgiLoadedPeSize[index],
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
#endif
        }

        void IDisposable.Dispose()
        {
#if FEATURE_CORECLR
            if (getSourceLineInfo != null)
            {
                IDisposable disposable = getSourceLineInfo.Target as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
#endif
        }

        [System.Security.SecuritySafeCritical]
        public virtual MethodBase GetMethodBase(int i) 
        { 
            // There may be a better way to do this.
            // we got RuntimeMethodHandles here and we need to go to MethodBase
            // but we don't know whether the reflection info has been initialized
            // or not. So we call GetMethods and GetConstructors on the type
            // and then we fetch the proper MethodBase!!
            IntPtr mh = rgMethodHandle[i];
            
            if (mh.IsNull()) 
                return null;

            IRuntimeMethodInfo mhReal = RuntimeMethodHandle.GetTypicalMethodDefinition(new RuntimeMethodInfoStub(mh, this));

            return RuntimeType.GetMethodBase(mhReal);
        }

        public virtual int GetOffset(int i) { return rgiOffset[i];}
        public virtual int GetILOffset(int i) { return rgiILOffset[i];}
        public virtual String GetFilename(int i) { return rgFilename == null ? null : rgFilename[i];}
        public virtual int GetLineNumber(int i) { return rgiLineNumber == null ? 0 : rgiLineNumber[i];}
        public virtual int GetColumnNumber(int i) { return rgiColumnNumber == null ? 0 : rgiColumnNumber[i];}

#if FEATURE_EXCEPTIONDISPATCHINFO
        public virtual bool IsLastFrameFromForeignExceptionStackTrace(int i) 
        { 
            return (rgiLastFrameFromForeignExceptionStackTrace == null)?false:rgiLastFrameFromForeignExceptionStackTrace[i];
        } 
#endif // FEATURE_EXCEPTIONDISPATCHINFO

        public virtual int GetNumberOfFrames() { return iFrameCount;}
        public virtual void SetNumberOfFrames(int i) { iFrameCount = i;}
    
        //
        // serialization implementation
        //
        [OnSerializing]
        [SecuritySafeCritical]
        void OnSerializing(StreamingContext context)
        {
            // this is called in the process of serializing this object.
            // For compatibility with Everett we need to assign the rgMethodBase field as that is the field
            // that will be serialized
            rgMethodBase = (rgMethodHandle == null) ? null : new MethodBase[rgMethodHandle.Length];
            if (rgMethodHandle != null) 
            {
                for (int i = 0; i < rgMethodHandle.Length; i++) 
                {
                    if (!rgMethodHandle[i].IsNull())
                        rgMethodBase[i] = RuntimeType.GetMethodBase(new RuntimeMethodInfoStub(rgMethodHandle[i], this));
                }
            }
        }

        [OnSerialized]
        void OnSerialized(StreamingContext context)
        {
            // after we are done serializing null the rgMethodBase field
            rgMethodBase = null;
        }

        [OnDeserialized]
        [SecuritySafeCritical]
        void OnDeserialized(StreamingContext context)
        {
            // after we are done deserializing we need to transform the rgMethodBase in rgMethodHandle
            rgMethodHandle = (rgMethodBase == null) ? null : new IntPtr[rgMethodBase.Length];
            if (rgMethodBase != null) 
            {
                for (int i = 0; i < rgMethodBase.Length; i++) 
                {
                    if (rgMethodBase[i] != null)
                        rgMethodHandle[i] = rgMethodBase[i].MethodHandle.Value;
                }
            }
            rgMethodBase = null;
        }
    }
    
    
    // Class which represents a description of a stack trace
    // There is no good reason for the methods of this class to be virtual.  
    // In order to ensure trusted code can trust the data it gets from a 
    // StackTrace, we use an InheritanceDemand to prevent partially-trusted
    // subclasses.
#if !FEATURE_CORECLR
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode=true)]
#endif
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class StackTrace
    {
        private StackFrame[] frames;
        private int m_iNumOfFrames;
        public const int METHODS_TO_SKIP = 0;
        private int m_iMethodsToSkip;

        // Constructs a stack trace from the current location.
#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]
#endif
        public StackTrace()
        {
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, false, null, null);
        }

        // Constructs a stack trace from the current location.
        //
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public StackTrace(bool fNeedFileInfo)
        {
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, fNeedFileInfo, null, null);
        }
    
        // Constructs a stack trace from the current location, in a caller's
        // frame
        //
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public StackTrace(int skipFrames)
        {
    
            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException("skipFrames", 
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
    
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
    
            CaptureStackTrace(skipFrames+METHODS_TO_SKIP, false, null, null);
        }
 
        // Constructs a stack trace from the current location, in a caller's
        // frame
        //
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public StackTrace(int skipFrames, bool fNeedFileInfo)
        {
    
            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException("skipFrames", 
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
    
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
    
            CaptureStackTrace(skipFrames+METHODS_TO_SKIP, fNeedFileInfo, null, null);
        }
 
    
        // Constructs a stack trace from the current location.
        public StackTrace(Exception e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            Contract.EndContractBlock();

            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, false, null, e);
        }

        // Constructs a stack trace from the current location.
        //
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public StackTrace(Exception e, bool fNeedFileInfo)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            Contract.EndContractBlock();

            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, fNeedFileInfo, null, e);
        }
    
        // Constructs a stack trace from the current location, in a caller's
        // frame
        //
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public StackTrace(Exception e, int skipFrames)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException("skipFrames", 
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
    
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
    
            CaptureStackTrace(skipFrames+METHODS_TO_SKIP, false, null, e);
        }
 
        // Constructs a stack trace from the current location, in a caller's
        // frame
        //
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public StackTrace(Exception e, int skipFrames, bool fNeedFileInfo)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException("skipFrames", 
                    Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
    
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
    
            CaptureStackTrace(skipFrames+METHODS_TO_SKIP, fNeedFileInfo, null, e);
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


        // Constructs a stack trace for the given thread
        //
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        [Obsolete("This constructor has been deprecated.  Please use a constructor that does not require a Thread parameter.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public StackTrace(Thread targetThread, bool needFileInfo)
        {    
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;

            CaptureStackTrace(METHODS_TO_SKIP, needFileInfo, targetThread, null);

        }

        [System.Security.SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void GetStackFramesInternal(StackFrameHelper sfh, int iSkip, bool fNeedFileInfo, Exception e);
    
        internal static int CalculateFramesToSkip(StackFrameHelper StackF, int iNumFrames)
        {
            int iRetVal = 0;
            String PackageName = "System.Diagnostics";
    
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
                    String ns = t.Namespace;
                    if (ns == null)     
                        break;
                    if (String.Compare(ns, PackageName, StringComparison.Ordinal) != 0)
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

            using (StackFrameHelper StackF = new StackFrameHelper(targetThread))
            {
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

#if FEATURE_EXCEPTIONDISPATCHINFO
                    sfTemp.SetIsLastFrameFromForeignExceptionStackTrace(StackF.IsLastFrameFromForeignExceptionStackTrace(i));
#endif // FEATURE_EXCEPTIONDISPATCHINFO

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
                    frames = null;
            }
        }
    
        // Property to get the number of frames in the stack trace
        //
        public virtual int FrameCount
        {
            get { return m_iNumOfFrames;}
        }
    
    
        // Returns a given stack frame.  Stack frames are numbered starting at
        // zero, which is the last stack frame pushed.
        //
        public virtual StackFrame GetFrame(int index)
        {
            if ((frames != null) && (index < m_iNumOfFrames) && (index >= 0))
                return frames[index+m_iMethodsToSkip];
    
            return null;
        }

        // Returns an array of all stack frames for this stacktrace.
        // The array is ordered and sized such that GetFrames()[i] == GetFrame(i)
        // The nth element of this array is the same as GetFrame(n). 
        // The length of the array is the same as FrameCount.
        // 
        [ComVisible(false)]
        public virtual StackFrame [] GetFrames()
        {
            if (frames == null || m_iNumOfFrames <= 0)
                return null;
                
            // We have to return a subset of the array. Unfortunately this
            // means we have to allocate a new array and copy over.
            StackFrame [] array = new StackFrame[m_iNumOfFrames];
            Array.Copy(frames, m_iMethodsToSkip, array, 0, m_iNumOfFrames);
            return array;
        }
    
        // Builds a readable representation of the stack trace
        //
#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] 
#endif
        public override String ToString()
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
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        internal String ToString(TraceFormat traceFormat)
        {
            bool displayFilenames = true;   // we'll try, but demand may fail
            String word_At = "at";
            String inFileLineNum = "in {0}:line {1}";

            if(traceFormat != TraceFormat.NoResourceLookup)
            {
                word_At = Environment.GetResourceString("Word_At");
                inFileLineNum = Environment.GetResourceString("StackTrace_InFileLineNumber");
            }
            
            bool fFirstFrame = true;
            StringBuilder sb = new StringBuilder(255);
            for (int iFrameIndex = 0; iFrameIndex < m_iNumOfFrames; iFrameIndex++)
            {
                StackFrame sf = GetFrame(iFrameIndex);
                MethodBase mb = sf.GetMethod();
                if (mb != null)
                {
                    // We want a newline at the end of every line except for the last
                    if (fFirstFrame)
                        fFirstFrame = false;
                    else
                        sb.Append(Environment.NewLine);
                    
                    sb.AppendFormat(CultureInfo.InvariantCulture, "   {0} ", word_At);

                    Type t = mb.DeclaringType;
                     // if there is a type (non global method) print it
                    if (t != null)
                    {
                        // Append t.FullName, replacing '+' with '.'
                        string fullName = t.FullName;
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
                        int k=0;
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
#if FEATURE_CORECLR
                    try
                    {
#endif
                        pi = mb.GetParameters();
#if FEATURE_CORECLR
                    }
                    catch
                    {
                        // The parameter info cannot be loaded, so we don't
                        // append the parameter list.
                    }
#endif
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

                            String typeName = "<UnknownType>";
                            if (pi[j].ParameterType != null)
                                typeName = pi[j].ParameterType.Name;
                            sb.Append(typeName);
                            sb.Append(' ');
                            sb.Append(pi[j].Name);
                        }   
                        sb.Append(')');
                    }

                    // source location printing
                    if (displayFilenames && (sf.GetILOffset() != -1))
                    {
                        // If we don't have a PDB or PDB-reading is disabled for the module,
                        // then the file name will be null.
                        String fileName = null;
                        
                        // Getting the filename from a StackFrame is a privileged operation - we won't want
                        // to disclose full path names to arbitrarily untrusted code.  Rather than just omit
                        // this we could probably trim to just the filename so it's still mostly usefull.
                        try
                        {
                            fileName = sf.GetFileName();
                        }
#if FEATURE_CAS_POLICY
                        catch (NotSupportedException)
                        {
                            // Having a deprecated stack modifier on the callstack (such as Deny) will cause
                            // a NotSupportedException to be thrown.  Since we don't know if the app can
                            // access the file names, we'll conservatively hide them.
                            displayFilenames = false;
                        }
#endif // FEATURE_CAS_POLICY
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

#if FEATURE_EXCEPTIONDISPATCHINFO
                    if (sf.GetIsLastFrameFromForeignExceptionStackTrace())
                    {
                        sb.Append(Environment.NewLine);
                        sb.Append(Environment.GetResourceString("Exception_EndStackTraceFromPreviousThrow"));
                    }
#endif // FEATURE_EXCEPTIONDISPATCHINFO
                }
            }

            if(traceFormat == TraceFormat.TrailingNewLine)
                sb.Append(Environment.NewLine);
            
            return sb.ToString(); 
        }

        // This helper is called from within the EE to construct a string representation
        // of the current stack trace.
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        private static String GetManagedStackTraceStringHelper(bool fNeedFileInfo)
        {
            // Note all the frames in System.Diagnostics will be skipped when capturing 
            // a normal stack trace (not from an exception) so we don't need to explicitly
            // skip the GetManagedStackTraceStringHelper frame.
            StackTrace st = new StackTrace(0, fNeedFileInfo);
            return st.ToString();
        }
    }

}
