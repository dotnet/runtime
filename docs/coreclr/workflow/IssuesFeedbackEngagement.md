
# Engage, Log Issues and Provide Feedback

## Reporting Problems (Bugs)

We track bugs, feature requests and other issues [in this repo](https://github.com/dotnet/coreclr/issues). 
If you have a problem and believe that the issue is in the native runtime you should log it there. If in the managed code log it in the [CoreFX repo](https://github.com/dotnet/corefx/issues) _even if the code is in this CoreCLR repo_ (ie., in mscorlib/System.Private.Corelib). The reason for this is we sometimes move managed types between the two and it makes sense to keep all the issues together.

Before you log a new issue, you should try using the search tool on the issue page on a few keywords to see if the issue was already logged.  

### NET Forums 
If you want to ask a question, or want wider discussion (to see if others share you issue), we encourage you to start a thread 
in the [.NET Foundation forums](http://forums.dotnetfoundation.org/). 

###Chat with the CoreCLR Community

For more real-time feedback you can also start a chat session by clicking on the icons below.  

[![.NET Slack Status](https://aspnetcoreslack.herokuapp.com/badge.svg?2)](http://tattoocoder.com/aspnet-slack-sign-up/) [![Join the chat at https://gitter.im/dotnet/coreclr](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/coreclr?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
 
### Security issues

To avoid tipping off malicious users wishing to exploit a vulnerability, 
security issues and bugs should be reported privately, via email, to the
Microsoft Security Response Center (MSRC) <secure@microsoft.com>. You should
receive a response within 24 hours. If for some reason you do not, please follow
up via email to ensure we received your original message. Further information,
including the MSRC PGP key, can be found in the
[Security TechCenter](https://technet.microsoft.com/en-us/security/ff852094.aspx) 

### Issues with the .NET Desktop runtime

The .NET Core repository is not an appropriate place to log issues for the 'Desktop' .NET Framework built into the Windows 
operating system.  If you are having issues with the Full .NET Framework or .NET Runtime the best ways to file a bug 
are at [Connect](http://connect.microsoft.com/VisualStudio) or through
[Product Support](https://support.microsoft.com/en-us/contactus?ws=support) if you have a contract.

