// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using TestLibrary;

namespace IDynamicInterfaceCastableTests
{
    public enum ImplementationToCall
    {
        Class,
        Interface,
        InterfacePrivate,
        InterfaceStatic,
        ImplInterfacePublic,
    }

    public interface ITest
    {
        Type GetMyType();
        ITest ReturnThis();
        int GetNumber();
        int CallImplemented(ImplementationToCall toCall);
    }

    public interface ITestGeneric<T>
    {
        T ReturnArg(T t);
    }

    public interface IDirectlyImplemented
    {
        int ImplementedMethod();
    }

    public interface INotImplemented { }

    [DynamicInterfaceCastableImplementation]
    public interface ITestImpl : ITest
    {
        ITest ITest.ReturnThis()
        {
            return this;
        }

        Type ITest.GetMyType()
        {
            return GetType();
        }

        public static int GetNumberReturnValue = 1;
        int ITest.GetNumber()
        {
            return GetNumberReturnValue;
        }

        public static int GetNumberPrivateReturnValue = 2;
        private int GetNumberPrivate()
        {
            return GetNumberPrivateReturnValue;
        }

        public static int GetNumberStaticReturnValue = 3;
        public static int GetNumberStatic()
        {
            return GetNumberStaticReturnValue;
        }

        public int GetNumberHelper()
        {
            Assert.Fail("Calling a public interface method with a default implementation should go through IDynamicInterfaceCastable for interface dispatch.");
            return 0;
        }

        int ITest.CallImplemented(ImplementationToCall toCall)
        {
            switch (toCall)
            {
                case ImplementationToCall.Class:
                    DynamicInterfaceCastable impl = (DynamicInterfaceCastable)this;
                    return impl.ImplementedMethod();
                case ImplementationToCall.Interface:
                    return GetNumber();
                case ImplementationToCall.InterfacePrivate:
                    return GetNumberPrivate();
                case ImplementationToCall.InterfaceStatic:
                    return GetNumberStatic();
                case ImplementationToCall.ImplInterfacePublic:
                    return GetNumberHelper();
            }

            return 0;
        }
    }

    [DynamicInterfaceCastableImplementation]
    public interface ITestReabstracted : ITest
    {
        abstract Type ITest.GetMyType();
        abstract ITest ITest.ReturnThis();
        abstract int ITest.GetNumber();
        abstract int ITest.CallImplemented(ImplementationToCall toCall);
    }

    [DynamicInterfaceCastableImplementation]
    public interface IDiamondTest : ITestImpl, ITestReabstracted { }

    public interface IOverrideTest : ITestImpl { }

    [DynamicInterfaceCastableImplementation]
    public interface IOverrideTestImpl : IOverrideTest
    {
        public static int GetNumberReturnValue_Override = 10;
        int ITest.GetNumber()
        {
            return GetNumberReturnValue_Override;
        }

        public static Type GetMyTypeReturnValue = typeof(int);
        Type ITest.GetMyType()
        {
            return GetMyTypeReturnValue;
        }
    }

    [DynamicInterfaceCastableImplementation]
    public interface ITestGenericImpl<T>: ITestGeneric<T>
    {
        T ITestGeneric<T>.ReturnArg(T t)
        {
            return t;
        }
    }

    [DynamicInterfaceCastableImplementation]
    public interface ITestGenericIntImpl: ITestGeneric<int>
    {
        int ITestGeneric<int>.ReturnArg(int i)
        {
            return i;
        }
    }

    [DynamicInterfaceCastableImplementation]
    public interface IDirectlyImplementedImpl : IDirectlyImplemented
    {
        int IDirectlyImplemented.ImplementedMethod()
        {
            return 0;
        }
    }

    public interface IOther
    {
        int OtherMethod();
    }

    public class DynamicInterfaceCastableException : Exception
    {
        public static string ErrorFormat = "REQUESTED={0}";
        public DynamicInterfaceCastableException(RuntimeTypeHandle interfaceType)
            : base(string.Format(ErrorFormat, Type.GetTypeFromHandle(interfaceType)))
        { }
    }

    public class DynamicInterfaceCastable : IDynamicInterfaceCastable, IDirectlyImplemented
    {
        private Dictionary<Type, Type> interfaceToImplMap;

        public DynamicInterfaceCastable(Dictionary<Type, Type> interfaceToImplMap)
        {
            this.interfaceToImplMap = interfaceToImplMap;
        }

