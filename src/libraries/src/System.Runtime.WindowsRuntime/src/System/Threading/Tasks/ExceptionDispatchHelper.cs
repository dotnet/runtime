// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace System.Threading.Tasks
{
    internal static class ExceptionDispatchHelper
    {
        internal static void ThrowAsync(Exception exception, SynchronizationContext targetContext)
        {
            if (exception == null)
                return;

            // TODO - decide how to cleanly do it so it lights up if TA is defined
            //if (exception is ThreadAbortException)
            //    return;


            ExceptionDispatchInfo exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);

            if (targetContext != null)
            {
                try
                {
                    targetContext.Post((edi) => ((ExceptionDispatchInfo)edi).Throw(), exceptionDispatchInfo);
                }
                catch
                {
                    // Something went wrong in the Post; let's try using the thread pool instead:
                    ThrowAsync(exception, null);
                }
                return;
            }

            bool scheduled = true;
            try
            {
                new SynchronizationContext().Post((edi) => ((ExceptionDispatchInfo)edi).Throw(), exceptionDispatchInfo);
            }
            catch
            {
                // Something went wrong when scheduling the thrower; we do our best by throwing the exception here:
                scheduled = false;
            }

            if (!scheduled)
                exceptionDispatchInfo.Throw();
        }
    }  // ExceptionDispatchHelper
}  // namespace

// ExceptionDispatchHelper.cs
