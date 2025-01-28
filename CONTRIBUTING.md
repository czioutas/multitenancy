# Contributing Guide

## Branch Naming Convention

When creating a new branch, please follow this naming pattern:
```
type/description
```

### Available Types

1. For Breaking Changes (Major Version Bump):
   - `breaking/` or `major/`
   - Example: `breaking/change-auth-system`

2. For New Features (Minor Version Bump):
   - `feature/` or `minor/`
   - Example: `feature/add-user-dashboard`

3. For Bug Fixes (Patch Version Bump):
   - `fix/` or `patch/`
   - Example: `fix/login-validation`

### Description Guidelines
- Use lowercase letters only
- Use hyphens (-) to separate words
- Be concise but descriptive
- No special characters

### Examples

✅ Good Examples:
```
feature/add-oauth-login
breaking/upgrade-to-net8
fix/resolve-null-reference
major/new-architecture
minor/add-logging
patch/update-dependency
```

❌ Bad Examples:
```
feat/newStuff          # Wrong type prefix, contains uppercase
feature_new_logging    # Uses underscores instead of hyphens
fix                    # Missing description
hotfix/URGENT-FIX!!    # Contains uppercase and special characters
```

### Branch Protection

Our repository enforces these naming conventions. Pull requests from branches that don't follow this naming pattern will be blocked until the branch is renamed correctly.

## Creating a New Branch

1. From your local repository:
   ```bash
   git checkout -b type/description
   ```

2. Or from GitHub interface:
   - Click "Branch: main"
   - Enter the new branch name following the convention
   - Click "Create branch"

## Additional Notes

- Branch names automatically determine version bumps based on their type
- Choose the type based on the changes you plan to make:
  - `breaking/` or `major/` for incompatible API changes
  - `feature/` or `minor/` for backwards-compatible functionality additions
  - `fix/` or `patch/` for backwards-compatible bug fixes