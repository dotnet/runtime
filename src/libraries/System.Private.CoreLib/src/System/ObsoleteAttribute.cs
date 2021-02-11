// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: Attribute for functions, etc that will be removed.
**
**
===========================================================*/

namespace System
{
    // This attribute is attached to members that are not to be used any longer.
    // Message is some human readable explanation of what to use
    // Error indicates if the compiler should treat usage of such a method as an
    //   error. (this would be used if the actual implementation of the obsolete
    //   method's implementation had changed).
    // DiagnosticId. Represents the ID the compiler will use when reporting a use of the API.
    // UrlFormat.The URL that should be used by an IDE for navigating to corresponding documentation. Instead of taking the URL directly,
    //   the API takes a format string. This allows having a generic URL that includes the diagnostic ID.
    //
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
        AttributeTargets.Interface | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Delegate,
        Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    sealed class ObsoleteAttribute : Attribute
    {
        public ObsoleteAttribute()
        {
        }

        public ObsoleteAttribute(string? message)
        {
            Message = message;
        }

        public ObsoleteAttribute(string? message, bool error)
        {
            Message = message;
            IsError = error;
        }

        public string? Message { get; }

        public bool IsError { get; }

        public string? DiagnosticId { get; set; }

        public string? UrlFormat { get; set; }
    }
}
