// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Exception used to wrap all non-CLS compliant exceptions.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class RuntimeWrappedException : Exception
    {
        private readonly object _wrappedException; // EE expects this name

        // Not an api but has to be public as System.Linq.Expression invokes this through Reflection when an expression
        // throws an object that doesn't derive from Exception.
        public RuntimeWrappedException(object thrownObject)
            : base(SR.RuntimeWrappedException)
        {
            HResult = HResults.COR_E_RUNTIMEWRAPPED;
            _wrappedException = thrownObject;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private RuntimeWrappedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _wrappedException = info.GetValue("WrappedException", typeof(object))!;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("WrappedException", _wrappedException, typeof(object));
        }

        public object WrappedException => _wrappedException;
    }
}
