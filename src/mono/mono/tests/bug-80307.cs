using System;
using System.IO;
using System.Web;
using System.Web.Hosting;

class TinyHost : MarshalByRefObject
{
	public static TinyHost CreateHost ()
	{
		string path = Directory.GetCurrentDirectory ();
		string bin = Path.Combine (path, "bin");
		string asm = Path.GetFileName (typeof (TinyHost).Assembly.Location);

		Directory.CreateDirectory (bin);
		File.Copy (asm, Path.Combine (bin, asm), true);

		return (TinyHost) ApplicationHost.CreateApplicationHost (
			typeof (TinyHost), "/", path);

	}

	public void Execute (string page)
	{
		SimpleWorkerRequest req = new SimpleWorkerRequest (
			page, "", Console.Out);
		HttpRuntime.ProcessRequest (req);
	}

	static void Main ()
	{
		TinyHost h = CreateHost ();
		StreamWriter w = new StreamWriter ("page.aspx");
		w.WriteLine (@"<%@ Page Language=""C#"" %>");
		w.WriteLine (@"<% Console.WriteLine(""Hello""); %>");
		w.Close ();
		h.Execute ("page.aspx");
	}
}
