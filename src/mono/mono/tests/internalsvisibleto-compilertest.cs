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
                var a = new CorrectCaseFriendAssembly.InternalClass(@internal: 0);
                Console.WriteLine("Access internal class internal ctor: OK");
            } catch (MemberAccessException) {
                failCount += 1;
                Console.WriteLine("Access friend internal ctor: Fail");
            }

            Console.WriteLine("-- Wrong case --");

            try {
                var a = new WrongCaseFriendAssembly.InternalClass(@internal: 0);
                Console.WriteLine("Access internal class internal ctor: OK");
            } catch (MemberAccessException) {
                failCount += 1;
                Console.WriteLine("Access friend internal ctor: Fail");
            }

            try {
                // This also works in the Windows CLR. Huh.
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
