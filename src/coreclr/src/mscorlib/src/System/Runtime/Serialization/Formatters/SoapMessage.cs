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
    using System.Runtime.Remoting.Messaging;
    using System.Runtime.Serialization;
    using System;
    // Class is used to return the call object for a SOAP call.
    // This is used when the top SOAP object is a fake object, it contains
    // a method name as the element name instead of the object name.
   [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public class SoapMessage : ISoapMessage
    {
        internal String[] paramNames;
        internal Object[] paramValues;
        internal Type[] paramTypes;
        internal String methodName;
        internal String xmlNameSpace;
        internal Header[] headers;
        
        // Name of parameters, if null the default param names will be used
        public String[] ParamNames
        {
            get {return paramNames;}
            set {paramNames = value;}
        }    
        
        // Parameter Values
        public Object[] ParamValues
        {
            get {return paramValues;}
            set {paramValues = value;}
        }

        public Type[] ParamTypes
        {
            get {return paramTypes;}
            set {paramTypes = value;}            
        }

        // MethodName
        public String MethodName
        {
            get {return methodName;}
            set {methodName = value;}
        }

        // MethodName XmlNameSpace
        public String XmlNameSpace
        {
            get {return xmlNameSpace;}
            set {xmlNameSpace = value;}
        }

        // Headers
        public Header[] Headers
        {
            get {return headers;}
            set {headers = value;}
        }

        
    }
}
#endif // FEATURE_REMOTING
