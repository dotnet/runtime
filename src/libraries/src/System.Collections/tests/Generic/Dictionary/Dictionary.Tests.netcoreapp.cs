// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public partial class Dictionary_IDictionary_NonGeneric_Tests : IDictionary_NonGeneric_Tests
    {
        protected override ModifyOperation ModifyEnumeratorThrows => ModifyOperation.Add | ModifyOperation.Insert | ModifyOperation.Clear;

        protected override ModifyOperation ModifyEnumeratorAllowed => ModifyOperation.Remove;
    }

}
