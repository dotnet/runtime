// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Threading;

namespace System.Diagnostics
{
    public partial class Activity
    {
        /// <summary>
        /// Returns the current operation (Activity) for the current thread.  This flows 
        /// across async calls.
        /// </summary>
        public static Activity Current
        {
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
            [System.Security.SecuritySafeCriticalAttribute]
#endif
            get
            {
                return (Activity)CallContext.LogicalGetData(FieldKey);
            }
            
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
            [System.Security.SecuritySafeCriticalAttribute]
#endif
            private set
            {
                CallContext.LogicalSetData(FieldKey, value);
            }
        }

#region private
        private static readonly string FieldKey = $"{typeof(Activity).FullName}.Value.{AppDomain.CurrentDomain.Id}";
#endregion
    }
}