// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Diagnostics
{
    public delegate void DataReceivedEventHandler(Object sender, DataReceivedEventArgs e);

    public class DataReceivedEventArgs : EventArgs
    {
        internal String _data;

        internal DataReceivedEventArgs(String data)
        {
            _data = data;
        }

        public String Data
        {
            get
            {
                return _data;
            }
        }
    }
}
