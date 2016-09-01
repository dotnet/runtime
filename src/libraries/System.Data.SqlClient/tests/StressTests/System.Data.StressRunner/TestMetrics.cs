// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;

namespace DPStressHarness
{
    public static class TestMetrics
    {
        private const string _defaultValue = "unknown";

        private static bool s_valid = false;
        private static bool s_reset = true;
        private static Stopwatch s_stopwatch = new Stopwatch();
        private static long s_workingSet;
        private static long s_peakWorkingSet;
        private static long s_privateBytes;
        private static Assembly s_targetAssembly;
        private static string s_fileVersion = _defaultValue;
        private static string s_privateBuild = _defaultValue;
        private static string s_runLabel = DateTime.Now.ToString();
        private static Dictionary<string, string> s_overrides;
        private static List<string> s_variations = null;
        private static List<string> s_selectedTests = null;
        private static bool s_isOfficial = false;
        private static string s_milestone = _defaultValue;
        private static string s_branch = _defaultValue;
        private static List<string> s_categories = null;
        private static bool s_profileMeasuredCode = false;
        private static int s_stressThreads = 16;
        private static int s_stressDuration = -1;
        private static int? s_exceptionThreshold = null;
        private static bool s_monitorenabled = false;
        private static string s_monitormachinename = "localhost";
        private static int s_randomSeed = 0;
        private static string s_filter = null;
        private static bool s_printMethodName = false;

        /// <summary>Starts the sample profiler.</summary>
        /// <remarks>
        /// Do not inline to avoid errors when the functionality is not used 
        /// and the profiling DLL is not available.
        /// </remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void InternalStartProfiling()
        {
            //            Microsoft.VisualStudio.Profiler.DataCollection.StartProfile(
            //                Microsoft.VisualStudio.Profiler.ProfileLevel.Global, 
            //                Microsoft.VisualStudio.Profiler.DataCollection.CurrentId);
        }

        /// <summary>Stops the sample profiler.</summary>
        /// <remarks>
        /// Do not inline to avoid errors when the functionality is not used 
        /// and the profiling DLL is not available.
        /// </remarks>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void InternalStopProfiling()
        {
            //            Microsoft.VisualStudio.Profiler.DataCollection.StopProfile(
            //                Microsoft.VisualStudio.Profiler.ProfileLevel.Global, 
            //                Microsoft.VisualStudio.Profiler.DataCollection.CurrentId);
        }

        public static void StartCollection()
        {
            s_valid = false;

            s_stopwatch.Reset();
            s_stopwatch.Start();
            s_reset = true;
        }

        public static void StartProfiling()
        {
            if (s_profileMeasuredCode)
            {
                InternalStartProfiling();
            }
        }

        public static void StopProfiling()
        {
            if (s_profileMeasuredCode)
            {
                InternalStopProfiling();
            }
        }

        public static void StopCollection()
        {
            s_stopwatch.Stop();

            Process p = Process.GetCurrentProcess();
            s_workingSet = p.WorkingSet64;
            s_peakWorkingSet = p.PeakWorkingSet64;
            s_privateBytes = p.PrivateMemorySize64;

            s_valid = true;
        }

        public static void PauseTimer()
        {
            s_stopwatch.Stop();
        }

        public static void UnPauseTimer()
        {
            if (s_reset)
            {
                s_stopwatch.Reset();
                s_reset = false;
            }

            s_stopwatch.Start();
        }

        private static void ThrowIfInvalid()
        {
            if (!s_valid) throw new InvalidOperationException("Collection must be stopped before accessing this metric.");
        }

        public static void Reset()
        {
            s_valid = false;
            s_reset = true;
            s_stopwatch = new Stopwatch();
            s_workingSet = new long();
            s_peakWorkingSet = new long();
            s_privateBytes = new long();
            s_targetAssembly = null;
            s_fileVersion = _defaultValue;
            s_privateBuild = _defaultValue;
            s_runLabel = DateTime.Now.ToString();
            s_overrides = null;
            s_variations = null;
            s_selectedTests = null;
            s_isOfficial = false;
            s_milestone = _defaultValue;
            s_branch = _defaultValue;
            s_categories = null;
            s_profileMeasuredCode = false;
            s_stressThreads = 16;
            s_stressDuration = -1;
            s_exceptionThreshold = null;
            s_monitorenabled = false;
            s_monitormachinename = "localhost";
            s_randomSeed = 0;
            s_filter = null;
            s_printMethodName = false;
        }

