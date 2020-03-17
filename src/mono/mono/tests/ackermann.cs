// $Id$
// http://www.bagley.org/~doug/shootout/

public class ackermann {

    public static int Main(string[] args) {
	int NUM = 8;
	if (args.Length > 0)
		NUM = System.Int32.Parse (args [0]);
	//return Ack(3, NUM) != 2045? 1: 0;
	System.Console.WriteLine("Ack(3," + NUM + "): " + Ack(3, NUM));
	return 0;
    }

    public static int Ack(int M, int N) {
	if (M == 0) return( N + 1 );
	if (N == 0) return( Ack(M - 1, 1) );
	return( Ack(M - 1, Ack(M, (N - 1))) );
    }

}
