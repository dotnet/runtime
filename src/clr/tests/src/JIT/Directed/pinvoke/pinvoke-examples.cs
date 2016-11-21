// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Test cases showing interaction of inlining and inline pinvoke,
// along with the impact of EH.

using System;
using System.Runtime.CompilerServices;


namespace PInvokeTest
{
    internal class Test
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int AsForceInline()
        {
            return Environment.ProcessorCount;
        }

        static int AsNormalInline()
        {
            return Environment.ProcessorCount;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AsNoInline()
        {
            return Environment.ProcessorCount;
        }

        static bool FromTryCatch()
        {
            bool result = false;
            try 
            {
                // All pinvokes should be inline, except on x64
                result = (Environment.ProcessorCount == AsNormalInline());
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
                result1 = (Environment.ProcessorCount == AsNormalInline());
                result2 = (Environment.ProcessorCount == AsNormalInline());
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
                result1 = (Environment.ProcessorCount == AsNormalInline());
            }
            finally
            {
                // These two pinvokes should *not* be inline (finally)
                result2 = (Environment.ProcessorCount == AsNormalInline());
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
                result1 = (Environment.ProcessorCount == AsNormalInline());
            }
            finally
            {
                try 
                {
                    // These two pinvokes should *not* be inline (finally)
                    result2 = (Environment.ProcessorCount == AsNormalInline());
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
            bool result = (Environment.ProcessorCount == AsForceInline());
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool FromInline2()
        {
            // These four pinvokes should be inline
            bool result1 = (Environment.ProcessorCount == AsNormalInline());
            bool result2 = (Environment.ProcessorCount == AsForceInline());
            return result1 && result2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool FromNoInline()
        {
            // The only pinvoke should be inline
            bool result = (Environment.ProcessorCount == AsNoInline());
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool FromNoInline2()
        {
            // Three pinvokes should be inline
            bool result1 = (Environment.ProcessorCount == AsNormalInline());
            bool result2 = (Environment.ProcessorCount == AsNoInline());
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
            // it just calls get_ProcessorCount.
            //
            // For the second call, the force inline works, and the
            // subsequent inline of get_ProcessorCount exposes a call
            // to the pinvoke GetProcessorCount.  This pinvoke will
            // not be inline.
            catch (Exception) when (Environment.ProcessorCount == AsForceInline())
            {
                result = true;
            }

            return result;
        }

        static bool FromColdCode()
        {
            int pc = 0;
            bool result1 = false;
            bool result2 = false;

            try
            {
                // This pinvoke should not be inline (cold)
                pc = Environment.ProcessorCount;
                throw new Exception("expected");
            }
            catch (Exception)
            {
                // These two pinvokes should not be inline (catch)
                //
                // For the first call the jit won't inline the
                // wrapper, so it just calls get_ProcessorCount.
                //
                // For the second call, the force inline works, and
                // the subsequent inline of get_ProcessorCount exposes
                // a call to the pinvoke GetProcessorCount.  This
                // pinvoke will not be inline.
                result1 = (pc == Environment.ProcessorCount);
                result2 = (pc == AsForceInline());
            }

            return result1 && result2;
        }

        private static int Main()
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
