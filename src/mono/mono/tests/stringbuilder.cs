using System.Text;

namespace test {
	public class Test {
		public static int Main() {
			StringBuilder b = new StringBuilder ();
			/*b.Append ('A');
			b.Append ('b');
			b.Append ('r');*/
			b.Append ("Abr");
			if (b.ToString() != "Abr") {
				System.Console.WriteLine ("Got: " + b.ToString());
				return 1;
			}
			b.Append ('a');
			b.Append ("cadabra");
			if (b.ToString() != "Abracadabra") {
				System.Console.WriteLine ("Got: " + b.ToString());
				return 2;
			}
			return 0;
			
		}
	}
}
