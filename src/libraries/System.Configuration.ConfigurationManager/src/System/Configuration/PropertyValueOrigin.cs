// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public enum PropertyValueOrigin
    {
        Default = 0, // Default is retrieved
        Inherited = 1, // It is inherited
        SetHere = 2 // It was set here
    }
}