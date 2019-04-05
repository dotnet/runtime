// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Exception to an invalid dll or executable format.
**
** 
===========================================================*/

#nullable enable
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class BadImageFormatException : SystemException
    {
        private string? _fileName;  // The name of the corrupt PE file.
        private string? _fusionLog;  // fusion log (when applicable)

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

        protected BadImageFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _fileName = info.GetString("BadImageFormat_FileName");
            _fusionLog = info.GetString("BadImageFormat_FusionLog");
        }

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

        public string? FileName
        {
            get { return _fileName; }
        }

        public override string ToString()
        {
            string s = GetType().ToString() + ": " + Message;

            if (_fileName != null && _fileName.Length != 0)
                s += Environment.NewLine + SR.Format(SR.IO_FileName_Name, _fileName);

            if (InnerException != null)
                s = s + " ---> " + InnerException.ToString();

            if (StackTrace != null)
                s += Environment.NewLine + StackTrace;

            if (_fusionLog != null)
            {
                if (s == null)
                    s = " ";
                s += Environment.NewLine;
                s += Environment.NewLine;
                s += _fusionLog;
            }

            return s;
        }

        public string? FusionLog
        {
            get { return _fusionLog; }
        }
    }
}
