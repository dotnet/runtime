// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.IO
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class FileLoadException : IOException
    {
        public FileLoadException()
            : base(SR.IO_FileLoad)
        {
            HResult = HResults.COR_E_FILELOAD;
        }

        public FileLoadException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_FILELOAD;
        }

        public FileLoadException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_FILELOAD;
        }

        public FileLoadException(string? message, string? fileName) : base(message)
        {
            HResult = HResults.COR_E_FILELOAD;
            FileName = fileName;
        }

        public FileLoadException(string? message, string? fileName, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_FILELOAD;
            FileName = fileName;
        }

        public override string Message => _message ??= FormatFileLoadExceptionMessage(FileName, HResult);

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
        protected FileLoadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            FileName = info.GetString("FileLoad_FileName");
            FusionLog = info.GetString("FileLoad_FusionLog");
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("FileLoad_FileName", FileName, typeof(string));
            info.AddValue("FileLoad_FusionLog", FusionLog, typeof(string));
        }
    }
}