        public bool IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
        {
            if (interfaceToImplMap != null && interfaceToImplMap.ContainsKey(Type.GetTypeFromHandle(interfaceType)))
                return true;

            if (throwIfNotImplemented)
                throw new DynamicInterfaceCastableException(interfaceType);

            return false;
        }

        public RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
        {
            Type implMaybe;
            if (interfaceToImplMap != null && interfaceToImplMap.TryGetValue(Type.GetTypeFromHandle(interfaceType), out implMaybe))
                return implMaybe.TypeHandle;

            return default(RuntimeTypeHandle);
        }

        public static int ImplementedMethodReturnValue = -1;
        public int ImplementedMethod()
        {
            return ImplementedMethodReturnValue;
        }
    }

    public class BadDynamicInterfaceCastable : IDynamicInterfaceCastable
    {
        public enum InvalidReturn
        {
            DefaultHandle,
            Class,
            NoAttribute,
            NotImplemented,
            NoDefaultImplementation,
            CallNotImplemented,
            UseOtherInterface,
            ThrowException,
            DiamondImplementation,
            ReabstractedImplementation,
        }

        public InvalidReturn InvalidImplementation { get; set; }

        public bool IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
        {
            if (InvalidImplementation == InvalidReturn.ThrowException)
                throw new DynamicInterfaceCastableException(interfaceType);

            return interfaceType.Equals(typeof(ITest).TypeHandle);
        }

        public RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
        {
            if (!interfaceType.Equals(typeof(ITest).TypeHandle))
                return default(RuntimeTypeHandle);

            switch (InvalidImplementation)
            {
                case InvalidReturn.Class:
                    return typeof(TestImpl).TypeHandle;
                case InvalidReturn.NoAttribute:
                    return typeof(INoAttributeImpl).TypeHandle;
                case InvalidReturn.NotImplemented:
                    return typeof(INotTestImpl).TypeHandle;
                case InvalidReturn.NoDefaultImplementation:
                    return typeof(ITestNoDefaultImpl).TypeHandle;
                case InvalidReturn.CallNotImplemented:
                    return typeof(ITestPartialImpl).TypeHandle;
                case InvalidReturn.UseOtherInterface:
                    return typeof(ITestOtherImpl).TypeHandle;
                case InvalidReturn.ThrowException:
                    throw new DynamicInterfaceCastableException(interfaceType);
                case InvalidReturn.ReabstractedImplementation:
                    return typeof(ITestReabstracted).TypeHandle;
                case InvalidReturn.DiamondImplementation:
                    return typeof(IDiamondTest).TypeHandle;
                case InvalidReturn.DefaultHandle:
                default:
                    return default(RuntimeTypeHandle);
            }
        }

        public static int UseOther(IOther other) { return other.OtherMethod(); }

        private class TestImpl : ITestImpl { }

        private interface INoAttributeImpl : ITestImpl { }

        [DynamicInterfaceCastableImplementation]
        private interface INotTestImpl { }

        [DynamicInterfaceCastableImplementation]
        private interface ITestNoDefaultImpl : ITest { }

        [DynamicInterfaceCastableImplementation]
        private interface ITestPartialImpl : ITest
        {
            ITest ITest.ReturnThis()
            {
                // Call method without default implementation
                CallImplemented(ImplementationToCall.Class);
                return this;
            }
        }

        [DynamicInterfaceCastableImplementation]
        private interface ITestOtherImpl : ITestImpl, IOther
        {
            int ITest.GetNumber()
            {
                return BadDynamicInterfaceCastable.UseOther(this);
            }
        }
    }

