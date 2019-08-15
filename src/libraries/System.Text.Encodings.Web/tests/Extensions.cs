// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace System.Text.Encodings.Web.Tests
{
    public static class Extensions
    {
        public static string[] ReadAllLines(this TextReader reader)
        {
            return ReadAllLinesImpl(reader).ToArray();
        }

        private static IEnumerable<string> ReadAllLinesImpl(TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}
