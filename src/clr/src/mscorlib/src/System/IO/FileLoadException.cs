// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
** 
** 
**
**
** Purpose: Exception for failure to load a file that was successfully found.
**
**
===========================================================*/

using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Runtime.Versioning;
using SecurityException = System.Security.SecurityException;

namespace System.IO {

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class FileLoadException : IOException {

        private String _fileName;   // the name of the file we could not load.
        private String _fusionLog;  // fusion log (when applicable)

        public FileLoadException() 
            : base(Environment.GetResourceString("IO.FileLoad")) {
            SetErrorCode(__HResults.COR_E_FILELOAD);
        }
    
        public FileLoadException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_FILELOAD);
        }
    
        public FileLoadException(String message, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_FILELOAD);
        }

        public FileLoadException(String message, String fileName) : base(message)
        {
            SetErrorCode(__HResults.COR_E_FILELOAD);
            _fileName = fileName;
        }

        public FileLoadException(String message, String fileName, Exception inner) 
            : base(message, inner) {
            SetErrorCode(__HResults.COR_E_FILELOAD);
            _fileName = fileName;
        }

        public override String Message
        {
            get {
                SetMessageField();
                return _message;
            }
        }

        private void SetMessageField()
        {
            if (_message == null)
                _message = FormatFileLoadExceptionMessage(_fileName, HResult);
        }

        public String FileName {
            get { return _fileName; }
        }

        public override String ToString()
        {
            String s = GetType().FullName + ": " + Message;

            if (_fileName != null && _fileName.Length != 0)
                s += Environment.NewLine + Environment.GetResourceString("IO.FileName_Name", _fileName);
            
            if (InnerException != null)
                s = s + " ---> " + InnerException.ToString();

            if (StackTrace != null)
                s += Environment.NewLine + StackTrace;

#if FEATURE_FUSION
            try
            {
                if(FusionLog!=null)
                {
                    if (s==null)
                        s=" ";
                    s+=Environment.NewLine;
                    s+=Environment.NewLine;
                    s+=FusionLog;
                }
            }
            catch(SecurityException)
            {
            
            }
#endif // FEATURE_FUSION

            return s;
        }

        protected FileLoadException(SerializationInfo info, StreamingContext context) : base (info, context) {
            // Base class constructor will check info != null.

            _fileName = info.GetString("FileLoad_FileName");

#if FEATURE_FUSION
            try
            {
                _fusionLog = info.GetString("FileLoad_FusionLog");
            }
            catch 
            {
                _fusionLog = null;
            }
#endif 
        }

        private FileLoadException(String fileName, String fusionLog,int hResult)
            : base(null)
        {
            SetErrorCode(hResult);
            _fileName = fileName;
            _fusionLog=fusionLog;
            SetMessageField();
        }

#if FEATURE_FUSION
        public String FusionLog {
            [System.Security.SecuritySafeCritical]  // auto-generated
            [SecurityPermissionAttribute( SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlEvidence | SecurityPermissionFlag.ControlPolicy)]
            get { return _fusionLog; }
        }
#endif // FEATURE_FUSION

        [System.Security.SecurityCritical]  // auto-generated_required
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            // Serialize data for our base classes.  base will verify info != null.
            base.GetObjectData(info, context);

            // Serialize data for this class
            info.AddValue("FileLoad_FileName", _fileName, typeof(String));

#if FEATURE_FUSION
            try
            {
                info.AddValue("FileLoad_FusionLog", FusionLog, typeof(String));
            }
            catch (SecurityException)
            {
            }
#endif
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static String FormatFileLoadExceptionMessage(String fileName,
            int hResult)
        {
            string format = null;
            GetFileLoadExceptionMessage(hResult, JitHelpers.GetStringHandleOnStack(ref format));

            string message = null;
            GetMessageForHR(hResult, JitHelpers.GetStringHandleOnStack(ref message));

            return String.Format(CultureInfo.CurrentCulture, format, fileName, message);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetFileLoadExceptionMessage(int hResult, StringHandleOnStack retString);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetMessageForHR(int hresult, StringHandleOnStack retString);
    }
}
