// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



namespace System.Data
{
    using System;

    public interface IDataParameterCollection : System.Collections.IList
    {

        object this[string parameterName]
        {
            get;
            set;
        }

        bool Contains(string parameterName);

        int IndexOf(string parameterName);

        void RemoveAt(string parameterName);
    }
}
