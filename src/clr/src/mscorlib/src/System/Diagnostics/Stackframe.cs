// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics {

    using System.Text;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    // There is no good reason for the methods of this class to be virtual.  
    // In order to ensure trusted code can trust the data it gets from a 
    // StackTrace, we use an InheritanceDemand to prevent partially-trusted
    // subclasses.
#if !FEATURE_CORECLR
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode=true)]
#endif
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class StackFrame
    {
        private MethodBase    method;
        private int           offset;
        private int            ILOffset;
        private String        strFileName;
        private int            iLineNumber;
        private int            iColumnNumber;
    
#if FEATURE_EXCEPTIONDISPATCHINFO
        [System.Runtime.Serialization.OptionalField]
        private bool            fIsLastFrameFromForeignExceptionStackTrace;
#endif // FEATURE_EXCEPTIONDISPATCHINFO

        internal void InitMembers()
        {
            method = null;
            offset = OFFSET_UNKNOWN;
            ILOffset = OFFSET_UNKNOWN;
            strFileName = null;
            iLineNumber = 0;
            iColumnNumber = 0;
#if FEATURE_EXCEPTIONDISPATCHINFO
            fIsLastFrameFromForeignExceptionStackTrace = false;
#endif // FEATURE_EXCEPTIONDISPATCHINFO

        }

        // Constructs a StackFrame corresponding to the active stack frame.
#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]
#endif
        public StackFrame()
        {
            InitMembers();
            BuildStackFrame (0 + StackTrace.METHODS_TO_SKIP, false);// iSkipFrames=0
        }
    
        // Constructs a StackFrame corresponding to the active stack frame.
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public StackFrame(bool fNeedFileInfo)
        {
            InitMembers();
            BuildStackFrame (0 + StackTrace.METHODS_TO_SKIP, fNeedFileInfo);// iSkipFrames=0
        }

        // Constructs a StackFrame corresponding to a calling stack frame.
        // 
        public StackFrame(int skipFrames)
        {
            InitMembers();
            BuildStackFrame (skipFrames + StackTrace.METHODS_TO_SKIP, false);
        }
    
        // Constructs a StackFrame corresponding to a calling stack frame.
        // 
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public StackFrame(int skipFrames, bool fNeedFileInfo)
        {
            InitMembers();
            BuildStackFrame (skipFrames + StackTrace.METHODS_TO_SKIP, fNeedFileInfo);
        }

    
        // Called from the class "StackTrace"
        // 
        internal StackFrame(bool DummyFlag1, bool DummyFlag2)
        {
            InitMembers();
        }
    
        // Constructs a "fake" stack frame, just containing the given file
        // name and line number.  Use when you don't want to use the 
        // debugger's line mapping logic.
        //
        public StackFrame(String fileName, int lineNumber)
        {
            InitMembers();
            BuildStackFrame (StackTrace.METHODS_TO_SKIP, false);
            strFileName = fileName;
            iLineNumber = lineNumber;        
            iColumnNumber = 0;
        }
    

        // Constructs a "fake" stack frame, just containing the given file
        // name, line number and column number.  Use when you don't want to 
        // use the debugger's line mapping logic.
        //
        public StackFrame(String fileName, int lineNumber, int colNumber)
        {
            InitMembers();
            BuildStackFrame (StackTrace.METHODS_TO_SKIP, false);
            strFileName = fileName;
            iLineNumber = lineNumber;        
            iColumnNumber = colNumber;
        }


        // Constant returned when the native or IL offset is unknown
        public const int  OFFSET_UNKNOWN = -1;
        
    
        internal virtual void SetMethodBase (MethodBase mb)
        {
            method = mb;
        }
    
        internal virtual void SetOffset (int iOffset)
        {
            offset = iOffset;
        }
    
        internal virtual void SetILOffset (int iOffset)
        {
            ILOffset = iOffset;
        }

        internal virtual void SetFileName (String strFName)
        {
            strFileName = strFName;
        }

        internal virtual void SetLineNumber (int iLine)
        {
            iLineNumber = iLine;
        }

        internal virtual void SetColumnNumber (int iCol)
        {
            iColumnNumber = iCol;
        }

#if FEATURE_EXCEPTIONDISPATCHINFO
        internal virtual void SetIsLastFrameFromForeignExceptionStackTrace (bool fIsLastFrame)
        {
            fIsLastFrameFromForeignExceptionStackTrace = fIsLastFrame;
        }

        internal virtual bool GetIsLastFrameFromForeignExceptionStackTrace()
        {
            return fIsLastFrameFromForeignExceptionStackTrace;
        }
#endif // FEATURE_EXCEPTIONDISPATCHINFO

        // Returns the method the frame is executing
        // 
        public virtual MethodBase GetMethod ()
        {
            Contract.Ensures(Contract.Result<MethodBase>() != null);

            return method;
        }
    
        // Returns the offset from the start of the native (jitted) code for the
        // method being executed
        // 
        public virtual int GetNativeOffset ()
        {
            return offset;
        }
    
    
        // Returns the offset from the start of the IL code for the
        // method being executed.  This offset may be approximate depending
        // on whether the jitter is generating debuggable code or not.
        // 
        public virtual int GetILOffset()
        {
            return ILOffset;
        }    
    
        // Returns the file name containing the code being executed.  This 
        // information is normally extracted from the debugging symbols
        // for the executable.
        //
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        public virtual String GetFileName()
        {
            if (strFileName != null)
            {
                // This isn't really correct, but we don't have
                // a permission that protects discovery of potentially
                // local urls so we'll use this.

                FileIOPermission perm = new FileIOPermission( PermissionState.None );
                perm.AllFiles = FileIOPermissionAccess.PathDiscovery;
                perm.Demand();
            }

            return strFileName;
        }
    
        // Returns the line number in the file containing the code being executed.  
        // This information is normally extracted from the debugging symbols
        // for the executable.
        //
        public virtual int GetFileLineNumber()
        {
            return iLineNumber;
        }

        // Returns the column number in the line containing the code being executed.  
        // This information is normally extracted from the debugging symbols
        // for the executable.
        //
        public virtual int GetFileColumnNumber()
        {
            return iColumnNumber;
        }

    
        // Builds a readable representation of the stack frame
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder(255);

            if (method != null)
            {
                sb.Append(method.Name);

                // deal with the generic portion of the method
                if (method is MethodInfo && ((MethodInfo)method).IsGenericMethod)
                {
                    Type[] typars = ((MethodInfo)method).GetGenericArguments();

                    sb.Append('<');
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

                    sb.Append('>');
                }

                sb.Append(" at offset ");
                if (offset == OFFSET_UNKNOWN)
                    sb.Append("<offset unknown>");
                else
                    sb.Append(offset);

                sb.Append(" in file:line:column ");

                bool useFileName = (strFileName != null);

                if (useFileName)
                {
                    try
                    {
                        // This isn't really correct, but we don't have
                        // a permission that protects discovery of potentially
                        // local urls so we'll use this.

                        FileIOPermission perm = new FileIOPermission(PermissionState.None);
                        perm.AllFiles = FileIOPermissionAccess.PathDiscovery;
                        perm.Demand();
                    }
                    catch (System.Security.SecurityException)
                    {
                        useFileName = false;
                    }
                }

                if (!useFileName)
                    sb.Append("<filename unknown>");
                else
                    sb.Append(strFileName);
                sb.Append(':');
                sb.Append(iLineNumber);
                sb.Append(':');
                sb.Append(iColumnNumber);
            }
            else
            {
                sb.Append("<null>");
            }
            sb.Append(Environment.NewLine);

            return sb.ToString(); 
        }
    
    
        private void BuildStackFrame(int skipFrames, bool fNeedFileInfo)
        {
            using (StackFrameHelper StackF = new StackFrameHelper(null))
            {
                StackF.InitializeSourceInfo(0, fNeedFileInfo, null);

                int iNumOfFrames = StackF.GetNumberOfFrames();

                skipFrames += StackTrace.CalculateFramesToSkip(StackF, iNumOfFrames);

                if ((iNumOfFrames - skipFrames) > 0)
                {
                    method = StackF.GetMethodBase(skipFrames);
                    offset = StackF.GetOffset(skipFrames);
                    ILOffset = StackF.GetILOffset(skipFrames);
                    if (fNeedFileInfo)
                    {
                        strFileName = StackF.GetFilename(skipFrames);
                        iLineNumber = StackF.GetLineNumber(skipFrames);
                        iColumnNumber = StackF.GetColumnNumber(skipFrames);
                    }
                }
            }
        }
    }
}
