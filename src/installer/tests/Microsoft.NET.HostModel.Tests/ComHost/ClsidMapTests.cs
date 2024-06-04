// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Microsoft.DotNet.CoreSetup.Test;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.NET.HostModel.ComHost.Tests
{
    public class ClsidMapTests : IClassFixture<ClsidMapTests.SharedTestState>
    {
        private readonly SharedTestState sharedTestState;

        public ClsidMapTests(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void PublicComVisibleTypeWithGuidAdded()
        {
            JObject clsidMap = CreateClsidMap(sharedTestState.ComLibrary);
            JProperty comVisibleEntry = clsidMap.Property(SharedTestState.ComVisibleGuid);
            Assert.NotNull(comVisibleEntry);
            JObject entry = (JObject)comVisibleEntry.Value;
            Assert.Equal(SharedTestState.ComVisibleTypeName, entry.Property("type").Value.ToString());
            Assert.Equal(System.Reflection.AssemblyName.GetAssemblyName(sharedTestState.ComLibrary.AppDll).FullName, entry.Property("assembly").Value.ToString());
        }

        [Fact]
        public void PublicComVisibleTypeWithoutGuidThrows()
        {
            var exception = Assert.Throws<MissingGuidException>(() => CreateClsidMap(sharedTestState.ComLibraryMissingGuid));
            Assert.Equal(SharedTestState.MissingGuidTypeName, exception.TypeName);
        }

        [Fact]
        public void PublicNestedTypeOfPublicTypeAdded()
        {
            JObject clsidMap = CreateClsidMap(sharedTestState.ComLibrary);
            JProperty comVisibleEntry = clsidMap.Property(SharedTestState.ComVisibleNestedGuid);
            Assert.NotNull(comVisibleEntry);
            JObject entry = (JObject)comVisibleEntry.Value;
            Assert.Equal(SharedTestState.ComVisibleNestedTypeName, entry.Property("type").Value.ToString());
        }

        [Fact]
        public void NonPublicTypeNotAdded()
        {
            JObject clsidMap = CreateClsidMap(sharedTestState.ComLibrary);
            JProperty comVisibleEntry = clsidMap.Property(SharedTestState.ComVisibleNonPublicGuid);
            Assert.Null(comVisibleEntry);
        }

        [Fact]
        public void PublicNestedTypeOfNonPublicTypeNotAdded()
        {
            JObject clsidMap = CreateClsidMap(sharedTestState.ComLibrary);
            JProperty comVisibleEntry = clsidMap.Property(SharedTestState.ComVisibleNonPublicNestedGuid);
            Assert.Null(comVisibleEntry);
        }

        [Fact]
        public void PublicComVisibleTypeWithDuplicateGuidThrows()
        {
            var exception = Assert.Throws<ConflictingGuidException>(() => CreateClsidMap(sharedTestState.ComLibraryConflictingGuid));
            Assert.Equal(Guid.Parse(SharedTestState.ConflictingGuid), exception.Guid);
            Assert.Equal(SharedTestState.ConflictingGuidTypeName1, exception.TypeName1);
            Assert.Equal(SharedTestState.ConflictingGuidTypeName2, exception.TypeName2);
        }

        [Fact]
        public void DefaultProgIdIsTypeName()
        {
            JObject clsidMap = CreateClsidMap(sharedTestState.ComLibrary);
            JProperty comVisibleEntry = clsidMap.Property(SharedTestState.ComVisibleGuid);
            Assert.NotNull(comVisibleEntry);
            JObject entry = (JObject)comVisibleEntry.Value;
            Assert.Equal(SharedTestState.ComVisibleTypeName, entry.Property("progid").Value.ToString());
        }

        [Fact]
        public void ExplicitProgIdUsed()
        {
            JObject clsidMap = CreateClsidMap(sharedTestState.ComLibrary);
            JProperty comVisibleEntry = clsidMap.Property(SharedTestState.ComVisibleCustomProgIdGuid);
            Assert.NotNull(comVisibleEntry);
            JObject entry = (JObject)comVisibleEntry.Value;
            Assert.Equal(SharedTestState.ComVisibleCustomProgIdProgId, entry.Property("progid").Value.ToString());
        }

        [Fact]
        public void ExplicitlyEmptyProgIdNotInClsidMap()
        {
            JObject clsidMap = CreateClsidMap(sharedTestState.ComLibrary);
            JProperty comVisibleEntry = clsidMap.Property(SharedTestState.ExplicitNoProgIdGuid);
            Assert.NotNull(comVisibleEntry);
            JObject entry = (JObject)comVisibleEntry.Value;
            Assert.Null(entry.Property("progid"));
        }

        private JObject CreateClsidMap(TestApp library)
        {
            using var testDirectory = TestArtifact.Create("clsidmap");
            string clsidMapPath = Path.Combine(testDirectory.Location, "test.clsidmap");

            using (var assemblyStream = new FileStream(library.AppDll, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read))
            using (PEReader peReader = new PEReader(assemblyStream))
            {
                if (peReader.HasMetadata)
                {
                    MetadataReader reader = peReader.GetMetadataReader();
                    ClsidMap.Create(reader, clsidMapPath);
                }
            }

            using (var clsidMapFile = File.OpenText(clsidMapPath))
            using (var clsidMapReader = new JsonTextReader(clsidMapFile) { MaxDepth = null })
            {
                return JObject.Load(clsidMapReader);
            }
        }

        public class SharedTestState : IDisposable
        {
            public const string NotComVisibleGuid = "{6e30943e-b8ab-4e02-a904-9f1b5bb1c97d}";
            public const string ComVisibleGuid = "{36e75747-aecd-43bf-9082-1a605889c762}";
            public const string ComVisibleTypeName = "ComLibrary.ComVisible";
            public const string ComVisibleNestedGuid = "{c82e4585-58bd-46e0-a76d-c0b6975e5984}";
            public const string ComVisibleNestedTypeName = "ComLibrary.ComVisible+Nested";
            public const string ComVisibleNonPublicGuid = "{cf55ff0a-19a6-45a6-9aea-52597be13fb5}";
            public const string ComVisibleNonPublicNestedGuid = "{8a0a7085-aca4-4651-9878-ca42747e2206}";
            public const string ComVisibleCustomProgIdGuid = "{f5ad253b-845e-4c91-95a7-3ff2fa0c91cd}";
            public const string ComVisibleCustomProgIdProgId = "CustomProgId";
            public const string ExplicitNoProgIdGuid = "{4c8bd844-593d-43cb-b605-f0bc52f674fa}";

            public const string MissingGuidTypeName = "ComLibrary.Server";

            public const string ConflictingGuidTypeName1 = "ComLibrary.Server";
            public const string ConflictingGuidTypeName2 = "ComLibrary.Server2";
            public const string ConflictingGuid = "{cc6e9910-18d5-484a-a2d2-fa8910fd0261}";

            public SharedTestState()
            {
                ComLibrary = TestApp.CreateFromBuiltAssets("ComLibrary");
                ComLibraryMissingGuid = TestApp.CreateFromBuiltAssets("ComLibraryMissingGuid");
                ComLibraryConflictingGuid = TestApp.CreateFromBuiltAssets("ComLibraryConflictingGuid");
            }

            public TestApp ComLibrary { get; }
            public TestApp ComLibraryMissingGuid { get; }
            public TestApp ComLibraryConflictingGuid { get; }

            public void Dispose()
            {
                ComLibrary?.Dispose();
                ComLibraryMissingGuid?.Dispose();
                ComLibraryConflictingGuid?.Dispose();
            }
        }
    }
}
