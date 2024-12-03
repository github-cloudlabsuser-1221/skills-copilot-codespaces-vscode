# Define variables
$solutionPath = "C:\path\to\your\solution.sln"
$projectPath = "C:\path\to\your\project.csproj"
$publishDir = "C:\path\to\publish\directory"

# Load the MSBuild module
Import-Module "$env:ProgramFiles\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

# Build the solution
Write-Host "Building the solution..."
& "$env:ProgramFiles\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" $solutionPath /p:Configuration=Release

# Publish the project using ClickOnce
Write-Host "Publishing the project using ClickOnce..."
& "$env:ProgramFiles\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" $projectPath /t:Publish /p:Configuration=Release /p:PublishDir=$publishDir

Write-Host "Build and publish completed."