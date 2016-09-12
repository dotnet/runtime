// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.Serialization
{
    [Serializable]
    public partial class InvalidDataContractException : Exception
    {
        protected InvalidDataContractException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

