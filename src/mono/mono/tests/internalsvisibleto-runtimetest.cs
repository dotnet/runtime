using System;

#if SIGN2048
using System.Reflection;
[assembly: AssemblyDelaySign(true)]
[assembly: AssemblyKeyFile(@"internalsvisibleto-2048.snk")]
#endif

namespace InternalsVisibleTo {
    class Program {
        static void Main (string[] args) {
            var failCount = 0;

            Console.WriteLine("-- Correct case --");

	    try {
		    var a = new CorrectCaseFriendAssembly.PublicClass ();
		    a.InternalMethod ();
		    Console.WriteLine ("Access friend internal method: OK");
	    } catch (MemberAccessException) {
		    failCount += 1;
		    Console.WriteLine ("Access friend internal method: Fail");
	    }

            try {
                var a = new CorrectCaseFriendAssembly.InternalClass(@private: false);
                Console.WriteLine("Access internal class private ctor: OK");
                // Microsoft behaves this way
            } catch (MemberAccessException) {
                Console.WriteLine("Access internal class private ctor: Fail");
                // FIXME: Mono behaves this way
                // failCount += 1;
            }

            try {
                var a = new CorrectCaseFriendAssembly.InternalClass(@internal: 0);
                Console.WriteLine("Access internal class internal ctor: OK");
            } catch (MemberAccessException) {
                failCount += 1;
                Console.WriteLine("Access friend internal ctor: Fail");
            }

            try {
                var b = new CorrectCaseFriendAssembly.InternalClass(@public: 'a');
                Console.WriteLine("Access internal class public ctor: OK");
                b.InternalMethod();
                Console.WriteLine("Access friend internal method: OK");
            } catch (MemberAccessException) {
                failCount += 1;
                Console.WriteLine("Access friend internal method with wrong case: Fail");
            }

            Console.WriteLine("-- Wrong case --");

            try {
                var a = new WrongCaseFriendAssembly.InternalClass(@private: false);
                // Microsoft behaves this way
                Console.WriteLine("Access internal class private ctor: OK");
            } catch (MemberAccessException) {
                // FIXME: Mono behaves this way
                Console.WriteLine("Access internal class private ctor: Fail");
                // failCount += 1;
            }

            try {
                var a = new WrongCaseFriendAssembly.InternalClass(@internal: 0);
                Console.WriteLine("Access internal class internal ctor: OK");
            } catch (MemberAccessException) {
                failCount += 1;
                Console.WriteLine("Access friend internal ctor: Fail");
            }

            try {
                var b = new WrongCaseFriendAssembly.InternalClass(@public: 'a');
                Console.WriteLine("Access internal class public ctor: OK");
                b.InternalMethod();
                Console.WriteLine("Access friend internal method: OK");
            } catch (MemberAccessException) {
                failCount += 1;
                Console.WriteLine("Access friend internal method: Fail");
            }

            try {
                // Surprisingly this works in the Windows CLR, even though it seems like it shouldn't
                WrongCaseFriendAssembly.InternalClass.PrivateStaticMethod();
                Console.WriteLine("Access friend private static method: OK");
            } catch (MemberAccessException) {
                Console.WriteLine("Access friend private static method: Fail");
                failCount += 1;
            }

            try {
                WrongCaseFriendAssembly.InternalClass.InternalStaticMethod();
                Console.WriteLine("Access friend internal static method: OK");
            } catch (MemberAccessException) {
                failCount += 1;
                Console.WriteLine("Access friend internal static method: Fail");
            }

            try {
                WrongCaseFriendAssembly.PublicClass.InternalStaticMethod();
                Console.WriteLine("Access public internal static method: OK");
            } catch (MemberAccessException) {
                failCount += 1;
                Console.WriteLine("Access public internal static method: Fail");
            }

            if (System.Diagnostics.Debugger.IsAttached)
                Console.ReadLine();

            Console.WriteLine("Incorrect results: {0}", failCount);
            Environment.ExitCode = failCount;
        }
    }
}
