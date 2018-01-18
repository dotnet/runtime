// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>
    /// Provides a mechanism for manual lifetime management.
    /// </summary>
    public interface IRetainable
    {
        /// <summary>
        /// Call this method to indicate that the IRetainable object is in use.
        /// Do not dispose until Release is called.
        /// </summary>
        void Retain();
        /// <summary>
        /// Call this method to indicate that the IRetainable object is no longer in use.
        /// The object can now be disposed.
        /// </summary>
        bool Release();
    }
}