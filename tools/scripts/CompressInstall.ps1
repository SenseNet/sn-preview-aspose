$srcPath = [System.IO.Path]::GetFullPath(($PSScriptRoot + '\..\..\src'))
$installFolderAspose = "$srcPath\nuget\content\Admin\tools"
$installPackagePathAspose = "$installFolderAspose\install-preview-aspose.zip"

New-Item $installFolderAspose -Force -ItemType Directory

# Create the install package.
Compress-Archive -Path "$srcPath\nuget\snadmin\install-aspose\*" -Force -CompressionLevel Optimal -DestinationPath $installPackagePathAspose

# Copy the install package to the project directory
Copy-Item $installPackagePathAspose -Destination "$srcPath\AsposePreviewProvider.Install" -Force
