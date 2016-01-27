// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition.Convention.UnitTests;
using System.Linq;
using System.Text;
using System.Reflection;
using Xunit;
using System.Composition.UnitTests.Util;

namespace System.Composition.Convention
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

            var attributes = GetExportAttributes(builder, typeof(ClassWithLifetimeConcerns));
            var exportedContracts = attributes.Select(e => e.ContractType).ToArray();
            AssertX.Equivalent(s_contractInterfaces, exportedContracts);
        }

        [Fact]
        public void WhenExportingInterfaces_PredicateSpecified_OnlyContractInterfacesAreSeenByThePredicate()
        {
            var seenInterfaces = new List<Type>();

            var builder = new ConventionBuilder();
            builder.ForType<ClassWithLifetimeConcerns>().ExportInterfaces(i => { seenInterfaces.Add(i); return true; });

            var attributes = GetExportAttributes(builder, typeof(ClassWithLifetimeConcerns));
            AssertX.Equivalent(s_contractInterfaces, seenInterfaces);
        }

        private static IEnumerable<ExportAttribute> GetExportAttributes(ConventionBuilder builder, Type type)
        {
            var list = builder.GetDeclaredAttributes(type, type.GetTypeInfo());
            return list.Cast<ExportAttribute>();
        }
    }
}
