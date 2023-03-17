using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[assembly:DisableRuntimeMarshalling]

namespace Unity.CoreCLRHelpers;

using StringPtr = IntPtr;
static unsafe partial class CoreCLRHost
{
    static ALCWrapper alcWrapper;
    static FieldInfo assemblyHandleField;

    internal static int InitMethod(HostStruct* functionStruct, int structSize)
    {
        if (Marshal.SizeOf<HostStruct>() != structSize)
            throw new Exception("Invalid struct size");

        alcWrapper = new ALCWrapper();
        assemblyHandleField = typeof(Assembly).Assembly.GetType("System.Reflection.RuntimeAssembly").GetField("m_assembly", BindingFlags.Instance | BindingFlags.NonPublic);
        if (assemblyHandleField == null)
            throw new Exception("Failed to find RuntimeAssembly.m_assembly field.");

        InitHostStruct(functionStruct);

        return 0;
    }

    static partial void InitHostStruct(HostStruct* functionStruct);

    [NoNativeWrapper]
    public static IntPtr /*Assembly*/ load_assembly_from_data(byte* data, long size)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyData(data, size);
        return (IntPtr)assemblyHandleField.GetValue(assembly);
    }

    [NoNativeWrapper]
    public static IntPtr /*Assembly*/ load_assembly_from_path(byte* path, int length)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyPath(Encoding.UTF8.GetString(path, length));
        return (IntPtr)assemblyHandleField.GetValue(assembly);

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
    public static StringPtr string_new_len([NativeCallbackType("MonoDomain*")] void* domain /* unused */, [NativeCallbackType("const char*")] sbyte* text, uint length)
    {
        var s = new string(text, 0, (int)length, Encoding.UTF8);
        return StringToPtr(s);

    }

    [return: NativeWrapperType("MonoString*")]
    [return: NativeCallbackType("ManagedStringPtr_t")]
    public static StringPtr string_new_utf16([NativeCallbackType("MonoDomain*")] void* domain /* unused */, ushort* text, [NativeCallbackType("gint32")] int length)
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

    static StringPtr StringToPtr(string s)
    {
        // Return raw object pointer for now with the NullGC.
        // This will become a GCHandle in the future.
        return Unsafe.As<string, StringPtr>(ref s);
    }
}
