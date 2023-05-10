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
    private static Dictionary<Assembly, AssemblyPair> m_assemblies = new Dictionary<Assembly, AssemblyPair>();

    private class AssemblyPair
    {
        public IntPtr handle;
        public string name;
    }

    internal static int InitMethod(HostStruct* functionStruct, int structSize)
    {
        if (Marshal.SizeOf<HostStruct>() != structSize)
            throw new Exception($"Invalid struct size, Managed was {Marshal.SizeOf<HostStruct>()} and Native was {structSize}");

        InitState();

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
        var ptr = GetHandleForAssembly(assembly);
        return ptr;
    }

    [NativeFunction(NativeFunctionOptions.DoNotGenerate)]
    public static IntPtr /*Assembly*/ load_assembly_from_path(byte* path, int length)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyPath(Encoding.UTF8.GetString(path, length));
        var ptr = GetHandleForAssembly(assembly);
        return ptr;
    }

    private static IntPtr GetHandleForAssembly(Assembly assembly)
    {
        if (!m_assemblies.TryGetValue(assembly, out var pair))
        {
            pair = new AssemblyPair() { handle = GCHandle.ToIntPtr(GCHandle.Alloc(assembly)), name = assembly.GetName().Name};
            m_assemblies.Add(assembly, pair);
        }

        return pair.handle;
    }

    private static Assembly GetAssemblyForHandle(IntPtr ptr)
    {
        return (Assembly)GCHandle.FromIntPtr(ptr).Target;
    }

    [NativeFunction(NativeFunctionOptions.DoNotGenerate)]
    [return: NativeCallbackType("MonoClass*")]
    public static IntPtr class_from_name(
        [NativeCallbackType("MonoImage*")] IntPtr image,
        [NativeCallbackType("const char*")] sbyte* name_space,
        [NativeCallbackType("const char*")] sbyte* name,
        bool ignoreCase)
    {
        Assembly assembly = GetAssemblyForHandle(image);
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
            Assembly assembly = GetAssemblyForHandle(image);
            return assembly.GetTypes().Length;
        }

        return 0;
    }

    [return: NativeCallbackType("MonoClass*")]
    public static IntPtr unity_class_get([NativeCallbackType("MonoImage*")] IntPtr image, uint type_token)
    {
        Assembly assembly = GetAssemblyForHandle(image);
        return RuntimeTypeHandle.ToIntPtr(assembly.Modules.Single().ModuleHandle.GetRuntimeTypeHandleFromMetadataToken((int)type_token));
    }

    [return: NativeCallbackType("MonoImage*")]
    public static IntPtr class_get_image([NativeCallbackType("MonoClass*")] IntPtr klass)
    {
        var type = klass.TypeFromHandleIntPtr();
        return GetHandleForAssembly(type.Assembly);
    }

    [return: NativeCallbackType("MonoImage*")]
    public static IntPtr image_loaded([NativeCallbackType("const char*")] sbyte* name)
    {
        string sname = new(name);

        // Use reflection to get the assembly name?

        foreach (var context in AssemblyLoadContext.All)
        {
            foreach (var asm in context.Assemblies)
            {
                if (Path.GetFileNameWithoutExtension(asm.Modules.Single().Name).Equals(sname))
                    return GetHandleForAssembly(asm);
            }
        }

        //Do we need to search and get the loaded instance?
        foreach (var asm in alcWrapper.Assemblies)
        {
            if (Path.GetFileNameWithoutExtension(asm.Modules.Single().Name).Equals(sname))
                return GetHandleForAssembly(asm);
        }

        foreach (var asm in AssemblyLoadContext.Default.Assemblies)
        {
            if (Path.GetFileNameWithoutExtension(asm.Modules.Single().Name).Equals(sname))
                return GetHandleForAssembly(asm);
        }

        return nint.Zero;
    }

    public unsafe struct MonoAssemblyName
    {
        public sbyte* name;
        public sbyte* culture;
        public sbyte* hash_value;
        public byte* public_key;
        // string of 16 hex chars + 1 NULL
        public fixed byte public_key_token[17];
        public uint hash_alg;
        public uint hash_len;
        public uint flags;
        public UInt16 major, minor, build, revision;
        // only used and populated by newer Mono
        public UInt16 arch;
        public byte without_version;
        public byte without_culture;
        public byte without_public_key_token;
    }

    [return: NativeCallbackType("MonoAssembly*")]
    public static IntPtr assembly_loaded([NativeCallbackType("MonoAssemblyName*")] MonoAssemblyName* aname)
    {
        return image_loaded(aname->name);
    }

    [return: NativeCallbackType("MonoImage*")]
    [NativeFunction(NativeFunctionOptions.DoNotGenerate)]
    public static IntPtr get_corlib()
    {
        return GetHandleForAssembly(typeof(Object).Assembly);
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
        [NativeCallbackType("gpointer")] IntPtr val) =>
        Marshal.PtrToStructure(val, klass.TypeFromHandleIntPtr()).ToNativeRepresentation();

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

    public static bool unity_class_has_attribute(
        [NativeCallbackType("MonoClass*")] IntPtr klass,
        [NativeCallbackType("MonoClass*")] IntPtr attr_klass)
    {
        return Attribute.GetCustomAttribute(klass.TypeFromHandleIntPtr(), attr_klass.TypeFromHandleIntPtr()) != null;
    }

    public static bool unity_assembly_has_attribute(
        [NativeCallbackType("MonoAssembly*")] IntPtr assembly,
        [NativeCallbackType("MonoClass*")] IntPtr attr_klass)
    {
        return Attribute.GetCustomAttribute(GetAssemblyForHandle(assembly), attr_klass.TypeFromHandleIntPtr()) != null;
    }

    public static bool unity_method_has_attribute(
        [NativeCallbackType("MonoMethod*")] IntPtr method,
        [NativeCallbackType("MonoClass*")] IntPtr attr_class)
    {
        MethodInfo mi = Unsafe.As<MethodInfo>(method);
        return mi.GetCustomAttribute(attr_class.TypeFromHandleIntPtr()) != null;
    }

    [return: NativeCallbackType("const char*")]
    public static IntPtr image_get_name([NativeCallbackType("MonoImage*")] IntPtr image)
    {
        if (m_assemblies.TryGetValue(GetAssemblyForHandle(image), out var pair))
            return Marshal.StringToHGlobalAnsi(pair.name);

        return IntPtr.Zero;
    }

    [return: NativeCallbackType("MonoReflectionMethod*")]
    public static IntPtr method_get_object(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)] [NativeCallbackType("MonoDomain*")]
        IntPtr domain,
        [NativeCallbackType("MonoMethod*")] IntPtr method,
        [NativeCallbackType("MonoClass*")] IntPtr refclass) =>
        MethodInfo.GetMethodFromHandle(method.MethodHandleFromHandleIntPtr(), RuntimeTypeHandle.FromIntPtr(refclass)).ToNativeRepresentation();
    
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
        IntPtr field) =>
        FieldInfo.GetFieldFromHandle(field.FieldHandleFromHandleIntPtr(), RuntimeTypeHandle.FromIntPtr(klass)).ToNativeRepresentation();

    private static StringPtr StringToPtr(string s)
    {
        // Return raw object pointer for now with the NullGC.
        // This will become a GCHandle in the future.
        return Unsafe.As<string, StringPtr>(ref s);
    }
}
