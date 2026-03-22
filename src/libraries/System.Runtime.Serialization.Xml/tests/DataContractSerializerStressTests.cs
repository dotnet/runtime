// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SerializationTypes;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
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
                Assert.Equal(myFamily.Members[i].Name, newFamily.Members[i].Name);
            }

            var resultMembers = newFamily.Members;
            for (int i = 0; i < length; i++)
            {
                Assert.Equal(resultMembers[i], resultMembers[i + length]);
            }
        }
    }

    [Fact]
    public static void ObjectToIdCache_RemoveAt_WrappedElementRemainsReachable()
    {
        // Deterministic regression test for ObjectToIdCache.RemoveAt() backward-shift deletion.
        //
        // Previously, an element whose probe chain wrapped around the array boundary
        // was not shifted when an earlier slot in that chain was vacated, leaving the
        // element permanently unreachable. This constructs the exact minimal scenario:
        //
        //   Slot:   0   1   2   3       4             5    6
        //   Before: X   Y   Z   W(del)  V(home=6)     -    U(home=6)
        //   After:  X   Y   Z   V       -             -    U
        //
        // V is at slot 4 because its home (6) was taken by U, so its probe chain went
        // 6→0→1→2→3→4. When W is removed from slot 3, V must shift to slot 3 or it
        // becomes permanently unreachable (the probe from home 6 hits the null at 3 first).

        Assembly dcsAssembly = typeof(DataContractSerializer).Assembly;
        Type cacheType = dcsAssembly.GetType("System.Runtime.Serialization.ObjectToIdCache")!;
        FieldInfo objsField = cacheType.GetField("m_objs", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo idsField = cacheType.GetField("m_ids", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo countField = cacheType.GetField("m_currentCount", BindingFlags.Instance | BindingFlags.NonPublic)!;
        MethodInfo removeAt = cacheType.GetMethod("RemoveAt", BindingFlags.Instance | BindingFlags.NonPublic)!;
        MethodInfo getId = cacheType.GetMethod("GetId")!;

        const int TableSize = 7;

        // Sample objects until we have enough whose identity hash codes mod TableSize
        // are: one each at positions 0, 1, 2, 3, and two at position 6.
        int[] slotsNeeded = { 1, 1, 1, 1, 0, 0, 2 };
        var bySlot = new System.Collections.Generic.List<object>[TableSize];
        for (int i = 0; i < TableSize; i++)
            bySlot[i] = new System.Collections.Generic.List<object>();

        int found = 0;
        while (found < 6)
        {
            var o = new object();
            int slot = (RuntimeHelpers.GetHashCode(o) & 0x7FFFFFFF) % TableSize;
            if (bySlot[slot].Count < slotsNeeded[slot])
            {
                bySlot[slot].Add(o);
                found++;
            }
        }

        object X = bySlot[0][0]; // home=0, placed at slot 0
        object Y = bySlot[1][0]; // home=1, placed at slot 1
        object Z = bySlot[2][0]; // home=2, placed at slot 2
        object W = bySlot[3][0]; // home=3, placed at slot 3 (will be removed)
        object U = bySlot[6][0]; // home=6, placed at slot 6
        object V = bySlot[6][1]; // home=6, probed 6→0→1→2→3→4, placed at slot 4

        // Construct the problematic cache state directly via reflection,
        // bypassing the normal insertion path to guarantee the exact layout.
        object cache = Activator.CreateInstance(cacheType)!;
        var objsArray = new object[TableSize];
        var idsArray = new int[TableSize];
        objsArray[0] = X; idsArray[0] = 1;
        objsArray[1] = Y; idsArray[1] = 2;
        objsArray[2] = Z; idsArray[2] = 3;
        objsArray[3] = W; idsArray[3] = 4;
        objsArray[4] = V; idsArray[4] = 5; // V is here because slots 6,0,1,2,3 were all occupied
        // objsArray[5] is null (empty slot)
        objsArray[6] = U; idsArray[6] = 6;
        objsField.SetValue(cache, objsArray);
        idsField.SetValue(cache, idsArray);
        countField.SetValue(cache, 1); // bypass Rehash trigger

        // Remove W from slot 3. The correct backward-shift deletion must move V
        // from slot 4 to slot 3 so that the probe chain from V's home (6) can still
        // find V (via 6→0→1→2→3). Without the move, the probe hits the null at
        // slot 3 and reports V as absent.
        removeAt.Invoke(cache, new object[] { 3 });

        // V must still be reachable with its original ID.
        object[] args = new object[] { V, false };
        int vId = (int)getId.Invoke(cache, args)!;
        Assert.Equal(5, vId);

        // U must still be reachable with its original ID.
        args = new object[] { U, false };
        int uId = (int)getId.Invoke(cache, args)!;
        Assert.Equal(6, uId);
    }
}
