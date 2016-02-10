// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization;
using Xunit;


public static partial class DataContractSerializerTests
{
    [Fact]
    public static void DCS_MyPersonSurrogate_Stress()
    {
        // This test is to verify a bug fix made in ObjectToIdCache.cs.
        // There was a bug with ObjectToIdCache.RemoveAt() method which might cause
        // one item be added into the cache more than once.
        // The issue could happen in the following scenario,
        // 1) ObjectToIdCache is used. ObjectToIdCache is used for DataContract types 
        //    marked with [DataContract(IsReference = true)].
        // 2) ObjectToIdCache.RemoveAt() is called. The method is called only when one uses
        //    DataContract Surrogate.
        // 3) There's hash-key collision in the cache and elements are deletes at certain position.
        //
        // The reason that I used multi-iterations here is because the issue does not always repro.
        // It was a matter of odds. The more iterations we run here the more likely we repro the issue. 
        for (int iterations = 0; iterations < 5; iterations++)
        {
            int length = 2000;
            DataContractSerializer dcs = new DataContractSerializer(typeof(FamilyForStress));
            dcs.SetSerializationSurrogateProvider(new MyPersonSurrogateProvider());
            var members = new NonSerializablePersonForStress[2 * length];
            for (int i = 0; i < length; i++)
            {
                var m = new NonSerializablePersonForStress("name", 30);

                // We put the same object at two different slots. Because the DataContract type
                // is marked with [DataContract(IsReference = true)], after serialization-deserialization,
                // the objects at this two places should be the same object to each other.
                members[i] = m;
                members[i + length] = m;
            }

            MemoryStream ms = new MemoryStream();
            FamilyForStress myFamily = new FamilyForStress
            {
                Members = members
            };
            dcs.WriteObject(ms, myFamily);
            ms.Position = 0;
            var newFamily = (FamilyForStress)dcs.ReadObject(ms);
            Assert.StrictEqual(myFamily.Members.Length, newFamily.Members.Length);
            for (int i = 0; i < myFamily.Members.Length; ++i)
            {
                Assert.StrictEqual(myFamily.Members[i].Name, newFamily.Members[i].Name);
            }

            var resultMembers = newFamily.Members;
            for (int i = 0; i < length; i++)
            {
                Assert.Equal(resultMembers[i], resultMembers[i + length]);
            }
        }
    }
}
