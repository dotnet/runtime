// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class CollectionChangedEventArgsTests
    {
        [Theory]
        [InlineData(CollectionChangeAction.Add, null)]
        [InlineData(CollectionChangeAction.Remove, "")]
        [InlineData(CollectionChangeAction.Refresh, "element")]
        [InlineData(CollectionChangeAction.Add - 1, "element")]
        [InlineData(CollectionChangeAction.Refresh + 1, "element")]
        public void Ctor_Action_Element(CollectionChangeAction action, object element)
        {
            var args = new CollectionChangeEventArgs(action, element);
            Assert.Equal(action, args.Action);
            Assert.Same(element, args.Element);
        }
    }
}
