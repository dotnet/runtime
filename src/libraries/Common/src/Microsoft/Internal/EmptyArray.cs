// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;

namespace Microsoft.Internal
{
    internal static class EmptyArray<T>
    {
        public static readonly T[] Value = new T[0];
    }
}
