// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics {
  
    
    using System;
    using System.Runtime.Versioning;
   // A Filter is used to decide whether an assert failure 
   // should terminate the program (or invoke the debugger).  
   // Typically this is done by popping up a dialog & asking the user.
   // 
   // The default filter brings up a simple Win32 dialog with 3 buttons.
    
    [Serializable]
    abstract internal class AssertFilter
    {
    
        // Called when an assert fails.  This should be overridden with logic which
        // determines whether the program should terminate or not.  Typically this
        // is done by asking the user.
        //
        // The windowTitle can be null.
        abstract public AssertFilters  AssertFailure(String condition, String message, 
                                  StackTrace location, StackTrace.TraceFormat stackTraceFormat, String windowTitle);
    
    }
    // No data, does not need to be marked with the serializable attribute
    internal class DefaultFilter : AssertFilter
    {
        internal DefaultFilter()
        {
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override AssertFilters  AssertFailure(String condition, String message, 
                                  StackTrace location, StackTrace.TraceFormat stackTraceFormat,
                                  String windowTitle)
    
        {
            return (AssertFilters) Assert.ShowDefaultAssertDialog (condition, message, location.ToString(stackTraceFormat), windowTitle);
        }
    }

}
