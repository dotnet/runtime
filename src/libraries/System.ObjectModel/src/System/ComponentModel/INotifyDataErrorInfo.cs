// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.ComponentModel
{
    public interface INotifyDataErrorInfo
    {
        bool HasErrors { get; }

        IEnumerable GetErrors(string? propertyName);

        event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    }
}
