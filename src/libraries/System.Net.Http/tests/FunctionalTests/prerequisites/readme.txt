// TODO: Add more details and scripts.
// Issue #4989 (Provide a way for developers to self-instantiate a server for networking API tests)

Some Windows tests require an Active Directory domain. Both the client machine running the tests and the server endpoint need to be on the same domain.

Set up IIS and ASP.NET on the test server. Create two application directories with the following paths:

/test/auth/negotiate
/test/auth/multipleschemes

Set authentication on both application directories to use integrated Windows authentication. Disable Anonymous access.

In the 'multipleschemes' application, you'll use the BasicAuthModule.cs and web.config files in a compiled ASP.NET project. This allows Basic auth to
be offered as well but ensures that it is listed first in the response headers.

Create an additional local user account on the test machine with a specific username and password (see DefaultCredentialsTest.cs).
