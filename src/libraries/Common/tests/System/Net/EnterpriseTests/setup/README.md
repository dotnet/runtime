# Enterprise Scenario Testing

## What Are Enterprise Scenarios?
There are many definitions for enterprise scenarios. But generally in terms of how .NET Core networking APIs are used, enterprise scenarios are those networking scenarios that are fundamentally used by businesses (a.k.a enterprises) compared with consumers. As such, they use networking components, protocols, and security authentication mechanisms that are not used by most consumers using their home networking and Internet connections.

## Networking Components of Enterprise Scenarios
Enterprise scenarios typically see the following kinds of components/protocols/security:
* Although possibly connected to the Internet, most of the networking topology is internal facing. There is use of some internal “directory” service for authentication of connected computers and users. On Windows, this can include Windows Active Directory domains with computers being domain-controllers, domain-joined, or standalone computers. On Linux, this includes Kerberos realms using KDCs and participating member computers. With .NET Core being cross-platform, this now includes connections with multiple domains and realms with various cross trust between them.
* Authentication protocols such as NTLM, Kerberos and Negotiate. These are used more than Basic and Digest. Negotiate/Kerberos requires both client and server computers to be “joined” to a common trusted directory service.
* TLS/SSL extensively used.
* The use of proxy servers for HTTP/HTTPS communication. These usually include authenticated proxies where the proxy server demands authentication typically with Negotiate or NTLM authentication schemes.
* Complex DNS architectures using various A and CNAME (alias) records.
* Use of the NegotiateStream class. This is typically seen as part of using WCF client/server architecture.
* Impersonation/Delegation of credentials. This occurs frequently in middle-tier scenarios that involve a client computer talking with a middle-tier computer (such as a web server) which makes an outbound call to another computer such as a database server. In this case, the credentials of the client computer are delegated across the middle-tier computer so that the outbound call to the database server is made in the context of the client’s credentials.


## Running the tests
These tests need to be run in the dedicated Enterprise Test environment. This environment can be created on a local dev machine as long as Docker in installed. The enterprise test environment is a collection of Linux docker containers (client and servers) connected on a common docker network.

Set the DOTNET_RUNTIME_REPO_ROOT environment variable to the path on your dev machine (the host machine) where the repo is installed:

```
# Windows cmd.exe shell example
set DOTNET_RUNTIME_REPO_ROOT=s:\GitHub\runtime
```

```bash
# Linux bash shell example
export DOTNET_RUNTIME_REPO_ROOT=/home/me/GitHub/runtime
```

Now you can start up the enterprise system on your dev machine.

```
# Build test machine images
cd %DOTNET_RUNTIME_REPO_ROOT%\src\libraries\Common\tests\System\Net\EnterpriseTests\setup
docker-compose build

# Start up test machines and network
docker-compose up -d

There should be three server containers and client:
- kdc serves as Kerberos key center and Primary Domain Controller using Samba
- apacheweb runs standard web server on port 80 and has various authentication methods enabled
- altweb is identical container but running web on non-standard port. This primarily matters for Kerberos and SPN calculation
- linuxclient is container where the tests actually run

# Connect to the 'linuxclient' container
docker exec -it linuxclient bash
```

At this point, you are in the linuxclient container. It is one "machine" which is part of an enterprise network.

Now build the repo as you would on a regular dev machine:

```bash
cd /repo
./build.sh --subset libs
```

Now you can run the enterprise tests. Currently, there are tests for System.Net.Http and System.Net.Security. You can run them in the same way you already run tests in the repo.


(System.Net.Http example shown)

```bash
cd /repo/src/libraries/System.Net.Http/tests/EnterpriseTests
/repo/dotnet.sh build /t:test
```

You can exit from the container bash shell:

```bash
exit
```

But the containers stay running. You can re-connect to the client again anytime with the same command:

```
docker exec -it linuxclient bash
```

You can edit source code on your local machine and then rebuild and rerun tests as needed.

When you are done with the enterprise test network, you can shut it down from your local dev machine.

```
cd %DOTNET_RUNTIME_REPO_ROOT%\src\libraries\Common\tests\System\Net\EnterpriseTests\setup
docker-compose down
```
