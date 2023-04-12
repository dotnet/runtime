// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class TypeLoadException : SystemException
    {
        public TypeLoadException()
            : base(SR.Arg_TypeLoadException)
        {
            HResult = HResults.COR_E_TYPELOAD;
        }

        public TypeLoadException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_TYPELOAD;
        }

        public TypeLoadException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_TYPELOAD;
        }

        public override string Message
        {
            get
            {
                SetMessageField();
                return _message!;
            }
        }

        public string TypeName => _className ?? string.Empty;

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected TypeLoadException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _className = info.GetString("TypeLoadClassName");
            _assemblyName = info.GetString("TypeLoadAssemblyName");
            _messageArg = info.GetString("TypeLoadMessageArg");
            _resourceId = info.GetInt32("TypeLoadResourceID");
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("TypeLoadClassName", _className, typeof(string));
            info.AddValue("TypeLoadAssemblyName", _assemblyName, typeof(string));
            info.AddValue("TypeLoadMessageArg", _messageArg, typeof(string));
            info.AddValue("TypeLoadResourceID", _resourceId);
        }

        // If ClassName != null, GetMessage will construct on the fly using it
        // and ResourceId (mscorrc.dll). This allows customization of the
        // class name format depending on the language environment.
        private string? _className;
        private string? _assemblyName;
        private readonly string? _messageArg;
        private readonly int _resourceId;
    }
}
