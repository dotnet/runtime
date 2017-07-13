using System;
using System.Runtime.CompilerServices;

#if SIGN2048
using System.Reflection;
[assembly: AssemblyDelaySign(true)]
[assembly: AssemblyKeyFile(@"internalsvisibleto-2048.snk")]
#endif

#if CORRECT_CASE
#if !SIGN2048
[assembly: InternalsVisibleTo("internalsvisibleto-runtimetest")]
[assembly: InternalsVisibleTo("internalsvisibleto-compilertest")]
#else
[assembly: InternalsVisibleTo("internalsvisibleto-runtimetest-sign2048, PublicKey=00240000048000001401000006020000002400005253413100080000010001002b524ed36058e444d0f2b12aeeeadab6f9a614dae43300d489746d143103a63c0178d0e316cc7a83156637d02b95b617c34bfa6877bc418118ce6d652e73211fdb80e5bc1878c6ef1b488dae12925390e7932dae9b22ada65ec76694a73b8e940db558d03ff5a3bee28017cb4448cd41dc946cc248e3313417f59092b9b62996de506c9446c7dceffed8e854cfa3d42eee30cdccbce934318b64ee20489178c00fa587f4ea666e4421eeae157fddf5ce7cfcf76e3b8b390005297f1b7e502c0f211c8c3f6886012cc4173aeedb4dc915d6d8f3821c19c0f1eedcccec8e839c1443ac96db7231ddebb391a5a92373aa87a6f2b2c8a9d57ad204e61813cc280da3")]
[assembly: InternalsVisibleTo("internalsvisibleto-compilertest-sign2048, PublicKey=00240000048000001401000006020000002400005253413100080000010001002b524ed36058e444d0f2b12aeeeadab6f9a614dae43300d489746d143103a63c0178d0e316cc7a83156637d02b95b617c34bfa6877bc418118ce6d652e73211fdb80e5bc1878c6ef1b488dae12925390e7932dae9b22ada65ec76694a73b8e940db558d03ff5a3bee28017cb4448cd41dc946cc248e3313417f59092b9b62996de506c9446c7dceffed8e854cfa3d42eee30cdccbce934318b64ee20489178c00fa587f4ea666e4421eeae157fddf5ce7cfcf76e3b8b390005297f1b7e502c0f211c8c3f6886012cc4173aeedb4dc915d6d8f3821c19c0f1eedcccec8e839c1443ac96db7231ddebb391a5a92373aa87a6f2b2c8a9d57ad204e61813cc280da3")]
#endif // SIGN2048
#else
#if !SIGN2048
[assembly: InternalsVisibleTo("iNtErnAlsVisibLETo-RUntimeTesT")]
[assembly: InternalsVisibleTo("iNtErnAlsVisibLETo-COmpilerTesT")]
#else
[assembly: InternalsVisibleTo("iNtErnAlsVisibLETo-RUntimeTesT-sign2048, PublicKey=00240000048000001401000006020000002400005253413100080000010001002b524ed36058e444d0f2b12aeeeadab6f9a614dae43300d489746d143103a63c0178d0e316cc7a83156637d02b95b617c34bfa6877bc418118ce6d652e73211fdb80e5bc1878c6ef1b488dae12925390e7932dae9b22ada65ec76694a73b8e940db558d03ff5a3bee28017cb4448cd41dc946cc248e3313417f59092b9b62996de506c9446c7dceffed8e854cfa3d42eee30cdccbce934318b64ee20489178c00fa587f4ea666e4421eeae157fddf5ce7cfcf76e3b8b390005297f1b7e502c0f211c8c3f6886012cc4173aeedb4dc915d6d8f3821c19c0f1eedcccec8e839c1443ac96db7231ddebb391a5a92373aa87a6f2b2c8a9d57ad204e61813cc280da3")]
[assembly: InternalsVisibleTo("iNtErnAlsVisibLETo-COmpilerTesT-sign2048, PublicKey=00240000048000001401000006020000002400005253413100080000010001002b524ed36058e444d0f2b12aeeeadab6f9a614dae43300d489746d143103a63c0178d0e316cc7a83156637d02b95b617c34bfa6877bc418118ce6d652e73211fdb80e5bc1878c6ef1b488dae12925390e7932dae9b22ada65ec76694a73b8e940db558d03ff5a3bee28017cb4448cd41dc946cc248e3313417f59092b9b62996de506c9446c7dceffed8e854cfa3d42eee30cdccbce934318b64ee20489178c00fa587f4ea666e4421eeae157fddf5ce7cfcf76e3b8b390005297f1b7e502c0f211c8c3f6886012cc4173aeedb4dc915d6d8f3821c19c0f1eedcccec8e839c1443ac96db7231ddebb391a5a92373aa87a6f2b2c8a9d57ad204e61813cc280da3")]
#endif // SIGN2048
#endif // !CORRECT_CASE

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
