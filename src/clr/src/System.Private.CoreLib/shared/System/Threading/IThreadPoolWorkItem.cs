// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Threading
{
    /// <summary>Represents a work item that can be executed by the ThreadPool.</summary>
    public interface IThreadPoolWorkItem
    {
        void Execute();
    }
}
