using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Web.Script.Serialization;

class C
{
	public static void 
	JsonValidateState ()
	{
		var monoType = Type.GetType ("Mono.Runtime", false);
		var convert = monoType.GetMethod("DumpStateSingle", BindingFlags.NonPublic | BindingFlags.Static);
		var output = (Tuple<String, ulong, ulong>) convert.Invoke(null, Array.Empty<object> ());
		var checker = new JavaScriptSerializer ();

		Console.WriteLine ("Validating: {0}", output.Item1);
		checker.DeserializeObject (output.Item1);
	}

	public static void 
	JsonValidateMerp (string configDir)
	{
		var monoType = Type.GetType ("Mono.Runtime", false);
		var m = monoType.GetMethod("EnableMicrosoftTelemetry", BindingFlags.NonPublic | BindingFlags.Static);

		// This leads to open -a /bin/cat, which errors out, but errors
		// in invoking merp are only logged errors, not fatal assertions.
		var merpGUIPath = "/bin/cat";
		var appBundleId = "com.xam.Minimal";
		var appSignature = "Test.Xam.Minimal";
		var appVersion = "123456";
		var eventType = "AppleAppCrash";
		var appPath = "/where/mono/lives";

		var m_params = new object[] { appBundleId, appSignature, appVersion, merpGUIPath, eventType, appPath, configDir };

		m.Invoke(null, m_params);	

		var add_method = monoType.GetMethod("AnnotateMicrosoftTelemetry", BindingFlags.NonPublic | BindingFlags.Static);
		var add_params = new object[] { "sessionId", "12345" };
		add_method.Invoke(null, add_params);

		try {
			throw new Exception ("");
		} catch (Exception exc) {
			var send = monoType.GetMethod("SendExceptionToTelemetry", BindingFlags.NonPublic | BindingFlags.Static);
			var send_params = new object[] {exc};

			bool caught_expected_exception = false;
			try {
				send.Invoke(null, send_params);
			} catch (Exception exc2) {
				if (exc2.InnerException != null && exc2.InnerException.Message == "We were unable to start the Microsoft Error Reporting client.")
					caught_expected_exception = true;
				else
					throw new Exception (String.Format ("Got exception from Merp icall with wrong message {0}", exc2.InnerException != null ? exc2.InnerException.Message : exc2.Message));
			}
		}

		var xmlFilePath = String.Format("{0}CustomLogsMetadata.xml", configDir);
		var paramsFilePath = String.Format("{0}MERP.uploadparams.txt", configDir);
		var crashFilePath = String.Format("{0}lastcrashlog.txt", configDir);

		var xmlFileExists = File.Exists (xmlFilePath);
		var paramsFileExists = File.Exists (paramsFilePath);
		var crashFileExists = File.Exists (crashFilePath);

		if (xmlFileExists) {
			File.ReadAllText (xmlFilePath);
			File.Delete (xmlFilePath);
		}

		if (paramsFileExists) {
			File.ReadAllText (paramsFilePath);
			File.Delete (paramsFilePath);
		}

		if (crashFileExists) {
			var crashFile = File.ReadAllText (crashFilePath);
			File.Delete (crashFilePath);

			var checker = new JavaScriptSerializer ();

			// Throws if invalid json
			Console.WriteLine("Validating: {0}",  crashFile);
			checker.DeserializeObject (crashFile);
		}

		if (!xmlFileExists)
			throw new Exception (String.Format ("Did not produce {0}", xmlFilePath));

		if (!paramsFileExists)
			throw new Exception (String.Format ("Did not produce {0}", paramsFilePath));

		if (!crashFileExists)
			throw new Exception (String.Format ("Did not produce {0}", crashFilePath));
	}

	public static void Main ()
	{
		JsonValidateState ();

		var configDir = "./merp-json-valid/";
		Directory.CreateDirectory (configDir);
		try {
			JsonValidateMerp (configDir);
		} finally {
			Directory.Delete (configDir, true);
		}
	}
}
