#undef DEBUG_ALC_WRAPPER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;

namespace Unity.CoreCLRHelpers
{
    class ALCWrapper : AssemblyLoadContext
    {
        readonly ALCWrapper m_SystemAlc;
        List<string> m_Paths = new();

        static ALCWrapper()
        {
            // .NET Core by default only supports ASCII and UTF-* encodings.
            // Assemblies built for .NET 4.6.1 assume a wider set of supported encodings.
            // Register a CodePagesEncodingProvider to get full support.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public ALCWrapper(string name, ALCWrapper systemAlc) : base(isCollectible: systemAlc != null, name: name)
        {
            m_SystemAlc = systemAlc;
#if DEBUG_ALC_WRAPPER
            CoreCLRHost.Log($"[ALCWrapper:#{Name}] Created");
#endif
        }

        ~ALCWrapper()
        {
        #if DEBUG_ALC_WRAPPER
            CoreCLRHost.Log($"[ALCWrapper:#{Name}] Finalize");
        #endif
        }

        public void AddSearchPath(string path)
        {
            string lastPath = path.Split(';').Last(s => !string.IsNullOrEmpty(s));
#if DEBUG_ALC_WRAPPER
            CoreCLRHost.Log($"[ALCWrapper:#{Name}] AddSearchPath {lastPath}");
#endif
            m_Paths.Add(lastPath);
        }

        /// <summary>
        /// Resolve full assembly path if assembly can be found in the lookup paths of the ALC.
        /// </summary>
        /// <param name="assemblyName">Assembly name</param>
        /// <returns>Full path to the assembly or null if not found</returns>
        string ResolveAssemblyFullPath(string assemblyName)
        {
            foreach (string p in m_Paths)
            {
                string assemblyPath = Path.Combine(p, $"{assemblyName}.dll");
                if (File.Exists(assemblyPath))
                {
#if DEBUG_ALC_WRAPPER
                    CoreCLRHost.Log($"[ALCWrapper:#{Name}] resolved {assemblyName}");
#endif
                    return assemblyPath;
                }
            }

            return null;
        }

        public Assembly FindAssemblyByName(string assemblySimpleName)
        {
            foreach (Assembly a in Assemblies)
            {
                if (Path.GetFileNameWithoutExtension(a.GetLoadedModules(getResourceModules: true)[0].Name).Equals(assemblySimpleName))
                    return a;
            }

            return null;
        }


        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern string InvokeFindPluginCallback(string path);

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string pluginPath = InvokeFindPluginCallback(unmanagedDllName);
#if DEBUG_ALC_WRAPPER
            CoreCLRHost.Log($"[ALCWrapper:#{Name}] LoadUnmanagedDll {unmanagedDllName} -> {pluginPath}");
#endif
            if (!string.IsNullOrEmpty(pluginPath) && Path.IsPathRooted(pluginPath))
                return LoadUnmanagedDllFromPath(pluginPath);

            return IntPtr.Zero;
        }

        public unsafe Assembly CallLoadFromAssemblyData(byte* data, long size)
        {
#if DEBUG_ALC_WRAPPER
            CoreCLRHost.Log($"[ALCWrapper:#{Name}] CallLoadFromAssemblyData {(IntPtr)data} {size}");
#endif
            using (var mem = new UnmanagedMemoryStream(data, size, size, FileAccess.Read))
            {
                return LoadFromStream(mem);
            }
        }

        public Assembly CallLoadFromAssemblyPath(string path)
        {
        #if DEBUG_ALC_WRAPPER
            CoreCLRHost.Log($"[ALCWrapper:#{Name}] CallLoadFromAssemblyPath {path}");
        #endif
            Assembly asm = LoadFromAssemblyPath(path);

            // If the directory containing the assembly we want to load has not been added to user or system paths yet,
            // add it to user paths, so we can resolve any potential dlls next to it, which this assembly might depend on.
            string parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent) && !m_Paths.Contains(parent))
            {
                if (m_SystemAlc == null || !m_SystemAlc.m_Paths.Contains(parent))
                    m_Paths.Add(parent);
            }

            return asm;
        }

        protected override Assembly Load(AssemblyName name)
        {
#if DEBUG_ALC_WRAPPER
            CoreCLRHost.Log($"[ALCWrapper:#{Name}] Load assembly {name}");
#endif
            string assemblyPath;
            if (m_SystemAlc != null)
            {
                assemblyPath = m_SystemAlc.ResolveAssemblyFullPath(name.Name);
                if (assemblyPath != null)
                {
                    // This is a system assembly - those cannot be cleanly unloaded - load it into the root ALC.
                    return m_SystemAlc.Load(name);
                }
            }

            // Check if the assembly already loaded in this ALC.
            foreach (var assembly in Assemblies)
            {
                if (assembly.GetName() == name)
                    return assembly;
            }

            // Resolve full path and load the assembly.
            assemblyPath = ResolveAssemblyFullPath(name.Name);
            if (assemblyPath == null)
            {
            #if DEBUG_ALC_WRAPPER
                CoreCLRHost.Log($"[ALCWrapper:#{Name}] assembly {name} not found.");
            #endif
                return null;
            }

            try
            {
#if DEBUG_ALC_WRAPPER
                CoreCLRHost.Log($"[ALCWrapper:#{Name}] Load assembly {name} from {assemblyPath}");
#endif
                var result = LoadFromAssemblyPath(assemblyPath);
                return result;
            }
            catch (Exception e)
            {
                CoreCLRHost.Log($"[ALCWrapper:#{Name}] Failed loading {name} from {assemblyPath}:\n{e}");
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
                CoreCLRHost.Log($"Caught {e} calling AppDomain.CurrentDomain.DomainUnload.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static WeakReference InitUnload(ALCWrapper alc)
        {
        #if DEBUG_ALC_WRAPPER
            CoreCLRHost.Log($"[ALCWrapper:#{alc.Name}] Unload");
        #endif

            var alcWeakRef = new WeakReference(alc);
            alc.Unload();
            return alcWeakRef;
        }

        public static Exception FinishUnload(WeakReference alcWeakRef)
        {
            for (int i = 0; alcWeakRef.IsAlive && (i < 10); i++)
            {
            #if DEBUG_ALC_WRAPPER
                CoreCLRHost.Log($"[ALCWrapper] Unload attempt: {i}");
            #endif
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

        #if DEBUG_ALC_WRAPPER
            CoreCLRHost.Log($"[ALCWrapper] FinishUnload result: {!alcWeakRef.IsAlive}");
        #endif

            if (alcWeakRef.IsAlive)
            {
                return new AssemblyLoadContextUnloadException();
            }
            return null;
        }
    }
}
