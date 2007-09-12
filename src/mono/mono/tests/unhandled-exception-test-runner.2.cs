using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace UnhandledExceptionTest {
	class RunningMode {
		private string monoRuntime;
		public string RUNTIME {
			get {
				return monoRuntime;
			}
		}
		public bool UseMonoRuntime {
			get {
				return (monoRuntime != null) && (monoRuntime != "");
			}
		}
		private bool generateTestConfigurations;
		public bool GTC {
			get {
				return generateTestConfigurations;
			}
		}
		
		private static bool ParseArgumentValue (string value) {
			if ((value.Length == 1)) {
				switch (value [0]) {
				case 'T':
					return true;
				case 'F':
					return false;
				default:
					Console.WriteLine ("Invalid argument value {0}", value);
					throw new ApplicationException ("Invalid argument value " + value);
				}
			} else {
				Console.WriteLine ("Invalid argument value {0}", value);
				throw new ApplicationException ("Invalid argument value " + value);
			}
		}
		public RunningMode (string runningMode) {
			string [] arguments = runningMode.Split (',');
			foreach (string argument in arguments) {
				string [] components = argument.Split (':');
				if (components.Length == 2) {
					switch (components [0]) {
					case "RUNTIME":
						monoRuntime = components [1];
						break;
					case "GTC":
						generateTestConfigurations = ParseArgumentValue (components [1]);
						break;
					default:
						Console.WriteLine ("Invalid argument {0}", argument);
						throw new ApplicationException ("Invalid argument " + argument);
					}
				} else {
					Console.WriteLine ("Invalid argument {0}", argument);
					throw new ApplicationException ("Invalid argument " + argument);
				}
			}
		}
	}
	
	class TestResult {
		private int exitCode;
		public int ExitCode {
			get {
				return exitCode;
			}
		}
		public bool EXITZERO {
			get {
				return (exitCode == 0);
			}
		}
		private bool continueMarker;
		public bool CONT {
			get {
				return continueMarker;
			}
		}
		private bool rootDomainUnhandledExceptionMarker;
		public bool RDUE {
			get {
				return rootDomainUnhandledExceptionMarker;
			}
		}
		private bool differentDomainUnhandledExceptionMarker;
		public bool DDUE {
			get {
				return differentDomainUnhandledExceptionMarker;
			}
		}
		private bool invalidArgument;
		public bool InvalidArgument {
			get {
				return invalidArgument;
			}
		}
		private string [] output;
		
		private static Regex continuePattern = new Regex ("MARKER-CONT");
		private static Regex rootDomainUnhandledExceptionPattern = new Regex ("MARKER-RDUE");
		private static Regex differentDomainUnhandledExceptionPattern = new Regex ("MARKER-DDUE");
		private static Regex invalidArgumentPattern = new Regex ("Invalid argument");
		
		private void UpdateFlags () {
			foreach (string outputLine in output) {
				if (continuePattern.Match (outputLine).Success) {
					continueMarker = true;
				}
				if (rootDomainUnhandledExceptionPattern.Match (outputLine).Success) {
					rootDomainUnhandledExceptionMarker = true;
				}
				if (differentDomainUnhandledExceptionPattern.Match (outputLine).Success) {
					differentDomainUnhandledExceptionMarker = true;
				}
				if (invalidArgumentPattern.Match (outputLine).Success) {
					invalidArgument = true;
				}
			}
		}
		public void PrintOutput () {
			Console.WriteLine ("--- Output start: ---");
			foreach (string outputLine in output) {
				Console.WriteLine (outputLine);
			}
			Console.WriteLine ("--- Output end. ---");
		}
		
		public TestResult (int exitCode, string [] output) {
			this.exitCode = exitCode;
			this.output = output;
			UpdateFlags ();
		}
	}
	
	class TestDescription {
		private bool use20Runtime;
		public bool USE20 {
			get {
				return use20Runtime;
			}
		}
		private bool rootConfigurationIsLegacy;
		public bool RCIL {
			get {
				return rootConfigurationIsLegacy;
			}
		}
		
		private bool exitCodeShouldBeZero;
		public bool EXITZERO {
			get {
				return exitCodeShouldBeZero;
			}
		}
		private bool continueMarkerExpected;
		public bool CONT {
			get {
				return continueMarkerExpected;
			}
		}
		private bool rootDomainUnhandledExceptionMarkerExpected;
		public bool RDUE {
			get {
				return rootDomainUnhandledExceptionMarkerExpected;
			}
		}
		private bool differentDomainUnhandledExceptionMarkerExpected;
		public bool DDUE {
			get {
				return differentDomainUnhandledExceptionMarkerExpected;
			}
		}
		
		private static string BoolToString (bool value) {
			return value ? "T" : "F";
		}
		public static readonly int MIN_CONFIG_CODE = 0;
		public static readonly int MAX_CONFIG_CODE = 255;
		public static TestDescription FromCode (int code) {
			if ((code >= MIN_CONFIG_CODE) && (code <= MAX_CONFIG_CODE)) {
				bool testUSE20 = (code & 0x01) != 0;
				bool testRCIL =  (code & 0x02) != 0;
				bool testDT =    (code & 0x04) != 0;
				bool testDA =    (code & 0x08) != 0;
				bool testDCIL =  (code & 0x10) != 0;
				bool testDTDA =  (code & 0x20) != 0;
				bool testHRA =   (code & 0x40) != 0;
				bool testHDA =   (code & 0x80) != 0;
				
				if (testDCIL && ! testDA) {
					return null;
				} else if (testDTDA && ! testDA) {
					return null;
				} else if (testHDA && ! testDA) {
					return null;
				} else {
					string testConfiguration = String.Format ("USE20:{0},RCIL:{1}",
							BoolToString (testUSE20), BoolToString (testRCIL));
					string testArguments = String.Format ("DT:{0},DA:{1},DCIL:{2},DTDA:{3},HRA:{4},HDA:{5}",
							BoolToString (testDT), BoolToString (testDA),
							BoolToString (testDCIL), BoolToString (testDTDA),
							BoolToString (testHRA), BoolToString (testHDA));
					string testExpectedResult = "EXITZERO:F,CONT:F,RDUE:F,DDUE:F";
					return new TestDescription (testConfiguration, testArguments, testExpectedResult);
				}
			} else {
				return null;
			}
		}
		
		
		private string configuration;
		public string Configuration {
			get {
				return configuration;
			}
		}
		private string arguments;
		public string Arguments {
			get {
				return arguments;
			}
		}
		private string expectedResult;
		public string ExpectedResult {
			get {
				return expectedResult;
			}
		}
		
		private static bool ParseArgumentValue (string value) {
			if ((value.Length == 1)) {
				switch (value [0]) {
				case 'T':
					return true;
				case 'F':
					return false;
				default:
					Console.WriteLine ("Invalid argument value {0}", value);
					throw new ApplicationException ("Invalid argument value " + value);
				}
			} else {
				Console.WriteLine ("Invalid argument value {0}", value);
				throw new ApplicationException ("Invalid argument value " + value);
			}
		}
		
		public TestDescription (string configuration, string arguments, string expectedResult) {
			this.configuration = configuration;
			this.arguments = arguments;
			this.expectedResult = expectedResult;
			
			string [] configurationFlags = configuration.Split (',');
			foreach (string configurationFlag in configurationFlags) {
				string [] components = configurationFlag.Split (':');
				if (components.Length == 2) {
					switch (components [0]) {
					case "USE20":
						use20Runtime = ParseArgumentValue (components [1]);
						break;
					case "RCIL":
						rootConfigurationIsLegacy = ParseArgumentValue (components [1]);
						break;
					default:
						Console.WriteLine ("Invalid argument {0}", components [0]);
						throw new ApplicationException ("Invalid argument " + components [0]);
					}
				} else {
					Console.WriteLine ("Invalid argument {0}", configurationFlag);
					throw new ApplicationException ("Invalid argument " + configurationFlag);
				}
			}
			string [] expectedResultFlags = expectedResult.Split (',');
			foreach (string expectedResultFlag in expectedResultFlags) {
				string [] components = expectedResultFlag.Split (':');
				if (components.Length == 2) {
					switch (components [0]) {
					case "EXITZERO":
						exitCodeShouldBeZero = ParseArgumentValue (components [1]);
						break;
					case "CONT":
						continueMarkerExpected = ParseArgumentValue (components [1]);
						break;
					case "RDUE":
						rootDomainUnhandledExceptionMarkerExpected = ParseArgumentValue (components [1]);
						break;
					case "DDUE":
						differentDomainUnhandledExceptionMarkerExpected = ParseArgumentValue (components [1]);
						break;
					default:
						Console.WriteLine ("Invalid argument {0}", components [0]);
						throw new ApplicationException ("Invalid argument " + components [0]);
					}
				} else {
					Console.WriteLine ("Invalid argument {0}", expectedResultFlag);
					throw new ApplicationException ("Invalid argument " + expectedResultFlag);
				}
			}
			
		}
		
		public bool Check (TestResult testResult) {
			if (EXITZERO && (testResult.ExitCode != 0)) {
				Console.WriteLine ("Test FAILED: exit code is {0}, expected zero", testResult.ExitCode);
				return false;
			}
			if (!CONT && testResult.CONT) {
				Console.WriteLine ("Test FAILED: unexpected CONT marker found");
				return false;
			}
			if (CONT && !testResult.CONT) {
				Console.WriteLine ("Test FAILED: expected CONT marker not found");
				return false;
			}
			if (!RDUE && testResult.RDUE) {
				Console.WriteLine ("Test FAILED: unexpected RDUE marker found");
				return false;
			}
			if (RDUE && !testResult.RDUE) {
				Console.WriteLine ("Test FAILED: expected RDUE marker not found");
				return false;
			}
			if (!DDUE && testResult.DDUE) {
				Console.WriteLine ("Test FAILED: unexpected  marker found");
				return false;
			}
			if (DDUE && !testResult.DDUE) {
				Console.WriteLine ("Test FAILED: expected DDUE marker not found");
				return false;
			}
			return true;
		}
	}
	
	class TestRun {
		private TestDescription description;
		private TestResult result;
		
		public TestRun (TestDescription description, RunningMode runningMode) {
			this.description = description;
			
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.CreateNoWindow = true;
			p.StartInfo.RedirectStandardOutput = true;
			p.EnableRaisingEvents = true;
			
			string program = "unhandled-exception-test-case";
			if (description.RCIL) {
				program = program + "-legacy";
			}
			program = program + (description.USE20 ? ".2.exe" : ".1.exe");
			if (runningMode.UseMonoRuntime) {
				p.StartInfo.FileName = runningMode.RUNTIME;
				p.StartInfo.Arguments = "--debug " + program + " " + description.Arguments;
			} else {
				p.StartInfo.FileName = program;
				p.StartInfo.Arguments = description.Arguments;
			}
			
			Console.WriteLine ("Starting process \"{0}\" \"{1}\"", p.StartInfo.FileName, p.StartInfo.Arguments);
			
			p.Start();
			// Do not wait for the child process to exit before
			// reading to the end of its redirected stream.
			// p.WaitForExit ();
			// Read the output stream first and then wait.
			string output = p.StandardOutput.ReadToEnd ();
			p.WaitForExit ();
			string[] outputLines = output.Split ('\n');
			
			result = new TestResult (p.ExitCode, outputLines);
		}
		
		public bool Check () {
			return description.Check (result);
		}
		
		private static string BoolToString (bool value) {
			return value ? "T" : "F";
		}
		public void Print () {
			Console.WriteLine ("Results of test {0} {1} {2}",
					description.Configuration, description.Arguments, description.ExpectedResult);
			Console.WriteLine ("Exit code is {0}, output is:", result.ExitCode);
			result.PrintOutput ();
			Console.WriteLine ("The following configuration would make the test pass:");
			Console.WriteLine ("new TestDescription (\"{0}\", \"{1}\", \"EXITZERO:{2},CONT:{3},RDUE:{4},DDUE:{5}\"),",
					description.Configuration, description.Arguments,
					BoolToString (result.EXITZERO), BoolToString (result.CONT),
					BoolToString (result.RDUE), BoolToString (result.DDUE));
		}
		
		public bool Process (RunningMode runningMode) {
			if (runningMode.GTC) {
				Console.WriteLine ("Generating test configuration...");
				Print ();
				return true;
			} else {
				Console.WriteLine ("Checking test result...");
				bool checkResult = Check ();
				if (! checkResult) {
					Print ();
				} else {
					Console.WriteLine ("Test PASSED");
				}
				return checkResult;
			}
		}
	}
	
	class UnhandledExceptionTester {
		private static TestDescription [] tests = new TestDescription [] {
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:F,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:F,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:F,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:F,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:F,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:F,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:F,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:F,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:F", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:F", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:F,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:F,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:F,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:F,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:F,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:F,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:F,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:F,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:F", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:F", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:T"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:T"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:T"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:T"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:T"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:T"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:T", "EXITZERO:F,CONT:F,RDUE:F,DDUE:T"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:F,HDA:T", "EXITZERO:T,CONT:T,RDUE:F,DDUE:T"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:F,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:F,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:T"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:T"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:T"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:F,DTDA:T,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:T"),
new TestDescription ("USE20:F,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:T"),
new TestDescription ("USE20:F,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:F,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:T"),
new TestDescription ("USE20:F,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:F", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:T", "EXITZERO:F,CONT:F,RDUE:T,DDUE:T"),
new TestDescription ("USE20:F,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:F"),
new TestDescription ("USE20:T,RCIL:T", "DT:T,DA:T,DCIL:T,DTDA:T,HRA:T,HDA:T", "EXITZERO:T,CONT:T,RDUE:T,DDUE:T"),
		};
		
		public static int Main (string [] args) {
			RunningMode runningMode = (args.Length > 0) ? new RunningMode (args [0]) : new RunningMode ("RUNTIME:mono,GTC:F");
			if (args.Length > 1) {
				Console.WriteLine ("Extra arguments unrecognized");
				return 1;
			}
			
			Console.WriteLine ("Starting test run, UseMonoRuntime is {0}, GTC is {1}", runningMode.UseMonoRuntime, runningMode.GTC);
			
			int result = 0;
			
			if (! runningMode.GTC) {
				foreach (TestDescription test in tests) {
					TestRun testRun = new TestRun (test, runningMode);
					if (! testRun.Process (runningMode)) {
						result ++;
					}
				}
			} else {
				for (int i = TestDescription.MIN_CONFIG_CODE; i <= TestDescription.MAX_CONFIG_CODE; i++) {
					TestDescription test = TestDescription.FromCode (i);
					if (test != null) {
						TestRun testRun = new TestRun (test, runningMode);
						testRun.Process (runningMode);
					}
				}
			}
			
			return result;
		}
	}
}


