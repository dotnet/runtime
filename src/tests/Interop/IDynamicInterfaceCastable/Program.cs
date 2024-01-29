// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

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

    public interface ITestGeneric<in T, out U>
    {
        U ReturnArg(T t);
        V DoubleGenericArg<V>(V t);
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
    public interface ITestGenericImpl<T, U>: ITestGeneric<T, U>
    {
        U ITestGeneric<T, U>.ReturnArg(T t)
        {
            if (!typeof(T).IsAssignableTo(typeof(U))
                && !t.GetType().IsAssignableTo(typeof(U)))
            {
                throw new Exception($"Invalid covariance conversion from {typeof(T)} or {t.GetType()} to {typeof(U)}");
            }

            return Unsafe.As<T, U>(ref t);
        }

        V ITestGeneric<T, U>.DoubleGenericArg<V>(V v)
        {
            if (v is int i)
            {
                Assert.True(typeof(V) == typeof(int));
                i *= 2;
                return Unsafe.As<int, V>(ref i);
            }
            else if (v is string s)
            {
                Assert.True(typeof(V) == typeof(string));
                s += s;
                return Unsafe.As<string, V>(ref s);
            }
            throw new Exception("Unable to double");
        }
    }

    [DynamicInterfaceCastableImplementation]
    public interface ITestGenericIntImpl: ITestGeneric<int, int>
    {
        int ITestGeneric<int, int>.ReturnArg(int i)
        {
            return i;
        }

        V ITestGeneric<int, int>.DoubleGenericArg<V>(V v)
        {
            if (v is int i)
            {
                Assert.True(typeof(V) == typeof(int));
                i *= 2;
                return Unsafe.As<int, V>(ref i);
            }
            else if (v is string s)
            {
                Assert.True(typeof(V) == typeof(string));
                s += s;
                return Unsafe.As<string, V>(ref s);
            }
            throw new Exception("Unable to double");
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

    [ActiveIssue("https://github.com/dotnet/runtime/issues/55742", TestRuntimes.Mono)]
    public class Program
    {
        [Fact]
        public static void ValidateBasicInterface()
        {
            Console.WriteLine($"Running {nameof(ValidateBasicInterface)}");

            object castableObj = new DynamicInterfaceCastable(new Dictionary<Type, Type> {
                { typeof(ITest), typeof(ITestImpl) }
            });

            Console.WriteLine(" -- Validate cast");

            // ITest -> ITestImpl
            Assert.True(castableObj is ITest);
            Assert.NotNull(castableObj as ITest);
            var testObj = (ITest)castableObj;

            Console.WriteLine(" -- Validate method call");
            Assert.Same(castableObj, testObj.ReturnThis());
            Assert.Equal(typeof(DynamicInterfaceCastable), testObj.GetMyType());

            Console.WriteLine(" -- Validate method call which calls methods using 'this'");
            Assert.Equal(DynamicInterfaceCastable.ImplementedMethodReturnValue, testObj.CallImplemented(ImplementationToCall.Class));
            Assert.Equal(ITestImpl.GetNumberReturnValue, testObj.CallImplemented(ImplementationToCall.Interface));
            Assert.Equal(ITestImpl.GetNumberPrivateReturnValue, testObj.CallImplemented(ImplementationToCall.InterfacePrivate));
            Assert.Equal(ITestImpl.GetNumberStaticReturnValue, testObj.CallImplemented(ImplementationToCall.InterfaceStatic));
            Assert.Throws<InvalidCastException>(() => testObj.CallImplemented(ImplementationToCall.ImplInterfacePublic));

            Console.WriteLine(" -- Validate delegate call");
            Func<ITest> func = new Func<ITest>(testObj.ReturnThis);
            Assert.Same(castableObj, func());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/1442", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
        public static void ValidateGenericInterface()
        {
            Console.WriteLine($"Running {nameof(ValidateGenericInterface)}");

            object castableObj = new DynamicInterfaceCastable(new Dictionary<Type, Type> {
                { typeof(ITestGeneric<int, int>), typeof(ITestGenericIntImpl) },
                { typeof(ITestGeneric<string, string>), typeof(ITestGenericImpl<string, string>) },
                { typeof(ITestGeneric<string, object>), typeof(ITestGenericImpl<object, string>) },
            });

            Console.WriteLine(" -- Validate cast");

            // ITestGeneric<int, int> -> ITestGenericIntImpl
            Assert.True(castableObj is ITestGeneric<int, int>, $"Should be castable to {nameof(ITestGeneric<int, int>)} via is");
            Assert.NotNull(castableObj as ITestGeneric<int, int>);
            ITestGeneric<int, int> testInt = (ITestGeneric<int, int>)castableObj;

            // ITestGeneric<string, string> -> ITestGenericImpl<string, string>
            Assert.True(castableObj is ITestGeneric<string, string>, $"Should be castable to {nameof(ITestGeneric<string, string>)} via is");
            Assert.NotNull(castableObj as ITestGeneric<string, string>);
            ITestGeneric<string, string> testStr = (ITestGeneric<string, string>)castableObj;

            // Validate Variance
            // ITestGeneric<string, object> -> ITestGenericImpl<object, string>
            Assert.True(castableObj is ITestGeneric<string, object>, $"Should be castable to {nameof(ITestGeneric<string, object>)} via is");
            Assert.NotNull(castableObj as ITestGeneric<string, object>);
            ITestGeneric<string, object> testVar = (ITestGeneric<string, object>)castableObj;

            // ITestGeneric<bool, bool> is not recognized
            Assert.False(castableObj is ITestGeneric<bool, bool>, $"Should not be castable to {nameof(ITestGeneric<bool, bool>)} via is");
            Assert.Null(castableObj as ITestGeneric<bool, bool>);
            var ex = Assert.Throws<DynamicInterfaceCastableException>(() => { var _ = (ITestGeneric<bool, bool>)castableObj; });
            Assert.Equal(string.Format(DynamicInterfaceCastableException.ErrorFormat, typeof(ITestGeneric<bool, bool>)), ex.Message);

            int expectedInt = 42;
            string expectedStr = "str";

            Console.WriteLine(" -- Validate method call");
            Assert.Equal(expectedInt, testInt.ReturnArg(42));
            Assert.Equal(expectedStr, testStr.ReturnArg(expectedStr));
            Assert.Equal(expectedStr, testVar.ReturnArg(expectedStr));

            Console.WriteLine(" -- Validate generic method call");
            Assert.Equal(expectedInt * 2, testInt.DoubleGenericArg<int>(42));
            Assert.Equal(expectedStr + expectedStr, testInt.DoubleGenericArg<string>("str"));
            Assert.Equal(expectedInt * 2, testStr.DoubleGenericArg<int>(42));
            Assert.Equal(expectedStr + expectedStr, testStr.DoubleGenericArg<string>("str"));
            Assert.Equal(expectedInt * 2, testVar.DoubleGenericArg<int>(42));
            Assert.Equal(expectedStr + expectedStr, testVar.DoubleGenericArg<string>("str"));

            Console.WriteLine(" -- Validate delegate call");
            Func<int, int> funcInt = new Func<int, int>(testInt.ReturnArg);
            Assert.Equal(expectedInt, funcInt(expectedInt));
            Func<string, string> funcStr = new Func<string, string>(testStr.ReturnArg);
            Assert.Equal(expectedStr, funcStr(expectedStr));
            Func<string, object> funcVar = new Func<string, object>(testVar.ReturnArg);
            Assert.Equal(expectedStr, funcVar(expectedStr));
        }

        [Fact]
        public static void ValidateOverriddenInterface()
        {
            Console.WriteLine($"Running {nameof(ValidateOverriddenInterface)}");

            object castableObj = new DynamicInterfaceCastable(new Dictionary<Type, Type> {
                { typeof(ITest), typeof(IOverrideTestImpl) },
                { typeof(IOverrideTest), typeof(IOverrideTestImpl) },
            });

            Console.WriteLine(" -- Validate cast");

            // IOverrideTest -> IOverrideTestImpl
            Assert.True(castableObj is IOverrideTest, $"Should be castable to {nameof(IOverrideTest)} via is");
            Assert.NotNull(castableObj as IOverrideTest);
            var testObj = (IOverrideTest)castableObj;

            Console.WriteLine(" -- Validate method call");
            Assert.Same(castableObj, testObj.ReturnThis());
            Assert.Equal(IOverrideTestImpl.GetMyTypeReturnValue, testObj.GetMyType());

            Console.WriteLine(" -- Validate method call which calls methods using 'this'");
            Assert.Equal(DynamicInterfaceCastable.ImplementedMethodReturnValue, testObj.CallImplemented(ImplementationToCall.Class));
            Assert.Equal(IOverrideTestImpl.GetNumberReturnValue_Override, testObj.CallImplemented(ImplementationToCall.Interface));
            Assert.Equal(ITestImpl.GetNumberPrivateReturnValue, testObj.CallImplemented(ImplementationToCall.InterfacePrivate));
            Assert.Equal(ITestImpl.GetNumberStaticReturnValue, testObj.CallImplemented(ImplementationToCall.InterfaceStatic));

            Console.WriteLine(" -- Validate delegate call");
            Func<ITest> func = new Func<ITest>(testObj.ReturnThis);
            Assert.Same(castableObj, func());
            Func<Type> funcGetType = new Func<Type>(testObj.GetMyType);
            Assert.Equal(IOverrideTestImpl.GetMyTypeReturnValue, funcGetType());
        }

        [Fact]
        public static void ValidateNotImplemented()
        {
            Console.WriteLine($"Running {nameof(ValidateNotImplemented)}");

            object castableObj = new DynamicInterfaceCastable(new Dictionary<Type, Type> {
                { typeof(ITest), typeof(ITestImpl) }
            });

            Assert.False(castableObj is INotImplemented, $"Should not be castable to {nameof(INotImplemented)} via is");
            Assert.Null(castableObj as INotImplemented);
            var ex = Assert.Throws<DynamicInterfaceCastableException>(() => { var _ = (INotImplemented)castableObj; });
            Assert.Equal(string.Format(DynamicInterfaceCastableException.ErrorFormat, typeof(INotImplemented)), ex.Message);
        }

        [Fact]
        public static void ValidateDirectlyImplemented()
        {
            Console.WriteLine($"Running {nameof(ValidateDirectlyImplemented)}");

            object castableObj = new DynamicInterfaceCastable(new Dictionary<Type, Type> {
                { typeof(ITest), typeof(ITestImpl) },
                { typeof(IDirectlyImplemented), typeof(IDirectlyImplementedImpl) },
            });

            Console.WriteLine(" -- Validate cast");
            Assert.True(castableObj is IDirectlyImplemented, $"Should be castable to {nameof(IDirectlyImplemented)} via is");
            Assert.NotNull(castableObj as IDirectlyImplemented);
            var direct = (IDirectlyImplemented)castableObj;

            Console.WriteLine(" -- Validate method call");
            Assert.Equal(DynamicInterfaceCastable.ImplementedMethodReturnValue, direct.ImplementedMethod());

            Console.WriteLine(" -- Validate delegate call");
            Func<int> func = new Func<int>(direct.ImplementedMethod);
            Assert.Equal(DynamicInterfaceCastable.ImplementedMethodReturnValue, func());
        }

        [Fact]
        public static void ValidateErrorHandling()
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
            Assert.Equal(string.Format(DynamicInterfaceCastableException.ErrorFormat, typeof(ITest)), ex.Message);
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
    }
}
