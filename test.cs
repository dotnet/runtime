using System;
using System.Runtime.InteropServices;

class Program {
    [DllImport("kernel32.dll")] static extern void Sleep(uint dwMilliseconds);
    static unsafe void Main() {
        Sleep(0);
        delegate* unmanaged<void> p = &Target;
        p();
    }
    [UnmanagedCallersOnly] static void Target() {
        Console.WriteLine(new System.Diagnostics.StackTrace().ToString());
    }
}
