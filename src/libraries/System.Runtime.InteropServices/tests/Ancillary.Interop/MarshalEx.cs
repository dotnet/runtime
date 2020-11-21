
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Marshalling helper methods that will likely live in S.R.IS.Marshal
    /// when we integrate our APIs with dotnet/runtime.
    /// </summary>
    public static class MarshalEx
    {
        /// <summary>
        /// Create an instance of the given <typeparamref name="TSafeHandle"/>.
        /// </summary>
        /// <typeparam name="TSafeHandle">Type of the SafeHandle</typeparam>
        /// <returns>New instance of <typeparamref name="TSafeHandle"/></returns>
        /// <remarks>
        /// The <typeparamref name="TSafeHandle"/> must be non-abstract and have a parameterless constructor.
        /// </remarks>
        public static TSafeHandle CreateSafeHandle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.NonPublicConstructors)]TSafeHandle>()
            where TSafeHandle : SafeHandle
        {
            if (typeof(TSafeHandle).IsAbstract || typeof(TSafeHandle).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, Type.EmptyTypes, null) == null)
            {
                throw new MissingMemberException($"The safe handle type '{typeof(TSafeHandle).FullName}' must be a non-abstract type with a parameterless constructor.");
            }

            TSafeHandle safeHandle = (TSafeHandle)Activator.CreateInstance(typeof(TSafeHandle), nonPublic: true)!;
            return safeHandle;
        }

        /// <summary>
        /// Sets the handle of <paramref name="safeHandle"/> to the specified <paramref name="handle"/>.
        /// </summary>
        /// <param name="safeHandle"><see cref="SafeHandle"/> instance to update</param>
        /// <param name="handle">Pre-existing handle</param>
        public static void SetHandle(SafeHandle safeHandle, IntPtr handle)
        {            
            typeof(SafeHandle).GetMethod("SetHandle", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(safeHandle, new object[] { handle });
        }
    }
}
