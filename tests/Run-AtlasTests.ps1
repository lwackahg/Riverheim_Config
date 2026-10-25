# Renders Riverheim worlds through Gurebu's Riverheim AtlasBuilder to test Riverheim Config without the game.
#
# AtlasBuilder is driven exactly as the AtlasBuilder UI launcher drives it: a .NET startup hook
# (RiverheimConfigHook.cs from the launcher repository, compiled together with this repository's
# RiverheimSettings.cs) fills AtlasBuilder's Experimental preset slot with the Default world plus the
# settings from a cfg file. Three checks:
#
#   Stock      a cfg holding Riverheim's own default values, with every value forced through the mod's
#              mapping, must render byte-identical to --preset Default; so must an all-default cfg.
#   Settings   every setting rendered at a low and a high value must change the image; a setting that
#              changes nothing has a broken or ineffective mapping, and a value that crashes Riverheim
#              must be kept out of the setting's range.
#   Reference  presets/BEST.cfg renders identically to the image kept in tests/reference, so a change in
#              the mapping between versions shows up.
#
# Which Riverheim renders (-Engine):
#   Installed  (default) the Riverheim.dll the player actually runs (Gurebu-Riverheim\Riverheim.dll next to
#              this repository, or -RiverheimDll). AtlasBuilder 1.0.0 bundles an older Riverheim, so the test
#              tool's retarget command writes that DLL as Riverheim.Core.dll and points AtlasBuilder's Default
#              preset at its own default preset version. AtlasBuilder's unmodified exe is then started with its
#              bundle extraction redirected to a pre-filled folder under tests\out that holds these two files, and
#              a second startup hook (tests\DepsHook.cs) resolves the BepInEx, Harmony and Unity assemblies that
#              Riverheim.dll references, from copies taken from the Valheim install (-ValheimDir, -BepInExCoreDir).
#              Nothing outside tests\out is written, and no game, mod or AtlasBuilder file is changed.
#   Bundled    the Riverheim inside AtlasBuilder 1.0.0 (what the exe shows players without any help).
#
# Needs: Windows PowerShell 5.1, the .NET 8 SDK (for the test tool), AtlasBuilder 1.0.0, a checkout of the
# launcher repository (github.com/lwackahg/atlasbuilder-ui-repo) for RiverheimConfigHook.cs and, for Installed,
# a Valheim install with BepInEx. Images are compared by hash; AtlasBuilder renders deterministically.
#
#   .\tests\Run-AtlasTests.ps1 -Tests Stock
#   .\tests\Run-AtlasTests.ps1 -Tests Settings -Only LandCohesion,SwirlRays -Modes Rich
#   .\tests\Run-AtlasTests.ps1 -Engine Bundled -Tests Stock
#   .\tests\Run-AtlasTests.ps1 -UpdateReference
[CmdletBinding()]
param(
    [string]$AtlasExe = '',
    [string]$LauncherRepo = '',
    [ValidateSet('Installed', 'Bundled')][string]$Engine = 'Installed',
    [string]$RiverheimDll = '',
    [string]$ValheimDir = '',
    [string]$BepInExCoreDir = '',
    [string[]]$Tests = @('Stock', 'Settings', 'Reference'),
    [string[]]$Only = @(),
    [int[]]$Seeds = @(400, 1337),
    [string[]]$Modes = @('Biomes'),
    [string]$SettingsMode = 'Rich',
    [int]$Resolution = 256,
    [switch]$UpdateReference,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
# powershell.exe -File passes "Stock,Settings" as one string; accept both forms.
$Tests = @($Tests -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$Only = @($Only -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$Modes = @($Modes -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$Seeds = @(($Seeds -join ',') -split ',' | ForEach-Object { [int]$_.Trim() })
$repoRoot = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $PSScriptRoot 'out'
$referenceDir = Join-Path $PSScriptRoot 'reference'
$toolProject = Join-Path $PSScriptRoot 'RiverheimConfig.Tests\RiverheimConfig.Tests.csproj'
$toolDll = Join-Path $PSScriptRoot 'RiverheimConfig.Tests\bin\Release\net8.0\rhconfig-test.dll'
$settingsSource = Join-Path $repoRoot 'src\RiverheimSettings.cs'
$depsHookSource = Join-Path $PSScriptRoot 'DepsHook.cs'
$invariant = [System.Globalization.CultureInfo]::InvariantCulture

function First-Existing {
    param([string[]]$Candidates, [string]$Leaf = '')
    foreach ($c in $Candidates) {
        if ($c -and (Test-Path -LiteralPath $(if ($Leaf) { Join-Path $c $Leaf } else { $c }))) { return $c }
    }
    return ''
}

if (-not $AtlasExe) { $AtlasExe = First-Existing @((Join-Path $repoRoot 'Gurebu-Riverheim_AtlasBuilder\Riverheim.AtlasBuilder.exe'), $env:RIVERHEIM_ATLASBUILDER) }
if (-not $AtlasExe -or -not (Test-Path -LiteralPath $AtlasExe -PathType Leaf)) {
    throw 'Riverheim.AtlasBuilder.exe not found. Pass -AtlasExe or put the AtlasBuilder package in Gurebu-Riverheim_AtlasBuilder\ next to this repository.'
}
if (-not $LauncherRepo) { $LauncherRepo = First-Existing @((Join-Path (Split-Path -Parent $repoRoot) 'atlasbuilder-ui-repo'), 'C:\tmp\abui', $env:ATLASBUILDER_UI_REPO) 'RiverheimConfigHook.cs' }
$hookSource = Join-Path $LauncherRepo 'RiverheimConfigHook.cs'
if (-not (Test-Path -LiteralPath $hookSource -PathType Leaf)) {
    throw 'RiverheimConfigHook.cs not found. Pass -LauncherRepo pointing at a checkout of the AtlasBuilder UI repository.'
}
if ($Engine -eq 'Installed') {
    if (-not $RiverheimDll) { $RiverheimDll = First-Existing @((Join-Path $repoRoot 'Gurebu-Riverheim\Riverheim.dll'), $env:RIVERHEIM_DLL) }
    if (-not $RiverheimDll -or -not (Test-Path -LiteralPath $RiverheimDll -PathType Leaf)) {
        throw 'Riverheim.dll not found. Pass -RiverheimDll (the Gurebu-Riverheim plugin) or put the package in Gurebu-Riverheim\ next to this repository, or use -Engine Bundled.'
    }
    if (-not $ValheimDir) { $ValheimDir = First-Existing @($env:VALHEIM_DIR, 'F:\SteamLibrary\steamapps\common\Valheim', 'C:\Program Files (x86)\Steam\steamapps\common\Valheim') 'valheim_Data\Managed\assembly_valheim.dll' }
    if (-not $ValheimDir -or -not (Test-Path -LiteralPath (Join-Path $ValheimDir 'valheim_Data\Managed\assembly_valheim.dll'))) {
        throw 'Valheim install not found. Pass -ValheimDir (the folder holding valheim.exe) or set VALHEIM_DIR, or use -Engine Bundled.'
    }
    if (-not $BepInExCoreDir) { $BepInExCoreDir = First-Existing @($env:BEPINEX_CORE_DIR, (Join-Path $ValheimDir 'BepInEx\core')) 'BepInEx.dll' }
    if (-not $BepInExCoreDir -or -not (Test-Path -LiteralPath (Join-Path $BepInExCoreDir 'Mono.Cecil.dll'))) {
        throw 'BepInEx core not found. Pass -BepInExCoreDir (a BepInEx\core folder with BepInEx.dll and Mono.Cecil.dll), or use -Engine Bundled.'
    }
}

# These stop the .NET SDK from phoning home or downloading workload manifests during the build.
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = '0'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'

[void](New-Item -ItemType Directory -Path $outDir -Force)
$report = [System.Collections.Generic.List[string]]::new()
$failures = 0
function Say { param([string]$Text) Write-Host $Text; $report.Add($Text) }

# ---- Test tool -------------------------------------------------------------------------------------------
if (-not $SkipBuild -or -not (Test-Path -LiteralPath $toolDll)) {
    Say 'Building the test tool...'
    $buildArgs = @('build', $toolProject, '-c', 'Release', '-v', 'q', '-nodeReuse:false')
    if ($BepInExCoreDir) { $buildArgs += "-p:MonoCecilDir=$BepInExCoreDir" }
    & dotnet @buildArgs | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Building tests\RiverheimConfig.Tests failed.' }
}
function Tool { param([string[]]$ToolArgs) & dotnet $toolDll @ToolArgs; if ($LASTEXITCODE -ne 0) { throw "rhconfig-test $($ToolArgs[0]) failed" } }

# ---- AtlasBuilder's unpacked assemblies ------------------------------------------------------------------------
# AtlasBuilder is a single-file app. Starting it once with no arguments (it only prints its help) unpacks its
# assemblies, which the hooks are compiled against and which the test tool reads the stock defaults from.
$exeInfo = Get-Item -LiteralPath $AtlasExe
$bundleDir = Join-Path $outDir ('bundle-{0}-{1}' -f $exeInfo.Length, $exeInfo.LastWriteTimeUtc.Ticks)
$bundledCore = Get-ChildItem -LiteralPath $bundleDir -Filter 'Riverheim.Core.dll' -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $bundledCore) {
    [void](New-Item -ItemType Directory -Path $bundleDir -Force)
    $unpack = [System.Diagnostics.ProcessStartInfo]::new($AtlasExe)
    $unpack.UseShellExecute = $false
    $unpack.CreateNoWindow = $true
    $unpack.RedirectStandardOutput = $true
    $unpack.RedirectStandardError = $true
    $unpack.EnvironmentVariables['DOTNET_BUNDLE_EXTRACT_BASE_DIR'] = $bundleDir
    $unpackProcess = [System.Diagnostics.Process]::Start($unpack)
    [void]$unpackProcess.StandardOutput.ReadToEndAsync()
    [void]$unpackProcess.StandardError.ReadToEndAsync()
    if (-not $unpackProcess.WaitForExit(60000)) { $unpackProcess.Kill(); throw 'AtlasBuilder did not finish unpacking within 60 seconds.' }
    $bundledCore = Get-ChildItem -LiteralPath $bundleDir -Filter 'Riverheim.Core.dll' -File -Recurse | Select-Object -First 1
    if ($null -eq $bundledCore) { throw 'Could not find the unpacked AtlasBuilder assemblies.' }
}

# ---- The installed Riverheim inside AtlasBuilder -------------------------------------------------------------
# extract\ is a copy of the normal bundle extraction with Riverheim.Core.dll and Riverheim.Dev.dll replaced by the
# retargeted ones; the exe reuses it when DOTNET_BUNDLE_EXTRACT_BASE_DIR points there. deps\ holds the assemblies
# Riverheim.dll references, for DepsHook. Both are rebuilt when the exe or Riverheim.dll changes.
$extractDir = $null
$depsDir = $null
if ($Engine -eq 'Installed') {
    $riverheimInfo = Get-Item -LiteralPath $RiverheimDll
    $installedDir = Join-Path $outDir ('installed-{0}-{1}-{2}-{3}' -f $exeInfo.Length, $exeInfo.LastWriteTimeUtc.Ticks, $riverheimInfo.Length, $riverheimInfo.LastWriteTimeUtc.Ticks)
    $extractDir = Join-Path $installedDir 'extract'
    $depsDir = Join-Path $installedDir 'deps'
    if (-not (Test-Path -LiteralPath (Join-Path $installedDir 'ready'))) {
        Say "Preparing AtlasBuilder with $RiverheimDll..."
        if (Test-Path -LiteralPath $installedDir) { Remove-Item -LiteralPath $installedDir -Recurse -Force }
        [void](New-Item -ItemType Directory -Path $extractDir -Force)
        [void](New-Item -ItemType Directory -Path $depsDir -Force)
        Copy-Item -Path (Join-Path $bundleDir '*') -Destination $extractDir -Recurse -Force
        $target = Get-ChildItem -LiteralPath $extractDir -Filter 'Riverheim.Core.dll' -File -Recurse | Select-Object -First 1
        Tool @('retarget', $RiverheimDll, (Join-Path $target.DirectoryName 'Riverheim.Dev.dll'), (Join-Path $installedDir 'retargeted'))
        Copy-Item -LiteralPath (Join-Path $installedDir 'retargeted\Riverheim.Core.dll'), (Join-Path $installedDir 'retargeted\Riverheim.Dev.dll') -Destination $target.DirectoryName -Force
        # Riverheim.dll's references: BepInEx and its Harmony/MonoMod/Cecil, and Valheim's own managed assemblies
        # (everything that is not part of the .NET framework, which the exe already carries).
        Get-ChildItem -LiteralPath $BepInExCoreDir -Filter '*.dll' -File |
            Where-Object { $_.Name -match '^(BepInEx|0Harmony|MonoMod\..*|Mono\.Cecil.*)\.dll$' } |
            Copy-Item -Destination $depsDir -Force
        Get-ChildItem -LiteralPath (Join-Path $ValheimDir 'valheim_Data\Managed') -Filter '*.dll' -File |
            Where-Object { $_.Name -notmatch '^(System\..*|mscorlib|netstandard|Microsoft\..*)\.dll$' } |
            Copy-Item -Destination $depsDir -Force
        Set-Content -LiteralPath (Join-Path $installedDir 'ready') -Value "$RiverheimDll`n$ValheimDir`n$BepInExCoreDir" -Encoding UTF8
    }
    $core = Get-ChildItem -LiteralPath $extractDir -Filter 'Riverheim.Core.dll' -File -Recurse | Select-Object -First 1
} else {
    $core = $bundledCore
}
$coreVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($core.FullName).FileVersion
$coreLabel = if ($Engine -eq 'Installed') { "installed Riverheim $coreVersion (from $RiverheimDll)" } else { "the Riverheim $coreVersion bundled in AtlasBuilder" }

# ---- Startup hooks, compiled with the C# 5 compiler that ships with Windows ----------------------------------
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
function Compile-Hook {
    param([string]$Output, [string[]]$Sources, [string[]]$References)
    $compilerArgs = @('-nologo', '-nostdlib+', '-noconfig', '-target:library', "-out:`"$Output`"")
    foreach ($name in $References) { $compilerArgs += "-reference:`"$(Join-Path $core.DirectoryName "$name.dll")`"" }
    foreach ($source in $Sources) { $compilerArgs += "`"$source`"" }
    $log = Join-Path $outDir 'csc.log'
    $build = Start-Process -FilePath $compiler -ArgumentList $compilerArgs -Wait -PassThru -NoNewWindow -RedirectStandardOutput $log
    if ($build.ExitCode -ne 0) { throw "Compiling $(Split-Path -Leaf $Output) failed (C# 5 compiler):`n$(Get-Content -LiteralPath $log -Raw)" }
}
$hookDll = Join-Path $outDir 'RiverheimConfigHook.dll'
Compile-Hook -Output $hookDll -Sources @($hookSource, $settingsSource) -References @('System.Private.CoreLib', 'System.Runtime', 'System.Console', 'System.Collections', 'netstandard', 'Riverheim.Core')
$depsHookDll = Join-Path $outDir 'DepsHook.dll'
if ($Engine -eq 'Installed') { Compile-Hook -Output $depsHookDll -Sources @($depsHookSource) -References @('System.Private.CoreLib', 'System.Runtime', 'System.Console') }
Say "Hooks compiled with $compiler against $($core.FullName)"
Say "Rendering with $coreLabel"

$stockCfg = Join-Path $outDir 'stock.cfg'
$defaultsCfg = Join-Path $outDir 'defaults.cfg'
Tool @('stock-cfg', $core.FullName, $stockCfg)
Tool @('cfg', $defaultsCfg)
Tool @('json', (Join-Path $outDir 'settings.json')) | Out-Null
$schema = Get-Content -LiteralPath (Join-Path $outDir 'settings.json') -Raw | ConvertFrom-Json

# ---- Rendering ---------------------------------------------------------------------------------------------
# AtlasBuilder 1.0.0 needs a real console for its progress display (a redirected stdout makes it crash), so each
# render runs in its own console window through cmd.exe, with only stderr captured.
function Render {
    param([string]$Name, [int]$Seed, [string]$Mode, [string]$Cfg, [bool]$Force)
    $folder = Join-Path $outDir "render\$Name"
    if (Test-Path -LiteralPath $folder) { Remove-Item -LiteralPath $folder -Recurse -Force }
    [void](New-Item -ItemType Directory -Path $folder -Force)
    $errLog = Join-Path $folder 'stderr.log'
    $preset = if ($Cfg) { 'Experimental' } else { 'Default' }
    $wrapper = Join-Path $folder 'run.cmd'
    @(
        '@echo off',
        ('"{0}" -p "{1}" -s {2} -m {3} -r {4} -n -1 --preset {5} 2> "{6}"' -f $AtlasExe, $folder, $Seed, $Mode, $Resolution, $preset, $errLog),
        'exit /b %ERRORLEVEL%'
    ) | Set-Content -LiteralPath $wrapper -Encoding ASCII
    $hooks = @()
    try {
        if ($Engine -eq 'Installed') {
            $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR = $extractDir
            $env:RIVERHEIM_DEPS_DIR = $depsDir
            $hooks += $depsHookDll
        }
        if ($Cfg) {
            $hooks += $hookDll
            $env:RIVERHEIM_CONFIG_PATH = $Cfg
            $env:RIVERHEIM_CONFIG_FORCE = if ($Force) { '1' } else { '0' }
        }
        if ($hooks.Count -gt 0) { $env:DOTNET_STARTUP_HOOKS = $hooks -join ';' }
        $process = Start-Process -FilePath 'cmd.exe' -ArgumentList @('/c', "`"$wrapper`"") -Wait -PassThru -WindowStyle Minimized
    } finally {
        foreach ($name in 'DOTNET_STARTUP_HOOKS', 'DOTNET_BUNDLE_EXTRACT_BASE_DIR', 'RIVERHEIM_DEPS_DIR', 'RIVERHEIM_CONFIG_PATH', 'RIVERHEIM_CONFIG_FORCE') {
            Remove-Item "Env:$name" -ErrorAction SilentlyContinue
        }
    }
    $rawErr = if (Test-Path -LiteralPath $errLog) { Get-Content -LiteralPath $errLog -Raw } else { $null }
    $stderr = if ($rawErr) { $rawErr.Trim() } else { '' }
    $image = Get-ChildItem -LiteralPath $folder -Filter '*.png' -File | Select-Object -First 1
    if ($process.ExitCode -ne 0 -or $null -eq $image) {
        # The first real exception line; the full output stays in stderr.log next to the render.
        $cause = @($stderr -split "`r?`n" | Where-Object { $_ -match 'Exception' -and $_ -notmatch 'AggregateException|TargetInvocationException|Unhandled exception\. |Failed to generate world' } | Select-Object -First 1)
        $summary = if ($cause.Count -gt 0) { $cause[0].Trim() } elseif ($stderr) { ($stderr -split "`r?`n")[0] } else { 'no output' }
        return [pscustomobject]@{ Name = $Name; Hash = $null; Image = $null; Error = "AtlasBuilder exit code $($process.ExitCode): $summary" }
    }
    $hash = (Get-FileHash -LiteralPath $image.FullName -Algorithm SHA256).Hash
    $problem = if ($stderr -match 'Could not apply|Could not apply settings|No settings file found') { $stderr } else { $null }
    return [pscustomobject]@{ Name = $Name; Hash = $hash; Image = $image.FullName; Error = $problem; Log = $stderr }
}

function Write-Cfg {
    param([string]$Path, [hashtable]$Overrides)
    # The all-defaults cfg with some values replaced; same format as the mod's config file.
    $lines = foreach ($line in Get-Content -LiteralPath $defaultsCfg) {
        $m = [regex]::Match($line, '^(\w+) = ')
        if ($m.Success -and $Overrides.ContainsKey($m.Groups[1].Value)) { '{0} = {1}' -f $m.Groups[1].Value, ([double]$Overrides[$m.Groups[1].Value]).ToString('0.##########', $invariant) } else { $line }
    }
    Set-Content -LiteralPath $Path -Value $lines -Encoding ASCII
}

# ---- Stock -------------------------------------------------------------------------------------------------
if ($Tests -contains 'Stock') {
    Say ''
    Say '== Stock: the mod''s mapping with stock values reproduces --preset Default =='
    Say "   (stock values read from $coreLabel; rhconfig-test check reports where the listed defaults differ)"
    foreach ($seed in $Seeds) {
        foreach ($mode in $Modes) {
            $base = Render -Name "stock\$seed-$mode-default" -Seed $seed -Mode $mode -Cfg $null -Force $false
            $forced = Render -Name "stock\$seed-$mode-forced" -Seed $seed -Mode $mode -Cfg $stockCfg -Force $true
            $untouched = Render -Name "stock\$seed-$mode-untouched" -Seed $seed -Mode $mode -Cfg $defaultsCfg -Force $false
            $listed = Render -Name "stock\$seed-$mode-listed" -Seed $seed -Mode $mode -Cfg $defaultsCfg -Force $true
            foreach ($r in @($base, $forced, $untouched, $listed)) { if ($r.Error) { Say "   ERROR $($r.Name): $($r.Error)"; $failures++ } }
            if ($base.Hash) {
                $ok1 = $forced.Hash -eq $base.Hash
                $ok2 = $untouched.Hash -eq $base.Hash
                $ok3 = $listed.Hash -eq $base.Hash
                if (-not $ok1) { $failures++ }
                if (-not $ok2) { $failures++ }
                if (-not $ok3 -and $Engine -eq 'Installed') { $failures++ }
                Say ("   seed {0} {1}: stock cfg forced {2}; all-default cfg {3}; listed defaults forced {4}" -f $seed, $mode,
                    $(if ($ok1) { 'IDENTICAL (pass)' } else { 'DIFFERS (FAIL)' }),
                    $(if ($ok2) { 'IDENTICAL (pass)' } else { 'DIFFERS (FAIL)' }),
                    $(if ($ok3) { 'IDENTICAL (pass)' } elseif ($Engine -eq 'Installed') { 'DIFFERS (FAIL: the listed defaults are not this Riverheim''s)' } else { 'differ (expected: the bundled Riverheim''s defaults are older than the listed ones)' }))
            }
        }
    }
}

# ---- Settings ----------------------------------------------------------------------------------------------
if ($Tests -contains 'Settings') {
    Say ''
    Say "== Settings: each setting at a low and a high value changes the $SettingsMode render (seed $($Seeds[0])) =="
    $baseline = Render -Name "settings\baseline" -Seed $Seeds[0] -Mode $SettingsMode -Cfg $null -Force $false
    if ($baseline.Error) { throw "Baseline render failed: $($baseline.Error)" }
    $noEffect = @()
    $crashed = @()
    foreach ($setting in $schema.settings) {
        if ($Only.Count -gt 0 -and $Only -notcontains $setting.key) { continue }
        $default = [double]$setting.default
        $step = [double]$setting.step
        $low = [Math]::Max([double]$setting.min, $default - 10 * $step)
        $high = [Math]::Min([double]$setting.max, $default + 10 * $step)
        # Keep the slow and the degenerate cases reasonable.
        switch ($setting.key) {
            'TileSpacing' { $low = 45; $high = 90 }
            'WorldRadius' { $low = 7000; $high = 14000 }
            'OriginDensity' { $low = 0.05; $high = 0.6 }
            'Budget' { $low = 0.001; $high = 0.02 }
            'MeadowsFadeEnd' { $low = 20; $high = 90 }
            'SwampStart' { $low = 8; $high = 40 }
            'SwampEnd' { $low = 35; $high = 85 }
            'PlainsStart' { $low = 10; $high = 50 }
            'PlainsEnd' { $low = 45; $high = 90 }
            'MistlandsStart' { $low = 35; $high = 85 }
        }
        if ($setting.integer) { $low = [Math]::Round($low); $high = [Math]::Round($high) }
        $results = @{}
        foreach ($pair in @(@('low', $low), @('high', $high))) {
            if ($pair[1] -eq $default) { $results[$pair[0]] = 'skipped (equals default)'; continue }
            $cfg = Join-Path $outDir "render\settings\$($setting.key)-$($pair[0]).cfg"
            [void](New-Item -ItemType Directory -Path (Split-Path -Parent $cfg) -Force)
            Write-Cfg -Path $cfg -Overrides @{ $setting.key = $pair[1] }
            $r = Render -Name "settings\$($setting.key)-$($pair[0])" -Seed $Seeds[0] -Mode $SettingsMode -Cfg $cfg -Force $false
            if ($r.Error) { $results[$pair[0]] = "ERROR $($r.Error)" }
            elseif ($r.Log -match 'not previewed') { $results[$pair[0]] = 'not in this AtlasBuilder (skipped by hook)' }
            elseif ($r.Hash -ne $baseline.Hash) { $results[$pair[0]] = 'changes map' }
            else { $results[$pair[0]] = 'NO CHANGE' }
        }
        $joined = $results.Values -join ' '
        $verdict = if ($joined -match 'ERROR') { 'CRASH' } elseif ($results.Values -contains 'changes map') { 'pass' } elseif ($joined -match 'not in this AtlasBuilder') { 'not previewable' } else { 'FAIL' }
        if ($verdict -eq 'FAIL') { $failures++; $noEffect += $setting.key }
        if ($verdict -eq 'CRASH') { $failures++; $crashed += $setting.key }
        Say ("   {0,-32} {1,-15} low {2} -> {3}; high {4} -> {5}" -f $setting.key, $verdict, $low.ToString($invariant), $results['low'], $high.ToString($invariant), $results['high'])
    }
    if ($noEffect.Count -gt 0) { Say "   Settings with no visible effect: $($noEffect -join ', ')" }
    if ($crashed.Count -gt 0) { Say "   Settings with a value that crashes Riverheim (narrow the range): $($crashed -join ', ')" }
}

# ---- Reference ---------------------------------------------------------------------------------------------
if ($Tests -contains 'Reference') {
    Say ''
    Say '== Reference: presets/BEST.cfg renders the same as the kept reference image =='
    [void](New-Item -ItemType Directory -Path $referenceDir -Force)
    $bestCfg = Join-Path $repoRoot 'presets\BEST.cfg'
    foreach ($mode in $Modes) {
        $r = Render -Name "reference\BEST-$($Seeds[0])-$mode" -Seed $Seeds[0] -Mode $mode -Cfg $bestCfg -Force $false
        if ($r.Error) { Say "   ERROR $($r.Error)"; $failures++; continue }
        $referenceImage = Join-Path $referenceDir "BEST_$($Seeds[0])_$mode.png"
        $referenceHash = Join-Path $referenceDir "BEST_$($Seeds[0])_$mode.sha256"
        if ($UpdateReference -or -not (Test-Path -LiteralPath $referenceHash)) {
            Copy-Item -LiteralPath $r.Image -Destination $referenceImage -Force
            Set-Content -LiteralPath $referenceHash -Value "$($r.Hash) $Engine Riverheim $coreVersion" -Encoding ASCII
            Say "   seed $($Seeds[0]) ${mode}: reference written ($($r.Hash.Substring(0, 12)), $Engine Riverheim $coreVersion)"
        } else {
            $expected = ((Get-Content -LiteralPath $referenceHash -Raw).Trim() -split ' ')[0]
            if ($expected -eq $r.Hash) { Say "   seed $($Seeds[0]) ${mode}: IDENTICAL to reference (pass)" }
            else { Say "   seed $($Seeds[0]) ${mode}: DIFFERS from reference (FAIL). The mapping changed, or Riverheim did; rerun with -UpdateReference if intended."; $failures++ }
        }
    }
}

Say ''
Say $(if ($failures -eq 0) { 'All AtlasBuilder checks passed.' } else { "$failures check(s) failed." })
$report | Set-Content -LiteralPath (Join-Path $outDir 'report.txt') -Encoding UTF8
exit $(if ($failures -eq 0) { 0 } else { 1 })
