// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
namespace System.Threading
{
    using System;
    using System.Security.Permissions;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Runtime;

    // After much discussion, we decided the Interlocked class doesn't need 
    // any HPA's for synchronization or external threading.  They hurt C#'s 
    // codegen for the yield keyword, and arguably they didn't protect much.  
    // Instead, they penalized people (and compilers) for writing threadsafe 
    // code.
    public static class Interlocked
    {        
        /******************************
         * Increment
         *   Implemented: int
         *                        long
         *****************************/

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int Increment(ref int location)
        {
            return Add(ref location, 1);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static long Increment(ref long location)
        {
            return Add(ref location, 1);
        }

        /******************************
         * Decrement
         *   Implemented: int
         *                        long
         *****************************/

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int Decrement(ref int location)
        {
            return Add(ref location, -1);
        }

        public static long Decrement(ref long location)
        {
            return Add(ref location, -1);
        }

        /******************************
         * Exchange
         *   Implemented: int
         *                        long
         *                        float
         *                        double
         *                        Object
         *                        IntPtr
         *****************************/

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        public static extern int Exchange(ref int location1, int value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]
        public static extern long Exchange(ref long location1, long value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]
        public static extern float Exchange(ref float location1, float value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]
        public static extern double Exchange(ref double location1, double value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        public static extern Object Exchange(ref Object location1, Object value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        public static extern IntPtr Exchange(ref IntPtr location1, IntPtr value);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Runtime.InteropServices.ComVisible(false)]
        [System.Security.SecuritySafeCritical]
        public static T Exchange<T>(ref T location1, T value) where T : class
        {
            _Exchange(__makeref(location1), __makeref(value));
            //Since value is a local we use trash its data on return
            //  The Exchange replaces the data with new data
            //  so after the return "value" contains the original location1
            //See ExchangeGeneric for more details           
            return value;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        private static extern void _Exchange(TypedReference location1, TypedReference value);

        /******************************
         * CompareExchange
         *    Implemented: int
         *                         long
         *                         float
         *                         double
         *                         Object
         *                         IntPtr
         *****************************/

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        public static extern int CompareExchange(ref int location1, int value, int comparand);    

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]
        public static extern long CompareExchange(ref long location1, long value, long comparand);    

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]
        public static extern float CompareExchange(ref float location1, float value, float comparand);    

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]
        public static extern double CompareExchange(ref double location1, double value, double comparand);    

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        public static extern Object CompareExchange(ref Object location1, Object value, Object comparand);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        public static extern IntPtr CompareExchange(ref IntPtr location1, IntPtr value, IntPtr comparand);

        /*****************************************************************
         * CompareExchange<T>
         * 
         * Notice how CompareExchange<T>() uses the __makeref keyword
         * to create two TypedReferences before calling _CompareExchange().
         * This is horribly slow. Ideally we would like CompareExchange<T>()
         * to simply call CompareExchange(ref Object, Object, Object); 
         * however, this would require casting a "ref T" into a "ref Object", 
         * which is not legal in C#.
         * 
         * Thus we opted to implement this in the JIT so that when it reads
         * the method body for CompareExchange<T>() it gets back the
         * following IL:
         *
         *     ldarg.0 
         *     ldarg.1
         *     ldarg.2
         *     call System.Threading.Interlocked::CompareExchange(ref Object, Object, Object)
         *     ret
         *
         * See getILIntrinsicImplementationForInterlocked() in VM\JitInterface.cpp
         * for details.
         *****************************************************************/
        
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Runtime.InteropServices.ComVisible(false)]
        [System.Security.SecuritySafeCritical]
        public static T CompareExchange<T>(ref T location1, T value, T comparand) where T : class
        {
            // _CompareExchange() passes back the value read from location1 via local named 'value'
            _CompareExchange(__makeref(location1), __makeref(value), comparand);
            return value;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        private static extern void _CompareExchange(TypedReference location1, TypedReference value, Object comparand);

        // BCL-internal overload that returns success via a ref bool param, useful for reliable spin locks.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        internal static extern int CompareExchange(ref int location1, int value, int comparand, ref bool succeeded);

        /******************************
         * Add
         *    Implemented: int
         *                         long
         *****************************/

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int ExchangeAdd(ref int location1, int value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern long ExchangeAdd(ref long location1, long value);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int Add(ref int location1, int value) 
        {
            return ExchangeAdd(ref location1, value) + value;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static long Add(ref long location1, long value) 
        {
            return ExchangeAdd(ref location1, value) + value;
        }
     
        /******************************
         * Read
         *****************************/
        public static long Read(ref long location)
        {
            return Interlocked.CompareExchange(ref location,0,0);
        }


        public static void MemoryBarrier()
        {
            Thread.MemoryBarrier();
        }
    }
}
