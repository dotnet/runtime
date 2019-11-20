using System.Collections.Generic;

using static GetConfigurationVariables_common;

public static class GetConfigurationVariables_defaults
{
    public static int Main() =>
        AssertConfigurationVariables(new KeyValuePair<string, string>[]
        {
            Pair("CpuGroup", "false"),
            Pair("HeapAffinitizeMask", "0"),
            Pair("HeapCount", "1"),
            Pair("HeapHardLimit", "0"),
            Pair("HeapHardLimitPercent", "0"),
            Pair("HighMemoryPercent", "90"),
            Pair("LargePages", "false"),
            Pair("LOHThreshold", "85000"),
            Pair("NoAffinitize", "false"),
            Pair("Server", "false"),
        });
}
