// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Tests
{
    /// <summary>
    /// Base class for invoke-based tests that support invoking both emit- and interpreter-based runtime implementations.
    /// </summary>
    public abstract class InvokeStrategy
    {
        public bool UseEmit { get; }

        public InvokeStrategy(bool useEmit)
        {
            UseEmit = useEmit;
        }

#if NETCOREAPP
        public static bool AreTestingBindingFlagsSupported => !PlatformDetection.IsReleaseRuntime && RuntimeFeature.IsDynamicCodeCompiled
            && !PlatformDetection.IsNotMonoRuntime; // Temporary until Mono is updated.

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime))] // Temporary until Mono is updated.
        public void MethodInvoker_AutoEmitOrInterpreted()
        {
            Exception e;
            MethodInfo method = typeof(ExceptionThrower).GetMethod(nameof(ExceptionThrower.ThrowForStackTrace), BindingFlags.Static | BindingFlags.Public)!;

            // The first time emit may or may not be used depending if it was already called and whether emit is available.
            e = Assert.Throws<TargetInvocationException>(() => method.Invoke(obj: null, parameters: null));
            Assert.Contains("LocateMe", e.InnerException.Message);

            // The second time uses emit if available.
            e = Assert.Throws<TargetInvocationException>(() => method.Invoke(obj: null, parameters: null));
            Assert.Contains("LocateMe", e.InnerException.Message);

            if (RuntimeFeature.IsDynamicCodeCompiled)
            {
                Assert.Contains("InvokeStub_", e.InnerException.StackTrace);
            }
            else
            {
                Assert.DoesNotContain("InvokeStub_", e.InnerException.StackTrace);
            }
        }

        [ConditionalFact(typeof(InvokeStrategy), nameof(InvokeStrategy.AreTestingBindingFlagsSupported))]
        public void MethodInvoker_ExplicitEmitAndInterpreted()
        {
            Exception e;
            MethodInfo method = typeof(ExceptionThrower).GetMethod(nameof(ExceptionThrower.ThrowForStackTrace), BindingFlags.Static | BindingFlags.Public)!;

            e = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(obj: null, (BindingFlags)TestingBindingFlags.InvokeWithEmit, binder: null, parameters: null, culture: null));
            Assert.Contains("LocateMe", e.InnerException.Message);
            Assert.Contains("InvokeStub_", e.InnerException.StackTrace);

            e = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(obj: null, (BindingFlags)TestingBindingFlags.InvokeWithInterpreter, binder: null, parameters: null, culture: null));
            Assert.Contains("LocateMe", e.InnerException.Message);
            Assert.DoesNotContain("InvokeStub_", e.InnerException.StackTrace);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime))] // Temporary until Mono is updated.
        public void ConstructorInvoker_AutoEmitOrInterpreted()
        {
            Exception e;
            ConstructorInfo method = typeof(ExceptionThrower).GetConstructor(Type.EmptyTypes)!;

            // The first time emit may or may not be used depending if it was already called and whether emit is available.
            e = Assert.Throws<TargetInvocationException>(() => method.Invoke(parameters: null));
            Assert.Contains("LocateMe", e.InnerException.Message);

            // The second time uses emit if available.
            e = Assert.Throws<TargetInvocationException>(() => method.Invoke(parameters: null));
            Assert.Contains("LocateMe", e.InnerException.Message);

            if (RuntimeFeature.IsDynamicCodeCompiled)
            {
                Assert.Contains("InvokeStub_", e.InnerException.StackTrace);
            }
            else
            {
                Assert.DoesNotContain("InvokeStub_", e.InnerException.StackTrace);
            }
        }

        [ConditionalFact(typeof(InvokeStrategy), nameof(InvokeStrategy.AreTestingBindingFlagsSupported))]
        public void ConstructorInvoker_ExplicitEmitAndInterpreted()
        {
            Exception e;
            ConstructorInfo method = typeof(ExceptionThrower).GetConstructor(Type.EmptyTypes)!;

            e = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke((BindingFlags)TestingBindingFlags.InvokeWithEmit, binder: null, parameters: null, culture: null));
            Assert.Contains("LocateMe", e.InnerException.Message);
            Assert.Contains("InvokeStub_", e.InnerException.StackTrace);

            e = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke((BindingFlags)TestingBindingFlags.InvokeWithInterpreter, binder: null, parameters: null, culture: null));
            Assert.Contains("LocateMe", e.InnerException.Message);
            Assert.DoesNotContain("InvokeStub_", e.InnerException.StackTrace);
        }
