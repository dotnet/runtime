using System.Text;

namespace test {
	public class Test {
		public static int Main() {
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < 500000; i++) {
   			   sb.Append ("hello");
                           sb.Append (" world!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                           sb.ToString ();
			   sb.Length = 0;
			}

			return 0;
			
		}
	}
}
