// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public partial class HostContext : IClassFixture<HostContext.SharedTestState>
    {
        public enum ExistingContextType
        {
            FrameworkDependent,
            SelfContained_WithIncludedFrameworks,
            SelfContained_NoIncludedFrameworks
        }

        public class FrameworkCompatibilityTestData : IXunitSerializable
        {
            // Requested
            public string Name;
            public string Version;
            public string RollForward;

            // Existing
            public ExistingContextType ExistingContext;

            // Expected
            public bool? IsCompatible;

            void IXunitSerializable.Deserialize(IXunitSerializationInfo info)
            {
                Name = info.GetValue<string>(nameof(Name));
                Version = info.GetValue<string>(nameof(Version));
                RollForward = info.GetValue<string>(nameof(RollForward));
                ExistingContext = info.GetValue<ExistingContextType>(nameof(ExistingContext));
                IsCompatible = info.GetValue<bool?>(nameof(IsCompatible));
            }

            void IXunitSerializable.Serialize(IXunitSerializationInfo info)
            {
                info.AddValue(nameof(Name), Name);
                info.AddValue(nameof(Version), Version);
                info.AddValue(nameof(RollForward), RollForward);
                info.AddValue(nameof(ExistingContext), ExistingContext);
                info.AddValue(nameof(IsCompatible), IsCompatible);
            }

            public override string ToString()
            {
                return $"{nameof(Name)}: {Name}, {nameof(Version)}: {Version}, {nameof(RollForward)}: {RollForward}, {nameof(ExistingContext)}: {ExistingContext}, {nameof(IsCompatible)}: {IsCompatible}";
            }
        }

        public static System.Collections.Generic.IEnumerable<object[]> GetFrameworkCompatibilityTestData(string scenario)
        {
            var testData = new System.Collections.Generic.List<FrameworkCompatibilityTestData>();
            switch (scenario)
            {
                case Scenario.ConfigMultiple:
                    testData.AddRange(GetFrameworkCompatibilityTestData(ExistingContextType.FrameworkDependent));
                    break;
                case Scenario.Mixed:
                case Scenario.NonContextMixedAppHost:
                case Scenario.NonContextMixedDotnet:
                    testData.AddRange(GetFrameworkCompatibilityTestData(ExistingContextType.FrameworkDependent));
                    testData.AddRange(GetFrameworkCompatibilityTestData(ExistingContextType.SelfContained_WithIncludedFrameworks));
                    testData.AddRange(GetFrameworkCompatibilityTestData(ExistingContextType.SelfContained_NoIncludedFrameworks));
                    break;
                default:
                    throw new Exception($"Unexpected scenario: {scenario}");
            }

            foreach (var data in testData)
            {
                yield return new object[] { scenario, data };
            }
        }

        private static System.Collections.Generic.IEnumerable<FrameworkCompatibilityTestData> GetFrameworkCompatibilityTestData(ExistingContextType existingContextType)
        {
            var exactVersion = Version.Parse(SharedTestState.NetCoreAppVersion);
            Assert.True(exactVersion.Major >= 1 && exactVersion.Minor >= 1);

            // Different versions of existing framework
            var requestedVersionsToTest = new Version[]
            {
                // Lower major
                new Version(exactVersion.Major - 1, exactVersion.Minor - 1, exactVersion.Build),
                // Lower minor
                new Version(exactVersion.Major, exactVersion.Minor - 1, exactVersion.Build),
                // Exact
                exactVersion,
                // Higher
                new Version(exactVersion.Major + 1, exactVersion.Minor - 1, exactVersion.Build),
            };
            foreach (Version requestedVersion in requestedVersionsToTest)
            {
                string[] rollForwardSettings;
                if (requestedVersion == exactVersion)
                {
                    rollForwardSettings = new string[] { Constants.RollForwardSetting.Disable };
                }
                else if (requestedVersion > exactVersion)
                {
                    rollForwardSettings = new string[] { Constants.RollForwardSetting.LatestMinor };
                }
                else
                {
                    rollForwardSettings = new string[]
                    {
                        Constants.RollForwardSetting.LatestPatch,
                        Constants.RollForwardSetting.Minor,
                        Constants.RollForwardSetting.LatestMinor,
                        Constants.RollForwardSetting.Major,
                        Constants.RollForwardSetting.LatestMajor
                    };
                }

                string requestedVersionString = requestedVersion.ToString();
                foreach (string rollForward in rollForwardSettings)
                {
                    bool? isCompatibleVersion;
                    if (existingContextType == ExistingContextType.SelfContained_NoIncludedFrameworks)
                    {
                        // Self-contained without included frameworks is always considered compatible
                        isCompatibleVersion = true;
                    }
                    else
                    {
                        // Determine expected compatibility
                        isCompatibleVersion = rollForward switch
                        {
                            Constants.RollForwardSetting.LatestPatch => requestedVersion.Major == exactVersion.Major && requestedVersion.Minor == exactVersion.Minor && requestedVersion.Build <= exactVersion.Build,
                            Constants.RollForwardSetting.Minor
                                or Constants.RollForwardSetting.LatestMinor => requestedVersion.Major == exactVersion.Major && requestedVersion.Minor <= exactVersion.Minor,
                            Constants.RollForwardSetting.Major
                                or Constants.RollForwardSetting.LatestMajor => requestedVersion.Major <= exactVersion.Major,
                            Constants.RollForwardSetting.Disable => requestedVersion == exactVersion,
                            _ => null
                        };
                    }

                    yield return new FrameworkCompatibilityTestData()
                    {
                        Name = Constants.MicrosoftNETCoreApp,
                        Version = requestedVersionString,
                        RollForward = rollForward,
                        ExistingContext = existingContextType,
                        IsCompatible = isCompatibleVersion
                    };
                }
            }

            // Unknown framework
            yield return new FrameworkCompatibilityTestData()
            {
                Name = "UnknownFramework",
                Version = exactVersion.ToString(),
                RollForward = null,
                ExistingContext = existingContextType,
                IsCompatible = existingContextType == ExistingContextType.SelfContained_NoIncludedFrameworks ? true : null
            };
        }
    }
}
