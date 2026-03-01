# API Review Process

.NET has a long standing history of taking API usability extremely seriously. Thus, we generally review every single API that is added to the product. This page discusses how we conduct design reviews for components that are open sourced.

## Which APIs should be reviewed?

We review all publicly exposed API proposed to be added to the product components that are built out of this repo. Those are in both the `System.*` and `Microsoft.*` namespaces. In some cases, we also review APIs that are added to other components outside of this repo. We mostly do this for high impact APIs, such as Roslyn, and when both the owner of the technology and we feel there is win-win for both sides if we review the APIs. However, we can't scale to review all APIs being added to the .NET ecosystem.

## Process

```mermaid
sequenceDiagram
    participant R as Requester
    participant O as Runtime Owners
    participant F as API review board <br> (FXDC)
    R ->> O: Files issue under dotnet/runtime
    note over O: Assigns owner
    
    note over R, O: Discussion

    O ->> F: Label api-ready-for-review
    
    note over F: Performs review

    alt is accepted
        F ->> R: Label api-approved
    else is work needed
        F ->> O: Label api-needs-work
    else is rejected
        F ->> R: Issue is closed
    end
```

## Steps

1. **Requester files an issue**. The issue description should contain a speclet that represents a sketch of the new APIs, including samples on how the APIs are being used. The goal isn't to get a complete API list, but a good handle on how the new APIs would roughly look like and in what scenarios they are being used. Please use [this template](https://github.com/dotnet/runtime/issues/new?assignees=&labels=api-suggestion&template=02_api_proposal.yml&title=%5BAPI+Proposal%5D%3A+). The issue should have the label `api-suggestion`. Here is [a good example](https://github.com/dotnet/runtime/issues/38344) of an issue following that template.

