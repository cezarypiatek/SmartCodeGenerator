This project is a completely refactored version of [CodeGeneration.Roslyn](https://github.com/AArnott/CodeGeneration.Roslyn)

## How to create a custom plugin

1. Create .NET Core 3.0+ project
2. Install `SmartCodeGenerator.Sdk` NuGet package
3. Define the attribute that will be used for marking places that trigger your code generation plugin
4. Create a class that implements `SmartCodeGenerator.Sdk.ICodeGenerator` and it's marked with `[CodeGenerator()]` attribute.
5. Build your project and publish the NuGet package.

## How to use a custom plugin

1. Install `SmartCodeGenerator.Engine` NuGet package
2. Install the package with your plugin
3. Add source code of the marking attribute into your codebase
4. Since now you can start marking code with your attribute and after recompilation, the generated code should be available at your disposal.

__IMPORTANT:__ The source code of the marking attribute need to be copied into a consuming project because there should be no runtime dependency between the project that uses the generator plugin and the generator plugin itself. 