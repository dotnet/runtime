// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Management.Tests
{
    [Collection("Mof Collection")]
    public class ManagementObjectSearcherTestsMofRequired
    {
        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsElevatedAndSupportsWmi))]
        [OuterLoop]
        public void Static_Instances()
        {
            using (var searcher = new ManagementObjectSearcher())
            {
                const string TestClass = "Class2";
                searcher.Scope.Path.NamespacePath = WmiTestHelper.Namespace;
                var selectQuery = new SelectQuery(TestClass);
                searcher.Query = selectQuery;

                ManagementObjectCollection instances = searcher.Get();
                Assert.Equal(2, instances.Count);
                foreach (ManagementObject instance in instances)
                    Assert.Equal(TestClass, instance.Path.ClassName);
            }
        }

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsElevatedAndSupportsWmi))]
        [OuterLoop]
        public void Static_Related_Instances()
        {
            using (var searcher = new ManagementObjectSearcher())
            {
                var relatedObjectQuery = new RelatedObjectQuery("Class3.Class3Key='Three2'");
                searcher.Scope.Path.NamespacePath = WmiTestHelper.Namespace;
                searcher.Query = relatedObjectQuery;
                ManagementObjectCollection instances = searcher.Get();
                Assert.Equal(1, instances.Count);

                foreach (ManagementObject instance in instances)
                    Assert.Equal("Class4", instance.Path.ClassName);
            }
        }

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsElevatedAndSupportsWmi))]
        [OuterLoop]
        public void Static_Relationship_Classes()
        {
            using (var searcher = new ManagementObjectSearcher())
            {
                var relationshipQuery = new RelationshipQuery("Class1.MyKey='One2'");
                searcher.Scope.Path.NamespacePath = WmiTestHelper.Namespace;
                searcher.Query = relationshipQuery;
                ManagementObjectCollection instances = searcher.Get();
                Assert.Equal(1, instances.Count);

                foreach (ManagementObject instance in instances)
                    Assert.Equal("AssocA", instance.Path.ClassName);
            }
        }
    }
}
