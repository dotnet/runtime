using System;
  
unsafe class Program
{
        public static int Main(string[] args)
        {
                return (int)(IntPtr)Generic<int>.GetPtr();
        }
}

unsafe class Generic<T> where T : unmanaged
{
        public static T* GetPtr()
        {
                return (T*)null;
        }
}