    public class Program
    {
        private static void ValidateBasicInterface()
        {
            Console.WriteLine($"Running {nameof(ValidateBasicInterface)}");

            object castableObj = new DynamicInterfaceCastable(new Dictionary<Type, Type> {
                { typeof(ITest), typeof(ITestImpl) }
            });

            Console.WriteLine(" -- Validate cast");

            // ITest -> ITestImpl
            Assert.IsTrue(castableObj is ITest, $"Should be castable to {nameof(ITest)} via is");
            Assert.IsNotNull(castableObj as ITest, $"Should be castable to {nameof(ITest)} via as");
            var testObj = (ITest)castableObj;

            Console.WriteLine(" -- Validate method call");
            Assert.AreSame(castableObj, testObj.ReturnThis(), $"{nameof(ITest.ReturnThis)} should return actual object");
            Assert.AreEqual(typeof(DynamicInterfaceCastable), testObj.GetMyType(), $"{nameof(ITest.GetMyType)} should return typeof(DynamicInterfaceCastable)");

            Console.WriteLine(" -- Validate method call which calls methods using 'this'");
            Assert.AreEqual(DynamicInterfaceCastable.ImplementedMethodReturnValue, testObj.CallImplemented(ImplementationToCall.Class));
            Assert.AreEqual(ITestImpl.GetNumberReturnValue, testObj.CallImplemented(ImplementationToCall.Interface));
            Assert.AreEqual(ITestImpl.GetNumberPrivateReturnValue, testObj.CallImplemented(ImplementationToCall.InterfacePrivate));
            Assert.AreEqual(ITestImpl.GetNumberStaticReturnValue, testObj.CallImplemented(ImplementationToCall.InterfaceStatic));
            Assert.Throws<InvalidCastException>(() => testObj.CallImplemented(ImplementationToCall.ImplInterfacePublic));

            Console.WriteLine(" -- Validate delegate call");
            Func<ITest> func = new Func<ITest>(testObj.ReturnThis);
            Assert.AreSame(castableObj, func(), $"Delegate call to {nameof(ITest.ReturnThis)} should return this");
        }

        private static void ValidateGenericInterface()
        {
            Console.WriteLine($"Running {nameof(ValidateGenericInterface)}");

            object castableObj = new DynamicInterfaceCastable(new Dictionary<Type, Type> {
                { typeof(ITestGeneric<int>), typeof(ITestGenericIntImpl) },
                { typeof(ITestGeneric<string>), typeof(ITestGenericImpl<string>) },
            });

            Console.WriteLine(" -- Validate cast");

            // ITestGeneric<int> -> ITestGenericIntImpl
            Assert.IsTrue(castableObj is ITestGeneric<int>, $"Should be castable to {nameof(ITestGeneric<int>)} via is");
            Assert.IsNotNull(castableObj as ITestGeneric<int>, $"Should be castable to {nameof(ITestGeneric<int>)} via as");
            ITestGeneric<int> testInt = (ITestGeneric<int>)castableObj;

            // ITestGeneric<string> -> ITestGenericImpl<string>
            Assert.IsTrue(castableObj is ITestGeneric<string>, $"Should be castable to {nameof(ITestGeneric<string>)} via is");
            Assert.IsNotNull(castableObj as ITestGeneric<string>, $"Should be castable to {nameof(ITestGeneric<string>)} via as");
            ITestGeneric<string> testStr = (ITestGeneric<string>)castableObj;

            // ITestGeneric<bool> is not recognized
            Assert.IsFalse(castableObj is ITestGeneric<bool>, $"Should not be castable to {nameof(ITestGeneric<bool>)} via is");
            Assert.IsNull(castableObj as ITestGeneric<bool>, $"Should not be castable to {nameof(ITestGeneric<bool>)} via as");
            var ex = Assert.Throws<DynamicInterfaceCastableException>(() => { var _ = (ITestGeneric<bool>)castableObj; });
            Assert.AreEqual(string.Format(DynamicInterfaceCastableException.ErrorFormat, typeof(ITestGeneric<bool>)), ex.Message);

            int expectedInt = 42;
            string expectedStr = "str";

            Console.WriteLine(" -- Validate method call");
            Assert.AreEqual(expectedInt, testInt.ReturnArg(42));
            Assert.AreEqual(expectedStr, testStr.ReturnArg(expectedStr));

            Console.WriteLine(" -- Validate delegate call");
            Func<int, int> funcInt = new Func<int, int>(testInt.ReturnArg);
            Assert.AreEqual(expectedInt, funcInt(expectedInt));
            Func<string, string> funcStr = new Func<string, string>(testStr.ReturnArg);
            Assert.AreEqual(expectedStr, funcStr(expectedStr));
        }

