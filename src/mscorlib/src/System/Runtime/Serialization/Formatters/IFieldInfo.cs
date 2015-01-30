// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

 /*============================================================
 **
 **
 **
 ** Purpose: Interface For Returning FieldNames and FieldTypes
 **
 **
 ===========================================================*/

namespace System.Runtime.Serialization.Formatters {

    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System;

[System.Runtime.InteropServices.ComVisible(true)]
    public interface IFieldInfo
    {
        // Name of parameters, if null the default param names will be used
        String[] FieldNames 
        {
            [System.Security.SecurityCritical]  // auto-generated_required
            get;
            [System.Security.SecurityCritical]  // auto-generated_required
            set;
        }
        Type[] FieldTypes 
        {
            [System.Security.SecurityCritical]  // auto-generated_required
            get;
            [System.Security.SecurityCritical]  // auto-generated_required
            set;
        }        
    }
}
