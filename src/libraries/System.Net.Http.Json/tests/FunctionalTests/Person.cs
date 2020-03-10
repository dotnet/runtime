// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Xunit;

namespace System.Net.Http.Json.Functional.Tests
{
    internal class Person
    {
        public int Age { get; set; }
        public string Name { get; set; }
        public Person Parent { get; set; }

        public void Validate()
        {
            Assert.Equal("David", Name);
            Assert.Equal(24, Age);
            Assert.Null(Parent);
        }

        public static Person Create()
        {
            return new Person { Name = "David", Age = 24 };
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