#endif

        public object? Invoke(MethodBase method, object? obj, object?[]? parameters)
        {
            return method.Invoke(
                obj,
                (BindingFlags)(UseEmit ? TestingBindingFlags.InvokeWithEmit : TestingBindingFlags.InvokeWithInterpreter),
                binder: null,
                parameters,
                culture: null);
        }

        public object? Invoke(
            MethodBase method,
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[]? parameters,
            CultureInfo? culture)
        {
            return method.Invoke(
                obj,
                invokeAttr | (BindingFlags)(UseEmit ? TestingBindingFlags.InvokeWithEmit : TestingBindingFlags.InvokeWithInterpreter),
                binder,
                parameters,
                culture);
        }

        public object? Invoke(ConstructorInfo method, object?[]? parameters)
        {
            return method.Invoke(
                (BindingFlags)(UseEmit ? TestingBindingFlags.InvokeWithEmit : TestingBindingFlags.InvokeWithInterpreter),
                binder: null,
                parameters,
                culture: null);
        }

        public object? Invoke(
            ConstructorInfo method,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[]? parameters,
            CultureInfo? culture)
        {
            return method.Invoke(
                invokeAttr | (BindingFlags)(UseEmit ? TestingBindingFlags.InvokeWithEmit : TestingBindingFlags.InvokeWithInterpreter),
                binder,
                parameters,
                culture);
        }

        public object? InvokeMember(
            Type type,
            string name,
            BindingFlags invokeAttr,
            Binder? binder,
            object? target,
            object?[]? args,
            ParameterModifier[]? modifiers,
            CultureInfo? culture,
            string[]? namedParameters)
        {
            return type.InvokeMember(
                name,
                invokeAttr | (BindingFlags)(UseEmit ? TestingBindingFlags.InvokeWithEmit : TestingBindingFlags.InvokeWithInterpreter),
                binder,
                target,
                args,
                modifiers,
                culture,
                namedParameters);
        }

        public object? GetValue(
            PropertyInfo property,
            object? obj)
        {
            return property.GetValue(
                obj,
                (BindingFlags)(UseEmit ? TestingBindingFlags.InvokeWithEmit : TestingBindingFlags.InvokeWithInterpreter),
                binder : null,
                index : null,
                culture : null);
        }

        public object? GetValue(
            PropertyInfo property,
            object? obj,
            object?[]? index)
        {
            return property.GetValue(
                obj,
                (BindingFlags)(UseEmit ? TestingBindingFlags.InvokeWithEmit : TestingBindingFlags.InvokeWithInterpreter),
                binder: null,
                index: index,
                culture: null);
        }

        public object? GetValue(
            PropertyInfo property,
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[]? index,
            CultureInfo? culture)
        {
            return property.GetValue(
                obj,
                invokeAttr | (BindingFlags)(UseEmit ? TestingBindingFlags.InvokeWithEmit : TestingBindingFlags.InvokeWithInterpreter),
                binder,
                index,
                culture);
        }

        public void SetValue(
            PropertyInfo property,
            object? obj,
            object? value)
        {
            property.SetValue(
                obj,
                value,
                (BindingFlags)(UseEmit ? TestingBindingFlags.InvokeWithEmit : TestingBindingFlags.InvokeWithInterpreter),
                binder: null,
                index: null,
                culture: null);
        }

        public void SetValue(
            PropertyInfo property,
            object? obj,
            object? value,
            object?[]? index)
        {
            property.SetValue(
                obj,
                value,
                (BindingFlags)(UseEmit ? TestingBindingFlags.InvokeWithEmit : TestingBindingFlags.InvokeWithInterpreter),
                binder: null,
                index: index,
                culture: null);
        }

        public void SetValue(
            PropertyInfo property,
            object? obj,
            object? value,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[]? index,
            CultureInfo? culture)
        {
            property.SetValue(
                obj,
                value,
                invokeAttr | (BindingFlags)(UseEmit ? TestingBindingFlags.InvokeWithEmit : TestingBindingFlags.InvokeWithInterpreter),
                binder,
                index,
                culture);
        }

        /// <summary>
        /// Internal enums for testing only. These are subject to change depending on test strategy.
        /// If changed, also change BindingFlags.cs.
        /// </summary>
        [Flags]
        internal enum TestingBindingFlags
        {
            /// <summary>
            /// Use IL Emit (if available) for Invoke()
            /// </summary>
            InvokeWithEmit = 0x20000000,

            /// <summary>
            /// Use the native interpreter for Invoke()
            /// </summary>
            InvokeWithInterpreter = 0x40000000
        }

        private class ExceptionThrower
        {
            public ExceptionThrower() => throw new Exception("LocateMe");
            public static void ThrowForStackTrace() => throw new Exception("LocateMe");
        }
    }
}
