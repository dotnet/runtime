// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test cases showing interaction of inlining and inline pinvoke,
// along with the impact of EH.

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;


namespace PInvokeTest
{
    static class PInvokeExampleNative
    {
        public static int GetConstant()
        {
            return GetConstantInternal();
        }

        [DllImport(nameof(PInvokeExampleNative))]
        private extern static int GetConstantInternal();
    }

    public class Test
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int AsForceInline()
        {
            return PInvokeExampleNative.GetConstant();
        }

        static int AsNormalInline()
        {
            return PInvokeExampleNative.GetConstant();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AsNoInline()
        {
            return PInvokeExampleNative.GetConstant();
        }

        static bool FromTryCatch()
        {
            bool result = false;
            try 
            {
                // All pinvokes should be inline, except on x64
                result = (PInvokeExampleNative.GetConstant() == AsNormalInline());
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }        

        static bool FromTryFinally()
        {
            bool result = false;
            bool result1 = false;
            bool result2 = false;
            try 
            {
                // All pinvokes should be inline, except on x64
                result1 = (PInvokeExampleNative.GetConstant() == AsNormalInline());
                result2 = (PInvokeExampleNative.GetConstant() == AsNormalInline());
            }
            finally
            {
                result = result1 && result2;
            }
            return result;
        }        

        static bool FromTryFinally2()
        {
            bool result = false;
            bool result1 = false;
            bool result2 = false;

            try 
            {
                // These two pinvokes should be inline, except on x64
                result1 = (PInvokeExampleNative.GetConstant() == AsNormalInline());
            }
            finally
            {
                // These two pinvokes should *not* be inline (finally)
                result2 = (PInvokeExampleNative.GetConstant() == AsNormalInline());
                result = result1 && result2;
            }

            return result;
        }        

        static bool FromTryFinally3()
        {
            bool result = false;
            bool result1 = false;
            bool result2 = false;

            try 
            {
                // These two pinvokes should be inline, except on x64
                result1 = (PInvokeExampleNative.GetConstant() == AsNormalInline());
            }
            finally
            {
                try 
                {
                    // These two pinvokes should *not* be inline (finally)
                    result2 = (PInvokeExampleNative.GetConstant() == AsNormalInline());
                }
                catch (Exception)
                {
                    result2 = false;
                }

                result = result1 && result2;
            }

            return result;
        }        

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool FromInline()
        {
            // These two pinvokes should be inline
            bool result = (PInvokeExampleNative.GetConstant() == AsForceInline());
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool FromInline2()
        {
            // These four pinvokes should be inline
            bool result1 = (PInvokeExampleNative.GetConstant() == AsNormalInline());
            bool result2 = (PInvokeExampleNative.GetConstant() == AsForceInline());
            return result1 && result2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool FromNoInline()
        {
            // The only pinvoke should be inline
            bool result = (PInvokeExampleNative.GetConstant() == AsNoInline());
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool FromNoInline2()
        {
            // Three pinvokes should be inline
            bool result1 = (PInvokeExampleNative.GetConstant() == AsNormalInline());
            bool result2 = (PInvokeExampleNative.GetConstant() == AsNoInline());
            return result1 && result2;
        }

        static bool FromFilter()
        {
            bool result = false;

            try
            {
                throw new Exception("expected");
            }
            // These two pinvokes should *not* be inline (filter)
            //
            // For the first call the jit won't inline the wrapper, so
            // it just calls GetConstant().
            //
            // For the second call, the force inline works, and the
            // subsequent inline of GetConstant() exposes a call
            // to the pinvoke GetConstantInternal().  This pinvoke will
            // not be inline.
            catch (Exception) when (PInvokeExampleNative.GetConstant() == AsForceInline())
            {
                result = true;
            }

            return result;
        }

        static bool FromColdCode()
        {
            int yield = -1;
            bool result1 = false;
            bool result2 = false;

            try
            {
                // This pinvoke should not be inline (cold)
                yield = PInvokeExampleNative.GetConstant();
                throw new Exception("expected");
            }
            catch (Exception)
            {
                // These two pinvokes should not be inline (catch)
                //
                // For the first call the jit won't inline the
                // wrapper, so it just calls GetConstant().
                //
                // For the second call, the force inline works, and
                // the subsequent inline of GetConstant() exposes
                // a call to the pinvoke GetConstantInternal().  This
                // pinvoke will not be inline.
                result1 = (yield == PInvokeExampleNative.GetConstant());
                result2 = (yield == AsForceInline());
            }

            return result1 && result2;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            bool result = true;

            result &= FromTryCatch();
            result &= FromTryFinally();
            result &= FromTryFinally2();
            result &= FromTryFinally3();
            result &= FromInline();           
            result &= FromInline2();
            result &= FromNoInline();
            result &= FromNoInline2();
            result &= FromFilter();
            result &= FromColdCode();

            return (result ? 100 : -1);
        }
    }
}
