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
using System.Runtime.Remoting;
using System.Diagnostics.Contracts;

namespace System.Runtime.CompilerServices
{
    [Serializable]
    public sealed class RuntimeWrappedException : Exception
    {
        private RuntimeWrappedException(Object thrownObject)
            : base(Environment.GetResourceString("RuntimeWrappedException"))
        {
            SetErrorCode(System.__HResults.COR_E_RUNTIMEWRAPPED);
            m_wrappedException = thrownObject;
        }

        public Object WrappedException
        {
            get { return m_wrappedException; }
        }

        private Object m_wrappedException;

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            Contract.EndContractBlock();
            base.GetObjectData(info, context);
            info.AddValue("WrappedException", m_wrappedException, typeof(Object));
        }

        internal RuntimeWrappedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            m_wrappedException = info.GetValue("WrappedException", typeof(Object));
        }
    }
}

