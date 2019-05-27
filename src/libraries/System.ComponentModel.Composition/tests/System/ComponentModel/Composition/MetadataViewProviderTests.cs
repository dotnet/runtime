// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Composition
{
    internal static class TransparentTestCase
    {
        public static int GetMetadataView_IMetadataViewWithDefaultedIntInTranparentType(ITrans_MetadataViewWithDefaultedInt view)
        {
            return view.MyInt;
        }
    }

    [MetadataViewImplementation(typeof(MetadataViewWithImplementation))]
    public interface IMetadataViewWithImplementation
    {
        string String1 { get; }
        string String2 { get; }
    }

    public class MetadataViewWithImplementation : IMetadataViewWithImplementation
    {
        public MetadataViewWithImplementation(IDictionary<string, object> metadata)
        {
            this.String1 = (string)metadata["String1"];
            this.String2 = (string)metadata["String2"];
        }

        public string String1 { get; private set; }
        public string String2 { get; private set; }
    }

    [MetadataViewImplementation(typeof(MetadataViewWithImplementationNoInterface))]
    public interface IMetadataViewWithImplementationNoInterface
    {
        string String1 { get; }
        string String2 { get; }
    }
    public class MetadataViewWithImplementationNoInterface
    {
        public MetadataViewWithImplementationNoInterface(IDictionary<string, object> metadata)
        {
            this.String1 = (string)metadata["String1"];
            this.String2 = (string)metadata["String2"];
        }

        public string String1 { get; private set; }
        public string String2 { get; private set; }
    }

    public class MetadataViewProviderTests
    {

        [Fact]
        public void GetMetadataView_InterfaceWithPropertySetter_ShouldThrowNotSupported()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Value"] = "value";

            Assert.Throws<NotSupportedException>(() =>
            {
                MetadataViewProvider.GetMetadataView<ITrans_MetadataTests_MetadataViewWithPropertySetter>(metadata);
            });
        }

        [Fact]
        public void GetMetadataView_InterfaceWithMethod_ShouldThrowNotSupportedException()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Value"] = "value";

            Assert.Throws<NotSupportedException>(() =>
            {
                MetadataViewProvider.GetMetadataView<ITrans_MetadataTests_MetadataViewWithMethod>(metadata);
            });
        }

        [Fact]
        public void GetMetadataView_InterfaceWithEvent_ShouldThrowNotSupportedException()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Value"] = "value";

            Assert.Throws<NotSupportedException>(() =>
            {
                MetadataViewProvider.GetMetadataView<ITrans_MetadataTests_MetadataViewWithEvent>(metadata);
            });
        }

        [Fact]
        public void GetMetadataView_InterfaceWithIndexer_ShouldThrowNotSupportedException()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Value"] = "value";

            Assert.Throws<NotSupportedException>(() =>
            {
                MetadataViewProvider.GetMetadataView<ITrans_MetadataTests_MetadataViewWithIndexer>(metadata);
            });
        }

        [Fact]
        public void GetMetadataView_AbstractClass_ShouldThrowMissingMethodException()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Value"] = "value";

            Assert.Throws<CompositionContractMismatchException>(() =>
            {
                MetadataViewProvider.GetMetadataView<AbstractClassMetadataView>(metadata);
            });
        }

        [Fact]
        public void GetMetadataView_AbstractClassWithConstructor_ShouldThrowMemberAccessException()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Value"] = "value";

            Assert.Throws<MemberAccessException>(() =>
            {
                MetadataViewProvider.GetMetadataView<AbstractClassWithConstructorMetadataView>(metadata);
            });
        }

        [Fact]
        public void GetMetadataView_IDictionaryAsTMetadataViewTypeArgument_ShouldReturnMetadata()
        {
            var metadata = new Dictionary<string, object>();

            var result = MetadataViewProvider.GetMetadataView<IDictionary<string, object>>(metadata);

            Assert.Same(metadata, result);
        }

        [Fact]
        public void GetMetadataView_IEnumerableAsTMetadataViewTypeArgument_ShouldReturnMetadata()
        {
            var metadata = new Dictionary<string, object>();

            var result = MetadataViewProvider.GetMetadataView<IEnumerable<KeyValuePair<string, object>>>(metadata);

            Assert.Same(metadata, result);
        }

        [Fact]
        public void GetMetadataView_DictionaryAsTMetadataViewTypeArgument_ShouldNotThrow()
        {
            var metadata = new Dictionary<string, object>();
            MetadataViewProvider.GetMetadataView<Dictionary<string, object>>(metadata);
        }

        [Fact]
        public void GetMetadataView_PrivateInterfaceAsTMetadataViewTypeArgument_ShouldhrowNotSupportedException()
        {
            var metadata = new Dictionary<string, object>();
            metadata["CanActivate"] = true;

            Assert.Throws<NotSupportedException>(() =>
                {
                    MetadataViewProvider.GetMetadataView<IActivator>(metadata);
                });
        }

        [Fact]
        public void GetMetadataView_DictionaryWithUncastableValueAsMetadataArgument_ShouldThrowCompositionContractMismatchException()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Value"] = true;

            Assert.Throws<CompositionContractMismatchException>(() =>
            {
                MetadataViewProvider.GetMetadataView<ITrans_MetadataTests_MetadataView>(metadata);
            });
        }

        [Fact]
        public void GetMetadataView_InterfaceWithTwoPropertiesWithSameNameDifferentTypeAsTMetadataViewArgument_ShouldThrowContractMismatch()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Value"] = 10;

            Assert.Throws<CompositionContractMismatchException>(() =>
            {
                MetadataViewProvider.GetMetadataView<ITrans_MetadataTests_MetadataView2>(metadata);
            });
        }

        [Fact]
        public void GetMetadataView_RawMetadata()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Value"] = 10;

            var view = MetadataViewProvider.GetMetadataView<RawMetadata>(new Dictionary<string, object>(metadata));

            Assert.True(view.Count == metadata.Count);
            Assert.True(view["Value"] == metadata["Value"]);
        }

        [Fact]
        public void GetMetadataView_InterfaceInheritance()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Value"] = "value";
            metadata["Value2"] = "value2";

            var view = MetadataViewProvider.GetMetadataView<ITrans_MetadataTests_MetadataView3>(metadata);
            Assert.Equal("value", view.Value);
            Assert.Equal("value2", view.Value2);
        }

        [Fact]
        public void GetMetadataView_CachesViewType()
        {
            var metadata1 = new Dictionary<string, object>();
            metadata1["Value"] = "value1";
            var view1 = MetadataViewProvider.GetMetadataView<ITrans_MetadataTests_MetadataView>(metadata1);
            Assert.Equal("value1", view1.Value);

            var metadata2 = new Dictionary<string, object>();
            metadata2["Value"] = "value2";
            var view2 = MetadataViewProvider.GetMetadataView<ITrans_MetadataTests_MetadataView>(metadata2);
            Assert.Equal("value2", view2.Value);

            Assert.Equal(view1.GetType(), view2.GetType());
        }

        private interface IActivator
        {
            bool CanActivate
            {
                get;
            }
        }

        public class RawMetadata : Dictionary<string, object>
        {
            public RawMetadata(IDictionary<string, object> dictionary) : base(dictionary) { }
        }

        public abstract class AbstractClassMetadataView
        {
            public abstract object Value { get; }
        }

        public abstract class AbstractClassWithConstructorMetadataView
        {
            public AbstractClassWithConstructorMetadataView(IDictionary<string, object> metadata) { }
            public abstract object Value { get; }
        }

        [Fact]
        public void GetMetadataView_IMetadataViewWithDefaultedInt()
        {
            var view = MetadataViewProvider.GetMetadataView<ITrans_MetadataViewWithDefaultedInt>(new Dictionary<string, object>());
            Assert.Equal(120, view.MyInt);
        }

        [Fact]
        public void GetMetadataView_IMetadataViewWithDefaultedIntInTranparentType()
        {
            var view = MetadataViewProvider.GetMetadataView<ITrans_MetadataViewWithDefaultedInt>(new Dictionary<string, object>());
            int result = TransparentTestCase.GetMetadataView_IMetadataViewWithDefaultedIntInTranparentType(view);
            Assert.Equal(120, result);
        }

        [Fact]
        public void GetMetadataView_IMetadataViewWithDefaultedIntAndInvalidMetadata()
        {
            Dictionary<string, object> metadata = new Dictionary<string, object>();
            metadata = new Dictionary<string, object>();
            metadata.Add("MyInt", 1.2);
            var view1 = MetadataViewProvider.GetMetadataView<ITrans_MetadataViewWithDefaultedInt>(metadata);
            Assert.Equal(120, view1.MyInt);

            metadata = new Dictionary<string, object>();
            metadata.Add("MyInt", "Hello, World");
            var view2 = MetadataViewProvider.GetMetadataView<ITrans_MetadataViewWithDefaultedInt>(metadata);
            Assert.Equal(120, view2.MyInt);
        }

        [Fact]
        public void GetMetadataView_MetadataViewWithImplementation()
        {
            Dictionary<string, object> metadata = new Dictionary<string, object>();
            metadata = new Dictionary<string, object>();
            metadata.Add("String1", "One");
            metadata.Add("String2", "Two");
            var view1 = MetadataViewProvider.GetMetadataView<IMetadataViewWithImplementation>(metadata);
            Assert.Equal("One", view1.String1);
            Assert.Equal("Two", view1.String2);
            Assert.Equal(view1.GetType(), typeof(MetadataViewWithImplementation));
        }

        [Fact]
        public void GetMetadataView_MetadataViewWithImplementationNoInterface()
        {
            var exception = Assert.Throws<CompositionContractMismatchException>(() =>
            {
                Dictionary<string, object> metadata = new Dictionary<string, object>();
                metadata = new Dictionary<string, object>();
                metadata.Add("String1", "One");
                metadata.Add("String2", "Two");
                var view1 = MetadataViewProvider.GetMetadataView<IMetadataViewWithImplementationNoInterface>(metadata);
            });
        }

        [Fact]
        public void GetMetadataView_IMetadataViewWithDefaultedBool()
        {
            var view = MetadataViewProvider.GetMetadataView<ITrans_MetadataViewWithDefaultedBool>(new Dictionary<string, object>());
            Assert.Equal(false, view.MyBool);
        }

        [Fact]
        public void GetMetadataView_IMetadataViewWithDefaultedInt64()
        {
            var view = MetadataViewProvider.GetMetadataView<ITrans_MetadataViewWithDefaultedInt64>(new Dictionary<string, object>());
            Assert.Equal(long.MaxValue, view.MyInt64);
        }

        [Fact]
        public void GetMetadataView_IMetadataViewWithDefaultedString()
        {
            var view = MetadataViewProvider.GetMetadataView<ITrans_MetadataViewWithDefaultedString>(new Dictionary<string, object>());
            Assert.Equal("MyString", view.MyString);
        }

        [Fact]
        public void GetMetadataView_IMetadataViewWithTypeMismatchDefaultValue()
        {
            var exception = Assert.Throws<CompositionContractMismatchException>(() =>
            {
                MetadataViewProvider.GetMetadataView<ITrans_MetadataViewWithTypeMismatchDefaultValue>(new Dictionary<string, object>());
            });

            Assert.IsType<TargetInvocationException>(exception.InnerException);
        }

        [Fact]
        public void GetMetadataView_IMetadataViewWithTypeMismatchOnUnbox()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Value"] = (short)9999;

            var exception = Assert.Throws<CompositionContractMismatchException>(() =>
            {
                MetadataViewProvider.GetMetadataView<ITrans_MetadataViewWithTypeMismatchDefaultValue>(new Dictionary<string, object>());
            });

            Assert.IsType<TargetInvocationException>(exception.InnerException);
        }

        [Fact]
        public void TestMetadataIntConversion()
        {
            var metadata = new Dictionary<string, object>();
            metadata["Value"] = (long)45;

            var exception = Assert.Throws<CompositionContractMismatchException>(() =>
            {
                MetadataViewProvider.GetMetadataView<ITrans_HasInt64>(metadata);
            });
        }
    }
}
