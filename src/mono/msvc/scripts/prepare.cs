//
// C# implementation of a handful of shell steps
// this is used to automate the buidl in Windows
//
using System;
using System.Text;
using System.IO;

class Prepare {
	delegate void filt (StreamReader sr, StreamWriter sw);
	
	static void Filter (string inpath, string outpath, filt filter)
	{
		using (var ins = new StreamReader (inpath)){
			using (var outs = new StreamWriter (outpath)){
				filter (ins, outs);
			}
		}
	}

	static void SystemDataConnectionReplace (string srcdir, string targetdir, string target, string ns, string factory, string conn)
	{
		var t = File.ReadAllText (Path.Combine (srcdir, "DbConnectionHelper.cs"));

		File.WriteAllText (Path.Combine (targetdir, target), t.Replace ("NAMESPACE", ns).Replace ("CONNECTIONFACTORYOBJECTNAME", factory).Replace ("CONNECTIONOBJECTNAME", conn));
	}

	static void SystemDataParameterReplace (string srcdir, string targetdir, string target, string resns, string ns, string parname)
	{
		var t = File.ReadAllText (Path.Combine (srcdir, "DbParameterHelper.cs"));

		File.WriteAllText (Path.Combine (targetdir, target), t.Replace ("RESNAMESPACE", resns).Replace ("NAMESPACE", ns).Replace ("PARAMETEROBJECTNAME", parname));
	}

	static void SystemDataParameterCollReplace (string srcdir, string targetdir, string target, string resns, string ns, string parname)
	{
		var t = File.ReadAllText (Path.Combine (srcdir, "DbParameterCollectionHelper.cs"));

		Console.WriteLine ("Creating " + Path.Combine (targetdir, target));
		File.WriteAllText (Path.Combine (targetdir, target), t.Replace ("RESNAMESPACE", resns).Replace ("PARAMETERCOLLECTIONOBJECTNAME", parname + "Collection").Replace ("NAMESPACE", ns).Replace ("PARAMETEROBJECTNAME", parname));
	}
	
	static void GenerateSystemData (string bdir)
	{
		var rs = Path.Combine (bdir, "class", "referencesource", "System.Data", "System", "Data", "ProviderBase");
		var sd = Path.Combine (bdir, "class", "System.Data");

		SystemDataConnectionReplace (rs, sd, "gen_OdbcConnection.cs", "System.Data.Odbc", "OdbcConnectionFactory.SingletonInstance", "OdbcConnection");
		SystemDataConnectionReplace (rs, sd, "gen_OleDbConnection.cs", "System.Data.OleDb", "OleDbConnectionFactory.SingletonInstance", "OleDbConnection");
		SystemDataConnectionReplace (rs, sd, "gen_SqlConnection.cs", "System.Data.SqlClient", "SqlConnectionFactory.SingletonInstance", "SqlConnection");

		SystemDataParameterReplace (rs, sd, "gen_OdbcParameter.cs", "System.Data", "System.Data.Odbc", "OdbcParameter");
		SystemDataParameterReplace (rs, sd, "gen_OleDbParameter.cs", "System.Data", "System.Data.OleDb", "OleDbParameter");
		SystemDataParameterReplace (rs, sd, "gen_SqlParameter.cs", "System.Data", "System.Data.SqlClient", "SqlParameter");

		SystemDataParameterCollReplace (rs, sd, "gen_OdbcParameterCollection.cs", "System.Data", "System.Data.Odbc", "OdbcParameter");
		SystemDataParameterCollReplace (rs, sd, "gen_OleDbParameterCollection.cs", "System.Data", "System.Data.OleDb", "OleDbParameter");
		SystemDataParameterCollReplace (rs, sd, "gen_SqlParameterCollection.cs", "System.Data", "System.Data.SqlClient", "SqlParameter");
	}
	
	static void Main (string [] args)
	{
		string bdir = args.Length == 0 ? "../../../mcs" : args [0];

		if (!Directory.Exists (Path.Combine(bdir, "class"))){
			Console.Error.WriteLine ("The directory {0} does not contain class at {1}", Path.GetFullPath (bdir), Environment.CurrentDirectory);
			Environment.Exit (1);
		}

		switch (args [1]){
		case "core":
			Filter (bdir + "/build/common/Consts.cs.in",
				bdir + "/build/common/Consts.cs",
				(i, o) => o.Write (i.ReadToEnd ().Replace ("@MONO_VERSION@", "2.5.0")));

			GenerateSystemData (bdir);
			break;
			
		default:
			Console.Error.WriteLine ("Unknonw option to prepare.exe {0}", args [1]);
			Environment.Exit (1);
			break;
		}
	}
	
}
