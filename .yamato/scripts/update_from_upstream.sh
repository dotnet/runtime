#!/bin/sh

# Prequisites:
# This script assumes git and github CLR (gh) are installed.

# Cause the script to fail if any individual command fails
set -e

# Fetch from upstream
git remote add upstream https://github.com/dotnet/runtime
git fetch upstream

# Create a new branch for a pull request to merge in upstream/main
# using the current date in the branch name.
git checkout unity-main
branch_name=bot-upstream-main-merge-$(date '+%Y-%m-%d')
git checkout -b $branch_name
git merge upstream/main -m "Merge with main from upstream"
git push --set-upstream origin $branch_name

# Login to Github
set -u
echo "$GITHUB_TOKEN" > .githubtoken
unset GITHUB_TOKEN
gh auth login --hostname github.com --with-token < .githubtoken
rm .githubtoken

# Yamato has a local mirror of github.com for cloning. This configuration
# causes problems for Github CLI, so set the remote manually here.
git remote set-url origin $GIT_REPOSITORY_URL

# Create a pull request back to unity-main
gh pr create --title "Sync with upstream main branch" --body "This is an automatically generated pull request to merge changes from the upstream main branch."

# For some unknown reason we often see this error from Github CLI:
#
# pull request create failed: HTTP 502: Something went wrong while executing your query.
# This may be the result of a timeout, or it could be a GitHub bug.
# Please include `B96C:2166:B667D:BCDCF:61A53AF5` when reporting this issue. (https://api.github.com/graphql)
#
# Often the pull request was actually created though, so manually check for it if this happens.
