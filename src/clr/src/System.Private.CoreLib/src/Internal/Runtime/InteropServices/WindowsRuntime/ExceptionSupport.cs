// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Internal.Runtime.InteropServices.WindowsRuntime
{
    public static class ExceptionSupport
    {
        /// <summary>
        /// Attach restricted error information to the exception if it may apply to that exception, returning
        /// back the input value
        /// </summary>
        public static Exception? AttachRestrictedErrorInfo(Exception? e)
        {
            // If there is no exception, then the restricted error info doesn't apply to it
            if (e != null)
            {
                try
                {
                    // Get the restricted error info for this thread and see if it may correlate to the current
                    // exception object.  Note that in general the thread's IRestrictedErrorInfo is not meant for
                    // exceptions that are marshaled Windows.Foundation.HResults and instead are intended for
                    // HRESULT ABI return values.   However, in many cases async APIs will set the thread's restricted
                    // error info as a convention in order to provide extended debugging information for the ErrorCode
                    // property.
                    IRestrictedErrorInfo restrictedErrorInfo = UnsafeNativeMethods.GetRestrictedErrorInfo();
                    if (restrictedErrorInfo != null)
                    {
                        string description;
                        string restrictedDescription;
                        string capabilitySid;
                        int restrictedErrorInfoHResult;
                        restrictedErrorInfo.GetErrorDetails(out description,
                                                            out restrictedErrorInfoHResult,
                                                            out restrictedDescription,
                                                            out capabilitySid);

                        // Since this is a special case where by convention there may be a correlation, there is not a
                        // guarantee that the restricted error info does belong to the async error code.  In order to
                        // reduce the risk that we associate incorrect information with the exception object, we need
                        // to apply a heuristic where we attempt to match the current exception's HRESULT with the
                        // HRESULT the IRestrictedErrorInfo belongs to.  If it is a match we will assume association
                        // for the IAsyncInfo case.
                        if (e.HResult == restrictedErrorInfoHResult)
                        {
                            string errorReference;
                            restrictedErrorInfo.GetReference(out errorReference);

                            e.AddExceptionDataForRestrictedErrorInfo(restrictedDescription,
                                                                     errorReference,
                                                                     capabilitySid,
                                                                     restrictedErrorInfo);
                        }
                    }
                }
                catch
                {
                    // If we can't get the restricted error info, then proceed as if it isn't associated with this
                    // error.
                }
            }

            return e;
        }

        /// <summary>
        /// Report that an exception has occurred which went user unhandled.  This allows the global error handler
        /// for the application to be invoked to process the error.
        /// </summary>
        /// <returns>true if the error was reported, false if not (ie running on Win8)</returns>
        public static bool ReportUnhandledError(Exception? ex)
        {
           return WindowsRuntimeMarshal.ReportUnhandledError(ex); 
        }
    }
}
