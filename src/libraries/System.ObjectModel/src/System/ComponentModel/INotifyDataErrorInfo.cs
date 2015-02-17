// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;

namespace System.ComponentModel
{
    public interface INotifyDataErrorInfo
    {
        bool HasErrors { get; }

        IEnumerable GetErrors(string propertyName);

        event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
    }
}
