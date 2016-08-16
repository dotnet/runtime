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
** Purpose: Exception for accessing a file that doesn't exist.
**
**
===========================================================*/

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using SecurityException = System.Security.SecurityException;
using System.Globalization;

namespace System.IO {
    // Thrown when trying to access a file that doesn't exist on disk.
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class FileNotFoundException : IOException {

        private String _fileName;  // The name of the file that isn't found.
        private String _fusionLog;  // fusion log (when applicable)
        
        public FileNotFoundException() 
            : base(Environment.GetResourceString("IO.FileNotFound")) {
            SetErrorCode(__HResults.COR_E_FILENOTFOUND);
        }
    
        public FileNotFoundException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_FILENOTFOUND);
        }
    
        public FileNotFoundException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_FILENOTFOUND);
        }

        public FileNotFoundException(String message, String fileName) : base(message)
        {
            SetErrorCode(__HResults.COR_E_FILENOTFOUND);
            _fileName = fileName;
        }

        public FileNotFoundException(String message, String fileName, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_FILENOTFOUND);
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
            if (_message == null) {
                if ((_fileName == null) &&
                    (HResult == System.__HResults.COR_E_EXCEPTION))
                    _message = Environment.GetResourceString("IO.FileNotFound");

                else if( _fileName != null)
                    _message = FileLoadException.FormatFileLoadExceptionMessage(_fileName, HResult);
            }
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
#endif            
            return s;
            
        }

        protected FileNotFoundException(SerializationInfo info, StreamingContext context) : base (info, context) {
            // Base class constructor will check info != null.

            _fileName = info.GetString("FileNotFound_FileName");
#if FEATURE_FUSION
            try
            {
                _fusionLog = info.GetString("FileNotFound_FusionLog");
            }
            catch 
            {
                _fusionLog = null;
            }
#endif
        }

        private FileNotFoundException(String fileName, String fusionLog,int hResult)
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
#endif

        [System.Security.SecurityCritical]  // auto-generated_required
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            // Serialize data for our base classes.  base will verify info != null.
            base.GetObjectData(info, context);

            // Serialize data for this class
            info.AddValue("FileNotFound_FileName", _fileName, typeof(String));

#if FEATURE_FUSION
            try
            {
                info.AddValue("FileNotFound_FusionLog", FusionLog, typeof(String));
            }
            catch (SecurityException)
            {
            }
#endif
        }
    }
}

