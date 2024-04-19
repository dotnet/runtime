// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.IO;

namespace System.Xml
{
    [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IApplicationResourceStreamResolver
    {
        // Methods
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Stream GetApplicationResourceStream(Uri relativeUri);
    }
}
