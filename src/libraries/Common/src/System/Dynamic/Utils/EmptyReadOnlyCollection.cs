// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System.Dynamic.Utils
{
    internal static class EmptyReadOnlyCollection<T>
    {
        public static ReadOnlyCollection<T> Instance = new TrueReadOnlyCollection<T>(new T[0]);
    }
}
