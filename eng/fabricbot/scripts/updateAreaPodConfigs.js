// Generates FabricBot config for all area pod triage/PR boards
//
// Running the script using node will update the `../generated*Configs.json` files with the new configuration.
// The generated JSON can then be pasted in the `.github/fabricbot.json` file in dotnet/runtime,
// see https://github.com/dotnet/runtime/blob/main/docs/infra/automation.md for more details.

const path = require('path');
const fs = require('fs');

let generatedRuntimeConfigsFile = path.join(__dirname, '..', 'generated', 'areapods-runtime.json');
let generatedApiDocsConfigsFile = path.join(__dirname, '..', 'generated', 'areapods-dotnet-api-docs.json');

let areaPods = [
  {
    "pod": "Eric / Jeff",
    "enabled": true,
    "areas": [
      "area-Meta"
    ]
  },
  {
    "pod": "Buyaa / Jose / Steve",
    "enabled": true,
    "areas": [
      "area-System.CodeDom",
      "area-System.Configuration",
      "area-System.Reflection",
      "area-System.Reflection.Emit",
      "area-System.Reflection.Metadata",
      "area-System.Resources",
      "area-System.Runtime.CompilerServices",
      "area-System.Text.RegularExpressions",
      "area-System.Threading.Channels",
      "area-System.Threading.Tasks",
      "area-System.DirectoryServices"
    ]
  },
  {
    "pod": "Eirik / Krzysztof / Layomi",
    "enabled": true,
    "areas": [
      "area-System.Collections",
      "area-System.Linq",
      "area-System.Text.Json",
      "area-System.Xml"
    ]
  },
  {
    "pod": "Eric / Maryam / Tarek",
    "enabled": true,
    "areas": [
      "area-DependencyModel",
      "area-Extensions-Caching",
      "area-Extensions-Configuration",
      "area-Extensions-DependencyInjection",
      "area-Extensions-Hosting",
      "area-Extensions-Logging",
      "area-Extensions-Options",
      "area-Extensions-Primitives",
      "area-System.ComponentModel",
      "area-System.ComponentModel.Composition",
      "area-System.Composition",
      "area-System.Diagnostics.Activity",
      "area-System.Globalization"
    ]
  },
  {
    "pod": "Carlos / Santi",
    "enabled": true,
    "areas": [
      "area-Infrastructure-libraries",
      "area-Microsoft.Win32",
      "area-System.Diagnostics.EventLog",
      "area-System.Diagnostics.PerformanceCounter",
      "area-System.Diagnostics.TraceSource",
      "area-System.Drawing",
      "area-System.Management",
      "area-System.ServiceProcess"
    ]
  },
  {
    "pod": "Adam / David",
    "enabled": true,
    "areas": [
      "area-Extensions-FileSystem",
      "area-System.Console",
      "area-System.Diagnostics.Process",
      "area-System.IO",
      "area-System.IO.Compression",
      "area-System.Linq.Parallel",
      "area-System.Memory"
    ]
  },
  {
    "pod": "Michael / Tanner",
    "enabled": true,
    "areas": [
      "area-System.Buffers",
      "area-System.Numerics",
      "area-System.Numerics.Tensors",
      "area-System.Runtime",
      "area-System.Runtime.Intrinsics"
    ]
  },
  {
    "pod": "Jeremy / Levi",
    "enabled": true,
    "areas": [
      "area-System.Formats.Asn1",
      "area-System.Formats.Cbor",
      "area-System.Security",
      "area-System.Text.Encoding",
      "area-System.Text.Encodings.Web"
    ]
  }
];

