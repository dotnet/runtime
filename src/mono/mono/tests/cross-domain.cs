using System;
using System.Net;
using System.Runtime.Remoting;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.Serialization;

public class Test: MarshalByRefObject
{	
	public static int Main (string[] args)
	{
		AppDomain domain = AppDomain.CreateDomain ("testdomain1");
		Test server = (Test) domain.CreateInstanceAndUnwrap(typeof(Test).Assembly.FullName, "Test");
		return server.RunTest ();
	}

	public int RunTest ()
	{
		try
		{
			object t = null;
			string s = (string)t;
			AppDomain domain = AppDomain.CreateDomain ("testdomain");
			Remo server = (Remo) domain.CreateInstanceAndUnwrap(typeof(Test).Assembly.FullName,"Remo");
			if (System.Threading.Thread.GetDomainID () == server.GetDomainId ())
				throw new TestException ("Object not created in new domain", 1);
	
			Dada d = new Dada ();
			d.p = 22;
			
			server.Run ();
			server.Run2 (88, "hola");
			server.Run3 (99, d, "adeu");
			
			string r = server.Run4 (200, d, "que");
			CheckValue (r, "vist", 140);
			
			try {
				server.Run5 (200, d, "que");
				throw new TestException ("Exception expected", 150);
			}
			catch (Exception ex) {
				CheckValue (ex.Message, "peta", 151);
			}
			
			Dada d2;
			d = server.Run6 (99, out d2, "adeu");
			CheckValue (d.p, 987, 161);
			CheckValue (d2.p, 987, 162);
				
			d.p = 22;
			d2 = server.Run7 (d);
			CheckValue (d.p, 22, 170);
			CheckValue (d2.p, 33, 170);
			
			byte[] ba = new byte[5];
			for (byte n=0; n<ba.Length; n++)
				ba [n] = n;
				
			server.Run8 (ba);

			for (int n=0; n<ba.Length; n++)
				CheckValue (ba[n], (byte) (ba.Length - n), 180);
			
			StringBuilder sb = new StringBuilder ("un");
			server.Run9_1 (sb);
			Test.CheckValue (sb, new StringBuilder ("un"), 190);

			StringBuilder sb2 = new StringBuilder ("prefix");
			StringBuilder sb3 = server.Run9_2 (sb2);
			Test.CheckValue (sb2, new StringBuilder ("prefix"), 192);
			Test.CheckValue (sb3, new StringBuilder ("prefix-middle"), 192);
			StringBuilder sb4 = server.Run9_3 (sb3);
			Test.CheckValue (sb3, new StringBuilder ("prefix-middle"), 193);
			Test.CheckValue (sb4, new StringBuilder ("prefix-middle-end"), 193);

			
		}
		catch (TestException ex)
		{
			Console.WriteLine ("TEST ERROR ({0}): {1}", ex.Code, ex);
			return ex.Code;
		}
		catch (Exception ex)
		{
			Console.WriteLine ("TEST ERROR: " + ex);
			return -1;
		}
		return 0;
	}
	
	public static void CheckDomain (object ob, Type t, int ec)
	{
		if (ob == null) return;
		if (ob.GetType () != t) {
			if (t.ToString() == ob.GetType().ToString())
				throw new TestException ("Parameter not properly marshalled", ec);
			else
				throw new TestException ("Wrong type (maybe wrong domain?)", ec);
		}
	}
	
	public static void CheckValue (object ob1, object ob2, int ec)
	{
		if ((ob1 == null || ob2 == null) && ob1 != ob2)
			throw new TestException ("Null objects are not equal", ec);
		
		if (ob2.GetType () != ob1.GetType ())
			throw new TestException ("Wrong type (maybe wrong domain?)", ec);
		
		if (ob1 is StringBuilder) {
			if (Object.ReferenceEquals (ob1, ob2))
				throw new TestException ("StringBuilders are ReferenceEquals", ec);

			StringBuilder sb1 = (StringBuilder) ob1;
			StringBuilder sb2 = (StringBuilder) ob2;

			if (sb1.ToString () != sb2.ToString ())
				throw new TestException ("String in StringBuilders are not equal", ec);

			if (sb1.Length != sb2.Length)
				throw new TestException ("Lengths in StringBuilders are not equal", ec);
		}
		else if (!ob1.Equals (ob2))
			throw new TestException ("Objects are not equal", ec);
	}
}

