// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public class ThreadExceptionEventArgs : EventArgs
    {
        private readonly Exception m_exception;

        public ThreadExceptionEventArgs(Exception t)
        {
            m_exception = t;
        }

        public Exception Exception
        {
            get
            {
                return m_exception;
            }
        }
    }

    public delegate void ThreadExceptionEventHandler(object sender, ThreadExceptionEventArgs e);
}