let areaPodConfig = {
  issueTriageRemove: ({pod, areas}) => ({
    "taskType": "trigger",
    "capabilityId": "IssueResponder",
    "subCapability": "IssuesOnlyResponder",
    "version": "1.0",
    "config": {
      "conditions": {
        "operator": "and",
        "operands": [
          {
            "name": "isInProjectColumn",
            "parameters": {
            "projectName": `Area Pod: ${pod} - Issue Triage`,
            "columnName": "Needs Triage",
            "isOrgProject": true
            }
          },
          {
            "operator": "and",
            "operands": areas.map(area => ({
              "operator": "not",
              "operands": [
                {
                  "name": "hasLabel",
                  "parameters": { "label": area }
                }
              ]
            }))
          }
        ]
      },
      "eventType": "issue",
      "eventNames": [
        "issues",
        "project_card"
      ],
      "taskName": `[Area Pod: ${pod} - Issue Triage] Remove relabeled issues`,
      "actions": [
        {
          "name": "removeFromProject",
          "parameters": {
            "projectName": `Area Pod: ${pod} - Issue Triage`,
            "isOrgProject": true
          }
        }
      ]
    }
  }),
  issueTriageNeedsTriage: ({pod, areas}) => ({
    "taskType": "trigger",
    "capabilityId": "IssueResponder",
    "subCapability": "IssuesOnlyResponder",
    "version": "1.0",
    "config":
    {
      "conditions":
      {
        "operator": "and",
        "operands":
        [
          {
            "operator": "or",
            "operands":
            [
              {
                "operator": "and",
                "operands":
                [
                  {
                    "operator": "or",
                    "operands": areas.map(area => ({
                      "name": "hasLabel",
                      "parameters": { "label": area }
                    }))
                  },
                  {
                    "operator": "or",
                    "operands":
                    [
                      {
                        "name": "isAction",
                        "parameters":
                        {
                          "action": "reopened"
                        }
                      },
                      {
                        "operator": "not",
                        "operands": [
                          {
                            "name": "isInMilestone",
                            "parameters":
                            {}
                          }
                        ]
                      }
                    ]
                  }
                ]
              },
              {
                "operator": "or",
                "operands": areas.map(area => ({
                  "name": "labelAdded",
                  "parameters": { "label": area }
                }))
              }
            ]
          },
          {
            "name": "isOpen",
            "parameters":
            {}
          },
          {
            "operator": "or",
            "operands":
            [
              {
                "operator": "not",
                "operands":
                [
                  {
                    "name": "isInProject",
                    "parameters":
                    {
                      "projectName": `Area Pod: ${pod} - Issue Triage`,
                      "isOrgProject": true
                    }
                  }
                ]
              },
              {
                "name": "isInProjectColumn",
                "parameters":
                {
                  "projectName": `Area Pod: ${pod} - Issue Triage`,
                  "isOrgProject": true,
                  "columnName": "Triaged"
                }
              }
            ]
          }
        ]
      },
      "eventType": "issue",
      "eventNames":
      [
        "issues",
        "project_card"
      ],
      "taskName": `[Area Pod: ${pod} - Issue Triage] Add new issue to Board`,
      "actions":
      [
        {
          "name": "addToProject",
          "parameters":
          {
            "projectName": `Area Pod: ${pod} - Issue Triage`,
            "columnName": "Needs Triage",
            "isOrgProject": true
          }
        }
      ]
    }
  }),
  issueTriageNeedsFurtherTriage: ({pod, areas}) => ({
    "taskType": "trigger",
    "capabilityId": "IssueResponder",
    "subCapability": "IssueCommentResponder",
    "version": "1.0",
    "config":
    {
      "conditions":
      {
        "operator": "and",
        "operands":
        [
          {
            "operator": "or",
            "operands": areas.map(area => ({
              "name": "hasLabel",
              "parameters": { "label": area }
            }))
          },
          {
            "operator": "not",
            "operands":
            [
              {
                "name": "isCloseAndComment",
                "parameters":
                {}
              }
            ]
          },
          {
            "operator": "not",
            "operands": [
              {
                "name": "activitySenderHasPermissions",
                "parameters": {
                  "permissions": "write"
                }
              }
            ]
          },
          {
            "operator": "or",
            "operands":
            [
              {
                "operator": "not",
                "operands":
                [
                  {
                    "name": "isInProject",
                    "parameters":
                    {
                      "projectName": `Area Pod: ${pod} - Issue Triage`,
                      "isOrgProject": true
                    }
                  }
                ]
              },
              {
                "name": "isInProjectColumn",
                "parameters":
                {
                  "projectName": `Area Pod: ${pod} - Issue Triage`,
                  "columnName": "Triaged",
                  "isOrgProject": true
                }
              }
            ]
          }
        ]
      },
      "eventType": "issue",
      "eventNames":
      [
        "issue_comment"
      ],
      "taskName": `[Area Pod: ${pod} - Issue Triage] Needs Further Triage`,
      "actions":
      [
        {
          "name": "addToProject",
          "parameters":
          {
            "projectName": `Area Pod: ${pod} - Issue Triage`,
            "columnName": "Needs Triage",
            "isOrgProject": true
          }
        }
      ]
    }
  }),
  issueTriageTriaged: ({pod, areas}) => ({
    "taskType": "trigger",
    "capabilityId": "IssueResponder",
    "subCapability": "IssuesOnlyResponder",
    "version": "1.0",
    "config":
    {
      "conditions":
      {
        "operator": "and",
        "operands":
        [
          {
            "name": "isInProject",
            "parameters": {
              "projectName": `Area Pod: ${pod} - Issue Triage`,
              "isOrgProject": true
            }
          },
          {
            "operator": "not",
            "operands": [
              {
                "name": "isInProjectColumn",
                "parameters": {
                  "projectName": `Area Pod: ${pod} - Issue Triage`,
                  "columnName": "Triaged"
                }
              }
            ]
          },
          {
            "operator": "or",
            "operands":
            [
              {
                "name": "addedToMilestone",
                "parameters":
                {}
              },
              {
                "name": "labelAdded",
                "parameters":
                {
                  "label": "needs-author-action"
                }
              },
              {
                "name": "labelAdded",
                "parameters":
                {
                  "label": "api-ready-for-review"
                }
              },
              {
                "name": "isAction",
                "parameters":
                {
                  "action": "closed"
                }
              }
            ]
          }
        ]
      },
      "eventType": "issue",
      "eventNames":
      [
        "issues",
        "project_card"
      ],
      "taskName": `[Area Pod: ${pod} - Issue Triage] Move to Triaged Column`,
      "actions":
      [
        {
          "name": "addToProject",
          "parameters":
          {
            "projectName": `Area Pod: ${pod} - Issue Triage`,
            "columnName": "Triaged",
            "isOrgProject": true
          }
        }
      ]
    }
  }),

  /* Pull Requests */

  pullRequestAdd: ({pod, areas}) => ({
    "taskType": "trigger",
    "capabilityId": "IssueResponder",
    "subCapability": "PullRequestResponder",
    "version": "1.0",
    "config": {
      "conditions": {
        "operator": "and",
        "operands": [
          {
            "operator": "or",
            "operands": areas.map(area => ({
              "name": "hasLabel",
              "parameters": { "label": area }
            }))
          },
          {
            "operator": "not",
            "operands": [
              {
                "name": "isInProject",
                "parameters": {
                  "projectName": `Area Pod: ${pod} - PRs`,
                  "isOrgProject": true
                }
              }
            ]
          }
        ]
      },
      "eventType": "pull_request",
      "eventNames": [
        "pull_request",
        "issues",
        "project_card"
      ],
      "taskName": `[Area Pod: ${pod} - PRs] Add new PR to Board`,
      "actions": [
        {
          "name": "addToProject",
          "parameters": {
            "projectName": `Area Pod: ${pod} - PRs`,
            "columnName": "Needs Champion",
            "isOrgProject": true
          }
        }
      ]
    }
  }),
  prRemove: ({pod, areas}) => ({
    "taskType": "trigger",
    "capabilityId": "IssueResponder",
    "subCapability": "PullRequestResponder",
    "version": "1.0",
    "config": {
      "conditions": {
        "operator": "and",
        "operands": [
          {
            "name": "isInProjectColumn",
            "parameters": {
            "projectName": `Area Pod: ${pod} - PRs`,
            "columnName": "Needs Champion",
            "isOrgProject": true
            }
          },
          {
            "operator": "and",
            "operands": areas.map(area => ({
              "operator": "not",
              "operands": [
                {
                  "name": "hasLabel",
                  "parameters": { "label": area }
                }
              ]
            }))
          }
        ]
      },
      "eventType": "pull_request",
      "eventNames": [
        "pull_request",
        "issues",
        "project_card"
      ],
      "taskName": `[Area Pod: ${pod} - PRs] Remove relabeled PRs`,
      "actions": [
        {
          "name": "removeFromProject",
          "parameters": {
            "projectName": `Area Pod: ${pod} - PRs`,
            "isOrgProject": true
          }
        }
      ]
    }
  })
};

