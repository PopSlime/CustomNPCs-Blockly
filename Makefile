CNPC_API_DIR ?= ../CustomNPCsAPI/output

editor: cnpc-blocks
	cd ./Editor && npm run build

cnpc-blocks: cnpc-block-generator
	dotnet ./Generator/bin/Release/net8.0/Generator.dll $(CNPC_API_DIR)
	find . -maxdepth 1 -type f -name "*.g.js" -exec mv '{}' ./Editor/src/custom-npcs/ \;

cnpc-block-generator:
	cd ./Generator && dotnet build -c Release
