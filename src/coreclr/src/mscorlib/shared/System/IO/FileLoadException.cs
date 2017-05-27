// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.IO
{
    public partial class FileLoadException : IOException
    {
        public FileLoadException()
            : base(SR.IO_FileLoad)
        {
            HResult = __HResults.COR_E_FILELOAD;
        }

        public FileLoadException(string message)
            : base(message)
        {
            HResult = __HResults.COR_E_FILELOAD;
        }

        public FileLoadException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_FILELOAD;
        }

        public FileLoadException(string message, string fileName) : base(message)
        {
            HResult = __HResults.COR_E_FILELOAD;
            FileName = fileName;
        }

        public FileLoadException(string message, string fileName, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_FILELOAD;
            FileName = fileName;
        }

        public override string Message
        {
            get
            {
                if (_message == null)
                {
                    _message = FormatFileLoadExceptionMessage(FileName, HResult);
                }
                return _message;
            }
        }

        public string FileName { get; }
        public string FusionLog { get; }

        public override string ToString()
        {
            string s = GetType().ToString() + ": " + Message;

            if (FileName != null && FileName.Length != 0)
                s += Environment.NewLine + SR.Format(SR.IO_FileName_Name, FileName);

            if (InnerException != null)
                s = s + " ---> " + InnerException.ToString();

            if (StackTrace != null)
                s += Environment.NewLine + StackTrace;

            if (FusionLog != null)
            {
                if (s == null)
                    s = " ";
                s += Environment.NewLine;
                s += Environment.NewLine;
                s += FusionLog;
            }

            return s;
        }

        protected FileLoadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
