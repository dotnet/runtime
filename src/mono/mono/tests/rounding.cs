public class TestJit {

	public static int Main() {
		long ticks = 631502475130080000L;
                long ticksperday = 864000000000L;

                double days = (double) ticks / ticksperday;

		if ((int)days != 730905)
			return 1;

		return 0;
	}
}

