// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics
{
    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

    public class DataReceivedEventArgs : EventArgs
    {
        internal readonly string _data;

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
