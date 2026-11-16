# Enterprise Scenario Testing

Detailed instructions for running these tests is located here:

src\libraries\Common\tests\System\Net\EnterpriseTests\setup\README.md

## PAC claims against a real KDC

The existing Docker environment uses an MIT KDC without Active Directory PAC data.
`RemoteIdentity_RealMitKdcWithoutPac_HasNoSidClaims` checks both `NegotiateAuthentication`
and `NegotiateStream` against that KDC.

To run `RemoteIdentity_RealKdcWithPac_HasSidClaims`, use a Linux client with MIT GSSAPI
and a reachable Active Directory or Samba AD KDC. Configure `krb5.conf` for that realm
and set `KRB5_KTNAME` to a keytab containing the service principal under test.
Set the following environment variables:

| Variable | Value |
| --- | --- |
| `DOTNET_RUNTIME_ENTERPRISETESTS_ENABLED` | `1` |
| `DOTNET_RUNTIME_ENTERPRISETESTS_PAC_ENABLED` | `1` |
| `DOTNET_RUNTIME_ENTERPRISETESTS_PAC_USER` | Client principal, including its realm |
| `DOTNET_RUNTIME_ENTERPRISETESTS_PAC_PASSWORD` | Client principal password |
| `DOTNET_RUNTIME_ENTERPRISETESTS_PAC_TARGET` | Service principal in the server keytab, e.g. `HOST/server.example.com` |
| `DOTNET_RUNTIME_ENTERPRISETESTS_PAC_NAME` | Expected authenticated client principal name |
| `DOTNET_RUNTIME_ENTERPRISETESTS_PAC_USER_SID` | User's directory `objectSid` |
| `DOTNET_RUNTIME_ENTERPRISETESTS_PAC_PRIMARY_GROUP_SID` | Domain SID followed by the user's `primaryGroupID` |
| `DOTNET_RUNTIME_ENTERPRISETESTS_PAC_GROUP_SIDS` | Semicolon-separated exact set of enabled group SIDs carried by the PAC |

Obtain expected SIDs independently from the directory, not from the API under test.
Include the primary group in the expected group set when it is enabled in the PAC.
Use an account with a known group membership and no cross-domain memberships to simplify setup.
Do not commit credentials or keytabs.

From the repository root, after building the Linux runtime and libraries, run:

```bash
./dotnet.sh build src/libraries/System.Net.Security/tests/EnterpriseTests/System.Net.Security.Enterprise.Tests.csproj /t:Test /p:XUnitOptions="-method System.Net.Security.Enterprise.Tests.NegotiateStreamLoopbackTest.RemoteIdentity_RealKdcWithPac_HasSidClaims"
```

Both API variants must return the configured SID claims. Enabling the test with missing
configuration or a KDC that does not issue usable PAC data fails the test rather than skipping it.
