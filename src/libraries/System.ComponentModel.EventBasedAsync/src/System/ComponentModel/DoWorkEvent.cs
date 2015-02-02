// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel
{
    public delegate void DoWorkEventHandler(object sender, DoWorkEventArgs e);

    public class DoWorkEventArgs : EventArgs
    {
        private object result;
        private object argument;
        private bool _cancel;

        public DoWorkEventArgs(object argument)
        {
            this.argument = argument;
        }

        public object Argument
        {
            get { return argument; }
        }

        public object Result
        {
            get { return result; }
            set { result = value; }
        }

        public bool Cancel
        {
            get { return _cancel; }
            set { _cancel = value; }
        }
    }
}

