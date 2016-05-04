// TODO: Add more details and scripts.
// Issue #4989 (Provide a way for developers to self-instantiate a server for networking API tests)

Some Windows tests require an Active Directory domain. Both the client machine running the tests and the server endpoint need to be on the same domain.

Set up IIS and ASP.NET on the test server. Create an application directory with the necessary path (see DefaultCredentialsTest.cs).
The directory should only have Integrated Windows authentication access enabled. Disable Anonymous access.

Create an additional local user account on the test machine with a specific username and password (see DefaultCredentialsTest.cs).
