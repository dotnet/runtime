// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.DiagnosticsTests
{
    public static class DiagnosticsTestData
    {
        public static string Sample =
@"<configuration>  
  <system.diagnostics>  
    <sources>  
      <source name=""TraceSourceApp""
        switchName=""sourceSwitch""
        switchType=""System.Diagnostics.SourceSwitch""
        foo=""bar"">
        <listeners>  
          <add name = ""console""
            type=""System.Diagnostics.ConsoleTraceListener"">  
            <filter type = ""System.Diagnostics.EventTypeFilter"" initializeData=""Error""/>  
          </add>  
          <add name = ""myListener"" />
          <remove name=""Default""/>  
        </listeners>  
      </source>  
    </sources>  
    <switches>  
      <add name = ""sourceSwitch"" value=""Error""/>  
    </switches>  
    <sharedListeners>  
      <add name = ""myListener""
        type=""System.Diagnostics.TextWriterTraceListener"" initializeData=""myListener.log"">  
        <filter type = ""System.Diagnostics.EventTypeFilter"" initializeData=""Error""/>  
      </add>  
    </sharedListeners>  
  </system.diagnostics>  
</configuration>";
    }
}
