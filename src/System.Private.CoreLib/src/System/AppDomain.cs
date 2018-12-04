// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System
{
    /// <summary>
    /// Domains represent an application within the runtime. Objects cannot be
    /// shared between domains and each domain can be configured independently.
    /// </summary>
    internal sealed class AppDomain
    {
        // Domain security information
        // These fields initialized from the other side only. (NOTE: order
        // of these fields cannot be changed without changing the layout in
        // the EE- AppDomainBaseObject in this case)

        public event AssemblyLoadEventHandler AssemblyLoad;

        private ResolveEventHandler _TypeResolve;

        public event ResolveEventHandler TypeResolve
        {
            add
            {
                lock (this)
                {
                    _TypeResolve += value;
                }
            }

            remove
            {
                lock (this)
                {
                    _TypeResolve -= value;
                }
            }
        }

        private ResolveEventHandler _ResourceResolve;

        public event ResolveEventHandler ResourceResolve
        {
            add
            {
                lock (this)
                {
                    _ResourceResolve += value;
                }
            }

            remove
            {
                lock (this)
                {
                    _ResourceResolve -= value;
                }
            }
        }

        private ResolveEventHandler _AssemblyResolve;

        public event ResolveEventHandler AssemblyResolve
        {
            add
            {
                lock (this)
                {
                    _AssemblyResolve += value;
                }
            }

            remove
            {
                lock (this)
                {
                    _AssemblyResolve -= value;
                }
            }
        }

        private EventHandler _processExit;

        private EventHandler _domainUnload;

        private UnhandledExceptionEventHandler _unhandledException;

        // Delegate that will hold references to FirstChance exception notifications
        private EventHandler<FirstChanceExceptionEventArgs> _firstChanceException;

        private IntPtr _pDomain;                      // this is an unmanaged pointer (AppDomain * m_pDomain)` used from the VM.

        public static AppDomain CurrentDomain => Thread.GetDomain();

        [Obsolete("AppDomain.GetCurrentThreadId has been deprecated because it does not provide a stable Id when managed threads are running on fibers (aka lightweight threads). To get a stable identifier for a managed thread, use the ManagedThreadId property on Thread.  http://go.microsoft.com/fwlink/?linkid=14202", false)]
        [DllImport(Interop.Libraries.Kernel32)]
        public static extern int GetCurrentThreadId();

        private AppDomain()
        {
            Debug.Fail("Object cannot be created through this constructor.");
        }

        // support reliability for certain event handlers, if the target
        // methods also participate in this discipline.  If caller passes
        // an existing MulticastDelegate, then we could use a MDA to indicate
        // that reliability is not guaranteed.  But if it is a single cast
        // scenario, we can make it work.

        public event EventHandler ProcessExit
        {
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock (this)
                        _processExit += value;
                }
            }
            remove
            {
                lock (this)
                    _processExit -= value;
            }
        }

        public event EventHandler DomainUnload
        {
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock (this)
                        _domainUnload += value;
                }
            }
            remove
            {
                lock (this)
                    _domainUnload -= value;
            }
        }

        public event UnhandledExceptionEventHandler UnhandledException
        {
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock (this)
                        _unhandledException += value;
                }
            }
            remove
            {
                lock (this)
                    _unhandledException -= value;
            }
        }

        // This is the event managed code can wireup against to be notified
        // about first chance exceptions.
        //
        // To register/unregister the callback, the code must be SecurityCritical.
        public event EventHandler<FirstChanceExceptionEventArgs> FirstChanceException
        {
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock (this)
                        _firstChanceException += value;
                }
            }
            remove
            {
                lock (this)
                    _firstChanceException -= value;
            }
        }

        // This method is called by the VM.
        private void OnAssemblyLoadEvent(RuntimeAssembly LoadedAssembly)
        {
            AssemblyLoad?.Invoke(this, new AssemblyLoadEventArgs(LoadedAssembly));
        }

        // This method is called by the VM.
        private RuntimeAssembly OnResourceResolveEvent(RuntimeAssembly assembly, string resourceName)
        {
            return InvokeResolveEvent(_ResourceResolve, assembly, resourceName);
        }

        // This method is called by the VM
        private RuntimeAssembly OnTypeResolveEvent(RuntimeAssembly assembly, string typeName)
        {
            return InvokeResolveEvent(_TypeResolve, assembly, typeName);
        }

        // This method is called by the VM.
        private RuntimeAssembly OnAssemblyResolveEvent(RuntimeAssembly assembly, string assemblyFullName)
        {
            return InvokeResolveEvent(_AssemblyResolve, assembly, assemblyFullName);
        }

        private RuntimeAssembly InvokeResolveEvent(ResolveEventHandler eventHandler, RuntimeAssembly assembly, string name)
        {
            if (eventHandler == null)
                return null;

            var args = new ResolveEventArgs(name, assembly);

            foreach (ResolveEventHandler handler in eventHandler.GetInvocationList())
            {
                Assembly asm = handler(this, args);
                RuntimeAssembly ret = GetRuntimeAssembly(asm);
                if (ret != null)
                    return ret;
            }

            return null;
        }

#if FEATURE_COMINTEROP
        // Called by VM - code:CLRPrivTypeCacheWinRT::RaiseDesignerNamespaceResolveEvent
        private string[] OnDesignerNamespaceResolveEvent(string namespaceName)
        {
            return System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMetadata.OnDesignerNamespaceResolveEvent(this, namespaceName);
        }
#endif // FEATURE_COMINTEROP

        private static RuntimeAssembly GetRuntimeAssembly(Assembly asm)
        {
            return
                asm == null ? null :
                asm is RuntimeAssembly rtAssembly ? rtAssembly :
                asm is AssemblyBuilder ab ? ab.InternalAssembly :
                null;
        }

        internal int GetId() => 1;
    }
}
