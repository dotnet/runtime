// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class PropertyDescriptorCollectionTests
    {
        [Fact]
        public void ConstructorNullTests()
        {
            var collection = new PropertyDescriptorCollection(null);

            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public void ConstructorTests()
        {
            var descriptors = new PropertyDescriptor[]
            {
                new MockPropertyDescriptor("descriptor1")
            };

            var collection = new PropertyDescriptorCollection(descriptors);

            Assert.Equal(descriptors.Cast<PropertyDescriptor>(), collection.Cast<PropertyDescriptor>());

            // These methods are implemented as explicit properties so we need to ensure they are what we expect
            Assert.False(((IDictionary)collection).IsReadOnly);
            Assert.False(((IDictionary)collection).IsFixedSize);
            Assert.False(((IList)collection).IsReadOnly);
            Assert.False(((IList)collection).IsFixedSize);
        }

        [Fact]
        public void ConstructorReadOnlyTests()
        {
            var descriptors = new PropertyDescriptor[]
            {
                new MockPropertyDescriptor("descriptor1")
            };

            var collection = new PropertyDescriptorCollection(descriptors, true);

            Assert.Equal(descriptors.Cast<PropertyDescriptor>(), collection.Cast<PropertyDescriptor>());

            // These methods are implemented as explicit properties so we need to ensure they are what we expect
            Assert.True(((IDictionary)collection).IsReadOnly);
            Assert.True(((IDictionary)collection).IsFixedSize);
            Assert.True(((IList)collection).IsReadOnly);
            Assert.True(((IList)collection).IsFixedSize);
        }

        [Fact]
        public void ReadOnlyThrows()
        {
            var collection = new PropertyDescriptorCollection(null, true);

            // The readonly check occurs before anything else, so we don't need to pass in actual valid values
            Assert.Throws<NotSupportedException>(() => collection.Add(null));
            Assert.Throws<NotSupportedException>(() => collection.Insert(1, null));
            Assert.Throws<NotSupportedException>(() => collection.RemoveAt(1));
            Assert.Throws<NotSupportedException>(() => collection.Remove(null));
            Assert.Throws<NotSupportedException>(() => collection.Clear());
        }

        [Fact]
        public void AddTests()
        {
            var collection = new PropertyDescriptorCollection(null);

            for (var i = 0; i < 100; i++)
            {
                Assert.Equal(i, collection.Add(new MockPropertyDescriptor($"name{i}")));
                Assert.Equal(i + 1, collection.Count);
            }
        }

        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(8)]
        [Theory]
        public void RemoveExistingTests(int index)
        {
            var propertyDescriptors = new PropertyDescriptor[]
            {
                new MockPropertyDescriptor("propertyDescriptor1"),
                new MockPropertyDescriptor("propertyDescriptor2"),
                new MockPropertyDescriptor("propertyDescriptor3"),
                new MockPropertyDescriptor("propertyDescriptor4"),
                new MockPropertyDescriptor("propertyDescriptor5"),
                new MockPropertyDescriptor("propertyDescriptor6"),
                new MockPropertyDescriptor("propertyDescriptor7"),
                new MockPropertyDescriptor("propertyDescriptor8"),
                new MockPropertyDescriptor("propertyDescriptor9")
            };

            // Must send in a copy to the constructor as the array itself is manipulated
            var collection = new PropertyDescriptorCollection(propertyDescriptors.ToArray());

            Assert.True(index >= 0 && index < propertyDescriptors.Length, $"Index '{index}' is out of bounds");

            collection.Remove(propertyDescriptors[index]);

            for (int i = 0; i < propertyDescriptors.Length; i++)
            {
                if (i == index)
                {
                    Assert.False(collection.Contains(propertyDescriptors[index]), "Should have removed descriptor");
                }
                else
                {
                    Assert.True(collection.Contains(propertyDescriptors[i]), $"Descriptor should be in collection: {i}");
                }
            }
        }

        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(8)]
        [Theory]
        public void RemoveAtTests(int index)
        {
            var propertyDescriptors = new PropertyDescriptor[]
            {
                new MockPropertyDescriptor("propertyDescriptor1"),
                new MockPropertyDescriptor("propertyDescriptor2"),
                new MockPropertyDescriptor("propertyDescriptor3"),
                new MockPropertyDescriptor("propertyDescriptor4"),
                new MockPropertyDescriptor("propertyDescriptor5"),
                new MockPropertyDescriptor("propertyDescriptor6"),
                new MockPropertyDescriptor("propertyDescriptor7"),
                new MockPropertyDescriptor("propertyDescriptor8"),
                new MockPropertyDescriptor("propertyDescriptor9")
            };

            // Must send in a copy to the constructor as the array itself is manipulated
            var collection = new PropertyDescriptorCollection(propertyDescriptors.ToArray());

            Assert.True(index >= 0 && index < propertyDescriptors.Length, $"Index '{index}' is out of bounds");

            collection.RemoveAt(index);

            for (int i = 0; i < propertyDescriptors.Length; i++)
            {
                if (i == index)
                {
                    Assert.False(collection.Contains(propertyDescriptors[index]), "Should have removed descriptor");
                }
                else
                {
                    Assert.True(collection.Contains(propertyDescriptors[i]), $"Descriptor should be in collection: {i}");
                }
            }
        }

        [InlineData("propertyDescriptor1", true, true)]
        [InlineData("propertyDescriptoR1", false, false)]
        [InlineData("propertyDescriptor2", true, true)]
        [InlineData("propertyDescriptor8", true, true)]
        [InlineData("propertyDescriptor9", true, true)]
        [InlineData("propertyDescriptorNotExistent", true, false)]
        [Theory]
        public void Find(string name, bool ignoreCase, bool exists)
        {
            const int LoopCount = 100;
            var propertyDescriptors = new PropertyDescriptor[]
            {
                new MockPropertyDescriptor("propertyDescriptor1"),
                new MockPropertyDescriptor("propertyDescriptor2"),
                new MockPropertyDescriptor("propertyDescriptor3"),
                new MockPropertyDescriptor("propertyDescriptor4"),
                new MockPropertyDescriptor("propertyDescriptor5"),
                new MockPropertyDescriptor("propertyDescriptor6"),
                new MockPropertyDescriptor("propertyDescriptor7"),
                new MockPropertyDescriptor("propertyDescriptor8"),
                new MockPropertyDescriptor("propertyDescriptor9")
            };

            // Loop through as there is caching that occurs
            for (int count = 0; count < LoopCount; count++)
            {
                var collection = new PropertyDescriptorCollection(propertyDescriptors);
                var result = collection.Find(name, ignoreCase);

                if (exists)
                {
                    Assert.NotNull(result);

                    PropertyDescriptor expected = propertyDescriptors
                        .First(p => string.Equals(p.Name, name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));

                    Assert.Equal(expected, result);
                }
                else
                {
                    Assert.Null(result);
                }
            }
        }

        private class MockPropertyDescriptor : PropertyDescriptor
        {
            public MockPropertyDescriptor(string name)
                : base(name, new Attribute[] { })
            {
            }

            public override Type ComponentType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override Type PropertyType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool CanResetValue(object component)
            {
                throw new NotImplementedException();
            }

            public override object GetValue(object component)
            {
                throw new NotImplementedException();
            }

            public override void ResetValue(object component)
            {
                throw new NotImplementedException();
            }

            public override void SetValue(object component, object value)
            {
                throw new NotImplementedException();
            }

            public override bool ShouldSerializeValue(object component)
            {
                throw new NotImplementedException();
            }
        }
    }
}
