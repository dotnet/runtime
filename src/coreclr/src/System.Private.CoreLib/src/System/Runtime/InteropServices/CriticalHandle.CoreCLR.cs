using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace System.Runtime.InteropServices
{
    public abstract partial class CriticalHandle : CriticalFinalizerObject, IDisposable
    {
        void ReleaseHandleCore()
        {
            // Save last error from P/Invoke in case the implementation of
            // ReleaseHandle trashes it (important because this ReleaseHandle could
            // occur implicitly as part of unmarshaling another P/Invoke).
            int lastError = Marshal.GetLastWin32Error();

            if (!ReleaseHandle())
                FireCustomerDebugProbe();

            Marshal.SetLastWin32Error(lastError);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void FireCustomerDebugProbe();
    }
}