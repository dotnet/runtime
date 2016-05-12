## General Test Infrastructure Notes ##

### Kinds of Build Properties ###
* Build Only
> `<CLRTestKind>BuildOnly</CLRTestKind>`

 * Builds an executable. 
 * Will not execute it. 

* Run Only
> `<CLRTestKind>RunOnly</CLRTestKind>`

 * Can use Ouput of Build and Run Project with different command line arguments. 
* Build and Run
> `<CLRTestKind>BuildAndRun</CLRTestKind>`

 * Builds an executable.
 * Will execute said executable. 
* Shared Libraries
> `<CLRTestKind>SharedLibrary</CLRTestKind>`

 * For building libraries common to zero or more tests. 


By default (i.e. if not specified explicitly) a project file is BuildAndRun.

### Priority ###
Testcases are categorized by their priority levels. The most important subset should be and is the smallest subset. This subset is called priority 0.
 * By default, a testcase is priority 0. You must elect to de-prioritize a test.
  * To de-prioritize a test, add a property _CLRTestPriority_ to the test's project file.
> `<CLRTestPriority>2</CLRTestPriority>`
 * Lower priority values are always run in conjunction when running higher priority value tests. I.e. if a developer elects to do a priority 2 test run, then all priority 0, 1 and 2 tests are run.

### Adding Tests ###
#### Converting an existing C# project ####
  * Remove AssemblyName
  * Swap in dir.props
  * Swap in dir.targets
  * Assign a CLRTestKind
  * (optional) Assign a priority value

