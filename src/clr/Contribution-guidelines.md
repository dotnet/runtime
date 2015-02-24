[CoreFx]: http://http://github.com/dotnet/corefx
[CoreCLR]: http://http://github.com/dotnet/coreclr
#Priorities
The current priority for the team is open sourcing [.NET Core](http://blogs.msdn.com/b/dotnet/archive/2014/12/04/introducing-net-core.aspx).

Because of this and the available bandwidth of our team we are going to have to balance the incoming PRs and issues with our current priority. 

What this means is that we are putting in place a lightweight process for handling incoming PRs and issues to make sure the right set of people are working on the right set of PRs.

#Bug bar
In order for a change to be accepted it needs to have a **demonstrably broad impact** of a **mainline scenario** and be **low risk**.  The change must also meet these [performance requirements](https://github.com/dotnet/coreclr/wiki/Performance).

While the definitions for mainline scenario and risk can be subjective, the area owner is the right person to ascertain them and provide a recommendation.

#Dealing with issues and PRs
-	Please don’t surprise us with a large PR. Before you start work on a PR, please **file an issue** and **work with the area owner** to ensure the issue is a mainline issue and possible ways to fix it.
-	Once a PR is received, it will be assigned to the area owner who is going to evaluate the PR against the bug bar and decide if the change should be accepted

#CoreCLR vs CoreFx
[CoreCLR][CoreCLR] is the repository that contains the .NET runtime and the core library that is tied to the runtime (mscorlib).

[CoreFx][CoreFx] is the repository that contains the new .NET Core libraries which is a better factored surface area that should serve us well going forward. 

What that means is that some of the APIs that have ‘traditionally’ existed inside mscorlib (ie. System.Console) are now moving to their own separate assemblies.

Because of backward compatibility (ie. Phone, full .NET Framework) where the APIs were present in mscorlib, those APIs can’t be removed from mscorlib. This leads to situations where the same (or very similar) code exists in both the [CoreCLR][CoreCLR] repository (as part of mscorlib) and in the [CoreFX][CoreFx] (as part of the new factored assembly).

The rule of thumb is: **Start with the [CoreFX][CoreFx] repository for any managed API changes and the [CoreCLR][CoreCLR] repository for any runtime changes unless otherwise directed by the area owner.** 

#Accepting contributions

For general .NET Contribution guidelines, please refer to them [here](https://github.com/dotnet/corefx/wiki/Contributing).

For coding guidelines for this repository, please do give priority to the current style of the project or file you're changing even if it diverges from the guidelines. 

Because our code is mirrored with our internal source control, changes made to this repository are going to make their  way into future releases of the Desktop and Phone CLR.

There are two broad categories of changes to the repository:

##Code changes
###New features / API surface area
We are moving towards a well factored surface area that should serve us well going forward. With that in mind, we would like to have any new APIs added to the new surface area instead of mscorlib. This means that issues regarding new APIs should move to the [CoreFX][CoreFx] repository where it would go through the normal API review process.

However, there are cases (we believe a small number) where changes should be done in mscorlib. We are going to address those on a case by case basis. 

If you are unsure where the change should go, file an issue on the [CoreFX][CoreFx] repository.

**For any feature that relates to the runtime (GC, JIT, etc) please continue to file issues on the [CoreCLR][CoreCLR] repository.**

###Bug fixes
There are generally two types of changes: functional and performance improvements.

For both these kinds of changes, in order to consider accepting a PR for them, they need to show a **marked improvement** in a **mainline scenario** and have **low risk**.

Bug fixes are generally about correcting a behavior for a give API/feature. The thing to keep in mind when fixing bugs is that there might be code out there that depends on the previous behavior of that API/feature. This means that bug fixes need to be done in such a way as to not impact the behavior of the API/feature on released versions of the .NET framework.

Types of changes that would impact compatibility (the list is by no means complete)

- A different behavior is observed after the change for an input
- A new/different exception is thrown
- An exception is no longer thrown
- A new instance field is added to a type (impacts serialization)
- Additional information can be found here: https://github.com/dotnet/corefx/wiki/Breaking-Changes 

If there is no good way to guard against a breaking change we are going to have to turn down the PR.
###Portability changes
We are most likely going to accept any good changes to bring [CoreCLR][CoreCLR] to new platforms and operating systems.
##Non-code changes
###Formatting changes
Because the code is mirrored with our internal source control system we would like to keep these kinds of changes to a minimum to avoid unnecessary merge conflicts.
We are aware of the inconsistencies in our code base and are looking into ways of fixing that. While we are going to review each PR, the formatting changes are likely going to be turned down.

###Typos
Typos are embarrassing so we are going consider accepting most of these PRs.

In order to keep the number of commits that fix typos we would like to ask that you either focus on a given area with your typos (ie. Mscorlib or the VM folder) or focus on one type of typo across the entire repository.

###Changes to the build system
We are going to consider accepting most of these assuming they provide value to the repository.