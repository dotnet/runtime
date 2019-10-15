// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.PerformanceData;
using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)] // In appcontainer, cannot write to perf counters
    public class PerformanceDataTests : IClassFixture<PerformanceDataTestsFixture>
    {
        PerformanceDataTestsFixture _fixture = null;

        public PerformanceDataTests(PerformanceDataTestsFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// This test was taken from System.Diagnostics.PerformanceData documentation https://msdn.microsoft.com/en-us/library/system.diagnostics.performancedata(v=vs.110).aspx
        /// To create provider.res file we've used some tools:
        /// ctrpp.exe -legacy provider.man
        /// rc.exe /r /i "c:\Program Files\Microsoft SDKs\Windows\v6.0\Include" provider.rc
        /// </summary>
        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public void PerformanceCounter_PerformanceData()
        {
            // We run test in isolated process to avoid interferences on internal performance counter shared state with other tests.
            // These interferences could lead to fail also after retries
            RemoteExecutor.Invoke((string providerId, string typingCounterSetId) =>
            {
                // Create the 'Typing' counter set.
                using (CounterSet typingCounterSet = new CounterSet(Guid.Parse(providerId), Guid.Parse(typingCounterSetId), CounterSetInstanceType.Single))
                {
                    // Add the counters to the counter set definition.
                    typingCounterSet.AddCounter(1, CounterType.RawData32, "Total Words Typed");
                    typingCounterSet.AddCounter(2, CounterType.Delta32, "Words Typed In Interval");
                    typingCounterSet.AddCounter(3, CounterType.RawData32, "Letter A Pressed");
                    typingCounterSet.AddCounter(4, CounterType.RawData32, "Words Containing A");
                    typingCounterSet.AddCounter(5, CounterType.SampleFraction, "Percent of Words Containing A");
                    typingCounterSet.AddCounter(6, CounterType.SampleBase, "Percent Base");
                    typingCounterSet.AddCounter(7, CounterType.SampleBase);

                    // Create an instance of the counter set (contains the counter data).
                    using (CounterSetInstance typingCsInstance = typingCounterSet.CreateCounterSetInstance("Typing Instance"))
                    {
                        typingCsInstance.Counters[1].Value = 0;
                        typingCsInstance.Counters[2].Value = 0;
                        typingCsInstance.Counters[3].Value = 0;
                        typingCsInstance.Counters[4].Value = 0;
                        typingCsInstance.Counters[5].Value = 0;
                        typingCsInstance.Counters[6].Value = 0;

                        // Instance counters readers
                        using (PerformanceCounter totalWordsTyped = Helpers.RetryOnAllPlatforms(() => new PerformanceCounter("Typing", "Total Words Typed")),
                                                    wordsTypedInInterval = Helpers.RetryOnAllPlatforms(() => new PerformanceCounter("Typing", "Words Typed In Interval")),
                                                    aKeyPressed = Helpers.RetryOnAllPlatforms(() => new PerformanceCounter("Typing", "Letter A Pressed")),
                                                    wordsContainingA = Helpers.RetryOnAllPlatforms(() => new PerformanceCounter("Typing", "Words Containing A")),
                                                    percentofWordsContaingA = Helpers.RetryOnAllPlatforms(() => new PerformanceCounter("Typing", "Percent of Words Containing A")))
                        {
                            typingCsInstance.Counters[1].Increment();
                            Assert.Equal(1, typingCsInstance.Counters[1].Value);
                            Assert.Equal(1, typingCsInstance.Counters[1].RawValue);
                            Assert.Equal(1, typingCsInstance.Counters["Total Words Typed"].RawValue);
                            Assert.Equal(1, totalWordsTyped.RawValue);


                            typingCsInstance.Counters[1].Increment();
                            Assert.Equal(2, typingCsInstance.Counters[1].Value);
                            Assert.Equal(2, typingCsInstance.Counters[1].RawValue);
                            Assert.Equal(2, typingCsInstance.Counters["Total Words Typed"].RawValue);
                            Assert.Equal(2, totalWordsTyped.RawValue);

                            typingCsInstance.Counters[2].IncrementBy(3);
                            Assert.Equal(3, typingCsInstance.Counters[2].Value);
                            Assert.Equal(3, typingCsInstance.Counters[2].RawValue);
                            Assert.Equal(3, typingCsInstance.Counters["Words Typed In Interval"].RawValue);
                            Assert.Equal(3, wordsTypedInInterval.RawValue);

                            typingCsInstance.Counters[3].RawValue = 4;
                            Assert.Equal(4, typingCsInstance.Counters[3].Value);
                            Assert.Equal(4, typingCsInstance.Counters[3].RawValue);
                            Assert.Equal(4, typingCsInstance.Counters["Letter A Pressed"].RawValue);
                            Assert.Equal(4, aKeyPressed.RawValue);

                            typingCsInstance.Counters[4].Value = 5;
                            Assert.Equal(5, typingCsInstance.Counters[4].Value);
                            Assert.Equal(5, typingCsInstance.Counters[4].RawValue);
                            Assert.Equal(5, typingCsInstance.Counters["Words Containing A"].RawValue);
                            Assert.Equal(5, wordsContainingA.RawValue);

                            typingCsInstance.Counters[4].Decrement();
                            Assert.Equal(4, typingCsInstance.Counters[4].Value);
                            Assert.Equal(4, typingCsInstance.Counters[4].RawValue);
                            Assert.Equal(4, typingCsInstance.Counters["Words Containing A"].RawValue);
                            Assert.Equal(4, wordsContainingA.RawValue);
                        }
                    }
                }
            }, _fixture._providerId.ToString(), _fixture._typingCounterSetId.ToString()).Dispose();
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public void PerformanceCounter_PerformanceData_CreateCounterSetInstance_EmptyCounters()
        {
            using (CounterSet typingCounterSet = new CounterSet(_fixture._providerId, _fixture._typingCounterSetId, CounterSetInstanceType.Single))
            {
                Assert.Throws<InvalidOperationException>(() => typingCounterSet.CreateCounterSetInstance("Typing Instance"));
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public void PerformanceCounter_PerformanceData_CreateCounterSetInstance_AlreadyExists()
        {
            using (CounterSet typingCounterSet = new CounterSet(_fixture._providerId, _fixture._typingCounterSetId, CounterSetInstanceType.Single))
            {
                typingCounterSet.AddCounter(6, CounterType.SampleBase, "Percent Base");
                using (CounterSetInstance typingCsInstance = typingCounterSet.CreateCounterSetInstance("Typing Instance"))
                {
                    AssertExtensions.Throws<ArgumentException>("instanceName", "InstanceName", () => typingCounterSet.CreateCounterSetInstance("Typing Instance"));
                }
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public void PerformanceCounter_PerformanceData_CounterSet_AlreadyRegistered()
        {
            using (CounterSet typingCounterSet = new CounterSet(_fixture._providerId, _fixture._typingCounterSetId, CounterSetInstanceType.Single))
            {
                AssertExtensions.Throws<ArgumentException>("counterSetGuid", "CounterSetGuid", () => new CounterSet(_fixture._providerId, _fixture._typingCounterSetId, CounterSetInstanceType.Single));
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        [InlineData("", typeof(ArgumentException))]
        [InlineData(null, typeof(ArgumentNullException))]
        public void PerformanceCounter_PerformanceData_CounterSet_InvalidInstanceName(string instanceName, Type exceptionType)
        {
            using (CounterSet typingCounterSet = new CounterSet(_fixture._providerId, _fixture._typingCounterSetId, CounterSetInstanceType.Single))
            {
                typingCounterSet.AddCounter(6, CounterType.SampleBase, "Percent Base");
                ArgumentException argumentException = (ArgumentException)Assert.Throws(exceptionType, () => typingCounterSet.CreateCounterSetInstance(instanceName));
                Assert.Equal("instanceName", argumentException.ParamName);
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        [InlineData("", "counterName", "counterName", typeof(ArgumentException))]
        [InlineData(null, "counterName", "CounterName", typeof(ArgumentNullException))]
        public void PerformanceCounter_PerformanceData_AddCounter_InvalidCounterName(string counterName, string netCoreParameterName, string netfxParameterName, Type exceptionType)
        {
            using (CounterSet typingCounterSet = new CounterSet(_fixture._providerId, _fixture._typingCounterSetId, CounterSetInstanceType.Single))
            {
                ArgumentException argumentException = (ArgumentException)Assert.Throws(exceptionType, () => typingCounterSet.AddCounter(8, CounterType.SampleBase, counterName));
                Assert.Equal(PlatformDetection.IsFullFramework ? netfxParameterName : netCoreParameterName, argumentException.ParamName);
            }
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        [InlineData("")]
        [InlineData(null)]
        public void PerformanceCounter_PerformanceData_InvalidCounterName_Indexer(string counterName)
        {
            using (CounterSet typingCounterSet = new CounterSet(_fixture._providerId, _fixture._typingCounterSetId, CounterSetInstanceType.Single))
            {
                typingCounterSet.AddCounter(6, CounterType.SampleBase, "Percent Base");
                using (CounterSetInstance typingCsInstance = typingCounterSet.CreateCounterSetInstance("Typing Instance"))
                {
                    AssertExtensions.Throws<ArgumentNullException>("counterName", "CounterName", () => typingCsInstance.Counters[counterName]);
                }
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public void PerformanceCounter_PerformanceData_Counter_NotFound()
        {
            using (CounterSet typingCounterSet = new CounterSet(_fixture._providerId, _fixture._typingCounterSetId, CounterSetInstanceType.Single))
            {
                typingCounterSet.AddCounter(6, CounterType.SampleBase, "Percent Base");
                using (CounterSetInstance typingCsInstance = typingCounterSet.CreateCounterSetInstance("Typing Instance"))
                {
                    Assert.Null(typingCsInstance.Counters["NotFound"]);
                    Assert.Null(typingCsInstance.Counters[1]);
                }
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public void PerformanceCounter_PerformanceData_Counter_AlreadyAdded()
        {
            using (CounterSet typingCounterSet = new CounterSet(_fixture._providerId, _fixture._typingCounterSetId, CounterSetInstanceType.Single))
            {
                typingCounterSet.AddCounter(6, CounterType.SampleBase, "Percent Base");
                Assert.Throws<ArgumentException>(() => typingCounterSet.AddCounter(6, CounterType.SampleBase, "Percent Base"));
            }
        }
    }

    public class PerformanceDataTestsFixture : IDisposable
    {
        public readonly Guid _providerId;
        public readonly Guid _typingCounterSetId;
        readonly string _manifestPathTmp = "";

        /// <summary>
        /// lodctr.exe to register performance counter
        /// </summary>
        void RegisterCounters()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_manifestPathTmp));
            File.Copy("provider.man", _manifestPathTmp);
            File.Copy("System.Diagnostics.PerformanceCounter.Tests.dll", Path.Combine(Path.GetDirectoryName(_manifestPathTmp), "System.Diagnostics.PerformanceCounter.Tests.dll"));
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "lodctr";
            psi.Arguments = "/m:\"" + _manifestPathTmp + "\"";
            psi.UseShellExecute = false;
            Process process = Process.Start(psi);
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);
        }

        /// <summary>
        /// unlodctr.exe to unregister performance counter
        /// </summary>
        /// <param name="manifestPathTmp"></param>
        void UnregisterCounters()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "unlodctr";
            psi.Arguments = "/m:\"" + _manifestPathTmp + "\"";
            psi.UseShellExecute = false;
            Process process = Process.Start(psi);
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);
        }

        public PerformanceDataTestsFixture()
        {
            _providerId = new Guid("{51D1685C-35ED-45be-99FE-17261A4F27F3}");
            _typingCounterSetId = new Guid("{582803C9-AACD-45e5-8C30-571141A22092}");
            _manifestPathTmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "provider.man");
            RegisterCounters();
        }

        public void Dispose()
        {
            UnregisterCounters();
            Directory.Delete(Path.GetDirectoryName(_manifestPathTmp), true);
        }
    }
}
