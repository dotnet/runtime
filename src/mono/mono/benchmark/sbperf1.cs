using System.Text;

namespace test {
	public class Test {
		public static int Main() {
			for (int i = 0; i < 500000; i++) {
			   StringBuilder sb = new StringBuilder ();
   			   sb.Append ("hello");
                           sb.Append (" world!");
                           sb.ToString ();
			}

			return 0;
			
		}
	}
}
