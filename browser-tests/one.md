Read the `browser-tests/plan.md`
Run the steps in `browser-tests/before-testing.md`.
Then we want to run `browser-tests/test-suite.md` process for `System.Runtime.Tests`. 


Read the `browser-tests/plan.md`
Run the steps in `browser-tests/before-testing.md` except building the runtime, that already done.
Then we want to run `browser-tests/test-suite.md` process for `System.Runtime.Tests`

Read the `browser-tests/plan.md`
The before testing steps are already done.
Now we want to run `browser-tests/test-suite.md` process for `System.Collections.Immutable.Tests`

let's run remaining tests from the plan.
- process them in order as they are listed in the Test Suites to Run
- process only one test suite at the time, while following browser-tests/test-suite.md
- it's fine to use browser-tests/run-test-suite.sh to do that, when it works
- create Summary.md after each suite
- update plan.md after each suite
- don't skip any suite from the list, even if they look like for windows
- work unattended