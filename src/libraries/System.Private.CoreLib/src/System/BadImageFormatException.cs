// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when the file image of an assembly or an executable program is invalid.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class BadImageFormatException : SystemException
    {
        private readonly string? _fileName;  // The name of the corrupt PE file.
        private readonly string? _fusionLog;  // fusion log (when applicable)

        public BadImageFormatException()
            : base(SR.Arg_BadImageFormatException)
        {
            HResult = HResults.COR_E_BADIMAGEFORMAT;
        }

        public BadImageFormatException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_BADIMAGEFORMAT;
        }

        public BadImageFormatException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_BADIMAGEFORMAT;
        }

        public BadImageFormatException(string? message, string? fileName) : base(message)
        {
            HResult = HResults.COR_E_BADIMAGEFORMAT;
            _fileName = fileName;
        }

        public BadImageFormatException(string? message, string? fileName, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_BADIMAGEFORMAT;
            _fileName = fileName;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected BadImageFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _fileName = info.GetString("BadImageFormat_FileName");
            _fusionLog = info.GetString("BadImageFormat_FusionLog");
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("BadImageFormat_FileName", _fileName, typeof(string));
            info.AddValue("BadImageFormat_FusionLog", _fusionLog, typeof(string));
        }

        public override string Message
        {
            get
            {
                SetMessageField();
                return _message!;
            }
        }

        private void SetMessageField()
        {
            if (_message == null)
            {
                if ((_fileName == null) &&
                    (HResult == HResults.COR_E_EXCEPTION))
                    _message = SR.Arg_BadImageFormatException;
                else
                    _message = FileLoadException.FormatFileLoadExceptionMessage(_fileName, HResult);
            }
        }

        public string? FileName => _fileName;

        public override string ToString()
        {
            string s = GetType().ToString() + ": " + Message;

            if (!string.IsNullOrEmpty(_fileName))
                s += Environment.NewLineConst + SR.Format(SR.IO_FileName_Name, _fileName);

            if (InnerException != null)
                s += InnerExceptionPrefix + InnerException.ToString();

            if (StackTrace != null)
                s += Environment.NewLineConst + StackTrace;

            if (_fusionLog != null)
            {
                s ??= " ";
                s += Environment.NewLineConst + Environment.NewLineConst + _fusionLog;
            }

            return s;
        }

        public string? FusionLog => _fusionLog;
    }
}