        public static string FileVersion
        {
            get { return s_fileVersion; }
            set { s_fileVersion = value; }
        }

        public static string PrivateBuild
        {
            get { return s_privateBuild; }
            set { s_privateBuild = value; }
        }

        public static Assembly TargetAssembly
        {
            get { return s_targetAssembly; }

            set
            {
                s_targetAssembly = value;
                s_fileVersion = VersionUtil.GetFileVersion(s_targetAssembly.ManifestModule.FullyQualifiedName);
                s_privateBuild = VersionUtil.GetPrivateBuild(s_targetAssembly.ManifestModule.FullyQualifiedName);
            }
        }

        public static string RunLabel
        {
            get { return s_runLabel; }
            set { s_runLabel = value; }
        }

        public static string Milestone
        {
            get { return s_milestone; }
            set { s_milestone = value; }
        }

        public static string Branch
        {
            get { return s_branch; }
            set { s_branch = value; }
        }

        public static bool IsOfficial
        {
            get { return s_isOfficial; }
            set { s_isOfficial = value; }
        }

        public static bool IsDefaultValue(string val)
        {
            return val.Equals(_defaultValue);
        }

        public static double ElapsedSeconds
        {
            get
            {
                ThrowIfInvalid();
                return s_stopwatch.ElapsedMilliseconds / 1000.0;
            }
        }

        public static long WorkingSet
        {
            get
            {
                ThrowIfInvalid();
                return s_workingSet;
            }
        }

        public static long PeakWorkingSet
        {
            get
            {
                ThrowIfInvalid();
                return s_peakWorkingSet;
            }
        }

        public static long PrivateBytes
        {
            get
            {
                ThrowIfInvalid();
                return s_privateBytes;
            }
        }


        public static Dictionary<string, string> Overrides
        {
            get
            {
                if (s_overrides == null)
                {
                    s_overrides = new Dictionary<string, string>(8);
                }
                return s_overrides;
            }
        }

        public static List<string> Variations
        {
            get
            {
                if (s_variations == null)
                {
                    s_variations = new List<string>(8);
                }

                return s_variations;
            }
        }

        public static List<string> SelectedTests
        {
            get
            {
                if (s_selectedTests == null)
                {
                    s_selectedTests = new List<string>(8);
                }

                return s_selectedTests;
            }
        }

        public static bool IncludeTest(TestAttributeBase test)
        {
            if (s_selectedTests == null || s_selectedTests.Count == 0)
                return true; // user has no selection - run all
            else
                return s_selectedTests.Contains(test.Title);
        }

        public static List<string> Categories
        {
            get
            {
                if (s_categories == null)
                {
                    s_categories = new List<string>(8);
                }

                return s_categories;
            }
        }

        public static bool ProfileMeasuredCode
        {
            get { return s_profileMeasuredCode; }
            set { s_profileMeasuredCode = value; }
        }

        public static int StressDuration
        {
            get { return s_stressDuration; }
            set { s_stressDuration = value; }
        }

        public static int StressThreads
        {
            get { return s_stressThreads; }
            set { s_stressThreads = value; }
        }

        public static int? ExceptionThreshold
        {
            get { return s_exceptionThreshold; }
            set { s_exceptionThreshold = value; }
        }

        public static bool MonitorEnabled
        {
            get { return s_monitorenabled; }
            set { s_monitorenabled = value; }
        }


        public static string MonitorMachineName
        {
            get { return s_monitormachinename; }
            set { s_monitormachinename = value; }
        }

        public static int RandomSeed
        {
            get { return s_randomSeed; }
            set { s_randomSeed = value; }
        }

        public static string Filter
        {
            get { return s_filter; }
            set { s_filter = value; }
        }

        public static bool PrintMethodName
        {
            get { return s_printMethodName; }
            set { s_printMethodName = value; }
        }
    }
}
