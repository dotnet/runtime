// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Attribute for functions, etc that will be removed.
**
** 
===========================================================*/

using System;

namespace System
{
    // This attribute is attached to members that are not to be used any longer.
    // Message is some human readable explanation of what to use
    // Error indicates if the compiler should treat usage of such a method as an
    //   error. (this would be used if the actual implementation of the obsolete 
    //   method's implementation had changed).
    // 
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
        AttributeTargets.Interface | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Delegate
        , Inherited = false)]
    public sealed class ObsoleteAttribute : Attribute
    {
        private String _message;
        private bool _error;

        public ObsoleteAttribute()
        {
            _message = null;
            _error = false;
        }

        public ObsoleteAttribute(String message)
        {
            _message = message;
            _error = false;
        }

        public ObsoleteAttribute(String message, bool error)
        {
            _message = message;
            _error = error;
        }

        public String Message
        {
            get { return _message; }
        }

        public bool IsError
        {
            get { return _error; }
        }
    }
}
