// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when an attempt to load a class fails due to the absence of an entry method.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class EntryPointNotFoundException : TypeLoadException
    {
        public EntryPointNotFoundException()
            : base(SR.Arg_EntryPointNotFoundException)
        {
            HResult = HResults.COR_E_ENTRYPOINTNOTFOUND;
        }

        public EntryPointNotFoundException(string? message)
            : base(message ?? SR.Arg_EntryPointNotFoundException)
        {
            HResult = HResults.COR_E_ENTRYPOINTNOTFOUND;
        }

        public EntryPointNotFoundException(string? message, Exception? inner)
            : base(message ?? SR.Arg_EntryPointNotFoundException, inner)
        {
            HResult = HResults.COR_E_ENTRYPOINTNOTFOUND;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected EntryPointNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