public class Remo: MarshalByRefObject
{
	int domid;
	
	public Remo ()
	{
		domid = System.Threading.Thread.GetDomainID ();
	}
	
	public int GetDomainId ()
	{
		return domid;
	}
	
	public void CheckThisDomain (int ec)
	{
		if (domid != System.Threading.Thread.GetDomainID ())
			throw new TestException ("Wrong domain", ec);
	}
	
	public void Run ()
	{
		CheckThisDomain (10);
	}
	
	public void Run2 (int a, string b)
	{
		CheckThisDomain (20);
		Test.CheckValue (a, 88, 21);
		Test.CheckValue (b, "hola", 22);
	}
	
	public void Run3 (int a, Dada d, string b)
	{
		CheckThisDomain (30);
		Test.CheckValue (a, 99, 31);
		Test.CheckValue (b, "adeu", 32);
		Test.CheckValue (d.p, 22, 33);
	}
	
	public string Run4 (int a, Dada d, string b)
	{
		CheckThisDomain (40);
		Test.CheckValue (a, 200, 41);
		Test.CheckValue (b, "que", 42);
		Test.CheckValue (d.p, 22, 43);
		return "vist";
	}
	
	public Dada Run5 (int a, Dada d, string b)
	{
		CheckThisDomain (50);
		Test.CheckValue (a, 200, 51);
		Test.CheckValue (b, "que", 52);
		Test.CheckValue (d.p, 22, 53);
		Peta ();
		return d;
	}
	
	public Dada Run6 (int a, out Dada d, string b)
	{
		CheckThisDomain (60);
		Test.CheckValue (a, 99, 61);
		Test.CheckValue (b, "adeu", 62);
		
		d = new Dada ();
		d.p = 987;
		return d;
	}
	
	public Dada Run7 (Dada d)
	{
		CheckThisDomain (70);
		Test.CheckValue (d.p, 22, 71);
		d.p = 33;
		return d;
	}

	public void Run8 ([In,Out] byte[] bytes)
	{
		CheckThisDomain (80);
		Test.CheckDomain (bytes, typeof(byte[]), 81);
		for (int n=0; n < bytes.Length; n++) {
			Test.CheckValue (bytes[n], (byte)n, 82);
			bytes[n] = (byte) (bytes.Length - n);
		}
	}

	public void Run9_1 ([In,Out] StringBuilder sb)
	{
		CheckThisDomain (90);
		Test.CheckValue (sb, new StringBuilder ("un"), 91);
		sb.Append ("-dos");
	}

	public StringBuilder Run9_2 ([In] StringBuilder sb)
	{
		CheckThisDomain (91);
		Test.CheckValue (sb, new StringBuilder ("prefix"), 92);
		sb.Append ("-middle");
		return sb;
	}

	public StringBuilder Run9_3 ([In,Out] StringBuilder sb)
	{
		CheckThisDomain (92);
		Test.CheckValue (sb, new StringBuilder ("prefix-middle"), 93);
		sb.Append ("-end");

		return sb;
	}

	public void Peta ()
	{
		throw new Exception ("peta");
	}
}

[Serializable]
public class Dada
{
	public int p;
}

[Serializable]
public class MyException: Exception
{
	public MyException (string s): base (s) {}
}

[Serializable]
public class TestException: Exception
{
	public int Code = -1;
	
	public TestException (SerializationInfo i, StreamingContext ctx): base (i, ctx) {
		Code = i.GetInt32  ("Code");
	}
	
	public TestException (string txt, int code): base (txt + " (code: " + code + ")")
	{
		Code = code;
	}
	
	public override void GetObjectData (SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData (info, context);
		info.AddValue ("Code", Code);
	}
}
