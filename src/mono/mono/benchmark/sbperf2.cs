using System.Text;

namespace test {
	public class Test {
		public static int Main() {
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < 1000000; i++) {
   			   sb.Append ("hello");
                           sb.Append (" world!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                           string str = sb.ToString ();
			   int len = str.Length;
			   sb.Length = 0;
			}

			return 0;
			
		}
	}
}
