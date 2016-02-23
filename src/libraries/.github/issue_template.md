<!--
This template includes boilerplate for different kinds of issues. Select the
one that is most suitable and delete the others.
-->

<!-- ===============
     Template 1: Bug
     =============== -->

Description of the problem

# Affected platforms

# Repro steps

1. One
2. Two
3. Three

# Expected behavior

# Actual behavior

<!-- =============================
     Template 2: Suggest a new API
     ============================= -->

Read http://aka.ms/apireview and include description of the problem.

# Rationale and Usage

Include some sample code that illustrates how the API will be used. If
applicable, include the way it could be done without the API to illustrate
how the new API makes things easier.

# Proposed API

Include an outline of the API. It doesn't have to be complete, but enough to
get a handle on what would be required in terms of surface area.

```C#
namespace System.Foo
{
    public class Fizz
    {
        public Fizz();
        public void int Epic(string message);
    }
}
```

# Details

Include interesting design points. As the discussion on this issue progresses,
you should update this section and include major decisions so that folks joining
later get everything at the top.

<!-- ================================================
     Template 3: Request an existing API to be ported
     ================================================ -->

# Scenario

Explain why you need the API to fulfill your scenario. If applicable, outline
the hoops you've to jump through if the API will not be ported.

# APIs

List the APIs. For individual API just list them. For broad technologies just
use the name of the technology or the top-level namespace (e.g. "XML schema" or
`System.Xml.Schema`).