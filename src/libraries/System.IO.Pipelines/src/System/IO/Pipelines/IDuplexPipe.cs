// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipelines
{
    /// <summary>Defines a class that provides a duplex pipe from which data can be read from and written to.</summary>
    public interface IDuplexPipe
    {
        /// <summary>Gets the <see cref="System.IO.Pipelines.PipeReader" /> half of the duplex pipe.</summary>
        PipeReader Input { get; }

        /// <summary>Gets the <see cref="System.IO.Pipelines.PipeWriter" /> half of the duplex pipe.</summary>
        PipeWriter Output { get; }
    }
}
