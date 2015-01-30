// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

// Windows.Foundation.Collections.IIterable`1 cannot be referenced from managed code because it's hidden
// by the metadata adapter. We redeclare the interface manually to be able to talk to native WinRT objects.
namespace System.Runtime.InteropServices.WindowsRuntime
{
    [ComImport]
    [Guid("faa585ea-6214-4217-afda-7f46de5869b3")]
    [WindowsRuntimeImport]
    internal interface IIterable<T> : IEnumerable<T>
    {
        [Pure]
        IIterator<T> First();
    }

    [ComImport]
    [Guid("036d2c08-df29-41af-8aa2-d774be62ba6f")]
    [WindowsRuntimeImport]
    internal interface IBindableIterable
    {
        [Pure]
        IBindableIterator First();
    }
}
