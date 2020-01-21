using System;
using System.Text;

[Obsolete("test")]
public static class Program
{
    public const string UnicodeLowSurrogate = "l\uDC00";
    public const string UnicodeHighSurrogate = "h\uD800";
    public const string UnicodeReplacementCharacter = "\uFFFD";

    public static int Main ()
    {
        int exitCode = 0;

        var tuples = new [] {
            ( typeof (Program), "0074 0065 0073 0074" ),
            ( typeof (A), "0068 FFFD FFFD" ), 
            ( typeof (B), "006C FFFD FFFD" ),
            ( typeof (C), "006C FFFD FFFD 0068 FFFD FFFD" ),
            ( typeof (D), "0068 FFFD FFFD 006C FFFD FFFD" )
        };

        foreach (var tup in tuples) {
            var type = tup.Item1;
            var a = ((ObsoleteAttribute)type.GetCustomAttributes(true)[0]);
            var m = a.Message;

            var sb = new StringBuilder();

            if (m != null) {
                foreach (var ch in m)
                    sb.AppendFormat("{0:X4} ", (uint)ch);
            } else {
                sb.Append("null");
            }

            var expected = tup.Item2;
            var actual = sb.ToString().Trim();
            if (actual != expected) {
                Console.WriteLine("Attribute on type {0} failed to decode:", type);
                Console.WriteLine(" expected '{0}' but got '{1}'", expected, actual);
                exitCode += 1;
            } else {
                Console.WriteLine("{0} {1}", type, actual);
            }
        }

        return exitCode;
    }
}

[Obsolete(Program.UnicodeHighSurrogate)]
public class A {
}

[Obsolete(Program.UnicodeLowSurrogate)]
public class B {
}

[Obsolete(Program.UnicodeLowSurrogate + Program.UnicodeHighSurrogate)]
public class C {
}

[Obsolete(Program.UnicodeHighSurrogate + Program.UnicodeLowSurrogate)]
public class D {
}