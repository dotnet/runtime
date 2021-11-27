// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;
using Xunit.Sdk;

namespace System.Text.RegularExpressions.Tests
{
    public partial class RegexParserTests
    {
  
        // End of Rust parser tests ==============

     
        public void Parse_Netcoreapp(string pattern, RegexOptions options, RegexParseError? error, int offset = -1)
        {
            Parse(pattern, options, error, offset);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        public void RegexParseException_Serializes()
        {
#pragma warning disable RE0001 // Regex issue: Not enough )'s
            ArgumentException e = Assert.ThrowsAny<ArgumentException>(() => new Regex("(abc|def"));
#pragma warning restore RE0001 // Regex issue: Not enough )'s

            var bf = new BinaryFormatter();
            var s = new MemoryStream();
            bf.Serialize(s, e);
            s.Position = 0;

            object deserialized = bf.Deserialize(s);
            Assert.IsType<ArgumentException>(deserialized);
            ArgumentException e2 = (ArgumentException)deserialized;
            Assert.Equal(e.Message, e2.Message);
        }

        /// <summary>
        /// Checks if action throws either a RegexParseException or an ArgumentException depending on the
        /// environment and the supplied error.
        /// </summary>
        /// <param name="error">The expected parse error</param>
        /// <param name="action">The action to invoke.</param>
        static partial void Throws(string pattern, RegexOptions options, RegexParseError error, int offset, Action action)
        {
            try
            {
                action();
            }
            catch (RegexParseException e)
            {
                RegexParseError regexParseError = e.Error;

                // Success if provided error matches and offset is correct.
                if (error == regexParseError)
                {
                    Assert.Equal(offset, e.Offset);
                    LogActual(pattern, options, regexParseError, e.Offset);
                    return;
                }

                LogActual(pattern, options, regexParseError, e.Offset);
                throw new XunitException($"Expected RegexParseException with error {error} offset {offset} -> Actual error: {regexParseError} offset {e.Offset})");
            }
            catch (Exception e)
            {
                throw new XunitException($"Expected RegexParseException for pattern '{pattern}' -> Actual: ({e})");
            }

            LogActual(pattern, options, RegexParseError.Unknown, -1);
            throw new XunitException($"Expected RegexParseException with error: ({error}) -> Actual: No exception thrown");
        }

       /// <summary>
        /// Checks that action succeeds or throws either a RegexParseException or an ArgumentException depending on the
        // environment and the action.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        static partial void MayThrow(Action action)
        {
            if (Record.Exception(action) is Exception e && e is not RegexParseException)
            {
                throw new XunitException($"Expected RegexParseException or no exception -> Actual: ({e})");
            }
        }
    }
}
