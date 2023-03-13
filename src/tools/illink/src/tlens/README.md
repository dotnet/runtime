# Trimming Lens

## Introduction

Trimming Lens is an advanced tool for developers to help them explore ways to reduce their trimmed app or library size. The tool does not replace ILLinker but it's intended to be used in combination with it to produce smaller outputs.

The tool uses lenses to explore different size optimization opportunities which ILLinker cannot easily do or it would not have such impact as when the code is refactored by the developer. You could use different lenses to have a compounding effect in the way that multiple lense can zoom out the same code to highlight areas for improvements. The impact of each lense varies on the library or application the tool is used with and it's common after optimizing one problem that more opportunities are found by the different lense.

## Available lenses

The list of all available lenses and their names can be found by running the tool with `-h` option. Currently available list covers

```
  duplicated-code            Methods which are possible duplicates
  fields-init                Constructors re-initializing fields to default values
  fields-unread              Fields that are set but never read
  ifaces-dispatch            Interfaces which are called sparsely
  ifaces-types               Interfaces with implementation but no type reference
  inverted-ctors             Constructors calling same type constructor with default values
  large-arrays               Methods creating large arrays
  large-cctors               Types with large static constructor
  large-strings              Methods using large strings literals
  operator-null              User operators used for null check
  single-calls               Methods called sparsely
  single-construction        Types with limited number of constructions
  unused-param               Methods with unused parameters
```

Each lense has its own rules and format how the findings are reported. In some cases, the findings are mere suggestions which need to be validated by checking broader context to determine if the area should be refactored or whether individual fixes would meet the size goals.

## Usage

```
tlens [options] input-files

Options:
  -l, --lens=NAME            NAME of the lens to use. Default set is used if none is specified.
  -h, --help                 Show this message and exit.
      --limit=VALUE          Maximum number of findings reported by lens (defaults to 30).
```

Consider following example where `tlens` can show suggestion for the code to be more trimmable.

```c#
interface IOperation
{
    void Run ();
}

class CommonOperation : IOperation
{
    void IOperation.Run ()
    {
        // Pulls complex dependencies
    }
}

class RareOperation : IOperation
{
    void IOperation.Run ()
    {
        // Pulls complex dependencies
    }

    public void Run (IOperation operation)
    {
        operation.Run();
    }
}

class App
{
    public static void Main ()
    {
        var rare = new RareOperation ();
        rare.Run (rare);
    }
}
```

if you run `tlens -l ifaces-dispatch myapp.dll` you should see a report like this one generated to the output.

```
=======================================
Possibly Optimizable Interface Dispatch
=======================================

Interface IOperation is implemented 2 times and called only at
	RareOperation::Run(IOperation)
```

One can argue that ILLinker should be able to figure this out automatically and rewrite the methods to do direct all but that's often very hard to do and it might not solve the problem most efficiently. Here the developer can decide to refactor the body of `RareOperation::IOperation.Run` into static helper method and avoid using interface dispatch altogether and possibly allowing ILLinker to remove `IOperation` method from `CommonOperation` class.