        private static void ValidateOverriddenInterface()
        {
            Console.WriteLine($"Running {nameof(ValidateOverriddenInterface)}");

            object castableObj = new DynamicInterfaceCastable(new Dictionary<Type, Type> {
                { typeof(ITest), typeof(IOverrideTestImpl) },
                { typeof(IOverrideTest), typeof(IOverrideTestImpl) },
            });

            Console.WriteLine(" -- Validate cast");

            // IOverrideTest -> IOverrideTestImpl
            Assert.IsTrue(castableObj is IOverrideTest, $"Should be castable to {nameof(IOverrideTest)} via is");
            Assert.IsNotNull(castableObj as IOverrideTest, $"Should be castable to {nameof(IOverrideTest)} via as");
            var testObj = (IOverrideTest)castableObj;

            Console.WriteLine(" -- Validate method call");
            Assert.AreSame(castableObj, testObj.ReturnThis(), $"{nameof(IOverrideTest.ReturnThis)} should return actual object");
            Assert.AreEqual(IOverrideTestImpl.GetMyTypeReturnValue, testObj.GetMyType(), $"{nameof(IOverrideTest.GetMyType)} should return {IOverrideTestImpl.GetMyTypeReturnValue}");

            Console.WriteLine(" -- Validate method call which calls methods using 'this'");
            Assert.AreEqual(DynamicInterfaceCastable.ImplementedMethodReturnValue, testObj.CallImplemented(ImplementationToCall.Class));
            Assert.AreEqual(IOverrideTestImpl.GetNumberReturnValue_Override, testObj.CallImplemented(ImplementationToCall.Interface));
            Assert.AreEqual(ITestImpl.GetNumberPrivateReturnValue, testObj.CallImplemented(ImplementationToCall.InterfacePrivate));
            Assert.AreEqual(ITestImpl.GetNumberStaticReturnValue, testObj.CallImplemented(ImplementationToCall.InterfaceStatic));

            Console.WriteLine(" -- Validate delegate call");
            Func<ITest> func = new Func<ITest>(testObj.ReturnThis);
            Assert.AreSame(castableObj, func(), $"Delegate call to {nameof(IOverrideTest.ReturnThis)} should return this");
            Func<Type> funcGetType = new Func<Type>(testObj.GetMyType);
            Assert.AreEqual(IOverrideTestImpl.GetMyTypeReturnValue, funcGetType(), $"Delegate call to {nameof(IOverrideTest.GetMyType)} should return {IOverrideTestImpl.GetMyTypeReturnValue}");
        }

        private static void ValidateNotImplemented()
        {
            Console.WriteLine($"Running {nameof(ValidateNotImplemented)}");

            object castableObj = new DynamicInterfaceCastable(new Dictionary<Type, Type> {
                { typeof(ITest), typeof(ITestImpl) }
            });

            Assert.IsFalse(castableObj is INotImplemented, $"Should not be castable to {nameof(INotImplemented)} via is");
            Assert.IsNull(castableObj as INotImplemented, $"Should not be castable to {nameof(INotImplemented)} via as");
            var ex = Assert.Throws<DynamicInterfaceCastableException>(() => { var _ = (INotImplemented)castableObj; });
            Assert.AreEqual(string.Format(DynamicInterfaceCastableException.ErrorFormat, typeof(INotImplemented)), ex.Message);
        }

        private static void ValidateDirectlyImplemented()
        {
            Console.WriteLine($"Running {nameof(ValidateDirectlyImplemented)}");

            object castableObj = new DynamicInterfaceCastable(new Dictionary<Type, Type> {
                { typeof(ITest), typeof(ITestImpl) },
                { typeof(IDirectlyImplemented), typeof(IDirectlyImplementedImpl) },
            });

            Console.WriteLine(" -- Validate cast");
            Assert.IsTrue(castableObj is IDirectlyImplemented, $"Should be castable to {nameof(IDirectlyImplemented)} via is");
            Assert.IsNotNull(castableObj as IDirectlyImplemented, $"Should be castable to {nameof(IDirectlyImplemented)} via as");
            var direct = (IDirectlyImplemented)castableObj;

            Console.WriteLine(" -- Validate method call");
            Assert.AreEqual(DynamicInterfaceCastable.ImplementedMethodReturnValue, direct.ImplementedMethod());

            Console.WriteLine(" -- Validate delegate call");
            Func<int> func = new Func<int>(direct.ImplementedMethod);
            Assert.AreEqual(DynamicInterfaceCastable.ImplementedMethodReturnValue, func());
        }

