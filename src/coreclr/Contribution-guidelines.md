#General .NET Contribution Guidelines

For general .NET Contribution guidelines, please refer to them [here](https://github.com/dotnet/corefx/wiki/Contributing).

For coding guidelines for this repository, please do give priority to the current style of the project or file you're changing even if it diverges from the guidelines. 

#Contribution Guidelines for the CoreCLR repo

Because the code is mirrored with our internal source control, changes made to this repo are going to make their way into future releases of the Desktop and Phone CLR.

Changes to the repo can be divided into two categories

##Code changes
###Performance improvements
As long as the change is purely an optimization, does not have a compatibility impact and shows a marked improvement we are going to accept it.

###New features / API surface area
We are moving towards a well factored surface area that should serve us well going forward. With that in mind, we would like to have any new APIs added to the new surface area instead of mscorlib. This means that issues regarding new APIs should move to the CoreFx repo where it would go through the normal API review process.

However, there are cases (we believe a small number) where changes should be done in mscorlib. We are going to address those on a case by case basis.

**For any feature that relates to the runtime (GC, JIT, etc) please continue to file issues on the CoreCLR repo.**

###Bug fixes
Bug fixes are generally about correcting a behavior for a give API/feature. The thing to keep in mind when fixing bugs is that there might be code out there that depends on the previous behavior of that API/feature. This means that bug fixes need to be done in such a way as to not impact the behavior of the API/feature on released versions of the .NET framework.

Types of changes that would impact compatibility (the list is by no means complete)

- A different behavior is observed after the change for an input
- A new/different exception is thrown
- An exception is no longer thrown
- A new instance field is added to a type (impacts serialization)
- Additional information can be found [on the CoreFx wiki](https://github.com/dotnet/corefx/wiki/Breaking-Changes): 

In the framework we have a couple of way to preserve backwards compatibility

- Using if-defs around the code.
- Using the mechanisms defined in the AppContext class. 

If there is no good way to guard against a breaking change we are going to have to turn down the PR.
##Non-code changes
###Formatting changes
Because the code is mirrored with our internal source control system we would like to keep these kinds of changes to a minimum to avoid unnecessary merge conflicts.
We are aware of the inconsistencies in our code base and are looking into ways of fixing that. While we are going to review each PR, the formatting changes are likely going to be turned down.
###Typos
Typos are embarrassing so we are going to accept most of these PR.

In order to keep the number of commits that fix typos we would like to ask that you either focus on a given area with your typos (ie. Mscorlib or the VM folder) or focus on one type of typo across the entire repo.

###Changes to the build system
We are most likely going to accept all of these assuming they provide value to the repo
