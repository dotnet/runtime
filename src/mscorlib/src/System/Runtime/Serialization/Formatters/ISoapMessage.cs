// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

 /*============================================================
 **
 **
 **
 ** Purpose: Interface For Soap Method Call
 **
 **
 ===========================================================*/
#if FEATURE_REMOTING
namespace System.Runtime.Serialization.Formatters {

    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Runtime.Remoting.Messaging;    
    using System;
    // Used to specify a call record to either the binary or xml serializer
    // The call record can be transmitted as the SOAP Top record which contains
    // a method name instead of an object name as the Top record's element name
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ISoapMessage
    {
        // Name of parameters, if null the default param names will be used

        String[] ParamNames {get; set;}
    
        // Parameter Values
        Object[] ParamValues {get; set;}

        // Parameter Types
        Type[] ParamTypes {get; set;}        
    
        // MethodName
        String MethodName {get; set;}

        // MethodName XmlNameSpace
        String XmlNameSpace {get; set;}

        // Headers
        Header[] Headers {get; set;}
    }
}
#endif // FEATURE_REMOTING

