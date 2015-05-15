// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyName_ProcessorArchitectureTests
    {
        private static ProcessorArchitecture[] GetValidValues()
        {
            return new[]
        {
            ProcessorArchitecture.None,
            ProcessorArchitecture.MSIL,
            ProcessorArchitecture.X86,
            ProcessorArchitecture.IA64,
            ProcessorArchitecture.Amd64,
            ProcessorArchitecture.Arm,
        };
        }

        private const ProcessorArchitecture CurrentMaxValue = ProcessorArchitecture.Arm;

        private static ProcessorArchitecture[] GetInvalidValues()
        {
            return new[]
        {
            (ProcessorArchitecture)(-1),
            (ProcessorArchitecture)(int.MinValue),
            (ProcessorArchitecture)(int.MaxValue),
            (ProcessorArchitecture)(CurrentMaxValue + 1),
            (ProcessorArchitecture)(~7 | 0),
            (ProcessorArchitecture)(~7 | 1),
            (ProcessorArchitecture)(~7 | 2),
            (ProcessorArchitecture)(~7 | 3),
            (ProcessorArchitecture)(~7 | 4),
            (ProcessorArchitecture)(~7 | 5),
            (ProcessorArchitecture)(~7 | 6),
        };
        }

        private static Dictionary<string, ProcessorArchitecture> GetValidNameValuePairs()
        {
            // Note that "None" is not valid as part of the name. To get it, the ProcessorArchitecture must be omitted.

            return new Dictionary<string, ProcessorArchitecture>
        {
             { "MSIL",  ProcessorArchitecture.MSIL  },
             { "msil",  ProcessorArchitecture.MSIL  },
             { "mSiL",  ProcessorArchitecture.MSIL  },
             { "x86",   ProcessorArchitecture.X86   },
             { "X86",   ProcessorArchitecture.X86   },
             { "IA64",  ProcessorArchitecture.IA64  },
             { "ia64",  ProcessorArchitecture.IA64  },
             { "Ia64",  ProcessorArchitecture.IA64  },
             { "Amd64", ProcessorArchitecture.Amd64 },
             { "AMD64", ProcessorArchitecture.Amd64 },
             { "aMd64", ProcessorArchitecture.Amd64 },
             { "Arm",   ProcessorArchitecture.Arm   },
             { "ARM",   ProcessorArchitecture.Arm   },
             { "ArM",   ProcessorArchitecture.Arm   },
        };
        }

        private static string[] GetInvalidNames()
        {
            return new[]
        {
            "None",
            "NONE",
            "NoNe",
            "null",
            "Bogus",
            "",
            "0",
            "1",
            "@!#$!@#$",
            "All your base are belong to us.",
        };
        }

        [Fact]
        public void Constructor_Default_HasNoneArchitecture()
        {
            Assert.Equal(ProcessorArchitecture.None, new AssemblyName().ProcessorArchitecture);
        }

        [Fact]
        public void Constructor_NoArchitectureInName_HasNoneArchitecture()
        {
            Assert.Equal(ProcessorArchitecture.None, new AssemblyName("Test").ProcessorArchitecture);
        }

        [Fact]
        public void Constructor_ValidArchitectureName_Succeeds()
        {
            foreach (KeyValuePair<string, ProcessorArchitecture> pair in GetValidNameValuePairs())
            {
                string fullName = "Test, ProcessorArchitecture=" + pair.Key;
                AssemblyName assemblyName = new AssemblyName(fullName);
                Assert.Equal(pair.Value, assemblyName.ProcessorArchitecture);
            }
        }

        [Fact]
        public void Constructor_InvalidArchitecture_ThrowsFileLoadException()
        {
            foreach (string name in GetInvalidNames())
            {
                string fullName = "Test, ProcessorArchitecture=" + name;
                Assert.Throws<FileLoadException>(() => new AssemblyName(fullName));
            }
        }

        [Fact]
        public void SetProcessorArchitecture_InvalidArchitecture_TakesLowerThreeBitsIfLessThanOrEqualToMax()
        {
            foreach (ProcessorArchitecture validArchitecture in GetValidValues())
            {
                foreach (ProcessorArchitecture invalidArchitecture in GetInvalidValues())
                {
                    var assemblyName = new AssemblyName();
                    assemblyName.ProcessorArchitecture = validArchitecture;
                    assemblyName.ProcessorArchitecture = invalidArchitecture;

                    ProcessorArchitecture maskedInvalidArchitecture = (ProcessorArchitecture)(((int)invalidArchitecture) & 0x7);
                    ProcessorArchitecture expectedResult = maskedInvalidArchitecture > CurrentMaxValue ? validArchitecture : maskedInvalidArchitecture;

                    Assert.Equal(expectedResult, assemblyName.ProcessorArchitecture);
                }
            }
        }

        [Fact]
        public void SetProcessorArchitecture_NoneArchitecture_Succeeds()
        {
            foreach (ProcessorArchitecture validArchitecture in GetValidValues())
            {
                var assemblyName = new AssemblyName();

                assemblyName.ProcessorArchitecture = validArchitecture;
                assemblyName.ProcessorArchitecture = ProcessorArchitecture.None;

                Assert.Equal(ProcessorArchitecture.None, assemblyName.ProcessorArchitecture);
            }
        }

        [Fact, ActiveIssue(846, PlatformID.AnyUnix)]
        public void GetFullNameAndToString_AreEquivalentAndDoNotPreserveArchitecture()
        {
            foreach (KeyValuePair<string, ProcessorArchitecture> pair in GetValidNameValuePairs())
            {
                string originalFullName = "Test, Culture=en-US, PublicKeyToken=b77a5c561934e089, ProcessorArchitecture=" + pair.Key;
                string expectedSerializedFullName = "Test, Culture=en-US, PublicKeyToken=b77a5c561934e089";

                var assemblyName = new AssemblyName(originalFullName);

                Assert.Equal(expectedSerializedFullName, assemblyName.FullName);
                Assert.Equal(expectedSerializedFullName, assemblyName.ToString());
            }
        }

        [Fact]
        public void SetProcessorArchitecture_ValidArchitecture_Succeeds()
        {
            foreach (ProcessorArchitecture validArchitecture in GetValidValues())
            {
                var assemblyName = new AssemblyName();
                assemblyName.ProcessorArchitecture = validArchitecture;
                Assert.Equal(validArchitecture, assemblyName.ProcessorArchitecture);
            }
        }

        [Fact]
        public void TestUpdateGuard()
        {
            var enumValues = (ProcessorArchitecture[])Enum.GetValues(typeof(ProcessorArchitecture));
            var expectedValues = GetValidValues();
            // Make sure that we don't add values to enum without updating parser and tests.
            Assert.Equal(expectedValues, enumValues);
        }
    }
}