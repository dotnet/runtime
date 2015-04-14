// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Framework.WebEncoders
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
