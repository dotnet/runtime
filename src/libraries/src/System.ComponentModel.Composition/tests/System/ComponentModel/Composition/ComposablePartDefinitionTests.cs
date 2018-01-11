// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using Xunit;

namespace System.ComponentModel.Composition
{
    public interface IContract {}
    public class ContractImpl : IContract {}
    public class MyEmptyClass
    {
        public ExportFactory<IContract> MyFactoryProperty { get; set; }
        public IContract MyProperty { get; set; }
    }

    public class MyEmptyClassWithFactoryConstructor : MyEmptyClass
    {
        [ImportingConstructor]
        public MyEmptyClassWithFactoryConstructor([Import]ExportFactory<IContract> myFactoryProperty) {  this.MyFactoryProperty = myFactoryProperty; }
    }
    public class MyEmptyClassWithStandardConstructor : MyEmptyClass
    {
        [ImportingConstructor]
        public MyEmptyClassWithStandardConstructor([Import]IContract myProperty) {  this.MyProperty = myProperty; }
    }

    internal static class Helpers
    {
        public static IEnumerable<Type> GetEnumerableOfTypes(params Type[] types)
        {
            return types;
        }

        public const string ComImportAvailable = nameof(Helpers) + "." + nameof(CheckComImportAvailable);

        private static bool CheckComImportAvailable() => PlatformDetection.IsWindows && PlatformDetection.IsNotWindowsNanoServer && !PlatformDetection.IsUap;
    }
    
    public class ComposablePartDefinitionTests
    {
        [Fact]
        public void Constructor1_ShouldNotThrow()
        {
            PartDefinitionFactory.Create();
        }

        [Fact]
        public void Constructor1_ShouldSetMetadataPropertyToEmptyDictionary()
        {
            var definition = PartDefinitionFactory.Create();

            Assert.Empty(definition.Metadata);
        }

        [Fact]
        public void Constructor1_ShouldSetMetadataPropertyToReadOnlyDictionary()
        {
            var definition = PartDefinitionFactory.Create();

            Assert.Throws<NotSupportedException>(() =>
            {
                definition.Metadata["Value"] = "Value";
            });
        }
    }
}

