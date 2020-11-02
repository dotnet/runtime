using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoEnc;

class Container {
	private Thickness _margin;

	public Thickness Margin
	{
		get { return _margin; }
		set { _margin = value; }
	}
}

class Thickness {
	public int val;

	public Thickness (int val)
	{
		this.val = val;
	}
}

public class Sample {
	private Container listView;

	public static int Main (string []args) {
		Assembly assm = typeof (Sample).Assembly;
		var replacer = EncHelper.Make ();

		Sample s = new Sample ();
		s.listView = new Container ();

		s.OnItemSelected (null, null);
		if (s.listView.Margin.val != 30)
			return 1;

		replacer.Update (assm);

		s.OnItemSelected (null, null);
		if (s.listView.Margin.val != 40)
			return 2;

		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void OnItemSelected (object sender, object s)
	{
		listView.Margin = new Thickness (40);
	}
}

