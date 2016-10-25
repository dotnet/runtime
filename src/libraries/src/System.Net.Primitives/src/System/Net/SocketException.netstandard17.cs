// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Net.Sockets
{
    [Serializable]
    public partial class SocketException : Win32Exception
    {
        protected SocketException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("SocketException::.ctor(serialized) " + NativeErrorCode.ToString() + ":" + Message);
            }
        }

        public override int ErrorCode => base.NativeErrorCode;
    }
}
