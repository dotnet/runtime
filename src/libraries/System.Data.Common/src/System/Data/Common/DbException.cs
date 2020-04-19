// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Common
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract class DbException : System.Runtime.InteropServices.ExternalException
    {
        /// <summary>
        /// When overriden in a derived class return true in all cases where a simple retry of the operation without any other change may be successful.
        /// </summary>
        public bool IsTransient { get; protected set; } = false;

        protected DbException() : base() { }

        protected DbException(string message) : base(message) { }

        protected DbException(string message, System.Exception innerException) : base(message, innerException) { }

        protected DbException(string message, int errorCode) : base(message, errorCode) { }

        protected DbException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
