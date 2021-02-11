/* This class works fine */
public class Works {

    private double val;

    public double this[int i, int j] {

        get { return val; }

        set { val = value; }

    }

    public Works(double val)
    { this.val = val; }

}

/* Same code as struct breaks */

public struct Breaks {

    private double val;

    public double this[int i, int j] {

        get { return val; }

        set { val = value; }

    }

    public Breaks(double val)
    { this.val = val; }

}

public class Tester {

    public static void Main(string[] args)

    {

        System.Console.WriteLine("This works");

        Works w = new Works(3.0);

        w[0, 0] += 3.0;

        System.Console.WriteLine("This breaks");

        Breaks b = new Breaks(3.0);

        b[0, 0] += 3.0;

    }

}
