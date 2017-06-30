// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Threading 
{
    // Thread.Abort() is not supported in .NET Core, so this is currently just a stub to make the type available at compile time
    public sealed class ThreadAbortException : SystemException
    {
        private ThreadAbortException()
        {
        }

        private ThreadAbortException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public object ExceptionState => null;
    }
}
