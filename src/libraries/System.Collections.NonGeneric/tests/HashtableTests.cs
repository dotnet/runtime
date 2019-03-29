// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

#pragma warning disable 618 // obsolete types

namespace System.Collections.Tests
{
    public class HashtableTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            var hash = new ComparableHashtable();
            VerifyHashtable(hash, null, null);
        }

        [Fact]
        public void Ctor_HashCodeProvider_Comparer()
        {
            var hash = new ComparableHashtable(CaseInsensitiveHashCodeProvider.DefaultInvariant, StringComparer.OrdinalIgnoreCase);
            VerifyHashtable(hash, null, hash.EqualityComparer);
            Assert.Same(CaseInsensitiveHashCodeProvider.DefaultInvariant, hash.HashCodeProvider);
            Assert.Same(StringComparer.OrdinalIgnoreCase, hash.Comparer);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void Ctor_HashCodeProvider_Comparer_NullInputs(bool nullProvider, bool nullComparer)
        {
            var hash = new ComparableHashtable(
                nullProvider ? null : CaseInsensitiveHashCodeProvider.DefaultInvariant,
                nullComparer ? null : StringComparer.OrdinalIgnoreCase);
            VerifyHashtable(hash, null, hash.EqualityComparer);
        }

        [Fact]
        public void Ctor_IDictionary()
        {
            // No exception
            var hash1 = new ComparableHashtable(new Hashtable());
            Assert.Equal(0, hash1.Count);

            hash1 = new ComparableHashtable(new Hashtable(new Hashtable(new Hashtable(new Hashtable(new Hashtable())))));
            Assert.Equal(0, hash1.Count);

            Hashtable hash2 = Helpers.CreateIntHashtable(100);
            hash1 = new ComparableHashtable(hash2);

            VerifyHashtable(hash1, hash2, null);
        }

        [Fact]
        public void Ctor_IDictionary_NullDictionary_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("d", () => new Hashtable((IDictionary)null)); // Dictionary is null
        }

        [Fact]
        public void Ctor_IDictionary_HashCodeProvider_Comparer()
        {
            // No exception
            var hash1 = new ComparableHashtable(new Hashtable(), CaseInsensitiveHashCodeProvider.DefaultInvariant, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(0, hash1.Count);

            hash1 = new ComparableHashtable(new Hashtable(new Hashtable(new Hashtable(new Hashtable(new Hashtable())))), CaseInsensitiveHashCodeProvider.DefaultInvariant, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(0, hash1.Count);

            Hashtable hash2 = Helpers.CreateIntHashtable(100);
            hash1 = new ComparableHashtable(hash2, CaseInsensitiveHashCodeProvider.DefaultInvariant, StringComparer.OrdinalIgnoreCase);

            VerifyHashtable(hash1, hash2, hash1.EqualityComparer);
        }

        [Fact]
        public void Ctor_IDictionary_HashCodeProvider_Comparer_NullDictionary_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("d", () => new Hashtable(null, CaseInsensitiveHashCodeProvider.Default, StringComparer.OrdinalIgnoreCase)); // Dictionary is null
        }

        [Fact]
        public void Ctor_IEqualityComparer()
        {
            RemoteExecutor.Invoke(() =>
            {
                // Null comparer
                var hash = new ComparableHashtable((IEqualityComparer)null);
                VerifyHashtable(hash, null, null);

                // Custom comparer
                IEqualityComparer comparer = StringComparer.CurrentCulture;
                hash = new ComparableHashtable(comparer);
                VerifyHashtable(hash, null, comparer);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(100)]
        public void Ctor_Int(int capacity)
        {
            var hash = new ComparableHashtable(capacity);
            VerifyHashtable(hash, null, null);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(100)]
        public void Ctor_Int_HashCodeProvider_Comparer(int capacity)
        {
            var hash = new ComparableHashtable(capacity, CaseInsensitiveHashCodeProvider.DefaultInvariant, StringComparer.OrdinalIgnoreCase);
            VerifyHashtable(hash, null, hash.EqualityComparer);
        }

        [Fact]
        public void Ctor_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new Hashtable(-1)); // Capacity < 0
            AssertExtensions.Throws<ArgumentException>("capacity", null, () => new Hashtable(int.MaxValue)); // Capacity / load factor > int.MaxValue
        }

        [Fact]
        public void Ctor_IDictionary_Int()
        {
            // No exception
            var hash1 = new ComparableHashtable(new Hashtable(), 1f, CaseInsensitiveHashCodeProvider.DefaultInvariant, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(0, hash1.Count);

            hash1 = new ComparableHashtable(new Hashtable(new Hashtable(new Hashtable(new Hashtable(new Hashtable(), 1f), 1f), 1f), 1f), 1f,
                CaseInsensitiveHashCodeProvider.DefaultInvariant, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(0, hash1.Count);

            Hashtable hash2 = Helpers.CreateIntHashtable(100);
            hash1 = new ComparableHashtable(hash2, 1f, CaseInsensitiveHashCodeProvider.DefaultInvariant, StringComparer.OrdinalIgnoreCase);

            VerifyHashtable(hash1, hash2, hash1.EqualityComparer);
        }

        [Fact]
        public void Ctor_IDictionary_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("d", () => new Hashtable(null, 1f)); // Dictionary is null

            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(new Hashtable(), 0.09f)); // Load factor < 0.1f
            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(new Hashtable(), 1.01f)); // Load factor > 1f

            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(new Hashtable(), float.NaN)); // Load factor is NaN
            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(new Hashtable(), float.PositiveInfinity)); // Load factor is infinity
            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(new Hashtable(), float.NegativeInfinity)); // Load factor is infinity
        }

        [Fact]
        public void Ctor_IDictionary_Int_HashCodeProvider_Comparer()
        {
            // No exception
            var hash1 = new ComparableHashtable(new Hashtable(), 1f);
            Assert.Equal(0, hash1.Count);

            hash1 = new ComparableHashtable(new Hashtable(new Hashtable(new Hashtable(new Hashtable(new Hashtable(), 1f), 1f), 1f), 1f), 1f);
            Assert.Equal(0, hash1.Count);

            Hashtable hash2 = Helpers.CreateIntHashtable(100);
            hash1 = new ComparableHashtable(hash2, 1f);

            VerifyHashtable(hash1, hash2, null);
        }

        [Fact]
        public void Ctor_IDictionary_IEqualityComparer()
        {
            RemoteExecutor.Invoke(() =>
            {
                // No exception
                var hash1 = new ComparableHashtable(new Hashtable(), null);
                Assert.Equal(0, hash1.Count);

                hash1 = new ComparableHashtable(new Hashtable(new Hashtable(new Hashtable(new Hashtable(new Hashtable(), null), null), null), null), null);
                Assert.Equal(0, hash1.Count);

                // Null comparer
                Hashtable hash2 = Helpers.CreateIntHashtable(100);
                hash1 = new ComparableHashtable(hash2, null);
                VerifyHashtable(hash1, hash2, null);

                // Custom comparer
                hash2 = Helpers.CreateIntHashtable(100);
                IEqualityComparer comparer = StringComparer.CurrentCulture;
                hash1 = new ComparableHashtable(hash2, comparer);
                VerifyHashtable(hash1, hash2, comparer);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void Ctor_IDictionary_IEqualityComparer_NullDictionary_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("d", () => new Hashtable((IDictionary)null, null)); // Dictionary is null
        }

        [Theory]
        [InlineData(0, 0.1)]
        [InlineData(10, 0.2)]
        [InlineData(100, 0.3)]
        [InlineData(1000, 1)]
        public void Ctor_Int_Int(int capacity, float loadFactor)
        {
            var hash = new ComparableHashtable(capacity, loadFactor);
            VerifyHashtable(hash, null, null);
        }

        [Theory]
        [InlineData(0, 0.1)]
        [InlineData(10, 0.2)]
        [InlineData(100, 0.3)]
        [InlineData(1000, 1)]
        public void Ctor_Int_Int_HashCodeProvider_Comparer(int capacity, float loadFactor)
        {
            var hash = new ComparableHashtable(capacity, loadFactor, CaseInsensitiveHashCodeProvider.DefaultInvariant, StringComparer.OrdinalIgnoreCase);
            VerifyHashtable(hash, null, hash.EqualityComparer);
        }

        [Fact]
        public void Ctor_Int_Int_GenerateNewPrime()
        {
            // The ctor for Hashtable performs the following calculation:
            // rawSize = capacity / (loadFactor * 0.72)
            // If rawSize is > 3, then it calls HashHelpers.GetPrime(rawSize) to generate a prime.
            // Then, if the rawSize > 7,199,369 (the largest number in a list of known primes), we have to generate a prime programatically
            // This test makes sure this works.
            int capacity = 8000000;
            float loadFactor = 0.1f / 0.72f;
            try
            {
                var hash = new ComparableHashtable(capacity, loadFactor);
            }
            catch (OutOfMemoryException)
            {
                // On memory constrained devices, we can get an OutOfMemoryException, which we can safely ignore.
            }
        }

        [Fact]
        public void Ctor_Int_Int_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new Hashtable(-1, 1f)); // Capacity < 0
            AssertExtensions.Throws<ArgumentException>("capacity", null, () => new Hashtable(int.MaxValue, 0.1f)); // Capacity / load factor > int.MaxValue

            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(100, 0.09f)); // Load factor < 0.1f
            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(100, 1.01f)); // Load factor > 1f

            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(100, float.NaN)); // Load factor is NaN
            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(100, float.PositiveInfinity)); // Load factor is infinity
            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(100, float.NegativeInfinity)); // Load factor is infinity
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void Ctor_Int_IEqualityComparer(int capacity)
        {
            RemoteExecutor.Invoke((rcapacity) =>
            {
                int.TryParse(rcapacity, out int capacityint);

                // Null comparer
                var hash = new ComparableHashtable(capacityint, null);
                VerifyHashtable(hash, null, null);

                // Custom comparer
                IEqualityComparer comparer = StringComparer.CurrentCulture;
                hash = new ComparableHashtable(capacityint, comparer);
                VerifyHashtable(hash, null, comparer);
                return RemoteExecutor.SuccessExitCode;
            }, capacity.ToString()).Dispose();
        }

        [Fact]
        public void Ctor_Int_IEqualityComparer_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new Hashtable(-1, null)); // Capacity < 0
            AssertExtensions.Throws<ArgumentException>("capacity", null, () => new Hashtable(int.MaxValue, null)); // Capacity / load factor > int.MaxValue
        }

        [Fact]
        public void Ctor_IDictionary_Int_IEqualityComparer()
        {
            RemoteExecutor.Invoke(() => {
                // No exception
                var hash1 = new ComparableHashtable(new Hashtable(), 1f, null);
                Assert.Equal(0, hash1.Count);

                hash1 = new ComparableHashtable(new Hashtable(new Hashtable(
                    new Hashtable(new Hashtable(new Hashtable(), 1f, null), 1f, null), 1f, null), 1f, null), 1f, null);
                Assert.Equal(0, hash1.Count);

                // Null comparer
                Hashtable hash2 = Helpers.CreateIntHashtable(100);
                hash1 = new ComparableHashtable(hash2, 1f, null);
                VerifyHashtable(hash1, hash2, null);

                hash2 = Helpers.CreateIntHashtable(100);
                // Custom comparer
                IEqualityComparer comparer = StringComparer.CurrentCulture;
                hash1 = new ComparableHashtable(hash2, 1f, comparer);
                VerifyHashtable(hash1, hash2, comparer);
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void Ctor_IDictionary_LoadFactor_IEqualityComparer_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("d", () => new Hashtable(null, 1f, null)); // Dictionary is null

            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(new Hashtable(), 0.09f, null)); // Load factor < 0.1f
            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(new Hashtable(), 1.01f, null)); // Load factor > 1f

            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(new Hashtable(), float.NaN, null)); // Load factor is NaN
            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(new Hashtable(), float.PositiveInfinity, null)); // Load factor is infinity
            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(new Hashtable(), float.NegativeInfinity, null)); // Load factor is infinity
        }

        [Theory]
        [InlineData(0, 0.1)]
        [InlineData(10, 0.2)]
        [InlineData(100, 0.3)]
        [InlineData(1000, 1)]
        public void Ctor_Int_Int_IEqualityComparer(int capacity, float loadFactor)
        {
            RemoteExecutor.Invoke((rcapacity, rloadFactor) =>
            {
                int.TryParse(rcapacity, out int capacityint);
                float.TryParse(rloadFactor, out float loadFactorFloat);

                // Null comparer
                var hash = new ComparableHashtable(capacityint, loadFactorFloat, null);
                VerifyHashtable(hash, null, null);
                Assert.Null(hash.EqualityComparer);

                // Custom compare
                IEqualityComparer comparer = StringComparer.CurrentCulture;
                hash = new ComparableHashtable(capacityint, loadFactorFloat, comparer);
                VerifyHashtable(hash, null, comparer);

                return RemoteExecutor.SuccessExitCode;
            }, capacity.ToString(), loadFactor.ToString()).Dispose();
        }

        [Fact]
        public void Ctor_Capacity_LoadFactor_IEqualityComparer_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new Hashtable(-1, 1f, null)); // Capacity < 0
            AssertExtensions.Throws<ArgumentException>("capacity", null, () => new Hashtable(int.MaxValue, 0.1f, null)); // Capacity / load factor > int.MaxValue

            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(100, 0.09f, null)); // Load factor < 0.1f
            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(100, 1.01f, null)); // Load factor > 1f

            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(100, float.NaN, null)); // Load factor is NaN
            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(100, float.PositiveInfinity, null)); // Load factor is infinity
            AssertExtensions.Throws<ArgumentOutOfRangeException>("loadFactor", () => new Hashtable(100, float.NegativeInfinity, null)); // Load factor is infinity
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public void DebuggerAttribute()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(new Hashtable());

            var hash = new Hashtable() { { "a", 1 }, { "b", 2 } };
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(hash);
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(Hashtable), Hashtable.Synchronized(hash));

            bool threwNull = false;
            try
            {
                DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(Hashtable), null);
            }
            catch (TargetInvocationException ex)
            {
                threwNull = ex.InnerException is ArgumentNullException;
            }

            Assert.True(threwNull);
        }

        [Fact]
        public void Add_ReferenceType()
        {
            var hash1 = new Hashtable();
            Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
            {
                // Value is a reference
                var foo = new Foo();
                hash2.Add("Key", foo);

                Assert.Equal("Hello World", ((Foo)hash2["Key"]).StringValue);

                // Changing original object should change the object stored in the Hashtable
                foo.StringValue = "Goodbye";
                Assert.Equal("Goodbye", ((Foo)hash2["Key"]).StringValue);
            });
        }

        [Fact]
        public void Add_ClearRepeatedly()
        {
            const int Iterations = 2;
            const int Count = 2;

            var hash = new Hashtable();
            for (int i = 0; i < Iterations; i++)
            {
                for (int j = 0; j < Count; j++)
                {
                    string key = "Key: i=" + i + ", j=" + j;
                    string value = "Value: i=" + i + ", j=" + j;
                    hash.Add(key, value);
                }

                Assert.Equal(Count, hash.Count);
                hash.Clear();
            }
        }

        [Fact]
        [OuterLoop]
        public void AddRemove_LargeAmountNumbers()
        {
            // Generate a random 100,000 array of ints as test data 
            var inputData = new int[100000];
            var random = new Random(341553);
            for (int i = 0; i < inputData.Length; i++)
            {
                inputData[i] = random.Next(7500000, int.MaxValue);
            }

            var hash = new Hashtable();

            int count = 0;
            foreach (long number in inputData)
            {
                hash.Add(number, count++);
            }

            count = 0;
            foreach (long number in inputData)
            {
                Assert.Equal(hash[number], count);
                Assert.True(hash.ContainsKey(number));

                count++;
            }

            foreach (long number in inputData)
            {
                hash.Remove(number);
            }

            Assert.Equal(0, hash.Count);
        }

        [Fact]
        public void DuplicatedKeysWithInitialCapacity()
        {
            // Make rehash get called because to many items with duplicated keys have been added to the hashtable
            var hash = new Hashtable(200);

            const int Iterations = 1600;
            for (int i = 0; i < Iterations; i += 2)
            {
                hash.Add(new BadHashCode(i), i.ToString());
                hash.Add(new BadHashCode(i + 1), (i + 1).ToString());

                hash.Remove(new BadHashCode(i));
                hash.Remove(new BadHashCode(i + 1));
            }

            for (int i = 0; i < Iterations; i++)
            {
                hash.Add(i.ToString(), i);
            }

            for (int i = 0; i < Iterations; i++)
            {
                Assert.Equal(i, hash[i.ToString()]);
            }
        }

        [Fact]
        public void DuplicatedKeysWithDefaultCapacity()
        {
            // Make rehash get called because to many items with duplicated keys have been added to the hashtable
            var hash = new Hashtable();

            const int Iterations = 1600;
            for (int i = 0; i < Iterations; i += 2)
            {
                hash.Add(new BadHashCode(i), i.ToString());
                hash.Add(new BadHashCode(i + 1), (i + 1).ToString());

                hash.Remove(new BadHashCode(i));
                hash.Remove(new BadHashCode(i + 1));
            }

            for (int i = 0; i < Iterations; i++)
            {
                hash.Add(i.ToString(), i);
            }

            for (int i = 0; i < Iterations; i++)
            {
                Assert.Equal(i, hash[i.ToString()]);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public void Clone(int count)
        {
            Hashtable hash1 = Helpers.CreateStringHashtable(count);
            Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
            {
                Hashtable clone = (Hashtable)hash2.Clone();

                Assert.Equal(hash2.Count, clone.Count);
                Assert.Equal(hash2.IsSynchronized, clone.IsSynchronized);
                Assert.Equal(hash2.IsFixedSize, clone.IsFixedSize);
                Assert.Equal(hash2.IsReadOnly, clone.IsReadOnly);

                for (int i = 0; i < clone.Count; i++)
                {
                    string key = "Key_" + i;
                    string value = "Value_" + i;

                    Assert.True(clone.ContainsKey(key));
                    Assert.True(clone.ContainsValue(value));
                    Assert.Equal(value, clone[key]);
                }
            });
        }

        [Fact]
        public void Clone_IsShallowCopy()
        {
            var hash = new Hashtable();
            for (int i = 0; i < 10; i++)
            {
                hash.Add(i, new Foo());
            }

            Hashtable clone = (Hashtable)hash.Clone();
            for (int i = 0; i < clone.Count; i++)
            {
                Assert.Equal("Hello World", ((Foo)clone[i]).StringValue);
                Assert.Same(hash[i], clone[i]);
            }

            // Change object in original hashtable
            ((Foo)hash[1]).StringValue = "Goodbye";
            Assert.Equal("Goodbye", ((Foo)clone[1]).StringValue);

            // Removing an object from the original hashtable doesn't change the clone
            hash.Remove(0);
            Assert.True(clone.Contains(0));
        }

        [Fact]
        public void Clone_HashtableCastedToInterfaces()
        {
            // Try to cast the returned object from Clone() to different types
            Hashtable hash = Helpers.CreateIntHashtable(100);

            ICollection collection = (ICollection)hash.Clone();
            Assert.Equal(hash.Count, collection.Count);

            IDictionary dictionary = (IDictionary)hash.Clone();
            Assert.Equal(hash.Count, dictionary.Count);
        }

        [Fact]
        public void ContainsKey()
        {
            Hashtable hash1 = Helpers.CreateStringHashtable(100);
            Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
            {
                for (int i = 0; i < hash2.Count; i++)
                {
                    string key = "Key_" + i;
                    Assert.True(hash2.ContainsKey(key));
                    Assert.True(hash2.Contains(key));
                }

                Assert.False(hash2.ContainsKey("Non Existent Key"));
                Assert.False(hash2.Contains("Non Existent Key"));

                Assert.False(hash2.ContainsKey(101));
                Assert.False(hash2.Contains("Non Existent Key"));

                string removedKey = "Key_1";
                hash2.Remove(removedKey);
                Assert.False(hash2.ContainsKey(removedKey));
                Assert.False(hash2.Contains(removedKey));
            });
        }

        [Fact]
        public void ContainsKey_EqualObjects()
        {
            var hash1 = new Hashtable();
            Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
            {
                var foo1 = new Foo() { StringValue = "Goodbye" };
                var foo2 = new Foo() { StringValue = "Goodbye" };

                hash2.Add(foo1, 101);

                Assert.True(hash2.ContainsKey(foo2));
                Assert.True(hash2.Contains(foo2));

                int i1 = 0x10;
                int i2 = 0x100;
                long l1 = (((long)i1) << 32) + i2; // Create two longs with same hashcode
                long l2 = (((long)i2) << 32) + i1;

                hash2.Add(l1, 101);
                hash2.Add(l2, 101);      // This will cause collision bit of the first entry to be set
                Assert.True(hash2.ContainsKey(l1));
                Assert.True(hash2.Contains(l1));

                hash2.Remove(l1);         // Remove the first item
                Assert.False(hash2.ContainsKey(l1));
                Assert.False(hash2.Contains(l1));

                Assert.True(hash2.ContainsKey(l2));
                Assert.True(hash2.Contains(l2));
            });
        }

        [Fact]
        public void ContainsKey_NullKey_ThrowsArgumentNullException()
        {
            var hash1 = new Hashtable();
            Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
            {
                AssertExtensions.Throws<ArgumentNullException>("key", () => hash2.ContainsKey(null)); // Key is null
                AssertExtensions.Throws<ArgumentNullException>("key", () => hash2.Contains(null)); // Key is null
            });
        }

        [Fact]
        public void ContainsValue()
        {
            Hashtable hash1 = Helpers.CreateStringHashtable(100);
            Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
            {
                for (int i = 0; i < hash2.Count; i++)
                {
                    string value = "Value_" + i;
                    Assert.True(hash2.ContainsValue(value));
                }

                Assert.False(hash2.ContainsValue("Non Existent Value"));
                Assert.False(hash2.ContainsValue(101));
                Assert.False(hash2.ContainsValue(null));

                hash2.Add("Key_101", null);
                Assert.True(hash2.ContainsValue(null));

                string removedKey = "Key_1";
                string removedValue = "Value_1";
                hash2.Remove(removedKey);
                Assert.False(hash2.ContainsValue(removedValue));
            });
        }

        [Fact]
        public void ContainsValue_EqualObjects()
        {
            var hash1 = new Hashtable();
            Helpers.PerformActionOnAllHashtableWrappers(hash1, hash2 =>
            {
                var foo1 = new Foo() { StringValue = "Goodbye" };
                var foo2 = new Foo() { StringValue = "Goodbye" };

                hash2.Add(101, foo1);

                Assert.True(hash2.ContainsValue(foo2));
            });
        }

        [Fact]
        public void Keys_ModifyingHashtable_ModifiesCollection()
        {
            Hashtable hash = Helpers.CreateStringHashtable(100);
            ICollection keys = hash.Keys;

            // Removing a key from the hashtable should update the Keys ICollection.
            // This means that the Keys ICollection no longer contains the key.
            hash.Remove("Key_0");

            IEnumerator enumerator = keys.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.NotEqual("Key_0", enumerator.Current);
            }
        }

        [Fact]
        public void Remove_SameHashcode()
        {
            // We want to add and delete items (with the same hashcode) to the hashtable in such a way that the hashtable
            // does not expand but have to tread through collision bit set positions to insert the new elements. We do this
            // by creating a default hashtable of size 11 (with the default load factor of 0.72), this should mean that
            // the hashtable does not expand as long as we have at most 7 elements at any given time?

            var hash = new Hashtable();
            var arrList = new ArrayList();
            for (int i = 0; i < 7; i++)
            {
                var hashConfuse = new BadHashCode(i);
                arrList.Add(hashConfuse);
                hash.Add(hashConfuse, i);
            }

            var rand = new Random(-55);

            int iCount = 7;
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    Assert.Equal(hash[arrList[j]], ((BadHashCode)arrList[j]).Value);
                }

                // Delete 3 elements from the hashtable
                for (int j = 0; j < 3; j++)
                {
                    int iElement = rand.Next(6);
                    hash.Remove(arrList[iElement]);
                    Assert.False(hash.ContainsValue(null));
                    arrList.RemoveAt(iElement);

                    int testInt = iCount++;
                    var hashConfuse = new BadHashCode(testInt);
                    arrList.Add(hashConfuse);
                    hash.Add(hashConfuse, testInt);
                }
            }
        }

        [Fact]
        public void SynchronizedProperties()
        {
            // Ensure Synchronized correctly reflects a wrapped hashtable
            var hash1 = Helpers.CreateStringHashtable(100);
            var hash2 = Hashtable.Synchronized(hash1);

            Assert.Equal(hash1.Count, hash2.Count);
            Assert.Equal(hash1.IsReadOnly, hash2.IsReadOnly);
            Assert.Equal(hash1.IsFixedSize, hash2.IsFixedSize);
            Assert.True(hash2.IsSynchronized);
            Assert.Equal(hash1.SyncRoot, hash2.SyncRoot);

            for (int i = 0; i < hash2.Count; i++)
            {
                Assert.Equal("Value_" + i, hash2["Key_" + i]);
            }
        }

        [Fact]
        public void Synchronized_NullTable_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("table", () => Hashtable.Synchronized(null)); // Table is null
        }

        [Fact]
        public void Values_ModifyingHashtable_ModifiesCollection()
        {
            Hashtable hash = Helpers.CreateStringHashtable(100);
            ICollection values = hash.Values;

            // Removing a value from the hashtable should update the Values ICollection.
            // This means that the Values ICollection no longer contains the value.
            hash.Remove("Key_0");

            IEnumerator enumerator = values.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.NotEqual("Value_0", enumerator.Current);
            }
        }

        [Fact]
        public void HashCodeProvider_Set_ImpactsSearch()
        {
            var hash = new ComparableHashtable(CaseInsensitiveHashCodeProvider.DefaultInvariant, StringComparer.OrdinalIgnoreCase);
            hash.Add("test", "test");

            // Should be able to find with the same and different casing
            Assert.True(hash.ContainsKey("test"));
            Assert.True(hash.ContainsKey("TEST"));

            // Changing the hash code provider, we shouldn't be able to find either
            hash.HashCodeProvider = new FixedHashCodeProvider
            {
                FixedHashCode = CaseInsensitiveHashCodeProvider.DefaultInvariant.GetHashCode("test") + 1
            };
            Assert.False(hash.ContainsKey("test"));
            Assert.False(hash.ContainsKey("TEST"));

            // Changing it back, should be able to find both again
            hash.HashCodeProvider = CaseInsensitiveHashCodeProvider.DefaultInvariant;
            Assert.True(hash.ContainsKey("test"));
            Assert.True(hash.ContainsKey("TEST"));
        }

        [Fact]
        public void HashCodeProvider_Comparer_CompatibleGetSet_Success()
        {
            var hash = new ComparableHashtable();
            Assert.Null(hash.HashCodeProvider);
            Assert.Null(hash.Comparer);

            hash = new ComparableHashtable();
            hash.HashCodeProvider = null;
            hash.Comparer = null;

            hash = new ComparableHashtable();
            hash.Comparer = null;
            hash.HashCodeProvider = null;

            hash.HashCodeProvider = CaseInsensitiveHashCodeProvider.DefaultInvariant;
            hash.Comparer = StringComparer.OrdinalIgnoreCase;
        }

        [Fact]
        public void HashCodeProvider_Comparer_IncompatibleGetSet_Throws()
        {
            var hash = new ComparableHashtable(StringComparer.CurrentCulture);

            AssertExtensions.Throws<ArgumentException>(null, () => hash.HashCodeProvider);
            AssertExtensions.Throws<ArgumentException>(null, () => hash.Comparer);

            AssertExtensions.Throws<ArgumentException>(null, () => hash.HashCodeProvider = CaseInsensitiveHashCodeProvider.DefaultInvariant);
            AssertExtensions.Throws<ArgumentException>(null, () => hash.Comparer = StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void Comparer_Set_ImpactsSearch()
        {
            var hash = new ComparableHashtable(CaseInsensitiveHashCodeProvider.DefaultInvariant, StringComparer.OrdinalIgnoreCase);
            hash.Add("test", "test");

            // Should be able to find with the same and different casing
            Assert.True(hash.ContainsKey("test"));
            Assert.True(hash.ContainsKey("TEST"));

            // Changing the comparer, should only be able to find the matching case
            hash.Comparer = StringComparer.Ordinal;
            Assert.True(hash.ContainsKey("test"));
            Assert.False(hash.ContainsKey("TEST"));

            // Changing it back, should be able to find both again
            hash.Comparer = StringComparer.OrdinalIgnoreCase;
            Assert.True(hash.ContainsKey("test"));
            Assert.True(hash.ContainsKey("TEST"));
        }

        private class FixedHashCodeProvider : IHashCodeProvider
        {
            public int FixedHashCode;
            public int GetHashCode(object obj) => FixedHashCode;
        }

        private static void VerifyHashtable(ComparableHashtable hash1, Hashtable hash2, IEqualityComparer ikc)
        {
            if (hash2 == null)
            {
                Assert.Equal(0, hash1.Count);
            }
            else
            {
                // Make sure that construtor imports all keys and values
                Assert.Equal(hash2.Count, hash1.Count);
                for (int i = 0; i < 100; i++)
                {
                    Assert.True(hash1.ContainsKey(i));
                    Assert.True(hash1.ContainsValue(i));
                }

                // Make sure the new and old hashtables are not linked
                hash2.Clear();
                for (int i = 0; i < 100; i++)
                {
                    Assert.True(hash1.ContainsKey(i));
                    Assert.True(hash1.ContainsValue(i));
                }
            }

            Assert.Equal(ikc, hash1.EqualityComparer);

            Assert.False(hash1.IsFixedSize);
            Assert.False(hash1.IsReadOnly);
            Assert.False(hash1.IsSynchronized);

            // Make sure we can add to the hashtable
            int count = hash1.Count;
            for (int i = count; i < count + 100; i++)
            {
                hash1.Add(i, i);
                Assert.True(hash1.ContainsKey(i));
                Assert.True(hash1.ContainsValue(i));
            }
        }

        private class ComparableHashtable : Hashtable
        {
            public ComparableHashtable() : base() { }

            public ComparableHashtable(int capacity) : base(capacity) { }

            public ComparableHashtable(int capacity, float loadFactor) : base(capacity, loadFactor) { }

            public ComparableHashtable(int capacity, IHashCodeProvider hcp, IComparer comparer) : base(capacity, hcp, comparer) { }

            public ComparableHashtable(int capacity, IEqualityComparer ikc) : base(capacity, ikc) { }

            public ComparableHashtable(IHashCodeProvider hcp, IComparer comparer) : base(hcp, comparer) { }

            public ComparableHashtable(IEqualityComparer ikc) : base(ikc) { }

            public ComparableHashtable(IDictionary d) : base(d) { }

            public ComparableHashtable(IDictionary d, float loadFactor) : base(d, loadFactor) { }

            public ComparableHashtable(IDictionary d, IHashCodeProvider hcp, IComparer comparer) : base(d, hcp, comparer) { }

            public ComparableHashtable(IDictionary d, IEqualityComparer ikc) : base(d, ikc) { }

            public ComparableHashtable(IDictionary d, float loadFactor, IHashCodeProvider hcp, IComparer comparer) : base(d, loadFactor, hcp, comparer) { }

            public ComparableHashtable(IDictionary d, float loadFactor, IEqualityComparer ikc) : base(d, loadFactor, ikc) { }

            public ComparableHashtable(int capacity, float loadFactor, IHashCodeProvider hcp, IComparer comparer) : base(capacity, loadFactor, hcp, comparer) { }

            public ComparableHashtable(int capacity, float loadFactor, IEqualityComparer ikc) : base(capacity, loadFactor, ikc) { }

            public new IEqualityComparer EqualityComparer => base.EqualityComparer;

            public IHashCodeProvider HashCodeProvider { get { return hcp; } set { hcp = value; } }

            public IComparer Comparer { get { return comparer; } set { comparer = value; } }
        }

        private class BadHashCode
        {
            public BadHashCode(int value)
            {
                Value = value;
            }

            public int Value { get; private set; }

            public override bool Equals(object o)
            {
                BadHashCode rhValue = o as BadHashCode;

                if (rhValue != null)
                {
                    return Value.Equals(rhValue.Value);
                }
                else
                {
                    throw new ArgumentException("is not BadHashCode type actual " + o.GetType(), nameof(o));
                }
            }

            // Return 0 for everything to force hash collisions.
            public override int GetHashCode() => 0;

            public override string ToString() => Value.ToString();
        }

        private class Foo
        {
            public string StringValue { get; set; } = "Hello World";

            public override bool Equals(object obj)
            {
                Foo foo = obj as Foo;
                return foo != null && StringValue == foo.StringValue;
            }

            public override int GetHashCode() => StringValue.GetHashCode();
        }
    }

    /// <summary>
    /// A hashtable can have a race condition:
    ///     A read operation on hashtable has three steps:
    ///        (1) calculate the hash and find the slot number.
    ///        (2) compare the hashcode, if equal, go to step 3. Otherwise end.
    ///        (3) compare the key, if equal, go to step 4. Otherwise end.
    ///        (4) return the value contained in the bucket.
    ///     The problem is that after step 3 and before step 4. A writer can kick in a remove the old item and add a new one 
    ///     in the same bucket. In order to make this happen easily, I created two long with same hashcode.
    /// </summary>
    public class Hashtable_ItemThreadSafetyTests
    {
        private object _key1;
        private object _key2;
        private object _value1 = "value1";
        private object _value2 = "value2";
        private Hashtable _hash;

        private bool _errorOccurred = false;
        private bool _timeExpired = false;

        private const int MAX_TEST_TIME_MS = 10000; // 10 seconds

        [Fact]
        [OuterLoop]
        public void GetItem_ThreadSafety()
        {
            int i1 = 0x10;
            int i2 = 0x100;

            // Setup key1 and key2 so they are different values but have the same hashcode
            // To produce a hashcode long XOR's the first 32bits with the last 32 bits
            long l1 = (((long)i1) << 32) + i2;
            long l2 = (((long)i2) << 32) + i1;
            _key1 = l1;
            _key2 = l2;

            _hash = new Hashtable(3); // Just one item will be in the hashtable at a time

            int taskCount = 3;
            var readers1 = new Task[taskCount];
            var readers2 = new Task[taskCount];

            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < readers1.Length; i++)
            {
                readers1[i] = Task.Run(new Action(ReaderFunction1));
            }

            for (int i = 0; i < readers2.Length; i++)
            {
                readers2[i] = Task.Run(new Action(ReaderFunction2));
            }

            Task writer = Task.Run(new Action(WriterFunction));

            var spin = new SpinWait();
            while (!_errorOccurred && !_timeExpired)
            {
                if (MAX_TEST_TIME_MS < stopwatch.ElapsedMilliseconds)
                {
                    _timeExpired = true;
                }

                spin.SpinOnce();
            }

            Task.WaitAll(readers1);
            Task.WaitAll(readers2);
            writer.Wait();

            Assert.False(_errorOccurred);
        }

        private void ReaderFunction1()
        {
            while (!_timeExpired)
            {
                object value = _hash[_key1];
                if (value != null)
                {
                    Assert.NotEqual(value, _value2);
                }
            }
        }

        private void ReaderFunction2()
        {
            while (!_errorOccurred && !_timeExpired)
            {
                object value = _hash[_key2];
                if (value != null)
                {
                    Assert.NotEqual(value, _value1);
                }
            }
        }

        private void WriterFunction()
        {
            while (!_errorOccurred && !_timeExpired)
            {
                _hash.Add(_key1, _value1);
                _hash.Remove(_key1);
                _hash.Add(_key2, _value2);
                _hash.Remove(_key2);
            }
        }
    }

    public class Hashtable_SynchronizedTests
    {
        private Hashtable _hash2;
        private int _iNumberOfElements = 20;

        [Fact]
        [OuterLoop]
        public void SynchronizedThreadSafety()
        {
            const int NumberOfWorkers = 3;

            // Synchronized returns a hashtable that is thread safe
            // We will try to test this by getting a number of threads to write some items
            // to a synchronized IList
            var hash1 = new Hashtable();
            _hash2 = Hashtable.Synchronized(hash1);

            var workers = new Task[NumberOfWorkers];
            for (int i = 0; i < workers.Length; i++)
            {
                var name = "Thread worker " + i;
                var task = new Action(() => AddElements(name));
                workers[i] = Task.Run(task);
            }

            Task.WaitAll(workers);

            // Check time
            Assert.Equal(_hash2.Count, _iNumberOfElements * NumberOfWorkers);

            for (int i = 0; i < NumberOfWorkers; i++)
            {
                for (int j = 0; j < _iNumberOfElements; j++)
                {
                    string strValue = "Thread worker " + i + "_" + j;
                    Assert.True(_hash2.Contains(strValue));
                }
            }

            // We cannot can make an assumption on the order of these items but
            // now we are going to remove all of these
            workers = new Task[NumberOfWorkers];
            for (int i = 0; i < workers.Length; i++)
            {
                string name = "Thread worker " + i;
                var task = new Action(() => RemoveElements(name));
                workers[i] = Task.Run(task);
            }

            Task.WaitAll(workers);

            Assert.Equal(_hash2.Count, 0);
        }

        private void AddElements(string strName)
        {
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                _hash2.Add(strName + "_" + i, "string_" + i);
            }
        }

        private void RemoveElements(string strName)
        {
            for (int i = 0; i < _iNumberOfElements; i++)
            {
                _hash2.Remove(strName + "_" + i);
            }
        }
    }

    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // Changed behavior
    public class Hashtable_SyncRootTests
    {
        private Hashtable _hashDaughter;
        private Hashtable _hashGrandDaughter;
        private const int NumberOfElements = 100;

        [Fact]
        public void SyncRoot()
        {
            // Different hashtables have different SyncRoots
            var hash1 = new Hashtable();
            var hash2 = new Hashtable();

            Assert.NotSame(hash1.SyncRoot, hash2.SyncRoot);
            Assert.Equal(hash1.SyncRoot.GetType(), typeof(Hashtable));

            // Cloned hashtables have different SyncRoots
            hash1 = new Hashtable();
            hash2 = Hashtable.Synchronized(hash1);
            Hashtable hash3 = (Hashtable)hash2.Clone();

            Assert.NotSame(hash2.SyncRoot, hash3.SyncRoot);
            Assert.NotSame(hash1.SyncRoot, hash3.SyncRoot);

            // Testing SyncRoot is not as simple as its implementation looks like. This is the working
            // scenario we have in mind.
            // 1) Create your Down to earth mother Hashtable
            // 2) Get a synchronized wrapper from it
            // 3) Get a Synchronized wrapper from 2)
            // 4) Get a synchronized wrapper of the mother from 1)
            // 5) all of these should SyncRoot to the mother earth

            var hashMother = new Hashtable();
            for (int i = 0; i < NumberOfElements; i++)
            {
                hashMother.Add("Key_" + i, "Value_" + i);
            }

            Hashtable hashSon = Hashtable.Synchronized(hashMother);
            _hashGrandDaughter = Hashtable.Synchronized(hashSon);
            _hashDaughter = Hashtable.Synchronized(hashMother);

            Assert.Same(hashSon.SyncRoot, hashMother.SyncRoot);
            Assert.Equal(hashSon.SyncRoot, hashMother.SyncRoot);
            Assert.Same(_hashGrandDaughter.SyncRoot, hashMother.SyncRoot);
            Assert.Same(_hashDaughter.SyncRoot, hashMother.SyncRoot);
            Assert.Same(hashSon.SyncRoot, hashMother.SyncRoot);

            // We are going to rumble with the Hashtables with some threads
            int iNumberOfWorkers = 30;
            var workers = new Task[iNumberOfWorkers];
            var ts2 = new Action(RemoveElements);
            for (int iThreads = 0; iThreads < iNumberOfWorkers; iThreads += 2)
            {
                var name = "Thread_worker_" + iThreads;
                var ts1 = new Action(() => AddMoreElements(name));

                workers[iThreads] = Task.Run(ts1);
                workers[iThreads + 1] = Task.Run(ts2);
            }

            Task.WaitAll(workers);

            // Check:
            // Either there should be some elements (the new ones we added and/or the original ones) or none
            var hshPossibleValues = new Hashtable();
            for (int i = 0; i < NumberOfElements; i++)
            {
                hshPossibleValues.Add("Key_" + i, "Value_" + i);
            }

            for (int i = 0; i < iNumberOfWorkers; i++)
            {
                hshPossibleValues.Add("Key_Thread_worker_" + i, "Thread_worker_" + i);
            }

            IDictionaryEnumerator idic = hashMother.GetEnumerator();

            while (idic.MoveNext())
            {
                Assert.True(hshPossibleValues.ContainsKey(idic.Key));
                Assert.True(hshPossibleValues.ContainsValue(idic.Value));
            }
        }

        private void AddMoreElements(string threadName)
        {
            _hashGrandDaughter.Add("Key_" + threadName, threadName);
        }

        private void RemoveElements()
        {
            _hashDaughter.Clear();
        }
    }
}
