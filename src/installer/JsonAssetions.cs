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
        public static JsonAssetions Should(this JToken jToken)
        {
            return new JsonAssetions(jToken);
        }
    }

    public class JsonAssetions: ReferenceTypeAssertions<JToken, JsonAssetions>
    {
        public JsonAssetions(JToken token)
        {
            Subject = token;
        }

        protected override string Context => nameof(JToken);

        public AndWhichConstraint<JsonAssetions, JToken> HaveProperty(string expected)
        {
            var token = Subject[expected];
            Execute.Assertion
                .ForCondition(token != null)
                .FailWith($"Expected {Subject} to have property '" + expected + "'");

            return new AndWhichConstraint<JsonAssetions, JToken>(this, token);
        }

        public AndConstraint<JsonAssetions> NotHaveProperty(string expected)
        {
            var token = Subject[expected];
            Execute.Assertion
                .ForCondition(token == null)
                .FailWith($"Expected {Subject} not to have property '" + expected + "'");

            return new AndConstraint<JsonAssetions>(this);
        }
    }
}
