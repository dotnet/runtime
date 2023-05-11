// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.IO
{
    // Thrown when trying to access a file that doesn't exist on disk.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class FileNotFoundException : IOException
    {
        public FileNotFoundException()
            : base(SR.IO_FileNotFound)
        {
            HResult = HResults.COR_E_FILENOTFOUND;
        }

        public FileNotFoundException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_FILENOTFOUND;
        }

        public FileNotFoundException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_FILENOTFOUND;
        }

        public FileNotFoundException(string? message, string? fileName)
            : base(message)
        {
            HResult = HResults.COR_E_FILENOTFOUND;
            FileName = fileName;
        }

        public FileNotFoundException(string? message, string? fileName, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_FILENOTFOUND;
            FileName = fileName;
        }

        public override string Message
        {
            get
            {
                SetMessageField();
                Debug.Assert(_message != null, "_message was null after calling SetMessageField");
                return _message;
            }
        }

        private void SetMessageField()
        {
            if (_message == null)
            {
                if ((FileName == null) &&
                    (HResult == System.HResults.COR_E_EXCEPTION))
                    _message = SR.IO_FileNotFound;
                else if (FileName != null)
                    _message = FileLoadException.FormatFileLoadExceptionMessage(FileName, HResult);
            }
        }

        public string? FileName { get; }
        public string? FusionLog { get; }

        public override string ToString()
        {
            string s = GetType().ToString() + ": " + Message;

            if (!string.IsNullOrEmpty(FileName))
                s += Environment.NewLineConst + SR.Format(SR.IO_FileName_Name, FileName);

            if (InnerException != null)
                s += Environment.NewLineConst + InnerExceptionPrefix + InnerException.ToString();

            if (StackTrace != null)
                s += Environment.NewLineConst + StackTrace;

            if (FusionLog != null)
            {
                s ??= " ";
                s += Environment.NewLineConst + Environment.NewLineConst + FusionLog;
            }
            return s;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected FileNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            FileName = info.GetString("FileNotFound_FileName");
            FusionLog = info.GetString("FileNotFound_FusionLog");
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("FileNotFound_FileName", FileName, typeof(string));
            info.AddValue("FileNotFound_FusionLog", FusionLog, typeof(string));
        }
    }
}
