// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// Interface that enables object deflation
    /// </summary>
    public interface IDeflatable
    {
        /// <summary>
        /// Deflate the object into a byte stream
        /// </summary>
        void Deflate(Stream destination);
    }
}
