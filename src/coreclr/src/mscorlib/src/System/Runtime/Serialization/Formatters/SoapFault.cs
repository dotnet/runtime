// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
 **
 **
 ** Purpose: Specifies information for a Soap Fault
 **
 **
 ===========================================================*/
#if FEATURE_REMOTING
namespace System.Runtime.Serialization.Formatters
{
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Metadata;
    using System.Globalization;
    using System.Security.Permissions;

    //* Class holds soap fault information

[Serializable]
[SoapType(Embedded=true)]    
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class SoapFault : ISerializable
    {
        String faultCode;
        String faultString;
        String faultActor;
        [SoapField(Embedded=true)] Object detail;

        public SoapFault()
        {
        }

        public SoapFault(String faultCode, String faultString, String faultActor, ServerFault serverFault)
        {
            this.faultCode = faultCode;
            this.faultString = faultString;
            this.faultActor = faultActor;
            this.detail = serverFault;
        }

        internal SoapFault(SerializationInfo info, StreamingContext context)
        {
            SerializationInfoEnumerator siEnum = info.GetEnumerator();        

            while(siEnum.MoveNext())
            {
                String name = siEnum.Name;
                Object value = siEnum.Value;
                SerTrace.Log(this, "SetObjectData enum ",name," value ",value);
                if (String.Compare(name, "faultCode", true, CultureInfo.InvariantCulture) == 0)
                {
                    int index = ((String)value).IndexOf(':');
                    if (index > -1)
                        faultCode = ((String)value).Substring(++index);
                    else
                        faultCode = (String)value;
                }
                else if (String.Compare(name, "faultString", true, CultureInfo.InvariantCulture) == 0)
                    faultString = (String)value;
                else if (String.Compare(name, "faultActor", true, CultureInfo.InvariantCulture) == 0)
                    faultActor = (String)value;
                else if (String.Compare(name, "detail", true, CultureInfo.InvariantCulture) == 0)
                    detail = value;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("faultcode", "SOAP-ENV:"+faultCode);
            info.AddValue("faultstring", faultString);
            if (faultActor != null)
                info.AddValue("faultactor", faultActor);
            info.AddValue("detail", detail, typeof(Object));
        }

        public String FaultCode
        {
            get {return faultCode;}
            set { faultCode = value;}
        }

        public String FaultString
        {
            get {return faultString;}
            set { faultString = value;}
        }


        public String FaultActor
        {
            get {return faultActor;}
            set { faultActor = value;}
        }


        public Object Detail
        {
            get {return detail;}
            set {detail = value;}
        }
    }

[Serializable]
[SoapType(Embedded=true)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ServerFault
    {
        String exceptionType;
        String message;
        String stackTrace;
        Exception exception;

        internal ServerFault(Exception exception)
        {
            this.exception = exception;
            //this.exceptionType = exception.GetType().AssemblyQualifiedName;
            //this.message = exception.Message;
        }

        public ServerFault(String exceptionType, String message, String stackTrace)
        {
            this.exceptionType = exceptionType;
            this.message = message;
            this.stackTrace = stackTrace;
        }


        public String ExceptionType
        {
            get {return exceptionType;}
            set { exceptionType = value;}
        }
        
        public String ExceptionMessage
        {
            get {return message;}
            set { message = value;}
        }


        public String StackTrace
        {
            get {return stackTrace;}
            set {stackTrace = value;}
        }

        internal Exception Exception
        {
            get {return exception;}
        }
    }
}
#endif // FEATURE_REMOTING
