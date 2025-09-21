# This script generates the nauth API client using Kiota and then fixes known issues with the generated code.

Write-Host "Starting API generation process..."

# Step 1: Generate the API client using Kiota
kiota generate -l csharp -c ApiClient -n Keynote_asp.Nauth.API_GEN -d ./API/Nauth.yaml -o ./API/Generated/Keynote_asp/Nauth/API_GEN

# Check if the kiota command was successful
if ($LASTEXITCODE -ne 0) {
    Write-Error "Kiota generation failed. Aborting script."
    exit 1
}

Write-Host "Kiota generation completed successfully."

# Step 2: Fix invalid property names and serialization keys in all generated files.
$generatedPath = ".\API\Generated\Keynote_asp\Nauth\API_GEN"
Write-Host "Searching for generated files in: $generatedPath"
$csFiles = Get-ChildItem -Path $generatedPath -Filter "*.cs" -Recurse

if ($csFiles.Count -eq 0) {
    Write-Warning "No C# files found to patch in '$generatedPath'."
    exit
}

Write-Host "Found $($csFiles.Count) C# files to process..."

foreach ($file in $csFiles) {
    $originalContent = Get-Content $file.FullName -Raw
    
    # 1. Replace invalid identifiers (like '2faId') with valid C# identifiers (like 'TwoFAId').
    # The \b is a word boundary, so it replaces the whole word only.
    $newContent = $originalContent -replace '\b2faId\b', 'TwoFAId' `
                                -replace '\b2FASecret\b', 'TwoFASecret'

    # The word boundary regex can miss property declarations for nullable types (e.g., string? 2faId).
    # This second set of replacements handles those specific cases.
    $newContent = $newContent -replace '(string\? )2faId', '${1}TwoFAId'
    $newContent = $newContent -replace '(string\? )2FASecret', '${1}TwoFASecret'

    # 2. After fixing the C# property names, we must ensure the string keys used for JSON serialization
    # match the original OpenAPI spec. Kiota sometimes generates the wrong key.
    # This changes keys like "2faId" or "TwoFAId" to the correct "_2faId".
    $newContent = $newContent -replace '("2faId"|"TwoFAId")', '"_2faId"'
    $newContent = $newContent -replace '("2FASecret"|"TwoFASecret")', '"_2FASecret"'

    if ($newContent -ne $originalContent) {
        Write-Host "Patched: $($file.Name)"
        Set-Content -Path $file.FullName -Value $newContent
    }
}

Write-Host "Patching complete."
