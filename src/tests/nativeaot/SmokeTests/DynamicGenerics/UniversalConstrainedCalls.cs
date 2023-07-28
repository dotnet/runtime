// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CoreFXTestLibrary;
using TypeOfRepo;
using System.Runtime.CompilerServices;

namespace UnivConstCalls
{
    public interface IConstrainedCallInterface<T>
    {
        T Method();
        string GenMethod<U>();
    }

    public class ConstrainedCallBaseType
    {
        public virtual string VirtualMethod()
        {
            return "NonOverridden";
        }

        public virtual string GenVirtualMethod<T>()
        {
            return "NonOverriddenGeneric";
        }
    }

    public class ReferenceConstrainedCallTypeNonGenInterface : IDisposable
    {
        string _callRes = null;
        public void Dispose() { _callRes = "ReferenceConstrainedCallTypeNonGenInterface.Dispose"; }
        public override string ToString() { return _callRes; }
    }
    public class GenReferenceConstrainedCallTypeNonGenInterface<T> : IDisposable
    {
        string _callRes = null;
        public void Dispose() { _callRes = "GenReferenceConstrainedCallTypeNonGenInterface<" + typeof(T) + ">.Dispose"; }
        public override string ToString() { return _callRes; }
    }
    public struct StructConstrainedCallTypeNonGenInterface : IDisposable
    {
        string _callRes;
        public void Dispose() { _callRes = "StructConstrainedCallTypeNonGenInterface.Dispose"; }
        public override string ToString() { return _callRes; }
    }
    public struct GenStructonstrainedCallTypeNonGenInterface<T> : IDisposable
    {
        string _callRes;
        public void Dispose() { _callRes = "GenStructonstrainedCallTypeNonGenInterface<" + typeof(T) + ">.Dispose"; }
        public override string ToString() { return _callRes; }
    }

    public class ReferenceConstrainedCallType : ConstrainedCallBaseType, IConstrainedCallInterface<string>
    {
        public string MethodVal;
        public string GenMethodVal;
        public string VirtualMethodVal;
        public string GenVirtualMethodVal;


        public string Method()
        {
            return MethodVal;
        }

        public string GenMethod<T>()
        {
            return GenMethodVal + typeof(T).ToString();
        }

        public override string VirtualMethod()
        {
            return VirtualMethodVal;
        }

        public override string GenVirtualMethod<T>()
        {
            return GenVirtualMethodVal + typeof(T).ToString();
        }
    }

    public struct NonGenericStructThatImplementsInterface : IConstrainedCallInterface<string>
    {
        public int valueToReturn;

        public string Method()
        {
            return valueToReturn.ToString();
        }
        public string GenMethod<T>()
        {
            return valueToReturn.ToString() + typeof(T).ToString();
        }
    }

    interface ISetValueToReturn
    {
        void SetValue(object o);
    }
    public struct GenericStructThatImplementsInterface<T> : IConstrainedCallInterface<T>, ISetValueToReturn
    {
        public T valueToReturn;

        public void SetValue(object o)
        {
            valueToReturn = (T)o;
        }

        public T Method()
        {
            return valueToReturn;
        }
        public string GenMethod<U>()
        {
            return valueToReturn.ToString() + typeof(U).ToString();
        }


        public override bool Equals(object obj)
        {
            ObjectFuncCallDetector.EqualsCalled = true;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            ObjectFuncCallDetector.GetHashCodeCalled = true;
            return base.GetHashCode();
        }

        public override string ToString()
        {
            ObjectFuncCallDetector.ToStringCalled = true;
            return base.ToString();
        }
    }

    public class ObjectFuncCallDetector
    {
        public static bool GetHashCodeCalled;
        public static bool EqualsCalled;
        public static bool ToStringCalled;
    }

    public struct NonGenericStructThatImplementsInterfaceAndOverridesObjectFuncs : IConstrainedCallInterface<string>
    {
        public int valueToReturn;

        public string Method()
        {
            return valueToReturn.ToString();
        }
        public string GenMethod<U>()
        {
            return valueToReturn.ToString() + typeof(U).ToString();
        }

        public override bool Equals(object obj)
        {
            ObjectFuncCallDetector.EqualsCalled = true;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            ObjectFuncCallDetector.GetHashCodeCalled = true;
            return base.GetHashCode();
        }

        public override string ToString()
        {
            ObjectFuncCallDetector.ToStringCalled = true;
            return base.ToString();
        }
    }


