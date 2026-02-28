$libraryName = "BeckhoffJsonReadWriter4026"
$libraryVersion = "1.0.0.0"
$libraryVendor = "fbarresi"

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

# Uninstall the library using RepTool
$arguments = @(
    "--profile=`"$profileName`""
    "--uninstallLibrary=`"$libraryName, $libraryVersion ($libraryVendor)`""
)

Write-Host "Uninstalling $libraryName..."
$process = Start-Process -FilePath $repToolPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow

if ($process.ExitCode -ne 0) {
    Write-Error "RepTool exited with code $($process.ExitCode)"
    exit $process.ExitCode
}

Write-Host "Library uninstalled successfully."
