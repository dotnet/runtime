// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: The exception class uses to wrap all non-CLS compliant exceptions.
**
**
=============================================================================*/

using System;
using System.Runtime.Serialization;
using System.Diagnostics.Contracts;

namespace System.Runtime.CompilerServices
{
    public sealed class RuntimeWrappedException : Exception
    {
        private RuntimeWrappedException(Object thrownObject)
            : base(SR.RuntimeWrappedException)
        {
            HResult = System.__HResults.COR_E_RUNTIMEWRAPPED;
            m_wrappedException = thrownObject;
        }

        public Object WrappedException
        {
            get { return m_wrappedException; }
        }

        private Object m_wrappedException;

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}