    public abstract class TestConstrainedCallBase
    {
        public abstract void StoreTargetObject(object onTarget);
        public abstract string MakeConstrainedCall();
        public abstract string MakeGenericConstrainedCall();

        public abstract string MakeToStringCall();
        public abstract int MakeGetHashCodeCall();
        public abstract bool MakeEqualsCall(object comparand);
    }

    public class UCGConstrainedCall<T, U, V> : TestConstrainedCallBase where T : IConstrainedCallInterface<V>
    {
        T storedValue;

        public override void StoreTargetObject(object onTarget)
        {
            storedValue = (T)onTarget;
        }

        public override string MakeConstrainedCall()
        {
            return storedValue.Method().ToString();
        }
        public override string MakeGenericConstrainedCall()
        {
            return storedValue.GenMethod<V>().ToString();
        }

        public override string MakeToStringCall()
        {
            return storedValue.ToString();
        }
        public override int MakeGetHashCodeCall()
        {
            return storedValue.GetHashCode();
        }

        public override bool MakeEqualsCall(object comparand)
        {
            return storedValue.Equals(comparand);
        }
    }


    public abstract class TestReferenceConstrainedCallBase
    {
        public abstract void StoreTargetObject(object onTarget);
        public abstract string MakeConstrainedCallInterfaceNonGeneric();
        public abstract string MakeConstrainedCallInterfaceGeneric<T>();
        public abstract string MakeConstrainedCallVirtualNonGeneric();
        public abstract string MakeConstrainedCallVirtualGeneric<T>();
    }

    public class UCGReferenceConstrainedCall<T, U> : TestReferenceConstrainedCallBase where T : ConstrainedCallBaseType, IConstrainedCallInterface<string>
    {
        T storedValue;

        public override void StoreTargetObject(object onTarget)
        {
            storedValue = (T)onTarget;
        }

        public override string MakeConstrainedCallInterfaceNonGeneric()
        {
            return storedValue.Method();
        }

        public override string MakeConstrainedCallInterfaceGeneric<Z>()
        {
            return storedValue.GenMethod<Z>();
        }

        public override string MakeConstrainedCallVirtualGeneric<Z>()
        {
            return storedValue.GenVirtualMethod<Z>();
        }

        public override string MakeConstrainedCallVirtualNonGeneric()
        {
            return storedValue.VirtualMethod();
        }
    }

    public class UCGNonGenInterfaceConstrainedCall<T, U> : TestReferenceConstrainedCallBase where T : IDisposable
    {
        T storedValue;

        public override void StoreTargetObject(object onTarget)
        {
            storedValue = (T)onTarget;
        }

        public override string MakeConstrainedCallInterfaceNonGeneric()
        {
            storedValue.Dispose();
            return storedValue.ToString();
        }

        public override string MakeConstrainedCallInterfaceGeneric<Z>() { return null; }
        public override string MakeConstrainedCallVirtualNonGeneric() { return null; }
        public override string MakeConstrainedCallVirtualGeneric<Z>() { return null; }
    }

    public static class Test
    {
        public static void ReferenceTypeCallsTestWorker(TestReferenceConstrainedCallBase o, string[] ExpectedResults)
        {
            Assert.AreEqual(ExpectedResults[0], o.MakeConstrainedCallInterfaceNonGeneric());
            Assert.AreEqual(ExpectedResults[1], o.MakeConstrainedCallInterfaceGeneric<short>());
            Assert.AreEqual(ExpectedResults[2], o.MakeConstrainedCallVirtualNonGeneric());
            Assert.AreEqual(ExpectedResults[3], o.MakeConstrainedCallVirtualGeneric<int>());
        }

        [TestMethod]
        public static void TestRefTypeCallsOnNonGenClass()
        {
            var instantiatedType = TypeOf.UCC_UCGReferenceConstrainedCall.MakeGenericType(TypeOf.UCC_ReferenceConstrainedCallType, TypeOf.Int16);
            var o = (TestReferenceConstrainedCallBase)Activator.CreateInstance(instantiatedType);

            ReferenceConstrainedCallType rcct = new ReferenceConstrainedCallType();
            rcct.MethodVal = "NonGenericInterfaceMethod";
            rcct.GenMethodVal = "GenericInterfaceMethod";
            rcct.VirtualMethodVal = "NonGenericVirtualMethod";
            rcct.GenVirtualMethodVal = "GenericVirtualMethod";
            o.StoreTargetObject(rcct);
            string []expectedResults = new string[]
            {
                rcct.MethodVal,
                rcct.GenMethodVal+TypeOf.Int16.ToString(),
                rcct.VirtualMethodVal,
                rcct.GenVirtualMethodVal+TypeOf.Int32.ToString(),
            };

            ReferenceTypeCallsTestWorker(o, expectedResults);
        }

