//
// AssemblyRunner.cs
//
// Author:
//   Rodrigo Kumpera (rkumpera@novell.com)
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Licensed under the MIT license. See LICENSE file in the project root for full license information.//
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

	public enum RunResult {
		valid,
		unverifiable,
		invalid,
		strict,
		none
	}

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

		static int ExecuteAndFetchReturnCode (String command) {
			ProcessStartInfo psi = new ProcessStartInfo (Environment.ExpandEnvironmentVariables (program), "/c " + command);
			psi.CreateNoWindow = true;
			psi.UseShellExecute = false;
			psi.RedirectStandardError = true;

		    Process activeProcess = Process.Start (psi);

	        activeProcess.WaitForExit();
			int res = activeProcess.ExitCode;
			activeProcess.Close ();

			return res;
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

		static RunResult testWithAppDomain (String path, String assemblyName) {
			AppDomain domain = NewDomain ();
			try {
				domain.ExecuteAssembly (path);
				return RunResult.valid;
			} catch (InvalidProgramException) {
				return RunResult.invalid;
			} catch (FileLoadException) {
				return RunResult.invalid;
			} catch (VerificationException) {
				return RunResult.unverifiable;
			} catch (TypeInitializationException ve) {
				if (ve.InnerException is VerificationException)
					return RunResult.unverifiable;
				Console.WriteLine ("Warning: test {0} thrown exception: {1} ", assemblyName, ve.InnerException);
				return RunResult.invalid;
			} catch (MissingMemberException) {
				return RunResult.invalid;
			} catch (MemberAccessException) {
				return RunResult.unverifiable;
			} catch (TypeLoadException) {
				return RunResult.invalid;
			} catch (BadImageFormatException) {
				return RunResult.invalid; 
			} catch (Exception e) {
				Console.WriteLine ("Warning: test {0} thrown exception {1}", assemblyName, e);
				return RunResult.valid;
			} finally {
				AppDomain.Unload (domain);
			}
		}

		/*
		 * This test with runtime is usefull to assert if the code is unverifiable but not invalid.
		 * This test should be used to diagnose if it's the case of code that was reported as invalid but actually is unverifiable.
		 */
		static RunResult testWithRuntime (String path) {
			String stderr = ExecuteAndFetchStderr (path);
			String[] knownErrors = new String[] {
				"MissingMethodException",
				"InvalidProgramException",
				"FileLoadException",
				"BadImageFormatException",
				"TypeLoadException",
				"VerificationException"
			};

			foreach (String str in knownErrors) {
				if (stderr.IndexOf (str) >= 0)
					return RunResult.invalid;
			} 
			return RunResult.valid;
		}

		/*
		 * This test can only assert if the code is unverifiable or not. Use it
		 * to check if it's the case for a strict check or the code is verifiable.
		 */
		static RunResult testWithPeverify (String path) {
			if (ExecuteAndFetchReturnCode ("peverify "+path) == 0)
				return RunResult.valid;
			return RunResult.unverifiable;
		}

		static RunResult decide (RunResult ad, RunResult rt, RunResult pv, String testName) {
			if (ad == RunResult.valid) {
				if (rt != RunResult.valid) { 
					Console.WriteLine ("Warning: test {0} returned valid under AD but {1} under runtime. PV said {2}, using runtime choice", testName, rt, pv);
					return rt;
				}
				if (pv != RunResult.valid)
					return RunResult.strict;
				return RunResult.valid;
			}
			
			if (ad == RunResult.unverifiable) {
				//the rt test cannot complain about unverifiable

				if (pv == RunResult.valid)
					Console.WriteLine ("Warning: test {0} returned unverifiable under AD but {1} under PV, using AD choice", testName, pv);

				if (rt == RunResult.invalid) {
					/*This warning doesn't help a lot since there are cases which this happens
					Console.WriteLine ("Warning: test {0} returned unverifiable under AD but {1} under runtime. PV said {2}, using runtime choice", testName, rt, pv);
					*/
					
					return rt;
				}

				return RunResult.unverifiable;
			}

			if (ad == RunResult.invalid) {
				//in some cases the runtime throws exceptions meant for invalid code but the code is only unverifiable
				//we double check that by checking if rt returns ok
				
				if (pv == RunResult.valid)
					Console.WriteLine ("Warning: test {0} returned invalid under AD but {1} under PV, using AD choice", testName, pv);

				if (rt == RunResult.valid)
					return RunResult.unverifiable;

				return RunResult.invalid;
			}
			Console.WriteLine ("ERROR: test {0} returned an unknown result under ad {1}, runtime said {2} and PV {3} -- FIXME --", testName, ad, rt, pv);
			return RunResult.none;
		}
		
		static void executeTest (String path, String assemblyName, RunResult expected) {
			RunResult ad = testWithAppDomain (path, assemblyName);
			RunResult rt = testWithRuntime (path);
			RunResult pv = testWithPeverify (path);
			
			RunResult veredict = decide (ad, rt, pv, assemblyName);
			if (veredict != expected)
				Console.WriteLine ("ERROR: test {0} expected {1} but got {2} AD {3} RT {4} PV {5}", assemblyName, expected, veredict, ad, rt, pv);
		}

		public static void Main (String[] args) {
			String dirName = ".";
			if (args.Length > 0)
				dirName = args[0];
			DirectoryInfo dir = new DirectoryInfo (dirName);
			foreach (FileInfo file in dir.GetFiles ()) {
				try {
					RunResult rr = RunResult.none;
					if (file.Name.StartsWith ("strict_"))
						rr = RunResult.strict;
					else if (file.Name.StartsWith ("valid_"))
						rr = RunResult.valid;
					else if (file.Name.StartsWith ("unverifiable_"))
						rr = RunResult.unverifiable;
					else if (file.Name.StartsWith ("invalid_"))
						rr = RunResult.invalid;
						
					if (file.Name.EndsWith (".exe") && rr != RunResult.none)
						executeTest (file.FullName, file.Name, rr);
				} catch (Exception e) {
					Console.WriteLine ("Warning: test {0} thrown exception {1}", file.FullName, e);
				}
			}
		}
	}
}
