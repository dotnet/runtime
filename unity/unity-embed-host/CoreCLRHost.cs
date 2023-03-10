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

    public static int InitMethod(HostStruct* functionStruct, int structSize)
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

    public static IntPtr /*Assembly*/ CallLoadFromAssemblyData(byte* data, long size)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyData(data, size);
        return (IntPtr)assemblyHandleField.GetValue(assembly);
    }

    public static IntPtr /*Assembly*/ CallLoadFromAssemblyPath(byte* path, int length)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyPath(Encoding.UTF8.GetString(path, length));
        return (IntPtr)assemblyHandleField.GetValue(assembly);

    }

    public static StringPtr string_from_utf16(ushort* text)
    {
        var s = new string((char*)text);
        return StringToPtr(s);

    }

    public static StringPtr string_new_len(void* domain /* unused */, sbyte* text, uint length)
    {
        var s = new string(text, 0, (int)length, Encoding.UTF8);
        return StringToPtr(s);

    }

    public static StringPtr string_new_utf16(void* domain /* unused */, ushort* text, uint length)
    {
        var s = new string((char*)text, 0, (int)length);
        return StringToPtr(s);

    }

    public static IntPtr gchandle_new_v2(IntPtr obj, bool pinned)
    {
        GCHandle handle = GCHandle.Alloc(Unsafe.As<IntPtr, Object>(ref obj), pinned ? GCHandleType.Pinned : GCHandleType.Normal);
        return GCHandle.ToIntPtr(handle);
    }

    public static IntPtr gchandle_new_weakref_v2(IntPtr obj, bool track_resurrection)
    {
        GCHandle handle = GCHandle.Alloc(Unsafe.As<IntPtr, Object>(ref obj), track_resurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
        return GCHandle.ToIntPtr(handle);
    }

    public static IntPtr gchandle_get_target_v2(IntPtr handleIn)
    {
        GCHandle handle = GCHandle.FromIntPtr(handleIn);
        object obj = handle.Target;
        return Unsafe.As<object, IntPtr>(ref obj);
    }

    static StringPtr StringToPtr(string s)
    {
        // Return raw object pointer for now with the NullGC.
        // This will become a GCHandle in the future.
        return Unsafe.As<string, StringPtr>(ref s);
    }
}
