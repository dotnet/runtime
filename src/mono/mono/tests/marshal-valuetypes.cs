using System;

public class Test: MarshalByRefObject
{
        public DateTime Stamp = new DateTime (1968, 1, 2);

        static int Main ()
        {
                AppDomain d = AppDomain.CreateDomain ("foo");
                Test t = (Test) d.CreateInstanceAndUnwrap (typeof (Test).Assembly.FullName, typeof (Test).FullName);
		if (t.Stamp != new DateTime (1968, 1, 2))
			return 1;
		return 0;
        }
}
