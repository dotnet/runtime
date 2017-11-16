// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    public abstract class ContextBoundObject : System.MarshalByRefObject
    {
        protected ContextBoundObject() { }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
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
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public partial class ContextStaticAttribute : System.Attribute
    {
        public ContextStaticAttribute() { }
    }
}