        [TestMethod]
        public static void TestUSCCallsOnNonGenStruct()
        {
            var t = TypeOf.UCC_UCGConstrainedCall.MakeGenericType(TypeOf.UCC_NonGenericStructThatImplementsInterface, TypeOf.Int16, TypeOf.String);
            var o = (TestConstrainedCallBase)Activator.CreateInstance(t);

            {
                NonGenericStructThatImplementsInterface testStruct = new NonGenericStructThatImplementsInterface();
                testStruct.valueToReturn = 42;
                o.StoreTargetObject(testStruct);
                Assert.AreEqual("42", o.MakeConstrainedCall());
                Assert.AreEqual("42" + TypeOf.String, o.MakeGenericConstrainedCall());

                // Test Non-overridden object methods
                Assert.AreEqual(testStruct.ToString(), o.MakeToStringCall());
                Assert.AreEqual(testStruct.GetHashCode(), o.MakeGetHashCodeCall());
                Assert.AreEqual(false, o.MakeEqualsCall(16));
                Assert.AreEqual(true, o.MakeEqualsCall(testStruct));
            }

            t = TypeOf.UCC_UCGConstrainedCall.MakeGenericType(TypeOf.UCC_NonGenericStructThatImplementsInterfaceAndOverridesObjectFuncs, TypeOf.Int16, TypeOf.String);
            o = (TestConstrainedCallBase)Activator.CreateInstance(t);

            NonGenericStructThatImplementsInterfaceAndOverridesObjectFuncs testStruct2 = new NonGenericStructThatImplementsInterfaceAndOverridesObjectFuncs();
            testStruct2.valueToReturn = 40;
            o.StoreTargetObject(testStruct2);
            Assert.AreEqual("40", o.MakeConstrainedCall());
            Assert.AreEqual("40" + TypeOf.String, o.MakeGenericConstrainedCall());

            // Test Overridden object methods
            string toStringResult = testStruct2.ToString();
            int getHashCodeResult = testStruct2.GetHashCode();

            ObjectFuncCallDetector.EqualsCalled = false;
            ObjectFuncCallDetector.ToStringCalled = false;
            ObjectFuncCallDetector.GetHashCodeCalled = false;

            Assert.AreEqual(toStringResult, o.MakeToStringCall());
            Assert.IsTrue(ObjectFuncCallDetector.ToStringCalled);
            Assert.AreEqual(getHashCodeResult, o.MakeGetHashCodeCall());
            Assert.IsTrue(ObjectFuncCallDetector.GetHashCodeCalled);
            Assert.AreEqual(false, o.MakeEqualsCall(16));
            Assert.IsTrue(ObjectFuncCallDetector.EqualsCalled);
            ObjectFuncCallDetector.EqualsCalled = false;
            Assert.AreEqual(true, o.MakeEqualsCall(testStruct2));
            Assert.IsTrue(ObjectFuncCallDetector.EqualsCalled);
        }

        [TestMethod]
        public static void TestUSCCallsOnSharedGenStruct()
        {
            // Use an explicit typeof here for GenericStructThatImplementsInterface<string> so that
            // that case uses the normal shared generic path, and not anything else.
            var t = TypeOf.UCC_UCGConstrainedCall.MakeGenericType(typeof(GenericStructThatImplementsInterface<string>), TypeOf.Int16, TypeOf.String);
            var o = (TestConstrainedCallBase)Activator.CreateInstance(t);

            {
                GenericStructThatImplementsInterface<string> testStruct = new GenericStructThatImplementsInterface<string>();
                testStruct.valueToReturn = "74";
                o.StoreTargetObject(testStruct);
                Assert.AreEqual("74", o.MakeConstrainedCall());
                Assert.AreEqual("74" + TypeOf.String, o.MakeGenericConstrainedCall());

                // Test Overridden object methods
                string toStringResult = testStruct.ToString();
                int getHashCodeResult = testStruct.GetHashCode();

                ObjectFuncCallDetector.EqualsCalled = false;
                ObjectFuncCallDetector.ToStringCalled = false;
                ObjectFuncCallDetector.GetHashCodeCalled = false;

                Assert.AreEqual(toStringResult, o.MakeToStringCall());
                Assert.IsTrue(ObjectFuncCallDetector.ToStringCalled);
                Assert.AreEqual(getHashCodeResult, o.MakeGetHashCodeCall());
                Assert.IsTrue(ObjectFuncCallDetector.GetHashCodeCalled);
                Assert.AreEqual(false, o.MakeEqualsCall(16));
                Assert.IsTrue(ObjectFuncCallDetector.EqualsCalled);
                ObjectFuncCallDetector.EqualsCalled = false;
                Assert.AreEqual(true, o.MakeEqualsCall(testStruct));
                Assert.IsTrue(ObjectFuncCallDetector.EqualsCalled);
            }
        }

