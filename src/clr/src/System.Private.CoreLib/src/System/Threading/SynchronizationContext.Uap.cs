// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Diagnostics;

namespace System.Threading
{
    public partial class SynchronizationContext
    {
        public static SynchronizationContext? Current
        {
            get
            {
                SynchronizationContext? context = Thread.CurrentThread._synchronizationContext;

                if (context == null && ApplicationModel.IsUap)
                    context = GetWinRTContext();

                return context;
            }
        }

        private static SynchronizationContext? GetWinRTContext()
        {
            Debug.Assert(Environment.IsWinRTSupported);
            Debug.Assert(ApplicationModel.IsUap);

            //
            // We call into the VM to get the dispatcher.  This is because:
            //
            //  a) We cannot call the WinRT APIs directly from mscorlib, because we don't have the fancy projections here.
            //  b) We cannot call into System.Runtime.WindowsRuntime here, because we don't want to load that assembly
            //     into processes that don't need it (for performance reasons).
            //
            // So, we check the VM to see if the current thread has a dispatcher; if it does, we pass that along to
            // System.Runtime.WindowsRuntime to get a corresponding SynchronizationContext.
            //
            object? dispatcher = GetWinRTDispatcherForCurrentThread();
            if (dispatcher != null)
                return GetWinRTSynchronizationContext(dispatcher);

            return null;
        }

        private static Func<object, SynchronizationContext>? s_createSynchronizationContextDelegate;

        private static SynchronizationContext GetWinRTSynchronizationContext(object dispatcher)
        {
            //
            // Since we can't directly reference System.Runtime.WindowsRuntime from mscorlib, we have to get the factory via reflection.
            // It would be better if we could just implement WinRTSynchronizationContextFactory in mscorlib, but we can't, because
            // we can do very little with WinRT stuff in mscorlib.
            //
            Func<object, SynchronizationContext>? createSynchronizationContextDelegate = s_createSynchronizationContextDelegate;
            if (createSynchronizationContextDelegate == null)
            {
                Type factoryType = Type.GetType("System.Threading.WinRTSynchronizationContextFactory, System.Runtime.WindowsRuntime", throwOnError: true)!;
                
                // Create an instance delegate for the Create static method
                MethodInfo createMethodInfo = factoryType.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
                createSynchronizationContextDelegate = (Func<object, SynchronizationContext>)Delegate.CreateDelegate(typeof(Func<object, SynchronizationContext>), createMethodInfo);

                s_createSynchronizationContextDelegate = createSynchronizationContextDelegate;
            }

            return createSynchronizationContextDelegate(dispatcher);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Interface)]
        private static extern object? GetWinRTDispatcherForCurrentThread();
    }
}
