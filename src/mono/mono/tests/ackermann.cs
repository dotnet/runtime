// $Id: ackermann.cs,v 1.2 2001/11/19 07:11:32 lupus Exp $
// http://www.bagley.org/~doug/shootout/

public class ackermann {

    public static int Main() {
	int NUM = 8;
	return Ack(3, NUM) != 2045? 1: 0;
	//System.out.print("Ack(3," + NUM + "): " + Ack(3, NUM) + "\n");
    }

    public static int Ack(int M, int N) {
	if (M == 0) return( N + 1 );
	if (N == 0) return( Ack(M - 1, 1) );
	return( Ack(M - 1, Ack(M, (N - 1))) );
    }

}