        [TestMethod]
        public static void TestUSCCallsOnUSCGenStruct()
        {
            var tUniversalGenericInnerStruct = TypeOf.UCC_GenericStructThatImplementsInterface.MakeGenericType(TypeOf.Int16);

            var t = TypeOf.UCC_UCGConstrainedCall.MakeGenericType(tUniversalGenericInnerStruct, TypeOf.Int16, TypeOf.Int16);
            var o = (TestConstrainedCallBase)Activator.CreateInstance(t);

            {
                ISetValueToReturn testStruct = (ISetValueToReturn)Activator.CreateInstance(tUniversalGenericInnerStruct);
                testStruct.SetValue((short)162);
                o.StoreTargetObject(testStruct);
                Assert.AreEqual("162", o.MakeConstrainedCall());
                Assert.AreEqual("162" + TypeOf.Int16, o.MakeGenericConstrainedCall());

                // Test Overridden object methods
                string toStringResult = testStruct.ToString();
                int getHashCodeResult = testStruct.GetHashCode();

                ObjectFuncCallDetector.EqualsCalled = false;
                ObjectFuncCallDetector.ToStringCalled = false;
                ObjectFuncCallDetector.GetHashCodeCalled = false;

                Assert.AreEqual(toStringResult, o.MakeToStringCall());
                Assert.IsTrue(ObjectFuncCallDetector.ToStringCalled);
                Assert.AreEqual(getHashCodeResult, o.MakeGetHashCodeCall());
                Assert.IsTrue(ObjectFuncCallDetector.GetHashCodeCalled);
                Assert.AreEqual(false, o.MakeEqualsCall(16));
                Assert.IsTrue(ObjectFuncCallDetector.EqualsCalled);
                ObjectFuncCallDetector.EqualsCalled = false;
                Assert.AreEqual(true, o.MakeEqualsCall(testStruct));
                Assert.IsTrue(ObjectFuncCallDetector.EqualsCalled);
            }
        }

        [TestMethod]
        public static void TestUSCNonGenInterfaceCallsOnStructs()
        {
            var typeArgs = new Type[]
            {
                typeof(ReferenceConstrainedCallTypeNonGenInterface),
                typeof(GenReferenceConstrainedCallTypeNonGenInterface<>).MakeGenericType(TypeOf.Double),
                typeof(StructConstrainedCallTypeNonGenInterface),
                typeof(GenStructonstrainedCallTypeNonGenInterface<>).MakeGenericType(TypeOf.Long),
            };

            var expectedResults = new string[]
            {
                "ReferenceConstrainedCallTypeNonGenInterface.Dispose",
                "GenReferenceConstrainedCallTypeNonGenInterface<System.Double>.Dispose",
                "StructConstrainedCallTypeNonGenInterface.Dispose",
                "GenStructonstrainedCallTypeNonGenInterface<System.Int64>.Dispose",
            };

            for (int i = 0; i < typeArgs.Length; i++)
            {
                var typeArg = typeArgs[i];
                var t = typeof(UCGNonGenInterfaceConstrainedCall<,>).MakeGenericType(typeArg, TypeOf.Short);
                var o = (TestReferenceConstrainedCallBase)Activator.CreateInstance(t);

                o.StoreTargetObject(Activator.CreateInstance(typeArg));
                string methodResult = o.MakeConstrainedCallInterfaceNonGeneric();
                Assert.AreEqual(expectedResults[i], methodResult);
            }
        }
    }
}
