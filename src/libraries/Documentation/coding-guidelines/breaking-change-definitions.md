Breaking Change Definitions
===========================

Behavioral Change
-----------------

A behavioral change represents changes to the behavior of a member. A behavioral change may including throwing a new exception, adding or removing internal method calls, or alternating the way in which a return value is calculated. Behavioral changes can be the hardest type of change to categorize as acceptable or not - they can be severe in impact, or relatively innocuous.

Binary Compatibility  
--------------------

Refers to the ability of existing consumers of an API to be able to use a newer version without recompilation. By definition, if an assembly's public signatures have been removed, or altered so that consumers can no longer access the same interface exposed by the assembly, the change is said to be a _binary incompatible change_.

Source Compatibility
--------------------

Refers to the ability of existing consumers of an API to recompile against a newer version without any source changes. By definition, if a consumer needs to make changes to its code in order for it to build successfully against a newer version of an API, the change is said to be a _source incompatible change_.

Design-Time Compatibility  
-------------------------

_Design-time compatibility_ refers to preserving the design-time experience across versions of Visual Studio and other design-time environments. This can involve details around the UI of the designer, but by far the most interesting design-time compatibility is project compatibility. A potential project (or solution), must be able to be opened, and used on a newer version of a designer.

Backwards Compatibility  
-----------------------

_Backwards compatibility_ refers to the ability of an existing consumer of an API to run against, and behave in the same way against a newer version. By definition, if a consumer is not able to run, or behaves differently against the newer version of the API, then the API is said to be _backwards incompatible_. 

Changes that affect backwards compatibility are strongly discouraged. All alternates should be actively considered, since developers will, by default, expect backwards compatibility in newer versions of an API.

Forwards Compatibility  
----------------------

_Forwards compatibility_ is the exact reverse of backwards compatibility; it refers to the ability of an existing consumer of an API to run against, and behave in the way against a _older_ version. By definition, if a consumer is not able to run, or behaves differently against an older version of the API, then the API is said to be _forwards incompatible_.

Changes that affect forwards compatibility are generally less pervasive, and there is not as stringent a demand to ensure that such changes are not introduced. Customers accept that a consumer which relies upon a newer API, may not function correctly against the older API.

This document does not attempt to detail forwards incompatibilities.
