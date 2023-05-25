using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Serialization;
using System.Text;

[assembly:DisableRuntimeMarshalling]

namespace Unity.CoreCLRHelpers;

using StringPtr = IntPtr;
static unsafe partial class CoreCLRHost
{
    static ALCWrapper alcWrapper;
    static FieldInfo assemblyHandleField;
    private static HostStructNative* _hostStructNative;
    private static readonly Dictionary<Assembly, AssemblyPair> m_assemblies = new ();

    private readonly struct AssemblyPair
    {
        public AssemblyPair(IntPtr handle, IntPtr name)
        {
            this.handle = handle;
            this.name = name;
        }
        public readonly IntPtr handle;
        public readonly IntPtr name;
    }

    internal static int InitMethod(HostStruct* functionStruct, int structSize, HostStructNative* functionStructNative, int structSizeNative)
    {
        if (Marshal.SizeOf<HostStruct>() != structSize)
            throw new Exception($"Invalid struct size {nameof(HostStruct)}, Managed was {Marshal.SizeOf<HostStruct>()} and Native was {structSize}");

        if (Marshal.SizeOf<HostStructNative>() != structSizeNative)
            throw new Exception($"Invalid struct size for {nameof(HostStructNative)}, Managed was {Marshal.SizeOf<HostStructNative>()} and Native was {structSizeNative}");

        InitState();

        _hostStructNative = functionStructNative;

        InitHostStruct(functionStruct);

        return 0;
    }

    internal static void InitState()
    {
        alcWrapper = new ALCWrapper();
        assemblyHandleField = typeof(Assembly).Assembly.GetType("System.Reflection.RuntimeAssembly").GetField("m_assembly", BindingFlags.Instance | BindingFlags.NonPublic);
        if (assemblyHandleField == null)
            throw new Exception("Failed to find RuntimeAssembly.m_assembly field.");
    }

    static partial void InitHostStruct(HostStruct* functionStruct);

