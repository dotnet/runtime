// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    public abstract class ContextBoundObject : System.MarshalByRefObject
    {
        protected ContextBoundObject() { }
    }

    [Serializable]
    public class ContextMarshalException : SystemException
    {
        public ContextMarshalException() : this(SR.Arg_ContextMarshalException, null)
        {
        }

        public ContextMarshalException(string message) : this(message, null)
        {
        }

        public ContextMarshalException(string message, Exception inner) : base(message, inner)
        {
            HResult = HResults.COR_E_CONTEXTMARSHAL;
        }

        protected ContextMarshalException(SerializationInfo info, StreamingContext context): base(info, context)
        {
            HResult = HResults.COR_E_CONTEXTMARSHAL;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public partial class ContextStaticAttribute : System.Attribute
    {
        public ContextStaticAttribute() { }
    }
}
