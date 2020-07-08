// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.Caching.Hosting
{
    public interface IFileChangeNotificationSystem
    {
        void StartMonitoring(string filePath, OnChangedCallback onChangedCallback, out object state, out DateTimeOffset lastWriteTime, out long fileSize);

        void StopMonitoring(string filePath, object state);
    }
}
