// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown as a wrapper around the exception thrown by the class initializer.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class TypeInitializationException : SystemException
    {
        private readonly string? _typeName;

        // This exception is not creatable without specifying the
        //    inner exception.
        private TypeInitializationException()
            : base(SR.TypeInitialization_Default)
        {
            HResult = HResults.COR_E_TYPEINITIALIZATION;
        }


        public TypeInitializationException(string? fullTypeName, Exception? innerException)
            : this(fullTypeName, SR.Format(SR.TypeInitialization_Type, fullTypeName), innerException)
        {
        }

        // This is called from within the runtime.  I believe this is necessary
        // for Interop only, though it's not particularly useful.
        internal TypeInitializationException(string? message) : base(message ?? SR.TypeInitialization_Default)
        {
            HResult = HResults.COR_E_TYPEINITIALIZATION;
        }

        internal TypeInitializationException(string? fullTypeName, string? message, Exception? innerException)
            : base(message ?? SR.TypeInitialization_Default, innerException)
        {
            _typeName = fullTypeName;
            HResult = HResults.COR_E_TYPEINITIALIZATION;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private TypeInitializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _typeName = info.GetString("TypeName");
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("TypeName", TypeName, typeof(string));
        }

        public string TypeName => _typeName ?? string.Empty;
    }
}
