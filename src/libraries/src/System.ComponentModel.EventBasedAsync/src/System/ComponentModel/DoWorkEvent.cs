// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    public delegate void DoWorkEventHandler(object sender, DoWorkEventArgs e);

    public class DoWorkEventArgs : CancelEventArgs
    {
        public DoWorkEventArgs(object argument)
        {
            Argument = argument;
        }

        public object Argument { get; }

        public object Result { get; set; }
    }
}
