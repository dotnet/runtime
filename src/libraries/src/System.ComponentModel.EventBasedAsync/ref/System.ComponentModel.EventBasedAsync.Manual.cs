// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.ComponentModel
{
    public partial class BackgroundWorker : IDisposable
    {
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }

    public partial class DoWorkEventArgs : System.EventArgs
    {
        public bool Cancel { get { return false; } set { } }
    }
}
