// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.ExceptionServices.Tests
{
    public class ExceptionHandlingTests
    {
        [Fact]
        [SkipOnMono("ExceptionHandling.GetCurrentException is not supported on Mono")]
        public void GetCurrentException_NoException_ReturnsNull()
        {
            Assert.Null(ExceptionHandling.GetCurrentException());
        }

        [Fact]
        [SkipOnMono("ExceptionHandling.GetCurrentException is not supported on Mono")]
        public void GetCurrentException_InFinallyAndCatch()
        {
            var exception = new InvalidOperationException();
            Exception? inFinally = null;
            Exception? inCatch = exception;

            try
            {
                try
                {
                    throw exception;
                }
                finally
                {
                    inFinally = ExceptionHandling.GetCurrentException();
                }
            }
            catch (InvalidOperationException)
            {
                inCatch = ExceptionHandling.GetCurrentException();
            }

            Assert.Same(exception, inFinally);
            Assert.Null(inCatch);
            Assert.Null(ExceptionHandling.GetCurrentException());
        }

        [Fact]
        [SkipOnMono("ExceptionHandling.GetCurrentException is not supported on Mono")]
        public void GetCurrentException_FinallyAfterCatch_ReturnsNull()
        {
            Exception? inFinally = new Exception();

            try
            {
                throw new InvalidOperationException();
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                inFinally = ExceptionHandling.GetCurrentException();
            }

            Assert.Null(inFinally);
        }

        [Fact]
        [SkipOnMono("ExceptionHandling.GetCurrentException is not supported on Mono")]
        public void GetCurrentException_InFilter()
        {
            var exception = new InvalidOperationException();
            Exception? inFilter = null;

            try
            {
                throw exception;
            }
            catch (InvalidOperationException) when ((inFilter = ExceptionHandling.GetCurrentException()) is not null)
            {
            }

            Assert.Same(exception, inFilter);
        }

        [Fact]
        [SkipOnMono("ExceptionHandling.GetCurrentException is not supported on Mono")]
        public void GetCurrentException_Rethrow()
        {
            var exception = new InvalidOperationException();
            Exception? inCatch = exception;
            Exception? inFinally = null;

            try
            {
                try
                {
                    try
                    {
                        throw exception;
                    }
                    catch
                    {
                        inCatch = ExceptionHandling.GetCurrentException();
                        throw;
                    }
                }
                finally
                {
                    inFinally = ExceptionHandling.GetCurrentException();
                }
            }
            catch (InvalidOperationException)
            {
            }

            Assert.Null(inCatch);
            Assert.Same(exception, inFinally);
        }

        [Fact]
        [SkipOnMono("ExceptionHandling.GetCurrentException is not supported on Mono")]
        public void GetCurrentException_NestedExceptionCaughtInFinally()
        {
            var outer = new InvalidOperationException();
            var inner = new ArgumentException();
            Exception? inInnerFinally = null;
            Exception? inInnerCatch = null;
            Exception? afterInnerCatch = null;

            try
            {
                try
                {
                    throw outer;
                }
                finally
                {
                    try
                    {
                        try
                        {
                            throw inner;
                        }
                        finally
                        {
                            inInnerFinally = ExceptionHandling.GetCurrentException();
                        }
                    }
                    catch (ArgumentException)
                    {
                        inInnerCatch = ExceptionHandling.GetCurrentException();
                    }

                    afterInnerCatch = ExceptionHandling.GetCurrentException();
                }
            }
            catch (InvalidOperationException)
            {
            }

            Assert.Same(inner, inInnerFinally);
            Assert.Same(outer, inInnerCatch);
            Assert.Same(outer, afterInnerCatch);
            Assert.Null(ExceptionHandling.GetCurrentException());
        }

        [Fact]
        [SkipOnMono("ExceptionHandling.GetCurrentException is not supported on Mono")]
        public void GetCurrentException_ExceptionEscapingFinally_SupersedesOriginal()
        {
            var original = new InvalidOperationException();
            var replacement = new ArgumentException();
            Exception? inOuterFinally = null;
            Exception? inCatch = replacement;

            try
            {
                try
                {
                    try
                    {
                        throw original;
                    }
                    finally
                    {
                        throw replacement;
                    }
                }
                finally
                {
                    inOuterFinally = ExceptionHandling.GetCurrentException();
                }
            }
            catch (ArgumentException)
            {
                inCatch = ExceptionHandling.GetCurrentException();
            }

            Assert.Same(replacement, inOuterFinally);
            Assert.Null(inCatch);
        }

        [Fact]
        [SkipOnMono("ExceptionHandling.GetCurrentException is not supported on Mono")]
        public void GetCurrentException_ThrownFromCallee()
        {
            Exception? inFinally = null;

            Exception thrown = Assert.Throws<InvalidOperationException>(() =>
            {
                try
                {
                    ThrowHelper();
                }
                finally
                {
                    inFinally = ExceptionHandling.GetCurrentException();
                }
            });

            Assert.Same(thrown, inFinally);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void ThrowHelper() => throw new InvalidOperationException();
        }

        [Fact]
        [SkipOnMono("ExceptionHandling.GetCurrentException is not supported on Mono")]
        public void GetCurrentException_HardwareException()
        {
            Exception? inFinally = null;

            Exception thrown = Assert.Throws<NullReferenceException>(() =>
            {
                try
                {
                    DereferenceNull(null);
                }
                finally
                {
                    inFinally = ExceptionHandling.GetCurrentException();
                }
            });

            Assert.Same(thrown, inFinally);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static int DereferenceNull(int[]? array) => array![0];
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [SkipOnMono("ExceptionHandling.GetCurrentException is not supported on Mono")]
        public void GetCurrentException_NonExceptionObject_ReturnsRuntimeWrappedException()
        {
            var dynamicMethod = new DynamicMethod("ThrowObject", typeof(void), [typeof(object)], typeof(ExceptionHandlingTests).Module);
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Throw);
            var throwObject = dynamicMethod.CreateDelegate<Action<object>>();

            object thrownObject = "thrown object";
            Exception? inFinally = null;
            Exception? inCatch = new Exception();

            RuntimeWrappedException caught = Assert.Throws<RuntimeWrappedException>(() =>
            {
                try
                {
                    try
                    {
                        throwObject(thrownObject);
                    }
                    finally
                    {
                        inFinally = ExceptionHandling.GetCurrentException();
                    }
                }
                catch (RuntimeWrappedException)
                {
                    inCatch = ExceptionHandling.GetCurrentException();
                    throw;
                }
            });

            RuntimeWrappedException wrapped = Assert.IsType<RuntimeWrappedException>(inFinally);
            Assert.Same(thrownObject, wrapped.WrappedException);
            Assert.Same(caught, wrapped);
            Assert.Null(inCatch);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        [SkipOnMono("ExceptionHandling.GetCurrentException is not supported on Mono")]
        public void GetCurrentException_OtherThread_ReturnsNull()
        {
            Exception? inFinally = null;
            Exception? onOtherThread = new Exception();

            try
            {
                try
                {
                    throw new InvalidOperationException();
                }
                finally
                {
                    inFinally = ExceptionHandling.GetCurrentException();
                    onOtherThread = Task.Factory.StartNew(ExceptionHandling.GetCurrentException, TaskCreationOptions.LongRunning).GetAwaiter().GetResult();
                }
            }
            catch (InvalidOperationException)
            {
            }

            Assert.NotNull(inFinally);
            Assert.Null(onOtherThread);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime))]
        public void GetCurrentException_Mono_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => ExceptionHandling.GetCurrentException());
        }
    }
}
