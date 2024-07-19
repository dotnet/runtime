# Constant propagation and unreachable branch removal

ILLink implements optimization which can propagate constant values across methods and based on these constants determine unreachable branches in code and remove those. This means that the code in the removed branch is not scanned for its dependencies which in turn won't be marked and can potentially be trimmed as well.

## Desired behavior

### Constant propagation

Method can return constant value if its code will always return the same value (and it's possible to statically analyze that as a fact), for example:

```csharp
    public bool Is32Bit { get => false; }
```

On 64bit platforms the property is compiled with constant value, and ILLInk can determine this. It's also possible to use substitutions to overwrite method's return value to a constant via the [substitutions XML file](/docs/tools/illink/data-formats.md#substitution-format).

If such method is used in another method and it influences its return value, it can mean that the caller method will itself always return the same value. For example:

```csharp
    public int SizeOfIntPtr { 
        get {
            if (Is32Bit)
                return 4;
            else
                return 8;
        }
    }
```

ILLink will be able to determine that the call to `Is32Bit` getter will always return `false` and thus the `SizeOfIntPtr` will in turn always return `8`.

### Unreachable branch removal

If some method's return value is detected as constant, it can be possible to optimize conditions in which the return value is used and potentially even remove entire branches of code. For example:

```csharp
    public void CopyMemory ()
    {
        if (Is32Bit)
        {
            CopyUsingDWords ();
        }
        else
        {
            CopyUsingQWords ();
        }
    }
```

In this case if building for 64bit platform the condition will be evaluated as `false` always, and thus the `true` branch of the `if` can be removed. This will in turn lead to also trimming `CopyUsingDWords` method (assuming it's not used from some other place).

### Explicit non-goals

For now ILLink will not inline any method calls. It's relatively tricky to determine if it's possible without breaking the application and leaving the actual calls in place makes debugging more predictable and easier (it's possible to set a breakpoint into the callee's body and it will be hit always).

## Algorithm

The implementation of this optimization is relatively complex since it's solving a potentially global problem in that results of optimization of one method potentially influence results of all methods which call it and so on. But we need the algorithm to work locally without global view. This is necessary because of lazy loading of assemblies, which means that before and during marking it's not guaranteed that all assemblies were discovered and loaded. At the same time this optimization must be complete before a given method is processed by `MarkStep` since we want to not mark dependencies from removed branches.

### Used data structures

* Dictionary of method -> value for all processed methods. The value of a method can be several things depending on the result of processing it:
  * Sentinel value "Processed but not changed" which means the method has been processed and no optimization was done on it. It's unknown if the method returns a constant value or not (yet, analysis hasn't occurred). If nothing needs to know the return value of the method then this can be a final state.
  * Sentinel value "Processed and is not constant" which means the method has been processed and its return value was not detected as constant. This is a final state.
  * Instruction which represents the constant return value of the method if it was detected as returning constant value. This is a final state.

* Processing stack which stores ordered list of processing node, each node representing a method and additional data about it. The stack is processed by always taking the top of the stack and attempting to process that node. Nodes are always added to the top of the stack and are always removed from the top of the stack. In some cases nodes are "moved", that is a node which is not on the top of the stack is moved to the top of the stack. For this reason the stack is implemented as a linked list (so that it's easy to point to nodes in it as well as moves nodes around).

* Helper structure for method -> stack node fast lookups.

### Processing methods

It starts by placing the requested method on top of the stack and then processing the stack until it's empty (at which point the requested method is guaranteed to be processed).

Processing the stack is a loop where:

* Loop until stack is empty
* The top of the stack is peeked (not actually popped) and the method there is processed
  1. The last attempt version of the method is set to the current version of the stack (for loop detection, see below)
  2. The method's body is scanned and all callees which can be used for constant propagation are detected
      * If the called method is already processed its value is used (if it has one)
        * There's an optimization here where methods are only marked as processed without analyzing for their return value. If such method is encountered here, the return value analyzer will run in-place to determine the value of the method (and the result is stored)
      * If the called method is not yet processed and is not on the stack, it's added to the top of the stack
      * If the called method is not yet processed but it's already on the stack, it's moved to the top of the stack - this makes it efficient since this promotes processing of dependencies before the dependents and thus reduces the number of times the dependents must be re-scanned.
  3. If the scan was not fully done because some callees are not yet processed, give up on this method and loop (pick up the new top of the stack)
  4. If the scan was successful
      * If there were not callees with constant values detected, mark the method as "Processed and unchanged" and remove it from the stack - loop
  5. If the method had any constants detected, run the branch removal logic to remove unused branches
  6. Regardless of branch removal results (even if nothing happened) use the new method body and the detected constants to analyze the method if it returns a constant itself - store the result
  7. Mark the method as processed and remove it from the stack - loop

### Dependency loop detection

The above algorithm could lead to endless loops if there's a recursion between multiple methods. For example code like this:

```csharp
void A () {
    if (Helper ())
        DoSomeWork ();

    return B ();
}

void B () {
    if (Helper ())
        DoSomeWork ();

    return A ();
}
```

In this case when `A`'s body is scanned (step 2 above) it will find a call to `B` and add it for processing and back off. Then `B` is top of the stack so it's scanned, and finds `A` to be on the stack but not yet processed. So it moves it to the top of the stack and backs off. Then `A` is processed... and so on.

To avoid this a versioning scheme is used to detect loops. There's a global version number maintained alongside the stack. Every time a new item is added or removed from the stack the stack version is incremented. This is used to detect "something has changed". Each node on the stack stores the stack version from the last time it was attempted to process that node/method. So in the above sample the flow would be something like:

* Stack `StackVersion = 0`
* `A` is added to the stack - `StackVersion = 1`
* `A` is attempted to be processed - `A.LastAttemptVersion = 1`
* `A` detects dependency on `B` and adds `B` to the top of the stack - `StackVersion = 2`
* `B` is attempted to be processed - `B.LastAttemptVersion = 2`
* `B` detects dependency on `A` and moves `A` to the top of the stack - no version changes - still 2
* `A` is attempted to be processed - `A.LastAttemptVersion = 2`
* `A` detects dependency on `B` and moves `B` to the top of the stack - no version changes - still 2
* `B` is attempted to be processed - at this point `B.LastAttemptVersion == 2` and also `StackVersion == 2`

To detect the loop each time a node is about to be processed its `LastAttemptVersion` is checked against the current stack version. If they're equal it means that nothing changes since last time the node was attempted to be processed. So it's expected that processing it again would produce the same results (that is no results, still has unprocessed dependencies). That's the cases where loop is detected.

### Dependency loop resolution

Once the loop is detected the algorithm has to make some changes to avoid looping forever. This is done by force processing one of the methods in the loop and thus removing it from the stack. To do this:

* The method at the top of stack at point of loop detections is processed
* When scanning its dependencies (step 2) it treats all unprocessed dependencies as "processed with non-constant result"
* This means the scan will always succeed (there won't be any unprocessed dependencies blocking the scan from finishing successfully)
* At this point the method is processed as normal - starting with step 4 above. This means the method will be marked as processed with some result and will be removed from the stack
* Since it will be removed from the stack, the stack version will be incremented - this means that the other methods on the stack will not detect loop again and will try to process again

Since this resolves one of the method in the loop, it should break the loop and he algorithm should be able to move on. If that's not the case, the loop detection will kick in with some higher version again and another method will be force-processed and so on.

### Complex loop cases

The above could still lead to undesirable behavior. In the sample above both `A` and `B` have a dependency on `Helper`. So far this was ignored in the description, but given the algorithm above, `Helper` will not be processed before the loop is detected. It will be added onto the stack, but at the end of scanning either `A` or `B` it will never become the top of the stack (since `A` ends with added `B` to the top, and `B` ends with adding `A` to the top). So it will never be even attempted to be processed. So if `Helper` is constant `false` the method which we force-process (`B` in the above example) will not see it that way and will treat it as non-const. This leads to not removing the call to `DoSomeWork`. This is wrong in the sense that the optimization should remove this call. Especially if `Helper` is driven by a [feature switch](https://github.com/dotnet/designs/blob/main/accepted/2020/feature-switch.md) such behavior is highly undesirable (it's not only a size issue where it keeps more code than necessary, but it could mean generating warnings from the code which should be removed).

To mitigate this the algorithm will do one more step before breaking the loop. If the loop is detected it will go over the stack from top to bottom and it will look for the last node with the current version (so basically the last node which is part of the loop, since all nodes which are part of the loop will end up with the same current version). Once it finds that node (there should be at least one more except the one on top of the stack) it will go over all the nodes from that node to the top of the stack and if it finds a node which doesn't have a current version (meaning it's not part of the loop and was not attempted to be processed recently) it will move it to the top of the stack - no version change.

If any such node is found, normal processing will resume. In the above example this will mean that `Helper` gets to the top of the stack, will be processed and removed from the stack (stack version increments). Now `A` and `B` will be processed again, eventually detecting the loop again. At this point the search for nodes which are not current version will end up empty. Only at that point the loop will be broken by force-processing one of the methods in the loop.

For illustration the flow with `Helper` being considered. A `Main` is added to put another method which illustrates the algorithm better:

* Stack `StackVersion = 0`
* `Main` is added to the stack - `StackVersion = 1`
* `Main` is attempted to be processed - `Main.LastAttemptVersion = 1`
* `A` is added to the stack - `StackVersion = 2`
* `A` is attempted to be processed - `A.LastAttemptVersion = 2`
* `A` detects dependency on `Helper` and adds `Helper` to the top of the stack - `StackVersion = 3`
* `A` detects dependency on `B` and adds `B` to the top of the stack - `StackVersion = 4`
* `B` is attempted to be processed - `B.LastAttemptVersion = 4`
* `B` detects dependency on `Helper` and moves `Helper` to the top of the stack - no version change - still 4
* `B` detects dependency on `A` and moves `A` to the top of the stack - no version changes - still 4
* `A` is attempted to be processed - `A.LastAttemptVersion = 4`
* `A` detects dependency on `Helper` and moves `Helper` to the top of the stack - no version change - still 4
* `A` detects dependency on `B` and moves `B` to the top of the stack - no version changes - still 4
* `B` is attempted to be processed - at this point `B.LastAttemptVersion == 4` and also `StackVersion == 4`

At this point the stack looks like this (top is the first line below) - `StackVersion == 4`:

* `B` - `LastAttemptVersion = 4`
* `Helper` - `LastAttemptVersion = -1` (never attempted to be processed)
* `A` - `LastAttemptVersion = 4`
* `Main` - `LastAttemptVersion = 1`

The algorithm above will go over the stack to find the "oldest" node with version `4` (the current version) - and will find `A`. Then it will go from `A` back to the top searching for nodes with version `!= 4`, it will find `Helper`. It moves `Helper` to the top of the stack. Then normal processing resumes

* `Helper` is processed fully (no dependencies) - removed from the stack - `StackVersion = 5`
* `B` is attempted to be processed - `B.LastAttemptVersion = 5`
* `B` detects dependency on `A` and moves `A` to the top of the stack - no version changes - still 5
* `A` is attempted to be processed - `A.LastAttemptVersion = 5`
* `A` detects dependency on `B` and moves `B` to the top of the stack - no version changes - still 5
* `B` is attempted to be processed - at this point `B.LastAttemptVersion == 5` and also `StackVersion == 5`

A loop is detected again - `StackVersion == 5` - the stack looks like this:

* `B` - `LastAttemptVersion = 5`
* `A` - `LastAttemptVersion = 5`
* `Main` - `LastAttemptVersion = 1`

The scan over the stack won't find any methods to move to the top. So it will break the loop by force-processing `B` and considering its dependency on `A` as non-const. `B` is removed from the stack - `StackVersion = 6`. Processing removes and now `A` will process fully since all of its dependencies are resolved and so on...

## Alternatives and improvements

### Use actual recursion in the analyzer

The processing of methods is recursive in nature since callers needs to know results of processing callees. To avoid actual recursion in the analyzer, the nodes are stored in the processing stack. If the necessary results are not yet known for a given method, the current method is postponed (moves down on the stack) and it will be retried later on. This is potentially expensive. An optimization would be to allow a limited recursion within the analyzer and only rely on the processing stack in cases a recursion limit is reached.

### Avoid scanning of potentially removed branches

Currently the scanning (step 2 above) goes over all instructions in the method's body and will request processing of all called methods. This means that even if the called method is behind a feature check which is disabled, it will still be processed (and all of its dependencies will be processed as well). In the end that branch will be removed and none of those methods will end up being marked. All the processing of those methods will be effectively thrown away.

To improve this behavior we would need to merge the scanning with the constant condition detection and branch removal. Basically steps 2-5 would have to become one. The idea is that the scanning would only request processing for methods which are on the main path through the method or in branches which can't be removed. This would probably mean that scanning would have to give up sooner (currently it always goes over the whole method body requesting processing of all dependencies) which wold likely lead to more frequent re-scanning of the method (to eventually reach the end). The advantage would be the potential to not process methods which are not needed. An experiment would have to be done to measure the numbers to determine if this is actually a beneficial change.
