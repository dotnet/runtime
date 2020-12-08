// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    // Call back validator. Uses a validation callback to avoid creation of new types
    public delegate void ValidatorCallback(object value);
}
