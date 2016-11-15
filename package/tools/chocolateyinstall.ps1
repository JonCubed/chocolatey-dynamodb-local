
$ErrorActionPreference = 'Stop';

$packageName= 'dynamodb-local'
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$packageDir = $env:chocolateyPackageFolder
$packageParameters = $env:chocolateyPackageParameters
$allowedPackageParams = @{}

# Defaults
$InstallationPath = $toolsDir

$optionKey = 'option'
$valueKey = 'value'
$separator = ' '

$match_pattern = "(?:-{1,2}|\/)(?<$optionKey>([\w_]+))(?:\s*[:= ]\s*(?<$valueKey>((([`"'])([\w- _=\\:\.]+)([`"'])))|([\w-_=\\:\.]+)){1}?|(?:-{1,2}|\/))?"

if (Test-Path variable:local:packageParameters -and $packageParameters -match $match_pattern ){
    $results = $packageParameters | Select-String $match_pattern -AllMatches
    $results.matches | % {
        Write-Debug "Option: $($_.Groups[$optionKey].Value.Trim()), Value: $($_.Groups[$valueKey].Value.Trim())"

        Set-Variable -Name $_.Groups[$optionKey].Value.Trim() -Value $_.Groups[$valueKey].Value.Trim()
    }
} else {
    Write-Debug "No Package Parameters Passed in"
}

if (Test-Path variable:local:inmemory -and Test-Path variable:local:dbPath)
{
    Write-Error 'You can not use DbPath and InMemory at the same time.'
}
if(Test-Path variable:local:cors) {
    $allowedPackageParams.Add('-cors', $CORS)
}

if(Test-Path variable:local:dbPath) {
    $allowedPackageParams.Add('-dbPath', $dbPath)
}

if(Test-Path variable:local:inMemory) {
    $allowedPackageParams.Add('-inMemory', $null)
}

if (Test-Path variable:local:optimizeDbBeforeStartup -and -not (Test-Path variable:local:dbPath))
{
    Write-Error 'You must specific DbPath, when using optimizeDbBeforeStartup.'
}
elseif(Test-Path variable:local:optimizeDbBeforeStartup) {
    $allowedPackageParams.Add('-optimizeDbBeforeStartup', $nul)
}

if(Test-Path variable:local:port) {
    $allowedPackageParams.Add('-port', $port)
}

if(Test-Path variable:local:sharedDb) {
    $allowedPackageParams.Add('-sharedDb', $nul)
}

# flatten params into a string
$jarArgs = ($allowedPackageParams.GetEnumerator() | % { $value='';if($_.Value) { $value = "$separator`"$($_.Value)`"" };"$($_.Key)$value" }) -join ' '


$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $InstallationPath
  url           = '[URL]'
  checksum      = '[CHECKSUM]'
  checksumType  = 'sha256'
}

# install location
Install-ChocolateyZipPackage @packageArgs

# get the path of where java is installed
$javaPath = (Get-Command Java).Path

if ($javaPath -eq $null -or $javaPath -ne '') {
  Write-Error "Java was not found, please make sure it is installed first."
}

$command = "-Djava.library.path=$packageDir/tools/DynamoDBLocal_lib -jar $packageDir\tools\DynamoDBLocal.jar $jarArgs"

# Create shim for Jar to make launching DynamoDB Local easier
Install-BinFile -Name DynamoDBLocal -Path "$javaPath" -Command "`"$command`""
















