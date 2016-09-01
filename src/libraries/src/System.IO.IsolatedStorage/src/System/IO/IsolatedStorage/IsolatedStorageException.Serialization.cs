// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace System.IO.IsolatedStorage
{
    [Serializable]
    public partial class IsolatedStorageException : Exception
    {
        protected IsolatedStorageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
