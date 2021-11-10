// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis.Diagnostics;

namespace XUnitWrapperGenerator;

public static class OptionsHelper
{
    public const string ReferenceSystemPrivateCoreLibOption = "build_property.ReferenceSystemPrivateCoreLib";
    public const string IsMergedTestRunnerAssemblyOption = "build_property.IsMergedTestRunnerAssembly";
    public const string PriorityOption = "build_property.Priority";
    public const string RuntimeFlavorOption = "build_property.RuntimeFlavor";

    private static bool GetBoolOption(this AnalyzerConfigOptions options, string key)
    {
        return options.TryGetValue(key, out string? value)
            && bool.TryParse(value, out bool result)
            && result;
    }

    private static int? GetIntOption(this AnalyzerConfigOptions options, string key)
    {
        return options.TryGetValue(key, out string? value)
            && int.TryParse(value, out int result)
                ? result : 0;
    }

    internal static bool ReferenceSystemPrivateCoreLib(this AnalyzerConfigOptions options) => options.GetBoolOption(ReferenceSystemPrivateCoreLibOption);

    internal static bool IsMergedTestRunnerAssembly(this AnalyzerConfigOptions options) => options.GetBoolOption(IsMergedTestRunnerAssemblyOption);

    internal static int? Priority(this AnalyzerConfigOptions options) => options.GetIntOption(PriorityOption);

    internal static string RuntimeFlavor(this AnalyzerConfigOptions options) => options.TryGetValue(RuntimeFlavorOption, out string? flavor) ? flavor : "CoreCLR";
}
