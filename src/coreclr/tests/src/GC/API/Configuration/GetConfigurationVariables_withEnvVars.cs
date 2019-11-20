using System.Collections.Generic;

using static GetConfigurationVariables_common;

public static class GetConfigurationVariables_withEnvVars
{
    public static int Main() =>
        AssertConfigurationVariables(new KeyValuePair<string, string>[]
        {
            Pair("CpuGroup", "false"),
            Pair("HeapAffinitizeMask", "0"),
            Pair("HeapCount", "1"),
            Pair("HeapHardLimit", "123456789"),
            Pair("HeapHardLimitPercent", "0"),
            Pair("HighMemoryPercent", "90"),
            Pair("LargePages", "false"),
            Pair("LOHThreshold", "85000"),
            // We did not explicitly set this, but due to the HeapHardLimit this is true.
            // See gc.cpp: `gc_heap::gc_thread_no_affinitize_p = (gc_heap::heap_hard_limit ? !affinity_config_specified_p : (GCConfig::GetNoAffinitize() != 0));`
            Pair("NoAffinitize", "true"),
            Pair("Server", "true"),
        });
}
