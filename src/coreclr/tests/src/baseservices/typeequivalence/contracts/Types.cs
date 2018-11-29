// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

[assembly:ImportedFromTypeLib("TypeEquivalenceTest")] // Required to support embeddable types
[assembly:Guid("3B491C47-B176-4CF3-8748-F19E303F1714")]

namespace TypeEquivalenceTypes
{
    [ComImport]
    [Guid("F34D4DE8-B891-4D73-B177-C8F1139A9A67")]
    public interface IEmptyType
    {
    }
}
