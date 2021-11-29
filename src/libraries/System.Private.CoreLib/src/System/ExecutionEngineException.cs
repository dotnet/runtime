// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
/*=============================================================================
**
**
**
** Purpose: The exception class for misc execution engine exceptions.
**          Currently, its only used as a placeholder type when the EE
**          does a FailFast.
**
**
=============================================================================*/

using System.Runtime.Serialization;

namespace System
{
    [Obsolete("ExecutionEngineException previously indicated an unspecified fatal error in the runtime. The runtime no longer raises this exception so this type is obsolete.")]
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class ExecutionEngineException : SystemException
    {
        public ExecutionEngineException()
            : base(SR.Arg_ExecutionEngineException)
        {
            HResult = HResults.COR_E_EXECUTIONENGINE;
        }

        public ExecutionEngineException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_EXECUTIONENGINE;
        }

        public ExecutionEngineException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_EXECUTIONENGINE;
        }

        private ExecutionEngineException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
