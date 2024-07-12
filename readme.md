# SerializeLib-Archive
An archive file format using [SerializeLib], implemented in C#.

## What does it do?
SerializeLib-Archive, aka SLAr, is an archiving file format for storing multiple files inside of one archive.

## Usage
To archive a directory.
```csharp
var slar = SLAr.CreateArchive("directory"); // Create the archive object for archiving all files in "directory"
Serializer.SerializeToFile(slar, "file.slar"); // Save the file to a .slar file using SerializeLib
slar.Dispose(); // Dispose the archive, this is needed to close the streams from opening the files in "directory"
```

To extract a directory
```csharp
// Direct
SLAr.Extract("file.slar", "out_directory"); // Quick extraction

// Open archive
using var slar = SLAr.OpenArchive("file.slar"); // Uses 'using' to dispose automatically
slar.extract("out_directory"); // Extracts the file, we still have access to the file through 'slar'.
```

To read a single archived file to a byte[]
```csharp
var file = slar["file.txt"]; // Get the file
var stream = file.Stream; // Get the PartialStream from this file

var fileName = file.Name; // To get the filename (including relative path)
var bytes = stream.ReadToByteArray(); // To get all the bytes of the file

// You can also use the stream like a normal stream, it's a read-only stream which can only access the part of the archive that contains the stream's file.
using var fs = File.Open("out.txt", FileMode.Create, FileAccess.Write);
stream.CopyTo(fs); // Write the file from within the slar to fs
```

[SerializeLib]: https://github.com/Mylo-Softworks/SerializeLib/