// Generates FabricBot config for all area pod triage/PR boards
//
// Running the script using node will update the `../generated*Configs.json` files with the new configuration.
// The generated JSON can then be pasted in the `.github/fabricbot.json` file in dotnet/runtime,
// see https://github.com/dotnet/runtime/blob/main/docs/infra/automation.md for more details.

const path = require('path');
const fs = require('fs');

let generatedRuntimeConfigsFile = path.join(__dirname, 'generated', 'areapods-runtime.json');
let generatedApiDocsConfigsFile = path.join(__dirname, 'generated', 'areapods-dotnet-api-docs.json');
let generatedMachineLearningConfigsFile = path.join(__dirname, 'generated', 'areapods-machinelearning.json');

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
    "pod": "Carlos / Jeremy",
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
    ],
    "repos": [
      "machinelearning"
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
  issueMovedToAnotherArea: ({pod, areas}) => ({
    "taskType": "trigger",
    "capabilityId": "IssueResponder",
    "subCapability": "IssuesOnlyResponder",
    "version": "1.0",
    "config": {
      "conditions": {
        "operator": "and",
        "operands": [
          {
            "operator": "not",
            "operands": [
              {
                "name": "isInProjectColumn",
                "parameters": {
                  "projectName": `Area Pod: ${pod} - Issue Triage`,
                  "columnName": "Triaged",
                  "isOrgProject": true
                }
              }
            ]
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
          },
          {
            "name": "isAction",
            "parameters": {
              "action": "unlabeled"
            }
          }
        ]
      },
      "eventType": "issue",
      "eventNames": [
        "issues",
        "project_card"
      ],
      "taskName": `[Area Pod: ${pod} - Issue Triage] Mark relabeled issues as Triaged`,
      "actions": [
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
  issueNeedsTriage: ({pod, areas}) => ({
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
                  (!!areas && {
                    "operator": "or",
                    "operands": areas.map(area => ({
                      "name": "hasLabel",
                      "parameters": { "label": area }
                    }))
                  }),
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
                ].filter(op => !!op) // We will have a falsy element in the array of we're not filtering by area label
              },
              (!!areas && {
                "operator": "or",
                "operands": areas.map(area => ({
                  "name": "labelAdded",
                  "parameters": { "label": area }
                }))
              })
            ].filter(op => !!op) // We will have a falsy element in the array of we're not filtering by area label
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
  issueNeedsFurtherTriage: ({pod, areas}) => ({
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
          (!!areas && {
            "operator": "or",
            "operands": areas.map(area => ({
              "name": "hasLabel",
              "parameters": { "label": area }
            }))
          }),
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
        ].filter(op => !!op) // We will have a falsy element in the array of we're not filtering by area label
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
  issueTriaged: ({pod, areas}) => ({
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
        },
        {
          "name": "removeLabel",
          "parameters": {
            "label": "untriaged"
          }
        }
      ]
    }
  }),

  /* Pull Requests */

  pullRequestNeedsChampion: ({pod, areas}) => ({
    "taskType": "trigger",
    "capabilityId": "IssueResponder",
    "subCapability": "PullRequestResponder",
    "version": "1.0",
    "config": {
      "conditions": {
        "operator": "and",
        "operands": [
          (!!areas && {
            "operator": "or",
            "operands": areas.map(area => ({
              "name": "hasLabel",
              "parameters": { "label": area }
            }))
          }),
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
        ].filter(op => !!op) // We will have a falsy element in the array of we're not filtering by area label
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
  pullRequestMovedToAnotherArea: ({pod, areas}) => ({
    "taskType": "trigger",
    "capabilityId": "IssueResponder",
    "subCapability": "PullRequestResponder",
    "version": "1.0",
    "config": {
      "conditions": {
        "operator": "and",
        "operands": [
          {
            "operator": "not",
            "operands": [
              {
                "name": "isInProjectColumn",
                "parameters": {
                "projectName": `Area Pod: ${pod} - PRs`,
                "columnName": "Done",
                "isOrgProject": true
                }
              }
            ]
          },
          (!!areas && {
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
          })
        ].filter(op => !!op) // We will have a falsy element in the array of we're not filtering by area label
      },
      "eventType": "pull_request",
      "eventNames": [
        "pull_request",
        "issues",
        "project_card"
      ],
      "taskName": `[Area Pod: ${pod} - PRs] Mark relabeled PRs as Done`,
      "actions": [
        {
          "name": "addToProject",
          "parameters":
          {
            "projectName": `Area Pod: ${pod} - PRs`,
            "columnName": "Done",
            "isOrgProject": true
          }
        },
      ]
    }
  })
};

// Generate runtime automation
let generatedRuntimeTasks = areaPods
  .filter(areaPod => areaPod.enabled)
  .flatMap(areaPod =>
    [
      areaPodConfig.issueNeedsTriage(areaPod),
      areaPodConfig.issueNeedsFurtherTriage(areaPod),
      areaPodConfig.issueMovedToAnotherArea(areaPod),
      areaPodConfig.issueTriaged(areaPod),
      areaPodConfig.pullRequestNeedsChampion(areaPod),
      areaPodConfig.pullRequestMovedToAnotherArea(areaPod),
    ]);

let generatedRuntimeJson = JSON.stringify(generatedRuntimeTasks, null, 2);
fs.writeFileSync(generatedRuntimeConfigsFile, generatedRuntimeJson);
console.log(`Written generated tasks to ${generatedRuntimeConfigsFile}`);

// Generate dotnet-api-docs automation
let generatedApiDocsTasks = areaPods
  .filter(areaPod => areaPod.enabled)
  .flatMap(areaPod =>
    [
      areaPodConfig.issueNeedsTriage(areaPod),
      areaPodConfig.issueNeedsFurtherTriage(areaPod),
      areaPodConfig.issueMovedToAnotherArea(areaPod),
      areaPodConfig.issueTriaged(areaPod),
      areaPodConfig.pullRequestNeedsChampion(areaPod),
      areaPodConfig.pullRequestMovedToAnotherArea(areaPod),
    ]);

let generatedApiDocsJson = JSON.stringify(generatedApiDocsTasks, null, 2);
fs.writeFileSync(generatedApiDocsConfigsFile, generatedApiDocsJson);
console.log(`Written generated tasks to ${generatedApiDocsConfigsFile}`);

// Generate machinelearning automation
let generatedMachineLearningTasks = areaPods
  .filter(areaPod => areaPod.enabled)
  // Filter to the pod that includes the machinelearning repo
  .filter(({repos}) => repos && repos.includes("machinelearning"))
  // Remove the `areas` property from the pod
  .map(({areas, ...podWithoutAreas}) => podWithoutAreas)
  .flatMap(areaPod =>
    [
      areaPodConfig.issueNeedsTriage(areaPod),
      areaPodConfig.issueNeedsFurtherTriage(areaPod),
      areaPodConfig.issueTriaged(areaPod),
      areaPodConfig.pullRequestNeedsChampion(areaPod)
      // The machinelearning repo doesn't have areas,
      // so the *MovedToAnotherArea tasks don't apply
    ]);

let generatedMachineLearningJson = JSON.stringify(generatedMachineLearningTasks, null, 2);
fs.writeFileSync(generatedMachineLearningConfigsFile, generatedMachineLearningJson);
console.log(`Written generated tasks to ${generatedMachineLearningConfigsFile}`);
