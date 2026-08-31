// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    /// <summary>Represents a work item that can be executed by the ThreadPool.</summary>
    public interface IThreadPoolWorkItem
    {
        void Execute();
    }
}
