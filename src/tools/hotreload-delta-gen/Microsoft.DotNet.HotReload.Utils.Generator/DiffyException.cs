// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.HotReload.Utils.Generator;

public class DiffyException : Exception {
    public int ExitStatus { get; }

    public DiffyException(int exitStatus) : base () {
        ExitStatus = exitStatus;
    }

    public DiffyException (string message, int exitStatus) : base (message) {
        ExitStatus = exitStatus;
    }

    public DiffyException (string message, Exception innerException, int exitStatus) : base (message, innerException) {
        ExitStatus = exitStatus;
    }
}

