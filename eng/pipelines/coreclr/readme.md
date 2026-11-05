# CoreCLR build infrastructure

## Azure Pipelines

   The following is the matrix of test runs that we have. This is
   duplicated for each OS/architecture (e.g `linux-x64`) combination in `platform-matrix.yml`.

```
   Product build       Test build              Test run
   (Azure DevOps)      (Azure DevOps)          (helix)

   --------------------------------------------------------------------------

   Debug

   Checked ----------> Pri0 -----------------> plain runtests
           |
           \---------> Pri1 -----------------> plain runtests
           |                \----------------> jitstress
           |                \----------------> gcstress
           |                \----------------> runincontext
           |                \----------------> maybe more (dynamically selected runtest modes)
           |
           \---------> Pri1 crossgen --------> plain runtests
                                     \-------> jitstress
                                     \-------> gcstress
                                     \-------> maybe more (dynamically selected runtest modes)

   Release ----------> Pri1 -----------------> plain runtests
           |
           \---------> Pri1 crossgen --------> plain runtests
```

Each build or test job is defined in Azure DevOps and will show
up in the UI in the order in which they are defined here. The
build and test build job matrix is defined statically, but
queue-time inputs can be used to control whether a job executes
(used to select which jobs run in ci vs for official builds), or
to select test modes. This should eventually be used to enable
requesting specific test runs from pull requests.

### Templates used to define `jobs`

Please update this if the factoring changes.

The Crossgen2, GC, and other runtime-test pipelines use
`templates/runtime-test-pipeline.yml` for pipeline resources and the Build
stage's Helix monitor. Their entrypoint files retain triggers, schedules, and
platform coverage. They import `templates/runtime-test-variables.yml` at the
entrypoint so common variables are available before the `jobs` parameter's
templates expand. The pipeline template accepts a `jobs` list so specialized
library-test and Crossgen2 comparison jobs can remain explicit.

`templates/runtime-test-jobs.yml` defines a product-build matrix with an ordered
`testRuns` list. Each entry supplies parameters to
`common/templates/runtimes/build-runtime-tests-and-send-to-helix.yml` relative
to `eng/pipelines/`. The product and tests build in the same job. Additional runs
can explicitly use `generatelayoutonly` to reuse the test build and
`runAfterPreviousFailure` to allow independent submissions after a previous
run fails. Keep run names unique within a job because they identify logs and
test-build success variables.

The JIT outerloop wrapper also uses `runtime-test-jobs.yml`, while retaining
its own scenario selection and additional ISA library-test stages.

This file defines the set of jobs in a platform-agnostic manner,
using the `platform-matrix.yml` template. This will create one job
for each platform from the passed-in jobTemplate (either a build
job or a test job). The `build-job.yml` and `test-job.yml` templates
use `xplat-job.yml` to handle some of the common logic for
abstracting over platforms. Finally, `xplat-job.yml` uses the arcade
`job.yml` job template, which sets up telemetry and signing support.

```
internal.yml -> platform-matrix.yml -------> build-job.yml -------> xplat-job.yml -> job.yml
                                    |  (passed-in jobTemplate)  |                    (arcade)
                                    \------> test-job.yml ------/
                                    \------> format-job.yml ----/
```
