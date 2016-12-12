// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// System.Net.HttpStreamAsyncResult
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Threading;

namespace System.Net
{
    internal class HttpStreamAsyncResult : IAsyncResult
    {
        private object _locker = new object();
        private ManualResetEvent _handle;
        private bool _completed;

        internal byte[] Buffer;
        internal int Offset;
        internal int Count;
        internal AsyncCallback Callback;
        internal object State;
        internal int SynchRead;
        internal Exception Error;

        public void Complete(Exception e)
        {
            Error = e;
            Complete();
        }

        public void Complete()
        {
            lock (_locker)
            {
                if (_completed)
                    return;

                _completed = true;
                if (_handle != null)
                    _handle.Set();

                if (Callback != null)
                    Callback.BeginInvoke(this, null, null);
            }
        }

        public object AsyncState
        {
            get { return State; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                lock (_locker)
                {
                    if (_handle == null)
                        _handle = new ManualResetEvent(_completed);
                }

                return _handle;
            }
        }

        public bool CompletedSynchronously
        {
            get { return (SynchRead == Count); }
        }

        public bool IsCompleted
        {
            get
            {
                lock (_locker)
                {
                    return _completed;
                }
            }
        }
    }
}

