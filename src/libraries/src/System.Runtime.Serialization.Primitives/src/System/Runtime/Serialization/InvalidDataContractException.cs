// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------

//------------------------------------------------------------

using System;

namespace System.Runtime.Serialization
{
    public class InvalidDataContractException : Exception
    {
        public InvalidDataContractException()
            : base()
        {
        }

        public InvalidDataContractException(String message)
            : base(message)
        {
        }

        public InvalidDataContractException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

