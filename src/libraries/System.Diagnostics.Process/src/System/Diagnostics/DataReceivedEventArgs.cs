// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

    public class DataReceivedEventArgs : EventArgs
    {
        private readonly string _data;

        internal DataReceivedEventArgs(string data)
        {
            _data = data;
        }

        public string Data
        {
            get { return _data; }
        }
    }
}
