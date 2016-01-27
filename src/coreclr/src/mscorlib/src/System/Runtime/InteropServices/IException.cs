// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Interface: _Exception
**
**
** Purpose: COM backwards compatibility with v1 Exception
**        object layout.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
    using System;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    
    [GuidAttribute("b36b5c63-42ef-38bc-a07e-0b34c98f164a")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    [CLSCompliant(false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _Exception
    {
#if !FEATURE_CORECLR
        // This contains all of our V1 Exception class's members.

        // From Object
        String ToString();
        bool Equals (Object obj);
        int GetHashCode ();
        Type GetType ();

        // From V1's Exception class
        String Message {
            get;
        }

        Exception GetBaseException();

        String StackTrace {
            get;
        }

        String HelpLink {
            get;
            set;
        }

        String Source {
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            get;
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            set;
        }
        [System.Security.SecurityCritical]  // auto-generated_required
        void GetObjectData(SerializationInfo info, StreamingContext context);
#endif

        //
        // This method is intentionally included in CoreCLR to make Exception.get_InnerException "newslot virtual final".
        // Some phone apps include MEF from desktop Silverlight. MEF's ComposablePartException depends on implicit interface 
        // implementations of get_InnerException to be provided by the base class. It works only if Exception.get_InnerException
        // is virtual.
        //
        Exception InnerException {
            get;
        }

#if !FEATURE_CORECLR        
        MethodBase TargetSite {
            get;
        }
#endif
   }

}
