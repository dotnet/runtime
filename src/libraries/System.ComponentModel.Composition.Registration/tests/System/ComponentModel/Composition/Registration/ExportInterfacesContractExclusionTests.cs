// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ComponentModel.Composition.Hosting;
using Xunit;

namespace System.ComponentModel.Composition.Registration.Tests
{
    public interface IContract1 { }

    public interface IContract2 { }

    public class ClassWithLifetimeConcerns : IContract1, IContract2, IDisposable, IPartImportsSatisfiedNotification
    {
        public void Dispose()
        {
        }

        public void OnImportsSatisfied()
        {
        }
    }

    static class DELETE_ME_TESTER
    {
        public static PartBuilder ExportInterfaces(this PartBuilder pb) { return null; }
    }

    public class ExportInterfacesContractExclusionTests
    {
        static readonly Type[] s_contractInterfaces = new[] { typeof(IContract1), typeof(IContract2) };

        [Fact]
        public void WhenExportingInterfaces_NoPredicate_OnlyContractInterfacesAreExported()
        {
            var rb = new RegistrationBuilder();

            rb.ForType<ClassWithLifetimeConcerns>()
                .ExportInterfaces();

            Primitives.ComposablePartDefinition part = new TypeCatalog(new[] { typeof(ClassWithLifetimeConcerns) }, rb).Single();

            var exportedContracts = part.ExportDefinitions.Select(ed => ed.ContractName).ToArray();
            var expectedContracts = s_contractInterfaces.Select(ci => AttributedModelServices.GetContractName(ci)).ToArray();

            Assert.Equal(expectedContracts, exportedContracts);
        }

        [Fact]
        public void WhenExportingInterfaces_PredicateSpecified_OnlyContractInterfacesAreSeenByThePredicate()
        {
            var seenInterfaces = new List<Type>();

            var rb = new RegistrationBuilder();
            
            rb.ForType<ClassWithLifetimeConcerns>()
                .ExportInterfaces(i => { seenInterfaces.Add(i); return true; });

            rb.MapType(typeof(ClassWithLifetimeConcerns).GetTypeInfo());

            Primitives.ComposablePartDefinition part = new TypeCatalog(new[] { typeof(ClassWithLifetimeConcerns) }, rb).Single();

            Assert.Equal(s_contractInterfaces, seenInterfaces);
        }
    }
}
