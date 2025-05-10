// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class QueryComprehensionTests : AsyncEnumerableTests
    {
        // Tests based on the C# specification section 12.20.3 Query expression translation

        /// <summary>12.20.3.2 Query expressions with continuations</summary>
        [Fact]
        public async Task QueryExpressionsWithContinuations()
        {
            await AssertEqual(
                from c in GetCities()
                group c by c.State into g
                select $"{g.Key}: {string.Join(", ", g.Select(c => c.Name))}",

                from c in GetCitiesAsync()
                group c by c.State into g
                select $"{g.Key}: {string.Join(", ", g.Select(c => c.Name))}");

            await AssertEqual(
                from g in (from c in GetCities()
                           group c by c.State)
                select $"{g.Key}: {string.Join(", ", g.Select(c => c.Name))}",

                from g in (from c in GetCitiesAsync()
                           group c by c.State)
                select $"{g.Key}: {string.Join(", ", g.Select(c => c.Name))}");
        }

        /// <summary>12.20.3.3 Explicit range variable types</summary>
        [Fact]
        public async Task ExplicitRangeVariableTypes()
        {
            await AssertEqual(
                from City c in GetCities() select c.Name,
                from City c in GetCitiesAsync() select c.Name);
        }

        // 12.20.3.5 From, let, where, join and orderby clauses
        [Fact]
        public async Task FromLetWhereJoinClauses()
        {
            await AssertEqual(
                from c1 in GetCities()
                from c2 in GetCities()
                where c1.Name != c2.Name
                select $"{c1.Name} => {c2.Name}",

                from c1 in GetCitiesAsync()
                from c2 in GetCitiesAsync()
                where c1.Name != c2.Name
                select $"{c1.Name} => {c2.Name}");

            await AssertEqual(
                from c1 in GetCities()
                orderby c1.Name
                from c2 in GetCities()
                orderby c2.Name descending
                where c1.Name != c2.Name
                select $"{c1.Name} => {c2.Name}",

                from c1 in GetCitiesAsync()
                orderby c1.Name
                from c2 in GetCities()
                orderby c2.Name descending
                where c1.Name != c2.Name
                select $"{c1.Name} => {c2.Name}");

            await AssertEqual(
                from c1 in GetCities()
                orderby c1.Name
                from c2 in GetCities()
                orderby c2.Name descending
                where c1.Name != c2.Name
                select $"{c1.Name} => {c2.Name}",

                from c1 in GetCitiesAsync()
                orderby c1.Name
                from c2 in GetCitiesAsync()
                orderby c2.Name descending
                where c1.Name != c2.Name
                select $"{c1.Name} => {c2.Name}");

            await AssertEqual(
                from c1 in GetCities()
                let c1Name = c1.Name
                from c2 in GetCities()
                let c2Name = c2.Name
                where c1Name != c2Name
                select $"{c1Name} => {c2Name}",

                from c1 in GetCitiesAsync()
                let c1Name = c1.Name
                from c2 in GetCitiesAsync()
                let c2Name = c2.Name
                where c1Name != c2Name
                select $"{c1Name} => {c2Name}");

            await AssertEqual(
                from c1 in GetCities()
                where c1.State == "MA"
                select c1.Name,

                from c1 in GetCitiesAsync()
                where c1.State == "MA"
                select c1.Name);

            await AssertEqual(
                from c1 in GetCities()
                join c2 in GetCities() on c1.State equals c2.State
                select c1.Name,

                from c1 in GetCitiesAsync()
                join c2 in GetCitiesAsync() on c1.State equals c2.State
                select c1.Name);

            await AssertEqual(
                from c1 in GetCities()
                join c2 in GetCities() on c1.State equals c2.State into g
                from c in g
                select c.Name,

                from c1 in GetCitiesAsync()
                join c2 in GetCitiesAsync() on c1.State equals c2.State into g
                from c in g
                select c.Name);
        }

        // 12.20.3.5 From, let, where, join and orderby clauses
        [Fact]
        public async Task OrderByClauses()
        {
            await AssertEqual(
                from c in GetCities()
                orderby c.State, c.Name descending
                select c.Name,

                from c in GetCitiesAsync()
                orderby c.State, c.Name descending
                select c.Name);
        }

        // 12.20.3.6 Select clauses
        [Fact]
        public async Task SelectClauses()
        {
            await AssertEqual(
                from c in GetCities()
                select c.Name,

                from c in GetCitiesAsync()
                select c.Name);
        }

        // 12.20.3.7 Group clauses
        [Fact]
        public async Task GroupClauses()
        {
            await AssertEqual(
                from c in GetCities()
                group c.Name by c.State,

                from c in GetCitiesAsync()
                group c.Name by c.State);
        }

        private record City(string Name, string State);

        private static async IAsyncEnumerable<City> GetCitiesAsync()
        {
            foreach (City city in GetCities())
            {
                await Task.Yield();
                yield return city;
            }
        }

        private static IEnumerable<City> GetCities()
        {
            yield return new("Birmingham", "AL");
            yield return new("Anchorage", "AK");
            yield return new("Phoenix", "AZ");
            yield return new("Tucson", "AZ");
            yield return new("Mesa", "AZ");
            yield return new("Little Rock", "AR");
            yield return new("Los Angeles", "CA");
            yield return new("San Diego", "CA");
            yield return new("San Jose", "CA");
            yield return new("San Francisco", "CA");
            yield return new("Fresno", "CA");
            yield return new("Sacramento", "CA");
            yield return new("Long Beach", "CA");
            yield return new("Oakland", "CA");
            yield return new("Bakersfield", "CA");
            yield return new("Denver", "CO");
            yield return new("Washington", "DC");
            yield return new("Jacksonville", "FL");
            yield return new("Miami", "FL");
            yield return new("Tampa", "FL");
            yield return new("Atlanta", "GA");
            yield return new("Chicago", "IL");
            yield return new("Indianapolis", "IN");
            yield return new("Louisville", "KY");
            yield return new("New Orleans", "LA");
            yield return new("Baltimore", "MD");
            yield return new("Boston", "MA");
            yield return new("Detroit", "MI");
            yield return new("Minneapolis", "MN");
            yield return new("Kansas City", "MO");
            yield return new("Las Vegas", "NV");
            yield return new("Albuquerque", "NM");
            yield return new("New York", "NY");
            yield return new("Charlotte", "NC");
            yield return new("Raleigh", "NC");
            yield return new("Columbus", "OH");
            yield return new("Oklahoma City", "OK");
            yield return new("Tulsa", "OK");
            yield return new("Portland", "OR");
            yield return new("Philadelphia", "PA");
            yield return new("Memphis", "TN");
            yield return new("Nashville", "TN");
            yield return new("Houston", "TX");
            yield return new("San Antonio", "TX");
            yield return new("Dallas", "TX");
            yield return new("Austin", "TX");
            yield return new("Fort Worth", "TX");
            yield return new("El Paso", "TX");
            yield return new("Arlington", "TX");
            yield return new("Virginia Beach", "VA");
            yield return new("Seattle", "WA");
            yield return new("Milwaukee", "WI");
        }
    }
}
