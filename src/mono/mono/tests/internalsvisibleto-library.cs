using System;
using System.Runtime.CompilerServices;

#if CORRECT_CASE
[assembly: InternalsVisibleTo("internalsvisibleto-runtimetest")]
[assembly: InternalsVisibleTo("internalsvisibleto-compilertest")]
#else
[assembly: InternalsVisibleTo("iNtErnAlsVisibLETo-RUntimeTesT")]
[assembly: InternalsVisibleTo("iNtErnAlsVisibLETo-COmpilerTesT")]
#endif

#if CORRECT_CASE
namespace CorrectCaseFriendAssembly {
#else
namespace WrongCaseFriendAssembly {
#endif

#if PERMISSIVE
    public
#else
    internal 
#endif
     class InternalClass
    {
        public InternalClass (char @public) {
            Console.WriteLine("InternalClass(public)");
        }

#if PERMISSIVE
        public
#else
        internal 
#endif
         InternalClass (int @internal) {
            Console.WriteLine("InternalClass(internal)");
        }

#if PERMISSIVE
        public
#else
        private
#endif
         InternalClass (bool @private) {
            Console.WriteLine("InternalClass(private)");
        }

        public static void PrivateStaticMethod () {
            Console.WriteLine("InternalClass.PrivateStaticMethod");
        }

#if PERMISSIVE
        public
#else
        internal 
#endif
         static void InternalStaticMethod () {
            Console.WriteLine("InternalClass.InternalStaticMethod");
        }

#if PERMISSIVE
        public
#else
        internal 
#endif
         void InternalMethod () {
            Console.WriteLine("InternalClass.InternalMethod");
        }

        public static void PublicStaticMethod () {
            Console.WriteLine("PublicStaticMethod");
        }

        public void PublicMethod () {
            Console.WriteLine("PublicMethod");
        }
    }

    public class PublicClass {

#if PERMISSIVE
        public
#else
        internal 
#endif
         PublicClass () {
        }

#if PERMISSIVE
        public
#else
        internal 
#endif
         static void InternalStaticMethod () {
            Console.WriteLine("PublicClass.InternalStaticMethod");
        }

#if PERMISSIVE
        public
#else
        internal 
#endif
         void InternalMethod () {
            Console.WriteLine("PublicClass.InternalMethod");
        }
    }
}
