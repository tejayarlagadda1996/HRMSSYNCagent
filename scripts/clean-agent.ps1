$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "..\package\uninstall-agent.ps1") @args
