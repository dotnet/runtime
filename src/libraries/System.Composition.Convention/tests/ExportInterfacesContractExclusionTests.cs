// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Composition.Convention.Tests
{
    public interface IContract1 { }

    public interface IContract2 { }

    public class ClassWithLifetimeConcerns : IContract1, IContract2, IDisposable
    {
        public void Dispose()
        {
        }

        public void OnImportsSatisfied()
        {
        }
    }

    internal static class DELETE_ME_TESTER
    {
        public static PartConventionBuilder ExportInterfaces(this PartConventionBuilder pb) { return null; }
    }
    public class ExportInterfacesContractExclusionTests
    {
        private static readonly Type[] s_contractInterfaces = new[] { typeof(IContract1), typeof(IContract2) };

        [Fact]
        public void WhenExportingInterfaces_NoPredicate_OnlyContractInterfacesAreExported()
        {
            var builder = new ConventionBuilder();
            builder.ForType<ClassWithLifetimeConcerns>().ExportInterfaces();

            IEnumerable<ExportAttribute> attributes = GetExportAttributes(builder, typeof(ClassWithLifetimeConcerns));
            Type[] exportedContracts = attributes.Select(e => e.ContractType).ToArray();
            Equivalent(s_contractInterfaces, exportedContracts);
        }

        [Fact]
        public void WhenExportingInterfaces_PredicateSpecified_OnlyContractInterfacesAreSeenByThePredicate()
        {
            var seenInterfaces = new List<Type>();

            var builder = new ConventionBuilder();
            builder.ForType<ClassWithLifetimeConcerns>().ExportInterfaces(i => { seenInterfaces.Add(i); return true; });

            IEnumerable<ExportAttribute> attributes = GetExportAttributes(builder, typeof(ClassWithLifetimeConcerns));
            Equivalent(s_contractInterfaces, seenInterfaces);
        }

        private static IEnumerable<ExportAttribute> GetExportAttributes(ConventionBuilder builder, Type type)
        {
            Attribute[] list = builder.GetDeclaredAttributes(type, type.GetTypeInfo());
            return list.Cast<ExportAttribute>();
        }

        private static void Equivalent<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            IDictionary<T, int> expectedCounts = expected.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
            IDictionary<T, int> actualCounts = actual.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

            Assert.Equal(expectedCounts, actualCounts);
        }
    }
}
