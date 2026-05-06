public class DescriptorTest
{
	public static void Main ()
	{
		var r = new Random (31415);
		var objs = new object [9];
		var which = 0;
		var last = new Filler [Bitmaps.NumWhich];
		for (var i = 0; i < 1000000000; ++i)
		{
			var o = Bitmaps.MakeAndFill (which, objs, r.Next (2) == 0);
			objs [r.Next (objs.Length)] = o;
			last [which] = o;

			if (i % 761 == 0)
			{
				var l = last [r.Next (Bitmaps.NumWhich)];
				if (l != null)
					l.Fill (objs);
			}

			/*
			  if (i % 10007 == 0)
			  Console.WriteLine (o.GetType ().Name + " " + which);
			*/

			if (i % 5 == 0)
				objs [r.Next (objs.Length)] = null;

			if (++which >= Bitmaps.NumWhich)
				which = 0;
		}
	}
}