2. **We assign an owner**. We'll assign a dedicated owner from our side that
sponsors the issue. This is usually [the area owner](issue-guide.md#areas) for which the API proposal or design change request was filed for.

3. **Discussion**. The goal of the discussion is to help the assignee to make a
decision whether we want to pursue the proposal or not. In this phase, the goal
isn't necessarily to perform an in-depth review; rather, we want to make sure
that the proposal is actionable, i.e. has a concrete design, a sketch of the
APIs and some code samples that show how it should be used. If changes are necessary, the owner will set the label `api-needs-work`. To make the changes, the requester should edit the top-most issue description. This allows folks joining later to understand the most recent proposal. To avoid confusion, the requester can maintain a tiny change log, like a bolded "Updates:" followed by a bullet point list of the updates that were being made. When the feedback is addressed, the requester should notify the owner to re-review the changes.

4. **Owner makes decision**. When the owner believes enough information is available to make a decision, they will update the issue accordingly:

    * **Mark for review**. If the owner believes the proposal is actionable, they will label the issue with `api-ready-for-review`. [Here is a good example](https://github.com/dotnet/runtime/issues/15725) of a strong API proposal.
    * **Close as not actionable**. In case the issue didn't get enough traction to be distilled into a concrete proposal, the owner will close the issue.
    * **Close as won't fix as proposed**. Sometimes, the issue that is raised is a good one but the owner thinks the concrete proposal is not the right way to tackle the problem. In most cases, the owner will try to steer the discussion in a direction that results in a design that we believe is appropriate. However, for some proposals the problem is at the heart of the design which can't easily be changed without starting a new proposal. In those cases, the owner will close the issue and explain the issue the design has.
    * **Close as won't fix**. Similarly, if proposal is taking the product in a direction we simply don't want to go, the issue might also get closed. In that case, the problem isn't the proposed design but in the issue itself.

5. **API gets reviewed**. The group conducting the review is called *FXDC*, which stands for *framework design core*. In the review, we'll take notes and provide feedback. Reviews are streamed live on [YouTube](https://www.youtube.com/@NETFoundation/streams). After the review, we'll publish the notes in the [API Review repository](https://github.com/dotnet/apireviews) and at the end of the relevant issue. A good example is the [review of immutable collections](https://github.com/dotnet/apireviews/tree/main/2015/01-07-immutable). Multiple outcomes are possible:

    * **Approved**. In this case the label `api-ready-for-review` is replaced
    with `api-approved`.
    * **Needs work**. In case we believe the proposal isn't ready yet, we'll
    replace the label `api-ready-for-review` with `api-needs-work`.
    * **Rejected**. In case we believe the proposal isn't a direction we want to go after, we simply write a comment and close the issue.

## Review schedule

 There are three methods to get an API review:

* **Get into the backlog**. Generally speaking, filing an issue in `dotnet/runtime` and applying the label `api-ready-for-review` on it will make your issue show up during API reviews. The downside is that we generally walk the backlog oldest-newest, so your issue might not be looked at for a while. Progress of issues can be tracked via https://aka.ms/ready-for-api-review.
* **Fast track**. If you need to bypass the backlog apply both `api-ready-for-review` and `blocking`. All blocking issues are looked at before we walk the backlog.
* **Dedicated review**. This only applies to area owners. If an issue you are the area owner for needs an hour or longer, send an email to FXDC and we book dedicated time. Rule of thumb: if the API proposal has more than a dozen APIs and/or the APIs have complex policy, then you need 60 min or more. When in doubt, send mail to FXDC.

API Review meetings typically occur once per week, on Tuesdays from 10am to 12pm Pacific Time. The schedule is available at https://apireview.net/schedule.

## Pull requests

Pull requests against **dotnet/runtime** shouldn't be submitted before getting approval. Also, we don't want to get work in progress (WIP) PR's. The reason being that we want to reduce the number pending PRs so that we can focus on the work the community expects we take action on.

If you want to collaborate with other people on the design, feel free to perform the work in a branch in your own fork. If you want to track your TODOs in the description of a PR, you can always submit a PR against your own fork. Also, feel free to advertise your PR by linking it from the issue you filed against **dotnet/runtime** in the first step above.

## API Design Guidelines

The .NET design guidelines are captured in the famous book [Framework Design Guidelines][FDG] by Krzysztof Cwalina, Jeremy Barton and Brad Abrams.

A digest with the most important guidelines are available in our [documentation](../coding-guidelines/framework-design-guidelines-digest.md). Long term, we'd like to publish the individual guidelines in standalone repo on which we can also accept PRs and -- more importantly for API reviews -- link to.

[FDG]: https://amazon.com/dp/0135896460

## API Review Notes

  /language:actionsWorkflow file for this run
.github/workflows/test-pull-request.yml at ae6e6f9
---
###########################
###########################
## Pull request testing ##
###########################
###########################
name: Latest Pull Request

# Documentation:
# - Workflow: https://help.github.com/en/articles/workflow-syntax-for-github-actions
# - SuperLinter: https://github.com/github/super-linter
# - Markdown linter: https://github.com/DavidAnson/markdownlint
# - Link validation: https://github.com/remarkjs/remark-validate-links

######################################################
# Start the job on a pull request to the main branch #
######################################################
on:
  pull_request:
    branches: [main]

###############
# Set the Job #
###############
jobs:
  validate:
    # Set the agent to run on
    runs-on: ubuntu-latest

    ##################
    # Load all steps #
    ##################
    steps:
      ##########################
      # Checkout the code base #
      ##########################
      - name: Checkout Code
        uses: actions/checkout@v3
        with:
          # Full git history is needed to get a proper list of changed files
          # within `super-linter`
          fetch-depth: 0

      ################################
      # Run Linters against code base #
      ################################
      - name: Lint Code Base
        #
        # Use full version number to avoid cases when a next
        # released version is buggy
        # About slim image: https://github.com/github/super-linter#slim-image
        uses: github/super-linter/slim@v4.9.4
        env:
          VALIDATE_ALL_CODEBASE: false
          DEFAULT_BRANCH: main
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          VALIDATE_GITHUB_ACTIONS: true
          VALIDATE_GITLEAKS: true
          #
          # The Markdown rules are defined at
          # .github/linters/.markdown-lint.yml
          #
          # Documentation on rules:
          # https://github.com/DavidAnson/markdownlint/blob/main/doc/Rules.md
          VALIDATE_MARKDOWN: true
          VALIDATE_YAML: true

      - name: Setup Node v16 for Yarn v3
        uses: actions/setup-node@v3
        with:
          node-version: '16.15.0' # Current LTS version

      - name: Enable Corepack for Yarn v3
        run: corepack enable

      - name: Install Yarn v3
        uses: borales/actions-yarn@v3
        with:
          cmd: set version stable

      - name: Install dependencies
        uses: borales/actions-yarn@v3
        env:
          YARN_ENABLE_IMMUTABLE_INSTALLS: false
        with:
          cmd: install

      - name: Build site
        if: ${{ success() }}
        uses: borales/actions-yarn@v3
        with:
          cmd: build.  The API review notes are being published in [API Review repository](https://github.com/dotnet/apireviews).
![1000107477](https://github.com/user-attachments/assets/c2bebef4-2aa2-4c37-a480-f1a9d1c36279)
![1000107476](https://github.com/user-attachments/assets/70bf34d5-255a-4cdd-b180-92e730481745)

![1000101407](https://github.com/user-attachments/assets/7aeb2260-7a66-4cf1-ad1f-fd78b8ca6ba9)
![1000107413](https://github.com/user-attachments/assets/3ecace0c-54e7-4f29-8e06-6d2523c7ec9c)

