// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
