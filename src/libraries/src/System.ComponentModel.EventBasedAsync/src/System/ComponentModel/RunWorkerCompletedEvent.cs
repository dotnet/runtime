// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    public delegate void RunWorkerCompletedEventHandler(object sender, RunWorkerCompletedEventArgs e);

    public class RunWorkerCompletedEventArgs : AsyncCompletedEventArgs
    {
        private readonly object _result;

        public RunWorkerCompletedEventArgs(object result,
                                           Exception error,
                                           bool cancelled)
            : base(error, cancelled, null)
        {
            _result = result;
        }

        public object Result
        {
            get
            {
                base.RaiseExceptionIfNecessary();
                return _result;
            }
        }

        // Hide from editor, since never used.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new object UserState
        {
            get
            {
                return base.UserState;
            }
        }
    }
}

