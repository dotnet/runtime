// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics {
  public partial class Activity {
    public Activity(string operationName) {}      
    public string OperationName { get { throw null; } }
    public string Id {get { throw null; } private set {} }
    public DateTime StartTimeUtc {get { throw null; } private set {} }
    public Activity Parent {get { throw null; } private set {} }
    public string ParentId {get { throw null; } private set {} }
    public string RootId {get { throw null; } private set {} }    
    public TimeSpan Duration {get { throw null; } private set {} }    
    public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> Tags { get { throw null; } }    
    public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> Baggage { get { throw null; } }
    public string GetBaggageItem(string key) {throw null;}
    public Activity AddTag(string key, string value) {throw null;}
    public Activity AddBaggage(string key, string value) {throw null;}
    public Activity SetParentId(string parentId) {throw null;}
    public Activity SetStartTime(DateTime startTimeUtc) {throw null;}
    public Activity SetEndTime(DateTime endTimeUtc) {throw null;}
    public Activity Start() {throw null;}
    public void Stop() {}
    public static Activity Current 
    {
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
        get { throw null; } 
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
       [System.Security.SecuritySafeCriticalAttribute]
#endif
        private set {}
    }
  }
  public abstract partial class DiagnosticSource {
    public Activity StartActivity(Activity activity, object args) {throw null;}
    public void StopActivity(Activity activity, object args) {}
  }
}

