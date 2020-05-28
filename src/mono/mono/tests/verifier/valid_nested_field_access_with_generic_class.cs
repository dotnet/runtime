
public class HashSet<T> {
	Link[] links = new Link[10];
	Link link = new Link ();

	struct Link {
		public int HashCode;
		public int Next;
	}

	struct Enumerator {
		HashSet<T> hashset;
		public Enumerator (HashSet<T> hashset) {
			this.hashset = hashset;
		}

		public void Test () {
			int val = hashset.links[0].Next;
		}

		public void Test2 () {
			int val = hashset.link.Next;
		}
	}

	public  void Test () {
		new Enumerator (this).Test();
		new Enumerator (this).Test2();
	}
}

public class Driver {

	public static void Main () {
		new HashSet<int>().Test();
	}
}

