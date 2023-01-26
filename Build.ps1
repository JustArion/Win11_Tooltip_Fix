dotnet restore ./src/;
dotnet publish ./src/ --no-restore --runtime win-x86 -p:PublishSingleFile=true --output ./src/Publish --no-self-contained --configuration Release;
echo "Built Files to './Publish'";