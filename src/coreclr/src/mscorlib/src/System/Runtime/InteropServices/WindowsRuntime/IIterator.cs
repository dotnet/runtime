// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//

using System;
using System.Diagnostics.Contracts;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [ComImport]
    [WindowsRuntimeImport]
    [Guid("6a79e863-4300-459a-9966-cbb660963ee1")]
    internal interface IIterator<T>
    {
        [Pure]
        T Current
        {
            get;
        }

        [Pure]
        bool HasCurrent
        {
            get;
        }

        bool MoveNext();

        [Pure]
        int GetMany([Out] T[] items);
    }

    [ComImport]
    [WindowsRuntimeImport]
    [Guid("6a1d6c07-076d-49f2-8314-f52c9c9a8331")]
    internal interface IBindableIterator
    {
        [Pure]
        object Current
        {
            get;
        }

        [Pure]
        bool HasCurrent
        {
            get;
        }

        bool MoveNext();
    }
}
