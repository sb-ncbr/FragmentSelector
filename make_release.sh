cd $(dirname $0)

dotnet build
dotnet build -c Release

rm -rf release
mkdir release

cp ./bin/Release/netcoreapp3.1/FragmentSelector.dll release/
cp ./bin/Release/netcoreapp3.1/FragmentSelector.runtimeconfig.json release/
cp ./config.txt release/
cp ./script_pymol.py release/
cp ./README.md release/

echo "Release in $PWD/release/"