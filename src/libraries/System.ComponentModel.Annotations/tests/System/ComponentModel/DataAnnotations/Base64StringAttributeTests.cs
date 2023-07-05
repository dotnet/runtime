// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class Base64StringAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            var attribute = new Base64StringAttribute();
            yield return new TestCase(attribute, "abc=");
            yield return new TestCase(attribute, "BQYHCA==");
            yield return new TestCase(attribute, "abc=  \t\n\t\r ");
            yield return new TestCase(attribute, "abc \r\n\t =  \t\n\t\r ");
            yield return new TestCase(attribute, "\t\tabc=\t\t");
            yield return new TestCase(attribute, "\r\nabc=\r\n");
            yield return new TestCase(attribute, Text2Base64(""));
            yield return new TestCase(attribute, Text2Base64("hello, world!"));
            yield return new TestCase(attribute, Text2Base64("hello, world!"));
            yield return new TestCase(attribute, Text2Base64(new string('x', 2048)));

            static string Text2Base64(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            var attribute = new Base64StringAttribute();
            yield return new TestCase(attribute, "@");
            yield return new TestCase(attribute, "^!");
            yield return new TestCase(attribute, "hello, world!");
            yield return new TestCase(attribute, new string('@', 2048));

            // Input must be at least 4 characters long
            yield return new TestCase(attribute, "No");

            // Length of input must be a multiple of 4
            yield return new TestCase(attribute, "NoMore");

            // Input must not contain invalid characters
            yield return new TestCase(attribute, "2-34");

            // Input must not contain 3 or more padding characters in a row
            yield return new TestCase(attribute, "a===");
            yield return new TestCase(attribute, "abc=====");
            yield return new TestCase(attribute, "a===\r  \t  \n");

            // Input must not contain padding characters in the middle of the string
            yield return new TestCase(attribute, "No=n");
            yield return new TestCase(attribute, "abcdabc=abcd");
            yield return new TestCase(attribute, "abcdab==abcd");
            yield return new TestCase(attribute, "abcda===abcd");
            yield return new TestCase(attribute, "abcd====abcd");

            // Input must not contain extra trailing padding characters
            yield return new TestCase(attribute, "=");
            yield return new TestCase(attribute, "abc===");
        }
    }
}
