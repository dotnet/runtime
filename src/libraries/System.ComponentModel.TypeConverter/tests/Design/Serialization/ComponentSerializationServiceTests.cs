// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.ComponentModel.Design.Serialization.Tests
{
    public class ComponentSerializationServiceTests
    {
        [Fact]
        public void DeserializeTo_SerializationStore_IContainer()
        {
            var service = new SubComponentSerializationService { ExpectedValidateRecycledTypes = true };
            service.DeserializeTo(null, null);
            Assert.True(service.CalledDeserializeTo);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DeserializeTo_SerializationStore_IContainer_Bool(bool validateRecycledTypes)
        {
            var service = new SubComponentSerializationService { ExpectedValidateRecycledTypes = validateRecycledTypes };
            service.DeserializeTo(null, null, validateRecycledTypes);
            Assert.True(service.CalledDeserializeTo);
        }

        public class SubComponentSerializationService : ComponentSerializationService
        {
            public bool CalledDeserializeTo { get; set; }
            public bool ExpectedValidateRecycledTypes { get; set; }

            public override void DeserializeTo(SerializationStore store, IContainer container, bool validateRecycledTypes, bool applyDefaults)
            {
                CalledDeserializeTo = true;
                Assert.True(applyDefaults);
                Assert.Equal(ExpectedValidateRecycledTypes, validateRecycledTypes);
            }

            public override SerializationStore CreateStore() => throw new NotImplementedException();

            public override ICollection Deserialize(SerializationStore store) => throw new NotImplementedException();

            public override ICollection Deserialize(SerializationStore store, IContainer container) => throw new NotImplementedException();

            public override SerializationStore LoadStore(Stream stream) => throw new NotImplementedException();

            public override void Serialize(SerializationStore store, object value) => throw new NotImplementedException();

            public override void SerializeAbsolute(SerializationStore store, object value) => throw new NotImplementedException();

            public override void SerializeMember(SerializationStore store, object owningObject, MemberDescriptor member) => throw new NotImplementedException();

            public override void SerializeMemberAbsolute(SerializationStore store, object owningObject, MemberDescriptor member) => throw new NotImplementedException();
        }
    }
}