        private static void ValidateErrorHandling()
        {
            Console.WriteLine($"Running {nameof(ValidateErrorHandling)}");

            var castableObj = new BadDynamicInterfaceCastable();
            var testObj = (ITest)castableObj;
            Exception ex;

            Console.WriteLine(" -- Validate non-interface");
            castableObj.InvalidImplementation = BadDynamicInterfaceCastable.InvalidReturn.Class;
            ex = Assert.Throws<InvalidOperationException>(() => testObj.GetMyType());
            Console.WriteLine($" ---- {ex.GetType().Name}: {ex.Message}");

            Console.WriteLine(" -- Validate missing attribute");
            castableObj.InvalidImplementation = BadDynamicInterfaceCastable.InvalidReturn.NoAttribute;
            ex = Assert.Throws<InvalidOperationException>(() => testObj.GetMyType());
            Console.WriteLine($" ---- {ex.GetType().Name}: {ex.Message}");

            Console.WriteLine(" -- Validate requested interface not implemented");
            castableObj.InvalidImplementation = BadDynamicInterfaceCastable.InvalidReturn.NotImplemented;
            ex = Assert.Throws<InvalidOperationException>(() => testObj.GetMyType());
            Console.WriteLine($" ---- {ex.GetType().Name}: {ex.Message}");

            Console.WriteLine(" -- Validate no default implementation");
            castableObj.InvalidImplementation = BadDynamicInterfaceCastable.InvalidReturn.NoDefaultImplementation;
            var noDefaultImpl = (ITest)castableObj;
            ex = Assert.Throws<EntryPointNotFoundException>(() => noDefaultImpl.ReturnThis());
            Console.WriteLine($" ---- {ex.GetType().Name}: {ex.Message}");

            Console.WriteLine(" -- Validate default implementation calling method with no default implementation");
            castableObj.InvalidImplementation = BadDynamicInterfaceCastable.InvalidReturn.CallNotImplemented;
            var callNotImpl = (ITest)castableObj;
            ex = Assert.Throws<EntryPointNotFoundException>(() => callNotImpl.ReturnThis());
            Console.WriteLine($" ---- {ex.GetType().Name}: {ex.Message}");

            Console.WriteLine(" -- Validate default implementation calling method taking different interface");
            castableObj.InvalidImplementation = BadDynamicInterfaceCastable.InvalidReturn.UseOtherInterface;
            var useOther = (ITest)castableObj;
            ex = Assert.Throws<InvalidCastException>(() => useOther.GetNumber());
            Console.WriteLine($" ---- {ex.GetType().Name}: {ex.Message}");

            Console.WriteLine(" -- Validate exception thrown");
            castableObj.InvalidImplementation = BadDynamicInterfaceCastable.InvalidReturn.ThrowException;
            ex = Assert.Throws<DynamicInterfaceCastableException>(() => { var _ = (ITest)castableObj; });
            Assert.AreEqual(string.Format(DynamicInterfaceCastableException.ErrorFormat, typeof(ITest)), ex.Message);
            Console.WriteLine($" ---- {ex.GetType().Name}: {ex.Message}");

            Console.WriteLine(" -- Validate reabstracted implementation");
            castableObj.InvalidImplementation = BadDynamicInterfaceCastable.InvalidReturn.ReabstractedImplementation;
            ex = Assert.Throws<EntryPointNotFoundException>(() => { testObj.ReturnThis(); });
            Console.WriteLine($" ---- {ex.GetType().Name}: {ex.Message}");

            Console.WriteLine(" -- Validate diamond inheritance case");
            castableObj.InvalidImplementation = BadDynamicInterfaceCastable.InvalidReturn.DiamondImplementation;
            ex = Assert.Throws<System.Runtime.AmbiguousImplementationException>(() => { testObj.ReturnThis(); });
            Console.WriteLine($" ---- {ex.GetType().Name}: {ex.Message}");

            Console.WriteLine(" -- Validate return default handle");
            castableObj.InvalidImplementation = BadDynamicInterfaceCastable.InvalidReturn.DefaultHandle;
            ex = Assert.Throws<InvalidCastException>(() => testObj.GetMyType());
            Console.WriteLine($" ---- {ex.GetType().Name}: {ex.Message}");
        }

        public static int Main()
        {
            try
            {
                ValidateBasicInterface();
                ValidateGenericInterface();
                ValidateOverriddenInterface();

                ValidateDirectlyImplemented();
                ValidateNotImplemented();

                ValidateErrorHandling();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}
