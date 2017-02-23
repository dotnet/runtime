// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Diagnostics.Contracts;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [ComImport]
    [Guid("30DA92C0-23E8-42A0-AE7C-734A0E5D2782")]
    [WindowsRuntimeImport]
    internal interface ICustomProperty
    {
        Type Type
        {
            [Pure]
            get;
        }

        string Name
        {
            [Pure]
            get;
        }

        [Pure]
        object GetValue(object target);

        void SetValue(object target, object value);

        [Pure]
        object GetValue(object target, object indexValue);

        void SetValue(object target, object value, object indexValue);

        bool CanWrite
        {
            [Pure]
            get;
        }

        bool CanRead
        {
            [Pure]
            get;
        }
    }
}

