//
// AssemblyRunner.cs
//
// Author:
//   Rodrigo Kumpera (rkumpera@novell.com)
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;
using System.Threading;


namespace AssemblyRunner {

	public class Runner {
	    private static string program = "\"%COMSPEC%\"";

		static String ExecuteAndFetchStderr (String command) {
			ProcessStartInfo psi = new ProcessStartInfo (Environment.ExpandEnvironmentVariables (program), "/c " + command);
			psi.CreateNoWindow = true;
			psi.UseShellExecute = false;
			psi.RedirectStandardError = true;

		    string stderr=null;
		    Process activeProcess=null;

			Thread stderrThread = new Thread (new ThreadStart (delegate {
				if (activeProcess != null)
					stderr = activeProcess.StandardError.ReadToEnd ();
			}));
			activeProcess = Process.Start (psi);
			
			stderrThread.Start ();
	        activeProcess.WaitForExit();
			stderrThread.Join();
			activeProcess.Close ();
			return stderr;
	    }

		static AppDomain NewDomain () {
			PolicyStatement statement = new PolicyStatement(new PermissionSet(PermissionState.None),PolicyStatementAttribute.Nothing);
			PermissionSet ps = new PermissionSet(PermissionState.None);
			ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
			ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.Assertion));
			ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.ControlAppDomain));
			ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.ControlDomainPolicy));
			ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.ControlEvidence));
			ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.ControlPolicy));
			ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.ControlPrincipal));
			ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.ControlThread));
			ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.Infrastructure));
			ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.RemotingConfiguration));
			ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.SerializationFormatter));
			ps.AddPermission(new FileIOPermission(PermissionState.Unrestricted));
			ps.AddPermission(new EnvironmentPermission(PermissionState.Unrestricted));
			ps.AddPermission(new ReflectionPermission(PermissionState.Unrestricted));
			ps.AddPermission(new RegistryPermission(PermissionState.Unrestricted));
			ps.AddPermission(new IsolatedStorageFilePermission(PermissionState.Unrestricted));
			ps.AddPermission(new EventLogPermission(PermissionState.Unrestricted));
			ps.AddPermission(new PerformanceCounterPermission(PermissionState.Unrestricted));
			ps.AddPermission(new DnsPermission(PermissionState.Unrestricted));
			ps.AddPermission(new UIPermission(PermissionState.Unrestricted));
   			PolicyStatement statement1 = new PolicyStatement(ps,PolicyStatementAttribute.Exclusive);
			CodeGroup group;
			group = new UnionCodeGroup(new AllMembershipCondition(),statement);
			group.AddChild(new UnionCodeGroup(new ZoneMembershipCondition(SecurityZone.MyComputer),statement1));
			PolicyLevel level = PolicyLevel.CreateAppDomainLevel();
			level.RootCodeGroup = group;

			AppDomain domain = AppDomain.CreateDomain ("test");
			domain.SetAppDomainPolicy(level);
			return domain;
		}

		static void executeTest (String assembly, String path) {
			String op = assembly.Substring (0, assembly.IndexOf ("_"));
			AppDomain domain = NewDomain ();
			try {
				domain.ExecuteAssembly (path);
				if (!op.Equals ("valid"))
					Console.WriteLine ("Test returned valid: "+assembly);
			} catch (InvalidProgramException ipe) {
				if (!op.Equals ("invalid"))
					Console.WriteLine ("Test returned invalid: "+assembly);
			} catch (FileLoadException ve) {
				if (!op.Equals ("invalid"))
					Console.WriteLine ("Test returned invalid: "+assembly);
			} catch (VerificationException ve) {
				RecheckUnverifiableResult (path, assembly, op);
			} catch (TypeInitializationException ve) {
				if (ve.InnerException is VerificationException)
					RecheckUnverifiableResult (path, assembly, op);
				else
					Console.WriteLine ("Warning: test thrown exception: "+assembly);
			} catch (MissingMemberException ve) {
				if (!op.Equals ("invalid"))
					Console.WriteLine ("Test returned invalid: "+assembly);
			} catch (MemberAccessException ve) {
				if (!op.Equals ("unverifiable"))
					Console.WriteLine ("Test returned unverifiable: "+assembly);
			} catch (TypeLoadException ve) {
				if (!op.Equals ("unverifiable"))
					Console.WriteLine ("Test returned unverifiable: "+assembly);
			} catch (Exception e) {
					Console.WriteLine ("Warning: test thrown exception: "+assembly);
			} finally {
				AppDomain.Unload (domain);
			}
		}

		/*
		This method exists because sometimes a VerificationException is throw for invalid code, so we try to run it as standard-alone a check again.
		*/
		static void RecheckUnverifiableResult (String path, String assembly, String op) {
			String stderr = ExecuteAndFetchStderr (path);
			bool invalid = stderr.IndexOf ("InvalidProgramException") >= 0 || stderr.IndexOf ("FileLoadException") >= 0;

			if (invalid) {
				if (!op.Equals ("invalid"))
					Console.WriteLine ("Test returned invalid: "+assembly);
			}else if (!op.Equals ("unverifiable"))
				Console.WriteLine ("Test returned unverifiable: "+assembly);
		}

		public static void Main (String[] args) {
			String dirName = ".";
			if (args.Length > 0)
				dirName = args[0];
			DirectoryInfo dir = new DirectoryInfo (dirName);
			foreach (FileInfo file in dir.GetFiles ()) {
				try {
				if (file.Name.EndsWith (".exe") && (file.Name.StartsWith ("valid_") || file.Name.StartsWith ("unverifiable_") || file.Name.StartsWith ("invalid_")))
					executeTest (file.Name, file.FullName);
				} catch (Exception e) {
				}
			}
		}
	}
}