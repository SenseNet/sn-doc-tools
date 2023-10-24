# sn-doc-tools

This is a repository containing tools for generating documentation of the sensenet ecosystem.

## SnDocumentGenerator

This is a tool that generates markdown files of the sensenet's documentation by processing the source code. There are two main topics: REST API and configuration. The files are generated by the documentation comments of the C# files. Processes only the XML style documetation, the single line (`/// example...`) and delimited (`/** example... *`) documentations are skipped.

The document generator does not use templates, the document formats are hardcoded. The generator recognizes and uses the following documetation elements and attributes: `<summary> <remarks> <para> <nodoc> <param name=""> <paramref name=""> <returns> <value>;text</value> <see cref=""> <seealso cref=""> <example> <exception>`

The tool is a console exe that can be called by the following shell line:
```
SnDocumentGenerator <InputDir> <OutputDir> [-cat|-op|-flat] [-all]
```
### Input directory (required)
If the given path is a directory, the tool looks up the project and C# files under it but can process a single file too if the input path points to a file.

### Output directory (required)
Defines the root directory of the generated files. If the directory does not exist, it will be created, otherwise its content will be deleted before the newly generated files are written.

The generated structure is the following under the output directory:
```
backend
  ODataOperations
    cheatsheet.md
    index.md
    ... more md files
  OptionClasses
    cheatsheet.md
    index.md
    ... more md files
frontend
  ODataOperations
    cheatsheet.md
    index.md
    ... more md files
  OptionClasses
    cheatsheet.md
    index.md
    ... more md files
generation.txt
```
- **backend**: documents for sensenet ecosystem developers
- **frontend**: documents for developer or operator who uses sensenet
- **cheatsheet.md**: compressed memo/example of all generated items.
- **index.md**: contains links to all generated items.

### Output structure (optional)
This option controls the target structure of the REST API documentation. Every operation is categorized int the documentation comments by the `<snCategory>` element. There is 3 kind of grouping by these switches:
- `-flat`: This is the default. There is no any categorizing, the files are in bulk in the target directory.
- `-cat`: Every category is one file that contains all operations.
- `-op`: Every operation is a separated file but grouped in separate folders per category.


### Detail (optional)
The tool differentiates the projects by the following categories:
- Modern: .NET, .NET Standard and .NET Core projects
- Old school: .NET Framework projects (max version 4.8.1)
- Test projects: the project file name ends with "tests" or "test"

The default document generation processes files only the modern projects. If the `-all` switch is present, the old school and test projects are processed too.
