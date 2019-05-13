// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Exception class for duplicate objects in WaitAll/WaitAny.
**
**
=============================================================================*/

using System.Runtime.Serialization;

namespace System
{
    // The DuplicateWaitObjectException is thrown when an object 
    // appears more than once in the list of objects to WaitAll or WaitAny.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class DuplicateWaitObjectException : ArgumentException
    {
        // Creates a new DuplicateWaitObjectException with its message 
        // string set to a default message.
        public DuplicateWaitObjectException()
            : base(SR.Arg_DuplicateWaitObjectException)
        {
            HResult = HResults.COR_E_DUPLICATEWAITOBJECT;
        }

        public DuplicateWaitObjectException(string? parameterName)
            : base(SR.Arg_DuplicateWaitObjectException, parameterName)
        {
            HResult = HResults.COR_E_DUPLICATEWAITOBJECT;
        }

        public DuplicateWaitObjectException(string? parameterName, string? message)
            : base(message, parameterName)
        {
            HResult = HResults.COR_E_DUPLICATEWAITOBJECT;
        }

        public DuplicateWaitObjectException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_DUPLICATEWAITOBJECT;
        }
        
        protected DuplicateWaitObjectException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
