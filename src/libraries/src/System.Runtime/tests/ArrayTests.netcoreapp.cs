// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static class ArrayNetcoreappTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/corert/issues/3650 - Wrong exception thrown", TargetFrameworkMonikers.UapAot)]
        public static void CreateInstance_Type_Int_Invalid()
        {
            foreach (Type nonRuntimeType in Helpers.NonRuntimeTypes)
            {
                // Type is not a valid RuntimeType
                AssertExtensions.Throws<ArgumentException>("elementType", () => Array.CreateInstance(nonRuntimeType, 0));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/corert/issues/3650 - Wrong exception thrown", TargetFrameworkMonikers.UapAot)]
        public static void CreateInstance_Type_Int_Int_Invalid()
        {
            foreach (Type nonRuntimeType in Helpers.NonRuntimeTypes)
            {
                // Type is not a valid RuntimeType
                AssertExtensions.Throws<ArgumentException>("elementType", () => Array.CreateInstance(nonRuntimeType, 0, 1));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/corert/issues/3650 - Wrong exception thrown", TargetFrameworkMonikers.UapAot)]
        public static void CreateInstance_Type_Int_Int_Int_Invalid()
        {
            foreach (Type nonRuntimeType in Helpers.NonRuntimeTypes)
            {
                // Type is not a valid RuntimeType
                AssertExtensions.Throws<ArgumentException>("elementType", () => Array.CreateInstance(nonRuntimeType, 0, 1, 2));
            }
        }
    }
}
