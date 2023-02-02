#undef DEBUG_ALC_WRAPPER

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;

namespace Unity.CoreCLRHelpers
{
    internal class ALCWrapper : AssemblyLoadContext
    {
        private static ALCWrapper rootDomain;
        private List<string> systemPaths;
        private List<string> userPaths;
        private static int idCount = 0;
        private int id;

        static ALCWrapper()
        {
            // .NET Core by default only supports ASCII and UTF-* encodings.
            // Assemblies built for .NET 4.6.1 assume a wider set of supported encodings.
            // Register a CodePagesEncodingProvider to get full support.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public ALCWrapper() : base(isCollectible: false)
        {
            // If this is the first ALC we create, we consider this the root domain, which
            // should load all "System" assemblies.
            if (rootDomain == null)
                rootDomain = this;
            id = idCount++;
        #if DEBUG_ALC_WRAPPER
            Console.WriteLine($"[ALCWrapper:#{id}] Created");
        #endif
            systemPaths = new List<string>();
            userPaths = new List<string>();
        }

        ~ALCWrapper()
        {
        #if DEBUG_ALC_WRAPPER
            Console.WriteLine($"[ALCWrapper:#{id}] Finalize");
        #endif
        }

        void AddPath(string inpaths, bool isSystemPath)
        {
        #if DEBUG_ALC_WRAPPER
            Console.WriteLine($"[ALCWrapper:#{id}] AddPath {inpaths} isSystemPath {isSystemPath}");
        #endif
            foreach (var p in inpaths.Split(Path.PathSeparator))
                (isSystemPath ? systemPaths : userPaths).Add(p);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern string InvokeFindPluginCallback(string path);

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string pluginPath = InvokeFindPluginCallback(unmanagedDllName);
#if DEBUG_ALC_WRAPPER
            Console.WriteLine($"[ALCWrapper:#{id}] LoadUnmanagedDll {unmanagedDllName} -> {pluginPath}");
#endif
            if (!string.IsNullOrEmpty(pluginPath) && Path.IsPathRooted(pluginPath))
                return LoadUnmanagedDllFromPath(pluginPath);

            return IntPtr.Zero;
        }

        internal unsafe Assembly CallLoadFromAssemblyData(byte* data, long size)
        {
#if DEBUG_ALC_WRAPPER
            Console.WriteLine($"[ALCWrapper:#{id}] CallLoadFromAssemblyData {(IntPtr)data} {size}");
#endif
            using (var mem = new UnmanagedMemoryStream(data, size, size, FileAccess.Read))
            {
                return LoadFromStream(mem);
            }
        }

        internal Assembly CallLoadFromAssemblyPath(string path)
        {
        #if DEBUG_ALC_WRAPPER
            Console.WriteLine($"[ALCWrapper:#{id}] CallLoadFromAssemblyPath {path}");
        #endif
            Assembly asm = LoadFromAssemblyPath(path);

            // If the directory containing the assembly we want to load has not been added to user or system paths yet,
            // add it to user paths, so we can resolve any potential dlls next to it, which this assembly might depend on.
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent) && !userPaths.Contains(parent) && !systemPaths.Contains(parent))
                userPaths.Add(parent);

            return asm;
        }

        protected override Assembly Load(AssemblyName name)
        {
#if DEBUG_ALC_WRAPPER
            Console.WriteLine($"[ALCWrapper:#{id}] Load assembly {name}");
#endif
            string assemblyPath = null;
            foreach (var p in systemPaths)
            {
                assemblyPath = Path.Combine(p, $"{name.Name}.dll");
                if (File.Exists(assemblyPath))
                {
#if DEBUG_ALC_WRAPPER
                    Console.WriteLine($"[ALCWrapper:#{id}] is System assembly {name}");
#endif
                    // This is a system assembly - those cannot be cleanly unloaded - load it into the root ALC.
                    if (this != rootDomain)
                        return rootDomain.Load(name);
                    break;
                }
                assemblyPath = null;
            }

            if (assemblyPath == null)
            {
                foreach (var p in userPaths)
                {
                    assemblyPath = Path.Combine(p, $"{name.Name}.dll");
                    if (File.Exists(assemblyPath))
                        break;
                    assemblyPath = null;
                }
            }

            if (assemblyPath == null)
            {
            #if DEBUG_ALC_WRAPPER
                Console.WriteLine($"[ALCWrapper:#{id}] assembly {name} not found.");
            #endif
                return null;
            }


            try
            {
#if DEBUG_ALC_WRAPPER
                Console.WriteLine($"[ALCWrapper:#{id}] Load assembly {name} from {assemblyPath}");
#endif
                var result = LoadFromAssemblyPath(assemblyPath);
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ALCWrapper:#{id}] Failed loading {name} from {assemblyPath}:\n{e}");
                return null;
            }
        }

        void DomainUnloadNotification()
        {
            try
            {
                var domainUnloadField =
                    typeof(AppDomain).GetField("DomainUnload", BindingFlags.Instance | BindingFlags.NonPublic);
                var eventDelegate = (EventHandler) domainUnloadField?.GetValue(AppDomain.CurrentDomain);
                if (eventDelegate != null)
                {
                    eventDelegate(AppDomain.CurrentDomain, EventArgs.Empty);
                    domainUnloadField?.SetValue(AppDomain.CurrentDomain, null);
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"Caught {e} calling AppDomain.CurrentDomain.DomainUnload.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        WeakReference InitUnload()
        {
            systemPaths = null;
            userPaths = null;
        #if DEBUG_ALC_WRAPPER
            Console.WriteLine($"[ALCWrapper:#{id}] Unload");
        #endif

            var alcWeakRef = new WeakReference(this);
            Unload();
            return alcWeakRef;
        }

        static Exception FinishUnload(WeakReference alcWeakRef)
        {
            for (int i = 0; alcWeakRef.IsAlive && (i < 10); i++)
            {
            #if DEBUG_ALC_WRAPPER
                Console.WriteLine($"[ALCWrapper] Unload attempt: {i}");
            #endif
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

        #if DEBUG_ALC_WRAPPER
            Console.WriteLine($"[ALCWrapper] FinishUnload success: {!alcWeakRef.IsAlive}");
        #endif

            if (alcWeakRef.IsAlive)
            {
                return new AssemblyLoadContextUnloadException();
            }
            return null;
        }
    }
}
