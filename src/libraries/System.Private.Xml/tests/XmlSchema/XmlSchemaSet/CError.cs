// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Xml.XmlSchemaTests
{
    public static class CError
    {
        public static bool Compare(bool equal, string message)
        {
            Assert.True(equal, message);
            return true;
        }

        public static bool Compare(object actual, object expected, string message)
        {
            Assert.Equal(expected, actual);
            return true;
        }

        public static bool Compare(object actual, object expected1, object expected2, string message)
        {
            Assert.Equal(expected1, actual);
            Assert.Equal(expected2, actual);
            return true;
        }
    }
}
