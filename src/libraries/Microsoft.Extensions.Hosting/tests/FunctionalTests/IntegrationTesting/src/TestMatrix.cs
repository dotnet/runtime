// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    public class TestMatrix : IEnumerable<object[]>
    {
        public IList<string> Tfms { get; set; } = new List<string>();
        public IList<ApplicationType> ApplicationTypes { get; set; } = new List<ApplicationType>();
        public IList<RuntimeArchitecture> Architectures { get; set; } = new List<RuntimeArchitecture>();
        private IList<Tuple<Func<TestVariant, bool>, string>> Skips { get; } = new List<Tuple<Func<TestVariant, bool>, string>>();

        public static TestMatrix Create()
        {
            return new TestMatrix();
        }

        public TestMatrix WithTfms(params string[] tfms)
        {
            Tfms = tfms;
            return this;
        }

        public TestMatrix WithApplicationTypes(params ApplicationType[] types)
        {
            ApplicationTypes = types;
            return this;
        }

        public TestMatrix WithAllApplicationTypes()
        {
            ApplicationTypes.Add(ApplicationType.Portable);
            ApplicationTypes.Add(ApplicationType.Standalone);
            return this;
        }
        public TestMatrix WithArchitectures(params RuntimeArchitecture[] archs)
        {
            Architectures = archs;
            return this;
        }

        public TestMatrix WithAllArchitectures()
        {
            Architectures.Add(RuntimeArchitecture.x64);
            Architectures.Add(RuntimeArchitecture.x86);
            return this;
        }

        public TestMatrix Skip(string message, Func<TestVariant, bool> check)
        {
            Skips.Add(new Tuple<Func<TestVariant, bool>, string>(check, message));
            return this;
        }

        private IEnumerable<TestVariant> Build()
        {
            // TFMs.
            if (!Tfms.Any())
            {
                throw new ArgumentException("No TFMs were specified.");
            }

            ResolveDefaultArchitecture();

            if (!ApplicationTypes.Any())
            {
                ApplicationTypes.Add(ApplicationType.Portable);
            }

            var variants = new List<TestVariant>();
            VaryByTfm(variants);

            CheckForSkips(variants);

            return variants;
        }

        private void ResolveDefaultArchitecture()
        {
            if (!Architectures.Any())
            {
                switch (RuntimeInformation.OSArchitecture)
                {
                    case Architecture.X86:
                        Architectures.Add(RuntimeArchitecture.x86);
                        break;
                    case Architecture.X64:
                        Architectures.Add(RuntimeArchitecture.x64);
                        break;
                    default:
                        throw new ArgumentException(RuntimeInformation.OSArchitecture.ToString());
                }
            }
        }

        private void VaryByTfm(List<TestVariant> variants)
        {
            foreach (var tfm in Tfms)
            {
                var skipTfm = SkipIfTfmIsNotSupportedOnThisOS(tfm);

                VaryByApplicationType(variants, tfm, skipTfm);
            }
        }

        private static string SkipIfTfmIsNotSupportedOnThisOS(string tfm)
        {
            if (Tfm.Matches(Tfm.Net461, tfm) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "This TFM is not supported on this operating system.";
            }

            return null;
        }

        private void VaryByApplicationType(List<TestVariant> variants, string tfm, string skip)
        {
            foreach (var t in ApplicationTypes)
            {
                var type = t;
                if (Tfm.Matches(Tfm.Net461, tfm) && type == ApplicationType.Portable)
                {
                    if (ApplicationTypes.Count == 1)
                    {
                        // Override the default
                        type = ApplicationType.Standalone;
                    }
                    else
                    {
                        continue;
                    }
                }

                VaryByArchitecture(variants, tfm, skip, type);
            }
        }

        private void VaryByArchitecture(List<TestVariant> variants, string tfm, string skip, ApplicationType type)
        {
            foreach (var arch in Architectures)
            {
                var archSkip = skip ?? SkipIfArchitectureNotSupportedOnCurrentSystem(arch);

                variants.Add(new TestVariant()
                {
                    Tfm = tfm,
                    ApplicationType = type,
                    Architecture = arch,
                    Skip = archSkip,
                });
            }
        }

        private string SkipIfArchitectureNotSupportedOnCurrentSystem(RuntimeArchitecture arch)
        {
            if (arch == RuntimeArchitecture.x64)
            {
                // Can't run x64 on a x86 OS.
                return (RuntimeInformation.OSArchitecture == Architecture.Arm || RuntimeInformation.OSArchitecture == Architecture.X86)
                    ? $"Cannot run {arch} on your current system." : null;
            }

            // No x86 runtimes available on MacOS or Linux.
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? null : $"No {arch} available for non-Windows systems.";
        }

        private void CheckForSkips(List<TestVariant> variants)
        {
            foreach (var variant in variants)
            {
                foreach (var skipPair in Skips)
                {
                    if (skipPair.Item1(variant))
                    {
                        variant.Skip = skipPair.Item2;
                        break;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<object[]>)this).GetEnumerator();
        }

        // This is what Xunit MemberData expects
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var v in Build())
            {
                yield return new[] { v };
            }
        }
    }
}
