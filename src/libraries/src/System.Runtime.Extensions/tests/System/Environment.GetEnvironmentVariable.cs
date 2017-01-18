// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class GetEnvironmentVariable
    {
        [Fact]
        public void InvalidArguments_ThrowsExceptions()
        {
            Assert.Throws<ArgumentNullException>("variable", () => Environment.GetEnvironmentVariable(null));
            Assert.Throws<ArgumentNullException>("variable", () => Environment.SetEnvironmentVariable(null, "test"));
            Assert.Throws<ArgumentException>("variable", () => Environment.SetEnvironmentVariable("", "test"));
            Assert.Throws<ArgumentException>("value", () => Environment.SetEnvironmentVariable("test", new string('s', 65 * 1024)));

#if netstandard17
            Assert.Throws<ArgumentException>("variable", () => Environment.SetEnvironmentVariable("", "test", EnvironmentVariableTarget.Machine));
            Assert.Throws<ArgumentNullException>("variable", () => Environment.SetEnvironmentVariable(null, "test", EnvironmentVariableTarget.User));
            Assert.Throws<ArgumentNullException>("variable", () => Environment.GetEnvironmentVariable(null, EnvironmentVariableTarget.Process));
            Assert.Throws<ArgumentOutOfRangeException>("target", () => Environment.GetEnvironmentVariable("test", (EnvironmentVariableTarget)42));
            Assert.Throws<ArgumentOutOfRangeException>("target", () => Environment.SetEnvironmentVariable("test", "test", (EnvironmentVariableTarget)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>("target", () => Environment.GetEnvironmentVariables((EnvironmentVariableTarget)(3)));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Throws<ArgumentException>("variable", () => Environment.SetEnvironmentVariable(new string('s', 256), "value", EnvironmentVariableTarget.User));
            }
#endif
        }

        [Fact]
        public void EmptyVariableReturnsNull()
        {
            Assert.Null(Environment.GetEnvironmentVariable(String.Empty));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GetEnvironmentVariable by design doesn't respect changes via setenv
        public void RandomLongVariableNameCanRoundTrip()
        {
            // NOTE: The limit of 32766 characters enforced by desktop
            // SetEnvironmentVariable is antiquated. I was
            // able to create ~1GB names and values on my Windows 8.1 box. On
            // desktop, GetEnvironmentVariable throws OOM during its attempt to
            // demand huge EnvironmentPermission well before that. Also, the old
            // test for long name case wasn't very good: it just checked that an
            // arbitrary long name > 32766 characters returned null (not found), but
            // that had nothing to do with the limit, the variable was simply not
            // found!

            string variable = "LongVariable_" + new string('@', 33000);
            const string value = "TestValue";

            try
            {
                SetEnvironmentVariableWithPInvoke(variable, value);

                Assert.Equal(value, Environment.GetEnvironmentVariable(variable));
            }
            finally
            {
                SetEnvironmentVariableWithPInvoke(variable, null);
            }
        }

        [Fact]
        public void RandomVariableThatDoesNotExistReturnsNull()
        {
            string variable = "TestVariable_SurelyThisDoesNotExist";
            Assert.Null(Environment.GetEnvironmentVariable(variable));
        }

        [Fact]
        public void VariableNamesAreCaseInsensitiveAsAppropriate()
        {
            string value = "TestValue";

            try
            {
                Environment.SetEnvironmentVariable("ThisIsATestEnvironmentVariable", value);

                Assert.Equal(value, Environment.GetEnvironmentVariable("ThisIsATestEnvironmentVariable"));

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    value = null;
                }

                Assert.Equal(value, Environment.GetEnvironmentVariable("thisisatestenvironmentvariable"));
                Assert.Equal(value, Environment.GetEnvironmentVariable("THISISATESTENVIRONMENTVARIABLE"));
                Assert.Equal(value, Environment.GetEnvironmentVariable("ThISISATeSTENVIRoNMEnTVaRIABLE"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("ThisIsATestEnvironmentVariable", null);
            }
        }

        [Fact]
        public void CanGetAllVariablesIndividually()
        {
            Random r = new Random();
            string envVar1 = "TestVariable_CanGetVariablesIndividually_" + r.Next().ToString();
            string envVar2 = "TestVariable_CanGetVariablesIndividually_" + r.Next().ToString();

            try
            {
                Environment.SetEnvironmentVariable(envVar1, envVar1);
                Environment.SetEnvironmentVariable(envVar2, envVar2);

                IDictionary envBlock = Environment.GetEnvironmentVariables();

                // Make sure the environment variables we set are part of the dictionary returned.
                Assert.True(envBlock.Contains(envVar1));
                Assert.True(envBlock.Contains(envVar1));

                // Make sure the values match the expected ones.
                Assert.Equal(envVar1, envBlock[envVar1]);
                Assert.Equal(envVar2, envBlock[envVar2]);

                // Make sure we can read the individual variables as well
                Assert.Equal(envVar1, Environment.GetEnvironmentVariable(envVar1));
                Assert.Equal(envVar2, Environment.GetEnvironmentVariable(envVar2));
            }
            finally
            {
                // Clear the variables we just set
                Environment.SetEnvironmentVariable(envVar1, null);
                Environment.SetEnvironmentVariable(envVar2, null);
            }
        }

        public void EnumerateYieldsDictionaryEntryFromIEnumerable()
        {
            // GetEnvironmentVariables has always yielded DictionaryEntry from IEnumerable
            IDictionary vars = Environment.GetEnvironmentVariables();
            IEnumerator enumerator = ((IEnumerable)vars).GetEnumerator();
            if (enumerator.MoveNext())
            {
                Assert.IsType<DictionaryEntry>(enumerator.Current);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            }
        }

#if netstandard17
        public void EnvironmentVariablesAreHashtable()
        {
            // On NetFX, the type returned was always Hashtable
            Assert.IsType<Hashtable>(Environment.GetEnvironmentVariables());
        }

        [InlineData(EnvironmentVariableTarget.Process)]
        [InlineData(EnvironmentVariableTarget.Machine)]
        [InlineData(EnvironmentVariableTarget.User)]
        public void EnvironmentVariablesAreHashtable(EnvironmentVariableTarget target)
        {
            // On NetFX, the type returned was always Hashtable
            Assert.IsType<Hashtable>(Environment.GetEnvironmentVariables(target));
        }

        [InlineData(EnvironmentVariableTarget.Process)]
        [InlineData(EnvironmentVariableTarget.Machine)]
        [InlineData(EnvironmentVariableTarget.User)]
        public void EnumerateYieldsDictionaryEntryFromIEnumerable(EnvironmentVariableTarget target)
        {
            // GetEnvironmentVariables has always yielded DictionaryEntry from IEnumerable
            IDictionary vars = Environment.GetEnvironmentVariables(target);
            IEnumerator enumerator = ((IEnumerable)vars).GetEnumerator();
            if (enumerator.MoveNext())
            {
                Assert.IsType<DictionaryEntry>(enumerator.Current);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            }
        }

        [OuterLoop] // manipulating environment variables broader in scope than the process
        [Theory]
        [InlineData(EnvironmentVariableTarget.Process)]
        [InlineData(EnvironmentVariableTarget.Machine)]
        [InlineData(EnvironmentVariableTarget.User)]
        public void EnumerateEnvironmentVariables(EnvironmentVariableTarget target)
        {
            IDictionary results = Environment.GetEnvironmentVariables(target);
            foreach (DictionaryEntry result in results)
            {
                string key = (string)result.Key;
                string value = (string)result.Value ?? string.Empty;

                // Make sure the iterated value we got matches the one we get explicitly
                Assert.NotNull(result.Key as string);
                Assert.Equal(value, Environment.GetEnvironmentVariable(key, target));

                try
                {
                    // Change it to something else.  Not all values can be changed and will silently
                    // not change, so we don't re-check and assert for equality.
                    Environment.SetEnvironmentVariable(key, value + "changed", target);
                }
                finally
                {
                    // Change it back
                    Environment.SetEnvironmentVariable(key, value, target);
                }
            }
        }
#endif

        private static void SetEnvironmentVariableWithPInvoke(string name, string value)
        {
            bool success =
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    SetEnvironmentVariable(name, value) :
                    (value != null ? setenv(name, value, 1) : unsetenv(name)) == 0;
            Assert.True(success);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        [DllImport("libc")]
        private static extern int setenv(string name, string value, int overwrite);

        [DllImport("libc")]
        private static extern int unsetenv(string name);
    }
}
