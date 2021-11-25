__System.Diagnostics.DebuggerBrowsable__

DebuggerBrowsableState:

Evaluation of an object with properties decorated with DebuggerBrowsable:
 - Collapsed - it is displayed normally, it means:
    - Simple type is displayed as:

            object_name > property_name,
    
    - Collection / Array is displayed as:

            object_name > property_name > property_idx(s), propery_value(s).
 - RootHidden:
    - Simple type - it is not displayed in the debugger window.
    - Collection / Array - its root is not displayed, so the values of a collection are appearing in a flat view.
 - Never - it is not displayed in the debugger window.

DebuggerBrowsable does not affect direct evaluation of an object propoerty, e.g. calling myObject.neverBrowsableProperty, decorated with *[DebuggerBrowsable(DebuggerBrowsableState.Never)]* will result in displaying the value regardless of the decorator.