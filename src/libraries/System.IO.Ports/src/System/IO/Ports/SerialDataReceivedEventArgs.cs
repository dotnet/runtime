// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Ports
{
    public class SerialDataReceivedEventArgs : EventArgs
    {
        internal SerialDataReceivedEventArgs(SerialData eventCode)
        {
            EventType = eventCode;
        }

        public SerialData EventType { get; private set; }
    }
}
