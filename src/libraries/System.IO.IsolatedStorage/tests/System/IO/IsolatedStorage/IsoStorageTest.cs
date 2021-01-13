// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.IO.IsolatedStorage
{
    // We put the tests in the "Store collection" to get them to pick up the StoreTestsFixture. This will run the fixture
    // at the start and end of the collection, cleaning the test environment.
    [Collection("Store collection")]
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

        public enum PresetScopes
        {
            UserStoreForApplication,
            UserStoreForAssembly,
            UserStoreForDomain,
            MachineStoreForAssembly,
            MachineStoreForApplication,
            MachineStoreForDomain
        }

        public static IsolatedStorageFile GetPresetScope(PresetScopes scope)
        {
            switch (scope)
            {
                case PresetScopes.UserStoreForApplication:
                    return IsolatedStorageFile.GetUserStoreForApplication();
                case PresetScopes.UserStoreForAssembly:
                    return IsolatedStorageFile.GetUserStoreForAssembly();
                case PresetScopes.UserStoreForDomain:
                    return IsolatedStorageFile.GetUserStoreForDomain();
                case PresetScopes.MachineStoreForApplication:
                    return IsolatedStorageFile.GetMachineStoreForApplication();
                case PresetScopes.MachineStoreForAssembly:
                    return IsolatedStorageFile.GetMachineStoreForAssembly();
                case PresetScopes.MachineStoreForDomain:
                    return IsolatedStorageFile.GetMachineStoreForDomain();
                default:
                    throw new InvalidOperationException("Unknown preset scope");
            }
        }

        public static IEnumerable<object[]> ValidStores
        {
            get
            {
                // While it would be nice to just kick back IsolatedStorageFile instances it is nearly impossible
                // as the collection will be enumerated completely before the first invocation of a [Theory].
                // Avoiding TheoryData and disabling DiscoveryEnumeration is not enough.

                TheoryData<PresetScopes> validScopes = new TheoryData<PresetScopes>
                {
                    PresetScopes.UserStoreForApplication,
                    PresetScopes.UserStoreForAssembly,
                    PresetScopes.UserStoreForDomain,
                };

                // https://github.com/dotnet/runtime/issues/2092
                if (OperatingSystem.IsWindows()
                    && !PlatformDetection.IsInAppContainer)
                {
                    validScopes.Add(PresetScopes.MachineStoreForApplication);
                    validScopes.Add(PresetScopes.MachineStoreForAssembly);
                    validScopes.Add(PresetScopes.MachineStoreForDomain);
                }

                return validScopes;
            }
        }

/*
 *      Template for Store test method
 *
        [Theory, MemberData(nameof(ValidStores))]
        public void ExampleTest(PresetScopes scope)
        {
            // If a dirty state will fail the test, use this
            TestHelper.WipeStores();

            using (var isf = GetPresetScope(scope))
            {
            }
        }
*/
    }
}
