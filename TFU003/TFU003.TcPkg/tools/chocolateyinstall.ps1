$libraryFileName = "BeckhoffJsonReadWriter.library"

# Locate the library file
$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$libraryFile = Join-Path $toolsDir $libraryFileName

if (-not (Test-Path $libraryFile)) {
    Write-Error "Library file not found: $libraryFile"
    exit 1
}

# Get TwinCAT installation path from registry
$tcRegPath = "HKCU:\SOFTWARE\Beckhoff\TwinCAT3"
$tcBasePath = $null

if (Test-Path $tcRegPath) {
    $tcBasePath = (Get-ItemProperty -Path $tcRegPath).InstallDir
}

if (-not $tcBasePath) {
    Write-Error "TwinCAT installation not found in registry"
    exit 1
}

# Find RepTool.exe under Build_4026.* directories
$repToolPath = Get-ChildItem -Path $tcBasePath -Recurse -Filter "RepTool.exe" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match "Build_4026\.\d+" } |
    Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $repToolPath) {
    Write-Error "RepTool.exe not found under TwinCAT Build_4026.* directories"
    exit 1
}

# Extract PLC profile from RepTool path
$profileMatch = [regex]::Match($repToolPath, "(Build_4026\.\d+)")
$profileName = "TwinCAT PLC Control_$($profileMatch.Value)"

# Install the library using RepTool
$arguments = @(
    "--profile=`"$profileName`""
    "--installLibrary=`"$libraryFile`""
)

Write-Host "Installing $libraryFileName using RepTool..."
$process = Start-Process -FilePath $repToolPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow

if ($process.ExitCode -ne 0) {
    Write-Error "RepTool exited with code $($process.ExitCode)"
    exit $process.ExitCode
}

Write-Host "Library installed successfully."
