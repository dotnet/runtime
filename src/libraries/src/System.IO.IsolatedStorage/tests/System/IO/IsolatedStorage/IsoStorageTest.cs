// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.IO.IsolatedStorage
{
    public class IsoStorageTest
    {
        public static IEnumerable<object[]> ValidScopes
        {
            get
            {
                return new TheoryData<IsolatedStorageScope>
                {
                    IsolatedStorageScope.User | IsolatedStorageScope.Assembly,
                    IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain,
                    IsolatedStorageScope.Roaming | IsolatedStorageScope.User | IsolatedStorageScope.Assembly,
                    IsolatedStorageScope.Roaming | IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain,
                    IsolatedStorageScope.Application | IsolatedStorageScope.User,
                    IsolatedStorageScope.Application | IsolatedStorageScope.User | IsolatedStorageScope.Roaming,
                    IsolatedStorageScope.Application | IsolatedStorageScope.Machine,
                    IsolatedStorageScope.Machine | IsolatedStorageScope.Assembly,
                    IsolatedStorageScope.Machine | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain
                };
            }
        }
    }
}
