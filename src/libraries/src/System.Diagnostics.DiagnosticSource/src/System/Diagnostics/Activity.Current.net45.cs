// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Diagnostics
{
    public partial class Activity
    {
        /// <summary>
        /// Gets or sets the current operation (Activity) for the current thread.  This flows 
        /// across async calls.
        /// </summary>
        public static Activity Current
        {
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
            [System.Security.SecuritySafeCriticalAttribute]
#endif
            get
            {
                ObjectHandle activityHandle = (ObjectHandle)CallContext.LogicalGetData(FieldKey);

                // Unwrap the Activity if it was set in the same AppDomain (as FieldKey is AppDomain-specific). 
                if (activityHandle != null)
                {
                    return (Activity)activityHandle.Unwrap();
                }
                return null;
            }

#if ALLOW_PARTIALLY_TRUSTED_CALLERS
            [System.Security.SecuritySafeCriticalAttribute]
#endif
            set
            {
                if (ValidateSetCurrent(value))
                {
                    SetCurrent(value);
                }
            }
        }

#region private

#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
        private static void SetCurrent(Activity activity)
        {
            // Applications may implicitly or explicitly call other AppDomains
            // that do not have DiagnosticSource DLL, therefore may not be able to resolve Activity type stored in the logical call context. 
            // To avoid it, we wrap Activity with ObjectHandle.
            CallContext.LogicalSetData(FieldKey, new ObjectHandle(activity));
        }

        // Slot name depends on the AppDomain Id in order to prevent AppDomains to use the same Activity
        // Cross AppDomain calls are considered as 'external' i.e. only Activity Id and Baggage should be propagated and 
        // new Activity should be started for the RPC calls (incoming and outgoing) 
        private static readonly string FieldKey = $"{typeof(Activity).FullName}_{AppDomain.CurrentDomain.Id}";
#endregion //private
    }
}
