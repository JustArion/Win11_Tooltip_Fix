dotnet restore;
dotnet publish --no-restore --runtime win-x86 -p:PublishSingleFile=true --output ./Publish --no-self-contained --configuration Release;
echo "Built Files to './Publish'";