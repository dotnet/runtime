// $Id: ackermann.cs,v 1.1 2001/11/19 06:52:53 lupus Exp $
// http://www.bagley.org/~doug/shootout/

public class ackermann {

    public static void Main() {
	int NUM = 8;
	Ack(3, NUM);
	return;
	//System.out.print("Ack(3," + NUM + "): " + Ack(3, NUM) + "\n");
    }

    public static int Ack(int M, int N) {
	if (M == 0) return( N + 1 );
	if (N == 0) return( Ack(M - 1, 1) );
	return( Ack(M - 1, Ack(M, (N - 1))) );
    }

}
