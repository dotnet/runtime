// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexCultureTests
    {
        /// <summary>
        /// See https://en.wikipedia.org/wiki/Dotted_and_dotless_I 
        /// </summary>
        [Fact]
        public void TurkishI_Is_Differently_LowerUpperCased_In_Turkish_Culture()
        {
            var turkish = new CultureInfo("tr-TR");
            string input = "Iıİi";

            Regex[] cultInvariantRegex = Create(input, CultureInfo.InvariantCulture, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            Regex[] turkishRegex =       Create(input, turkish,                      RegexOptions.IgnoreCase);

            // same input and regex does match so far so good
            Assert.All(cultInvariantRegex, rex => Assert.Equal(true, rex.IsMatch(input)) );

            // when the Regex was created with a turkish locale the lower cased turkish version will
            // no longer match the input string which contains upper and lower case iiiis hence even the input string 
            // will no longer match
            Assert.All(turkishRegex,       rex => Assert.Equal(false, rex.IsMatch(input)));

            // Now comes the tricky part depending on the use locale in ToUpper the results differ
            // Hence the regular expression will not match if different locales were used
            Assert.All(cultInvariantRegex, rex => Assert.Equal(true, rex.IsMatch(input.ToLowerInvariant())));
            Assert.All(cultInvariantRegex, rex => Assert.Equal(false, rex.IsMatch(input.ToLower(turkish))));

            Assert.All(turkishRegex, rex => Assert.Equal(false, rex.IsMatch(input.ToLowerInvariant())));
            Assert.All(turkishRegex, rex => Assert.Equal(true, rex.IsMatch(input.ToLower(turkish))));
        }

        /// <summary>
        /// Create regular expression once compiled and once interpreted to check if both behave the same
        /// </summary>
        /// <param name="input">Input regex string</param>
        /// <param name="info">thread culture to use when creating the regex</param>
        /// <param name="additional">Additional regex options</param>
        /// <returns></returns>
        Regex[] Create(string input, CultureInfo info, RegexOptions additional)
        {
            CultureInfo current = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = info;

                // When RegexOptions.IgnoreCase is supplied the current thread culture is used to lowercase the input string.
                // Except if RegexOptions.CultureInvariant is additionally added locale dependent effects on the generated code or state machine may happen.
                var localizedRegex = new Regex[] { new Regex(input, additional), new Regex(input, RegexOptions.Compiled | additional) };
                return localizedRegex;
            }
            finally
            {
                CultureInfo.CurrentCulture = current;
            }
        }
    }
}
