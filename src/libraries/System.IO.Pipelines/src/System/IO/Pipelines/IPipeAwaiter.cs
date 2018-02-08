// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Pipelines
{
    /// <summary>
    /// Represents a source of async operation
    /// </summary>
    public interface IPipeAwaiter<out T>
    {
        /// <summary>
        /// Gets whether async operation has completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Gets the result async operation.
        /// </summary>
        /// <returns></returns>
        T GetResult();

        /// <summary>
        /// Schedules the continuation action that's invoked when the instance completes.
        /// </summary>
        void OnCompleted(Action continuation);
    }
}
