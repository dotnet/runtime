// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis.Diagnostics;

namespace XUnitWrapperGenerator;

public static class OptionsHelper
{
    private const string ReferenceSystemPrivateCoreLibOption = "build_property.ReferenceSystemPrivateCoreLib";
    private const string IsMergedTestRunnerAssemblyOption = "build_property.IsMergedTestRunnerAssembly";
    private const string PriorityOption = "build_property.Priority";
    private const string RuntimeFlavorOption = "build_property.RuntimeFlavor";
    private const string IsOutOfProcessTestAssemblyOption = "build_metadata.AdditionalFiles.IsOutOfProcessTestAssembly";
    private const string TestFilterOption = "build_metadata.AdditionalFiles.TestFilter";
    private const string TestAssemblyRelativePathOption = "build_metadata.AdditionalFiles.TestAssemblyRelativePath";
    private const string TestDisplayNameOption = "build_metadata.AdditionalFiles.TestDisplayName";

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

    internal static bool IsOutOfProcessTestAssembly(this AnalyzerConfigOptions options) => options.GetBoolOption(IsOutOfProcessTestAssemblyOption);

    internal static string? TestFilter(this AnalyzerConfigOptions options) => options.TryGetValue(TestFilterOption, out string? filter) ? filter : null;

    internal static string? TestAssemblyRelativePath(this AnalyzerConfigOptions options) => options.TryGetValue(TestAssemblyRelativePathOption, out string? flavor) ? flavor : null;

    internal static string? TestDisplayName(this AnalyzerConfigOptions options) => options.TryGetValue(TestDisplayNameOption, out string? flavor) ? flavor : null;
}
