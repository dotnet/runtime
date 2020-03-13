
public class GenRandom {
    	static int last = 42;

	public static double gen_random(double max) {
    
	    last = (last * 3877 + 29573) % 139968;
    	return( max * last / 139968 );
	}

	public static int Main() {
    	int N = 900000;
	    double result = 0;
    
    	while (N-- != 0) {
			result = gen_random(100.0);
    	}
	    //printf("%.9f\n", result);
    	return(0);
	}
}