    [NativeFunction(NativeFunctionOptions.DoNotGenerate)]
    public static IntPtr /*Assembly*/ load_assembly_from_data(byte* data, long size)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyData(data, size);
        return GetPairForAssembly(assembly).handle;
    }

    [NativeFunction(NativeFunctionOptions.DoNotGenerate)]
    public static IntPtr /*Assembly*/ load_assembly_from_path(byte* path, int length)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyPath(Encoding.UTF8.GetString(path, length));
        return GetPairForAssembly(assembly).handle;
    }

    private static AssemblyPair GetPairForAssembly(Assembly assembly)
    {
        lock (m_assemblies)
        {
            if (!m_assemblies.TryGetValue(assembly, out var pair))
            {
                pair = new AssemblyPair(GCHandle.ToIntPtr(GCHandle.Alloc(assembly)),
                    Marshal.StringToHGlobalAnsi(assembly.GetName().Name));
                m_assemblies[assembly] = pair;
            }
            return pair;
        }
    }

    [NativeFunction(NativeFunctionOptions.DoNotGenerate)]
    [return: NativeCallbackType("MonoClass*")]
    public static IntPtr class_from_name(
        [NativeCallbackType("MonoImage*")] IntPtr image,
        [NativeCallbackType("const char*")] sbyte* name_space,
        [NativeCallbackType("const char*")] sbyte* name,
        bool ignoreCase)
    {
        Assembly assembly = image.AssemblyFromGCHandleIntPtr();
        var ns = new string(name_space);
        var klass_name = new string(name);
        string assemblyQualified = $"{ns}.{klass_name}";
        assemblyQualified = assemblyQualified.Replace('/', '+');

        return assembly.GetType(assemblyQualified, false, ignoreCase).TypeHandleIntPtr();
    }

    public static int image_get_table_rows([NativeCallbackType("MonoImage*")] IntPtr image, int table_id)
    {
        const int MONO_TABLE_TYPEDEF = 2;
        if (table_id == MONO_TABLE_TYPEDEF)
        {
            Assembly assembly = image.AssemblyFromGCHandleIntPtr();
            return assembly.GetTypes().Length;
        }

        return 0;
    }

    [return: NativeCallbackType("MonoClass*")]
    public static IntPtr unity_class_get([NativeCallbackType("MonoImage*")] IntPtr image, uint type_token)
    {
        Assembly assembly = image.AssemblyFromGCHandleIntPtr();
        return RuntimeTypeHandle.ToIntPtr(assembly.GetLoadedModules(getResourceModules: true)[0].ModuleHandle.GetRuntimeTypeHandleFromMetadataToken((int)type_token));
    }

    [return: NativeCallbackType("MonoImage*")]
    public static IntPtr class_get_image([NativeCallbackType("MonoClass*")] IntPtr klass)
    {
        var type = klass.TypeFromHandleIntPtr();
        return GetPairForAssembly(type.Assembly).handle;
    }

    [return: NativeCallbackType("MonoImage*")]
    public static IntPtr image_loaded([NativeCallbackType("const char*")] sbyte* name)
    {
        string sname = new(name);
        foreach (var context in AssemblyLoadContext.All)
        {
            foreach (var asm in context.Assemblies)
            {
                if (Path.GetFileNameWithoutExtension(asm.GetLoadedModules(getResourceModules: true)[0].Name).Equals(sname))
                    return GetPairForAssembly(asm).handle;
            }
        }

        foreach (var asm in alcWrapper.Assemblies)
        {
            if (Path.GetFileNameWithoutExtension(asm.GetLoadedModules(getResourceModules: true)[0].Name).Equals(sname))
                return GetPairForAssembly(asm).handle;
        }

        return nint.Zero;
    }

    [return: NativeCallbackType("MonoAssembly*")]
    public static IntPtr assembly_loaded([NativeCallbackType("MonoAssemblyName*")] MonoAssemblyName* aname)
        => image_loaded(aname->name);

    [return: NativeCallbackType("MonoImage*")]
    [NativeFunction(NativeFunctionOptions.DoNotGenerate)]
    public static IntPtr get_corlib()
    {
        return GetPairForAssembly(typeof(Object).Assembly).handle;
    }

    [return: NativeWrapperType("MonoString*")]
    [return: NativeCallbackType("ManagedStringPtr_t")]
    public static StringPtr string_from_utf16([NativeCallbackType("const gunichar2*")] ushort* text)
    {
        var s = new string((char*)text);
        return StringToPtr(s);
    }

    [return: NativeWrapperType("MonoString*")]
    [return: NativeCallbackType("ManagedStringPtr_t")]
    public static StringPtr string_new_len(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoDomain*")] void* domain /* unused */,
        [NativeCallbackType("const char*")] sbyte* text, uint length)
    {
        var s = new string(text, 0, (int)length, Encoding.UTF8);
        return StringToPtr(s);
    }

    [return: NativeWrapperType("MonoString*")]
    [return: NativeCallbackType("ManagedStringPtr_t")]
    public static StringPtr string_new_utf16(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoDomain*")] void* domain /* unused */,
        ushort* text, [NativeCallbackType("gint32")] int length)
    {
        var s = new string((char*)text, 0, length);
        return StringToPtr(s);
    }

    [return: NativeCallbackType("uintptr_t")]
    public static IntPtr gchandle_new_v2([NativeCallbackType("MonoObject*")] IntPtr obj, bool pinned)
    {
        GCHandle handle = GCHandle.Alloc(obj.ToManagedRepresentation(), pinned ? GCHandleType.Pinned : GCHandleType.Normal);
        return GCHandle.ToIntPtr(handle);
    }

    [return: NativeCallbackType("uintptr_t")]
    public static IntPtr gchandle_new_weakref_v2([NativeCallbackType("MonoObject*")] IntPtr obj, bool track_resurrection)
    {
        GCHandle handle = GCHandle.Alloc(obj.ToManagedRepresentation(), track_resurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
        return GCHandle.ToIntPtr(handle);
    }

    [return: NativeWrapperType("MonoObject*")]
    public static IntPtr gchandle_get_target_v2([NativeWrapperType("uintptr_t")] IntPtr handleIn)
        => handleIn.ToGCHandle().Target.ToNativeRepresentation();

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr object_isinst([NativeCallbackType("MonoObject*")] IntPtr obj, [NativeCallbackType("MonoClass*")] IntPtr klass)
        => obj.ToManagedRepresentation().GetType().IsAssignableTo(klass.TypeFromHandleIntPtr()) ? obj : nint.Zero;

    [return: NativeCallbackType("MonoClass*")]
    public static IntPtr object_get_class([NativeCallbackType("MonoObject*")] IntPtr obj)
        => obj.ToManagedRepresentation().TypeHandleIntPtr();

    [NativeFunction("coreclr_class_from_systemtypeinstance")]
    [return: NativeCallbackType("MonoClass*")]
    public static IntPtr class_from_systemtypeinstance(
        [ManagedWrapperOptions(ManagedWrapperOptions.Custom, nameof(Type))]
        [NativeCallbackType("MonoObject*")]
        IntPtr systemTypeInstance)
        => ((Type)systemTypeInstance.ToManagedRepresentation()).TypeHandle.Value;

    [return: NativeCallbackType("MonoArray*")]
    public static IntPtr array_new(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoDomain*")] IntPtr domain,
        [NativeCallbackType("MonoClass*")] IntPtr klass, [NativeCallbackType("guint32")] int n)
        => Array.CreateInstance(klass.TypeFromHandleIntPtr(), n).ToNativeRepresentation();

    [return: NativeCallbackType("MonoArray*")]
    public static IntPtr unity_array_new_2d(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoDomain*")] IntPtr domain,
        [NativeCallbackType("MonoClass*")] IntPtr klass,
        [NativeCallbackType("size_t")] int size0, [NativeCallbackType("size_t")] int size1)
        => Array.CreateInstance(klass.TypeFromHandleIntPtr(), size0, size1).ToNativeRepresentation();

    [return: NativeCallbackType("MonoArray*")]
    public static IntPtr unity_array_new_3d(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoDomain*")] IntPtr domain,
        [NativeCallbackType("MonoClass*")] IntPtr klass,
        [NativeCallbackType("size_t")] int size0, [NativeCallbackType("size_t")] int size1, [NativeCallbackType("size_t")] int size2)
        => Array.CreateInstance(klass.TypeFromHandleIntPtr(), size0, size1, size2).ToNativeRepresentation();

    [NativeFunction("coreclr_array_length")]
    [return: NativeCallbackType("int")]
    public static int array_length([NativeCallbackType("MonoArray*")] IntPtr array)
        => ((Array)array.ToManagedRepresentation()).Length;

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr value_box(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)] [NativeCallbackType("MonoDomain*")]
        IntPtr domain,
        [NativeCallbackType("MonoClass*")] IntPtr klass,
        [NativeCallbackType("gpointer")] IntPtr val)
        => Marshal.PtrToStructure(val, klass.TypeFromHandleIntPtr()).ToNativeRepresentation();

    [return: NativeCallbackType("MonoException*")]
    public static IntPtr get_exception_argument_null([NativeCallbackType("const char*")] sbyte* arg)
    {
        var ns = new string(arg);
        return new ArgumentNullException(ns).ToNativeRepresentation();
    }

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr object_new(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoDomain*")] IntPtr domain,
        [NativeCallbackType("MonoClass*")] IntPtr klass)
        => FormatterServices.GetUninitializedObject(klass.TypeFromHandleIntPtr()).ToNativeRepresentation();

    [return: NativeCallbackType("MonoException*")]
    public static IntPtr exception_from_name_msg(
        [NativeCallbackType("MonoImage*")] IntPtr image,
        [NativeCallbackType("const char*")] sbyte* name_space,
        [NativeCallbackType("const char*")] sbyte* name,
        [NativeCallbackType("const char*")] sbyte* msg)
    {
        Type type = class_from_name(image, name_space, name, false).TypeFromHandleIntPtr();
        return Activator.CreateInstance(type, new string(msg)).ToNativeRepresentation();
    }

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr type_get_object(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)] [NativeCallbackType("MonoDomain*")]
        IntPtr domain,
        [NativeCallbackType("MonoType*")] IntPtr type)
        => type.TypeFromHandleIntPtr().ToNativeRepresentation();

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr unity_class_get_attribute(
        [NativeCallbackType("MonoClass*")] IntPtr klass,
        [NativeCallbackType("MonoClass*")] IntPtr attr_klass)
    {
        Attribute[] attrs = Attribute.GetCustomAttributes(klass.TypeFromHandleIntPtr(), attr_klass.TypeFromHandleIntPtr());
        // Need to do it this way to mimic old behavior where we only cared about the first found
        return attrs.Length == 0 ? IntPtr.Zero : attrs[0].ToNativeRepresentation();
    }

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr unity_assembly_get_attribute(
        [NativeCallbackType("MonoAssembly*")] IntPtr assembly,
        [NativeCallbackType("MonoClass*")] IntPtr attr_klass)
    {
        Attribute[] attrs = Attribute.GetCustomAttributes(assembly.AssemblyFromGCHandleIntPtr(), attr_klass.TypeFromHandleIntPtr());
        return attrs.Length == 0 ? IntPtr.Zero : attrs[0].ToNativeRepresentation();
    }

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr unity_method_get_attribute(
        [NativeCallbackType("MonoMethod*")] IntPtr method,
        [NativeCallbackType("MonoClass*")] IntPtr attr_class)
    {
        MethodBase mb = MethodBase.GetMethodFromHandle(method.MethodHandleFromHandleIntPtr());
        if (mb == null)
            return IntPtr.Zero;
        Attribute[] attrs = mb.GetCustomAttributes(attr_class.TypeFromHandleIntPtr()).ToArray();
        return attrs.Length == 0 ? IntPtr.Zero : attrs[0].ToNativeRepresentation();
    }

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr unity_field_get_attribute(
        [NativeCallbackType("MonoClass*")] IntPtr klass,
        [NativeCallbackType("MonoClassField*")] IntPtr field,
        [NativeCallbackType("MonoClass*")] IntPtr attr_class)
    {
        Attribute[] attrs = FieldInfo.GetFieldFromHandle(field.FieldHandleFromHandleIntPtr(), RuntimeTypeHandle.FromIntPtr(klass))
            .GetCustomAttributes(attr_class.TypeFromHandleIntPtr()).ToArray();
        return attrs.Length == 0 ? IntPtr.Zero : attrs[0].ToNativeRepresentation();
    }

    [return: NativeCallbackType("const char*")]
    public static IntPtr image_get_name([NativeCallbackType("MonoImage*")] IntPtr image)
        => GetPairForAssembly(image.AssemblyFromGCHandleIntPtr()).name;

    [return: NativeCallbackType("MonoReflectionMethod*")]
    public static IntPtr method_get_object(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)] [NativeCallbackType("MonoDomain*")]
        IntPtr domain,
        [NativeCallbackType("MonoMethod*")] IntPtr method,
        [NativeCallbackType("MonoClass*")] IntPtr refclass)
        => MethodInfo.GetMethodFromHandle(method.MethodHandleFromHandleIntPtr(), RuntimeTypeHandle.FromIntPtr(refclass)).ToNativeRepresentation();

    [return: NativeCallbackType("void*")]
    public static IntPtr unity_method_get_function_pointer([NativeCallbackType("MonoMethod*")] IntPtr method)
    {
        var handle = method.MethodHandleFromHandleIntPtr();
        return handle.GetFunctionPointer();
    }

    [return: NativeCallbackType("MonoReflectionField*")]
    public static IntPtr field_get_object(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)] [NativeCallbackType("MonoDomain*")]
        IntPtr domain,
        [NativeCallbackType("MonoClass*")] IntPtr klass,
        [NativeCallbackType("MonoClassField*")]
        IntPtr field)
        => FieldInfo.GetFieldFromHandle(field.FieldHandleFromHandleIntPtr(), RuntimeTypeHandle.FromIntPtr(klass)).ToNativeRepresentation();

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr assembly_get_object(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)] [NativeCallbackType("MonoDomain*")]
        IntPtr domain,
        [NativeCallbackType("MonoAssembly*")]
        IntPtr assembly)
        => assembly.AssemblyFromGCHandleIntPtr().ToNativeRepresentation();

    [return: NativeCallbackType("MonoMethod*")]
    public static IntPtr object_get_virtual_method([NativeCallbackType("MonoObject*")] IntPtr obj,
        [NativeCallbackType("MonoMethod*")] IntPtr method)
    {
        var managedObject = obj.ToManagedRepresentation();
        var baseMethodInfo = (MethodInfo)MethodInfo.GetMethodFromHandle(method.MethodHandleFromHandleIntPtr());

        if (baseMethodInfo == null)
            throw new ArgumentException($"Invalid method handle");

        Type type = managedObject.GetType();

        if (type == baseMethodInfo.DeclaringType)
            return method;

        // Mono's implementation of object_get_virtual_method had this check.
        if (baseMethodInfo.IsFinal || !baseMethodInfo.IsVirtual)
            return method;

        if (!baseMethodInfo.DeclaringType!.IsInterface)
        {
            var bindingFlags = BindingFlags.Instance;
            if (baseMethodInfo.IsPublic)
                bindingFlags |= BindingFlags.Public;
            else
                bindingFlags |= BindingFlags.NonPublic;

            var objectMethods = type.GetMethods(bindingFlags);

            foreach (var objectMethod in objectMethods)
            {
                var objectBaseDefinition = objectMethod.GetBaseDefinition();
                // GetBaseDefinition will return the base most method in the hierarchy.  Which means, it won't be the same as the baseMethodInfo if there are any intermediate overrides
                // because of this there are 2 scenarios we need to handle
                // 1) When the GetBaseDefinition returns baseMethodInfo, that is the most straight forward case
                // 2) If (1) is not true, then we need to handle the situation where there is an intermediate override.  In this case,
                //      if objectMethod.GetBaseDefinition() shares the same value as baseMethodInfo.GetBaseDefinition() then we have found the method we are looking for
                if (objectBaseDefinition == baseMethodInfo || objectBaseDefinition == baseMethodInfo.GetBaseDefinition())
                    return objectMethod.MethodHandle.MethodHandleIntPtr();
            }
        }
        else
        {
            InterfaceMapping ifaceMap;
            try
            {
                ifaceMap = type.GetInterfaceMap(baseMethodInfo.DeclaringType);
            }
            catch (ArgumentException)
            {
                // Type does not implement the interface
                return IntPtr.Zero;
            }

            for (int i = 0; i < ifaceMap.InterfaceMethods.Length; i++)
            {
                MethodInfo ifaceMethod = ifaceMap.InterfaceMethods[i];
                if (ifaceMethod == baseMethodInfo)
                {
                    return ifaceMap.TargetMethods[i].MethodHandle.MethodHandleIntPtr();
                }
            }
        }

        return IntPtr.Zero;
    }

    static void Log(string message)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(message);
        fixed (byte* p = bytes)
            _hostStructNative->unity_log(p);
    }

    private static StringPtr StringToPtr(string s)
    {
        // Return raw object pointer for now with the NullGC.
        // This will become a GCHandle in the future.
        return Unsafe.As<string, StringPtr>(ref s);
    }
}
