// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [ComImport]
    [WindowsRuntimeImport]
    [Guid("6a79e863-4300-459a-9966-cbb660963ee1")]
    internal interface IIterator<T>
    {
        T Current
        {
            get;
        }

        bool HasCurrent
        {
            get;
        }

        bool MoveNext();

        int GetMany([Out] T[] items);
    }

    [ComImport]
    [WindowsRuntimeImport]
    [Guid("6a1d6c07-076d-49f2-8314-f52c9c9a8331")]
    internal interface IBindableIterator
    {
        object? Current
        {
            get;
        }

        bool HasCurrent
        {
            get;
        }

        bool MoveNext();
    }
}
