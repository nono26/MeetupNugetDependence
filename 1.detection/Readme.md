# dotnet CLI 

[dotnet list package --vulnerable](backend.dependencies/result/Vulnerable/resultTopLevelVulnerable.txt)

[dotnet list package --include-transitive](backend.dependencies/result/Vulnerable/packageTransitivity.json)

[dotnet list package --include-transitive --vulnerable --format json](./backend.dependencies/result/Vulnerable/packageVunerable.json)

[dotnet list package --include-transitive --deprecated](./backend.dependencies/result/Deprecated/packageDeprecated.txt)

[dotnet list package --outdated](./backend.dependencies/result/Outdated/packageOutdated.txt)
