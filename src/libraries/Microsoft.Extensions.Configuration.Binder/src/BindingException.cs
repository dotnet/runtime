// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Extensions.Configuration.Binder
{
    [Serializable]
    public class BindingException : Exception
    {
        public BindingException()
        {
        }

        public BindingException(string message) : base(message)
        {
        }

        public BindingException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BindingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
