// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Data
{
    public interface IDataParameterCollection : IList
    {
        object this[string parameterName] { get; set; }
        bool Contains(string parameterName);
        int IndexOf(string parameterName);
        void RemoveAt(string parameterName);
    }
}
