// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

//
//
// Authors:
//      Marek Habersack <mhabersack@novell.com>
//
// Copyright (C) 2010 Novell, Inc. (http://novell.com/)
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

using System;
using System.Runtime.Caching;
using System.Runtime.Caching.Hosting;

namespace MonoTests.Common
{
    internal class TestNotificationSystem : IServiceProvider, IFileChangeNotificationSystem
    {
        private OnChangedCallback _callback;

        public bool StartMonitoringCalled { get; private set; }
        public uint StartMonitoringCallCount { get; private set; }
        public bool StopMonitoringCalled { get; private set; }
        public uint StopMonitoringCallCount { get; private set; }
        public bool UseNullState { get; set; }

        public object GetService(Type serviceType)
        {
            return this;
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            return GetService(serviceType);
        }

        public void FakeChanged(string filePath)
        {
            if (_callback == null)
                return;

            _callback(null);
        }

        public void StartMonitoring(string filePath, OnChangedCallback onChangedCallback, out object state, out DateTimeOffset lastWriteTime, out long fileSize)
        {
            if (UseNullState)
                state = null;
            else
                state = filePath;
            lastWriteTime = DateTimeOffset.FromFileTime(DateTime.Now.Ticks);
            _callback = onChangedCallback;
            fileSize = 10;
            StartMonitoringCalled = true;
            StartMonitoringCallCount++;
        }

        public void StopMonitoring(string filePath, object state)
        {
            StopMonitoringCalled = true;
            StopMonitoringCallCount++;
        }

        void IFileChangeNotificationSystem.StartMonitoring(string filePath, OnChangedCallback onChangedCallback, out object state, out DateTimeOffset lastWriteTime, out long fileSize)
        {
            StartMonitoring(filePath, onChangedCallback, out state, out lastWriteTime, out fileSize);
        }

        void IFileChangeNotificationSystem.StopMonitoring(string filePath, object state)
        {
            StopMonitoring(filePath, state);
        }
    }
}
