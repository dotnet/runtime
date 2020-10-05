
using System.Reflection;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Marshalling helper methods that will likely live in S.R.IS.Marshal
    /// when we integrate our APIs with dotnet/runtime.
    /// </summary>
    public static class MarshalEx
    {
        public static TSafeHandle CreateSafeHandle<TSafeHandle>()
            where TSafeHandle : SafeHandle
        {
            if (typeof(TSafeHandle).IsAbstract || typeof(TSafeHandle).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, Type.EmptyTypes, null) == null)
            {
                throw new MissingMemberException($"The safe handle type '{typeof(TSafeHandle).FullName}' must be a non-abstract type with a parameterless constructor.");
            }

            TSafeHandle safeHandle = (TSafeHandle)Activator.CreateInstance(typeof(TSafeHandle), nonPublic: true)!;
            return safeHandle;
        }

        public static void SetHandle(SafeHandle safeHandle, IntPtr handle)
        {            
            typeof(SafeHandle).GetMethod("SetHandle", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(safeHandle, new object[] { handle });
        }
    }
}
