using System;
using System.Collections;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

class Program {

	// note: you cannot load a file directly into a PermissionSet
	// but we can hack around this by using PermissionSetAttribute ;-)
	static PermissionSet LoadFromFile (string filename)
	{
		// the SecurityAction is meaningless here
		PermissionSetAttribute psa = new PermissionSetAttribute (SecurityAction.Demand);
		psa.File = filename;
		return psa.CreatePermissionSet ();
	}

	// source: http://blogs.msdn.com/shawnfa/archive/2004/10/22/246549.aspx	
	static PermissionSet GetNamedPermissionSet (string name)
	{
		bool foundName = false;
		PermissionSet pset = new PermissionSet (PermissionState.Unrestricted);

		IEnumerator e = SecurityManager.PolicyHierarchy ();
		while (e.MoveNext ()) {
			PolicyLevel pl = e.Current as PolicyLevel;

			PermissionSet levelpset = pl.GetNamedPermissionSet (name);
			if ((levelpset != null) && (pset != null)) {
				foundName = true;
				pset = pset.Intersect (levelpset);
			}
		}

		if (pset == null || !foundName)
			return new PermissionSet (PermissionState.None);

		return new NamedPermissionSet (name, pset);
	}

	// source: http://blogs.msdn.com/shawnfa/archive/2004/10/25/247379.aspx
	static AppDomain CreateRestrictedDomain (string filename)
	{
		PermissionSet emptySet = new PermissionSet (PermissionState.None);
		PolicyStatement emptyPolicy = new PolicyStatement (emptySet);
		UnionCodeGroup root = new UnionCodeGroup (new AllMembershipCondition (), emptyPolicy);

		PermissionSet userSet = null;
		if (filename [0] == '@')
			userSet = GetNamedPermissionSet (filename.Substring (1));
		else
			userSet = LoadFromFile (filename);

		PolicyStatement userPolicy = new PolicyStatement (userSet);
		root.AddChild (new UnionCodeGroup (new AllMembershipCondition (), userPolicy));

		PolicyLevel pl = PolicyLevel.CreateAppDomainLevel ();
		pl.RootCodeGroup = root;

		AppDomain ad = AppDomain.CreateDomain ("Restricted");
		ad.SetAppDomainPolicy (pl);
		return ad;
	}

	static int Main (string[] args)
	{
		switch (args.Length) {
		case 0:
			Console.WriteLine ("Create a restricted sandbox to execute an assembly.");
			Console.WriteLine ("Usage: mono sandbox.exe [@namedpermissionset | permissionset.xml] assembly.exe [parameters ...]");
			return 0;
		case 1:
			Console.WriteLine ("Using default (current) appdomain to load '{0}'...", args [0]);
			return AppDomain.CurrentDomain.ExecuteAssembly (args [0]);
		case 2:
			AppDomain ad = CreateRestrictedDomain (args [0]);
			return ad.ExecuteAssembly (args [1]);
		default:
			ad = CreateRestrictedDomain (args [0]);
			string[] newargs = new string [args.Length - 2];
			for (int i=2; i < args.Length; i++)
				newargs [i-2] = args [i];
			return ad.ExecuteAssembly (args [1], null, newargs);
		}
	}
}
