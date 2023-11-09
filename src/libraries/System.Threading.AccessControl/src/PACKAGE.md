## About

<!-- A description of the package and where one can find more documentation -->

System.Threading.AccessControl provides types that enable you to control access to threading synchronization primitives.
This includes the ability to control access to Mutexes, Semaphores, and Events using Windows Access Control Lists (ACLs).

## Key Features

<!-- The key features of this package -->

* Extension methods to allow ACL modifications on `Mutex`, `Semaphore`, and `EventWaitHandle`.
* Simplified security management for threading synchronization objects.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

```csharp
using System.Security.AccessControl;
using System.Security.Principal;

// Create a string representing the current user.
string user = $"{Environment.UserDomainName}\\{Environment.UserName}";

// Create a security object that grants no access
MutexSecurity mutexSecurity = new MutexSecurity();

// Add a rule that grants the current user the right to enter or release the mutex
MutexAccessRule rule = new MutexAccessRule(user, MutexRights.Synchronize | MutexRights.Modify, AccessControlType.Allow);
mutexSecurity.AddAccessRule(rule);

// Add a rule that denies the current user the right to change permissions on the mutex
rule = new MutexAccessRule(user, MutexRights.ChangePermissions, AccessControlType.Deny);
mutexSecurity.AddAccessRule(rule);

// Display the rules in the security object
ShowSecurity(mutexSecurity);

// Add a rule that allows the current user the right to read permissions on the mutex
// This rule is merged with the existing Allow rule
rule = new MutexAccessRule(user, MutexRights.ReadPermissions, AccessControlType.Allow);
mutexSecurity.AddAccessRule(rule);

// Display the rules in the security object
ShowSecurity(mutexSecurity);

static void ShowSecurity(MutexSecurity security)
{
    Console.WriteLine("\nCurrent access rules:\n");

    foreach (MutexAccessRule ar in security.GetAccessRules(true, true, typeof(NTAccount)))
    {
        Console.WriteLine($"   User: {ar.IdentityReference}");
        Console.WriteLine($"   Type: {ar.AccessControlType}");
        Console.WriteLine($" Rights: {ar.MutexRights}");
        Console.WriteLine();
    }
}

/*
 * This code example produces output similar to following:
 * 
 * Current access rules:
 * 
 *    User: TestDomain\TestUser
 *    Type: Deny
 *  Rights: ChangePermissions
 * 
 *    User: TestDomain\TestUser
 *    Type: Allow
 *  Rights: Modify, Synchronize
 * 
 * 
 * Current access rules:
 * 
 *    User: TestDomain\TestUser
 *    Type: Deny
 *  Rights: ChangePermissions
 * 
 *    User: TestDomain\TestUser
 *    Type: Allow
 *  Rights: Modify, ReadPermissions, Synchronize
 */
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Threading.EventWaitHandleAcl`
* `System.Threading.MutexAcl`
* `System.Threading.SemaphoreAcl`
* `System.Threading.ThreadingAclExtensions`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.threading?view=dotnet-plat-ext-7.0)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Threading.AccessControl is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
