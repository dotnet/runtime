// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel
{
    public delegate void RunWorkerCompletedEventHandler(object sender, RunWorkerCompletedEventArgs e);

    public class RunWorkerCompletedEventArgs : AsyncCompletedEventArgs
    {
        private object result;

        public RunWorkerCompletedEventArgs(object result,
                                           Exception error,
                                           bool cancelled)
            : base(error, cancelled, null)
        {
            this.result = result;
        }

        public object Result
        {
            get
            {
                base.RaiseExceptionIfNecessary();
                return result;
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

