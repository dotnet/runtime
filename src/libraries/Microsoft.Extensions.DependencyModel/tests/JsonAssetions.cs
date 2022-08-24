// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public static class JsonAssertionExtensions
    {
        public static JsonAssertions Should(this JToken jToken)
        {
            return new JsonAssertions(jToken);
        }
    }

    public class JsonAssertions: ReferenceTypeAssertions<JToken, JsonAssertions>
    {
        public JsonAssertions(JToken token)
        {
            Subject = token;
        }

        protected override string Context => nameof(JToken);

        public AndWhichConstraint<JsonAssertions, JToken> HaveProperty(string expected)
        {
            var token = Subject[expected];
            Execute.Assertion
                .ForCondition(token != null)
                .FailWith("Expected {0} to have property '{1}'", Subject, expected);

            return new AndWhichConstraint<JsonAssertions, JToken>(this, token);
        }

        public AndConstraint<JsonAssertions> NotHaveProperty(string expected)
        {
            var token = Subject[expected];
            Execute.Assertion
                .ForCondition(token == null)
                .FailWith("Expected {0} to have property '{1}'", Subject, expected);

            return new AndConstraint<JsonAssertions>(this);
        }

        public AndWhichConstraint<JsonAssertions, JObject> HavePropertyAsObject(string expected)
        {
            return HaveProperty(expected).Subject.Should().BeOfType<JObject>();
        }

        public AndConstraint<ObjectAssertions> HavePropertyValue<T>(string expected, T value)
        {
            return HaveProperty(expected).Subject.Value<T>().Should().Be(value);
        }
    }
}
