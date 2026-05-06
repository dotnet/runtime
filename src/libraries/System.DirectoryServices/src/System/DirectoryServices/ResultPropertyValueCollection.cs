// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.DirectoryServices
{
    /// <devdoc>
    /// Specifies a collection of values for a multi-valued property.
    /// </devdoc>
    public class ResultPropertyValueCollection : ReadOnlyCollectionBase
    {
        internal ResultPropertyValueCollection(object[] values)
        {
            InnerList.AddRange(values ?? Array.Empty<object>());
        }

        public object this[int index]
        {
            get
            {
                object returnValue = InnerList[index]!;
                if (returnValue is Exception)
                    throw (Exception)returnValue;
                else
                    return returnValue;
            }
        }

        public bool Contains(object? value) => InnerList.Contains(value);

        public int IndexOf(object? value) => InnerList.IndexOf(value);

        public void CopyTo(object?[] values, int index) => InnerList.CopyTo(values, index);
    }
}
