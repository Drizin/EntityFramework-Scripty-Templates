# EntityFramework-Scripty-Templates
Scripty templates to generate EntityFramework model from Existing Database

These are based on [Simon Hughes T4 templates](https://github.com/sjh37/EntityFramework-Reverse-POCO-Code-First-Generator) (http://www.reversepoco.com/).

Since T4 templates don't have good tools for development/debugging (either in .NET Framework, let alone in .NET Core),  
I decided to use [Scripty (by Dave Glick)](https://github.com/daveaglick/Scripty) to create my templates.

Scripty templates are based on Roslyn so they can be created using regular C#, using Visual Studio, intellisense, compiled, etc. 
Much easier than T4.

The idea of this project is to initially create an identical port of Simon templates, then refactor some code to 
make it easier to extend and customize, and finally convert these templates to .NET Core. 


## Installation / Usage
Current project is a Console Application which runs on .NET Framework 4.6+, and contains both the Templates
and Scripty calls to execute those templates. Templates are all into `CSX.cs` files with full code completion (intellisense) available 
to make it easier to modify the templates.

The Console Application will invoke Scripty, but you can refer to [Scripty page](https://github.com/daveaglick/Scripty) if you want
to configure those templates to be automatically executed inside your project.

## Contributing
1. Fork it!
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a pull request :D

## History
- 2019-06-29: Copied reverse engineer classes from EntityFramework-Reverse-POCO-Code-First-Generator, removed T4 and Visual Studio dependencies, and ported to Scripty. (This is my first Scripty template!)
- 2019-06-30: Finished migration of POCO/DbContext scripts. Identical to the originals.

## Credits
- Simon Hughes for his amazing [T4 templates](https://github.com/sjh37/EntityFramework-Reverse-POCO-Code-First-Generator)
- Dave Glick for this great tool [Scripty](https://github.com/daveaglick/Scripty)

## License
MIT License
