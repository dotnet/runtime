# Directory Layout

```
.github/skills/ci-pipeline-monitor/
├── pipelines.md          # Pipeline definitions (edit to add/remove)
├── report-template.md    # Template for test reports
├── log-template.md       # Template for debug logs
├── SKILL.md              # Skill instructions for the agent
├── README.md             # Human-facing documentation
├── scripts/              # Python scripts + monitor.db
│   ├── setup_and_fetch_builds.py
│   ├── extract_failed_tests.py
│   ├── fetch_helix_logs.py
│   ├── generate_report.py
│   ├── validate_results.py
│   └── monitor.db        # SQLite database (created by scripts)
├── references/           # Detailed instructions (loaded on demand)
│   ├── triage-workflow.md
│   ├── verbatim-rules.md
│   ├── validation-checks.md
│   └── directory-layout.md  # This file
├── logs/                 # Debug logs + test reports
│   ├── ci-pipeline-monitor-*.log
│   └── test-report-*.md
└── helix-logs/           # Full Helix console logs (one .log file per test)
```

`helix-logs/` stores the complete, unmodified Helix console log for every
failed test. One file per unique console URL. The agent reads these files
during triage to extract error messages and stack traces. These files are
NOT mixed with `logs/` (which contains only debug logs and test reports).
