using System;

public class Test: MarshalByRefObject
{
        public DateTime Stamp = new DateTime (1968, 1, 2);
	public double perc = 5.4;

        static int Main ()
        {
                AppDomain d = AppDomain.CreateDomain ("foo");
                Test t = (Test) d.CreateInstanceAndUnwrap (typeof (Test).Assembly.FullName, typeof (Test).FullName);
		if (t.Stamp != new DateTime (1968, 1, 2))
			return 1;
		t.perc = 7.2;
		if (t.perc != 7.2)
			return 2;
		return 0;
        }
}
