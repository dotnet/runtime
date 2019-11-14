// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class EventDescriptorCollectionTests
    {
        [Fact]
        public void EmptyCollectionShouldBeCached()
        {
            Assert.Same(EventDescriptorCollection.Empty, EventDescriptorCollection.Empty);
        }

        [Fact]
        public void CreateEmptyCollectionWithNull()
        {
            var collection = new EventDescriptorCollection(null);

            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public void CollectionSyncProperties()
        {
            AttributeCollection collection = new AttributeCollection(null);

            Assert.Null(((ICollection)collection).SyncRoot);
            Assert.False(((ICollection)collection).IsSynchronized);
        }

        [Fact]
        public void ConstructorTests()
        {
            var descriptors = new EventDescriptor[]
            {
                new MockEventDescriptor("descriptor1")
            };

            var collection = new EventDescriptorCollection(descriptors);

            Assert.Equal(descriptors, collection.Cast<EventDescriptor>());

            // These methods are implemented as explicit properties so we need to ensure they are what we expect
            Assert.False(((IList)collection).IsReadOnly);
            Assert.False(((IList)collection).IsFixedSize);
        }

        [Fact]
        public void ConstructorReadOnlyTests()
        {
            var descriptors = new EventDescriptor[]
            {
                new MockEventDescriptor("descriptor1")
            };

            var collection = new EventDescriptorCollection(descriptors, true);

            Assert.Equal(descriptors, collection.Cast<EventDescriptor>());

            // These methods are implemented as explicit properties so we need to ensure they are what we expect
            Assert.True(((IList)collection).IsReadOnly);
            Assert.True(((IList)collection).IsFixedSize);
        }

        [Fact]
        public void ReadOnlyThrows()
        {
            var collection = new EventDescriptorCollection(null, true);

            // The readonly check occurs before anything else, so we don't need to pass in actual valid values
            Assert.Throws<NotSupportedException>(() => collection.Add(null));
            Assert.Throws<NotSupportedException>(() => collection.Insert(1, null));
            Assert.Throws<NotSupportedException>(() => collection.RemoveAt(1));
            Assert.Throws<NotSupportedException>(() => collection.Remove(null));
            Assert.Throws<NotSupportedException>(() => collection.Clear());
        }

        [Fact]
        public void CountTests()
        {
            var descriptors = new EventDescriptor[]
            {
                new MockEventDescriptor("descriptor1"),
                new MockEventDescriptor("descriptor2")
            };
            var collection = new EventDescriptorCollection(descriptors);

            Assert.Equal(2, descriptors.Length);
            Assert.Equal(2, ((ICollection)descriptors).Count);
        }

        private class MockEventDescriptor : EventDescriptor
        {
            public MockEventDescriptor(string name)
                : base(name, new Attribute[] { })
            {
            }

            public override Type ComponentType { get; } = typeof(EventArgs);

            public override Type EventType { get; } = typeof(EventArgs);

            public override bool IsMulticast { get; } = false;

            public override void AddEventHandler(object component, Delegate value)
            {
            }

            public override void RemoveEventHandler(object component, Delegate value)
            {
            }
        }
    }
}
