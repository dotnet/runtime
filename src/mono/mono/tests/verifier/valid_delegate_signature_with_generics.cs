using System;

public class RoutedEventArgs {
	
}

public class RoutedPropertyChangedEventArgs<T> : RoutedEventArgs {
	
}

public delegate void RoutedPropertyChangedEventHandler<T>(
    Object sender,
    RoutedPropertyChangedEventArgs<T> e
);


class Program {

	public void Test(object sender, RoutedEventArgs evt) {}
	
	void Fun () {
		var del = new RoutedPropertyChangedEventHandler<double> (Test);
		del (null, null);
	}
	
    static int Main (string[] args)
    {
		new Program ().Fun ();
		return 0;
	}
}