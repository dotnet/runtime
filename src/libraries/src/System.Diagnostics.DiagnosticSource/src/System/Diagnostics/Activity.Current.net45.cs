// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Diagnostics
{
    [Serializable]
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
        
        [Serializable]
        private partial class KeyValueListNode
        {
        }

        private static readonly string FieldKey = $"{typeof(Activity).FullName}";
#endregion
    }
}