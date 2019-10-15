// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Tests
{
    internal class MockEventDescriptor : EventDescriptor
    {
        public MockEventDescriptor(string name = null, Attribute[] attributes = null)
            : base(name ?? nameof(MockEventDescriptor), attributes ?? new Attribute[0])
        { }

        public override Type ComponentType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Type EventType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsMulticast
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override void AddEventHandler(object component, Delegate value)
        {
            throw new NotImplementedException();
        }

        public override void RemoveEventHandler(object component, Delegate value)
        {
            throw new NotImplementedException();
        }
    }
}
