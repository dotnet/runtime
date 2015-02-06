// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel
{
    public delegate void DoWorkEventHandler(object sender, DoWorkEventArgs e);

    public class DoWorkEventArgs : EventArgs
    {
        private readonly object _argument;
        private object _result;
        private bool _cancel;

        public DoWorkEventArgs(object argument)
        {
            _argument = argument;
        }

        public object Argument
        {
            get 
            {
                return _argument;
            }
        }

        public object Result
        {
            get
            {
                return _result;
            }

            set
            {
                _result = value;
            }
        }

        public bool Cancel
        {
            get
            {
                return _cancel;
            }

            set
            {
                _cancel = value; 
            }
        }
    }
}
