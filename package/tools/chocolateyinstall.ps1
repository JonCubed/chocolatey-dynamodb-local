
$ErrorActionPreference = 'Stop';

$packageName= 'dynamodb-local'
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  url           = '[URL]'
  checksum      = '[CHECKSUM]'
  checksumType  = 'sha256'

}

Install-ChocolateyZipPackage @packageArgs


















