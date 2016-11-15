# Chocolatey Package for DynamoDB Local

Chocolatey Package Source for DynamoDb Local

## Usage

```powershell
>.\build.ps1 -experimental
```

> Experiment flag is required due to string interpolation usage.

### Build Arguments
The following arguments are available

|Argument|Description|
|--------|-----------|
|-SkipHash|Will not recalculate the hash if a new version is not found and a hash is already saved.|
|-Force|Will force the pack to run even if no new version.|

## To Do

- [x] Support state file
- [x] Add package params to match options available for jar
- [ ] Add uninstall script
- [ ] publish package
