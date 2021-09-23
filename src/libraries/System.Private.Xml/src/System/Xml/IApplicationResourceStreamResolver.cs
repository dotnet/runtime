// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.ComponentModel;

namespace System.Xml
{
    // we must specify the error flag as false so that we can typeforward this type without hitting a compile error.
    [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IApplicationResourceStreamResolver
    {
        // Methods
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Stream GetApplicationResourceStream(Uri relativeUri);
    }
}