// Generate runtime automation
let generatedRuntimeTasks = areaPods
  .filter(areaPod => areaPod.enabled)
  .flatMap(areaPod =>
    [
      areaPodConfig.issueTriageNeedsTriage(areaPod),
      areaPodConfig.issueTriageNeedsFurtherTriage(areaPod),
      areaPodConfig.issueTriageRemove(areaPod),
      areaPodConfig.issueTriageTriaged(areaPod),
      areaPodConfig.pullRequestAdd(areaPod),
      areaPodConfig.prRemove(areaPod),
    ]);

let generatedRuntimeJson = JSON.stringify(generatedRuntimeTasks, null, 2);
fs.writeFileSync(generatedRuntimeConfigsFile, generatedRuntimeJson);
console.log(`Written generated tasks to ${generatedRuntimeConfigsFile}`);

// Generate dotnet-api-docs automation
let generatedApiDocsTasks = areaPods
  .filter(areaPod => areaPod.enabled)
  .flatMap(areaPod =>
    [
      areaPodConfig.issueTriageNeedsTriage(areaPod),
      areaPodConfig.issueTriageNeedsFurtherTriage(areaPod),
      areaPodConfig.issueTriageRemove(areaPod),
      // areaPodConfig.issueTriageTriaged(areaPod),
      areaPodConfig.pullRequestAdd(areaPod),
      areaPodConfig.prRemove(areaPod),
    ]);

let generatedApiDocsJson = JSON.stringify(generatedApiDocsTasks, null, 2);
fs.writeFileSync(generatedApiDocsConfigsFile, generatedApiDocsJson);
console.log(`Written generated tasks to ${generatedApiDocsConfigsFile}`);
