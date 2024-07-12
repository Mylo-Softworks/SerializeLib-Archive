using SerializeLib;
using SerializeLib.Attributes;

namespace SLar;

/// <summary>
/// SerializeLib Archive tools
/// </summary>
public static class SLAr
{
    /// <summary>
    /// Create a new SLArchive.
    /// </summary>
    /// <param name="files">A tuple with the filenames, and read streams of the target files to put in the archive.</param>
    /// <param name="metaData">The metadata to provide, or null if no metadata is provided.</param>
    /// <returns>The created SLArchive object.</returns>
    public static SLArchive CreateArchive((string, Stream)[] files, List<MetaTag>? metaData = null)
    {
        return new SLArchive(files.Select(tuple => new SLArFile(tuple.Item1, tuple.Item2)).ToList(), metaData ?? new List<MetaTag>());
    }

    /// <summary>
    /// Create a new SLArchive.
    /// </summary>
    /// <param name="files">The files, as a tuple of 2 strings, (archive path, disk path).</param>
    /// <param name="metaData">The metadata to provide, or null if no metadata is provided.</param>
    /// <returns>The created SLArchive object.</returns>
    public static SLArchive CreateArchive((string, string)[] files, List<MetaTag>? metaData = null)
    {
        return CreateArchive(files.Select(s => (s.Item1,
                File.Open(s.Item2, FileMode.Open, FileAccess.Read) as Stream)).ToArray(), metaData);
    }

    /// <summary>
    /// Create a new SLArchive from all files in a directory.
    /// </summary>
    /// <param name="directory">The directory to archive.</param>
    /// <param name="metaData">The metadata to provide, or null if no metadata is provided.</param>
    /// <returns>The created SLArchive object.</returns>
    public static SLArchive CreateArchive(string directory, List<MetaTag>? metaData = null)
    {
        var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
            .Select(s => 
                (StripPrefix(s, directory), // Strip directory path
                    File.Open(s, FileMode.Open, FileAccess.Read) as Stream)).ToArray();
        return CreateArchive(files, metaData);
    }

    /// <summary>
    /// Extract a .slar file to a directory.
    /// </summary>
    /// <param name="archive">The file path to the .slar file to extract.</param>
    /// <param name="outDirectory">The output directory.</param>
    public static void Extract(string archive, string outDirectory)
    {
        using var slar = OpenArchive(archive);
        slar.Extract(outDirectory);
    }

    /// <summary>
    /// Extract a SLArchive to a directory.
    /// </summary>
    /// <param name="archive">The SLArchive to extract.</param>
    /// <param name="outDirectory">The output directory.</param>
    public static void Extract(this SLArchive archive, string outDirectory)
    {
        foreach (var slarFile in archive.SLArFiles)
        {
            var filePath = Path.Join(outDirectory, slarFile.Name);

            var dirName = Path.GetDirectoryName(filePath);
            if (dirName != null)
            {
                Directory.CreateDirectory(dirName);
            }
            
            using var fs = File.Open(filePath, FileMode.Create, FileAccess.Write);
            slarFile.Stream.CopyTo(fs); // Write file over to output
        }
    }

    /// <summary>
    /// Open a SLArchive from a file path.
    /// </summary>
    /// <param name="file">The file path to the .slar file to open.</param>
    /// <returns>A SLArchive for the .slar file that was opened.</returns>
    public static SLArchive OpenArchive(string file)
    {
        var stream = File.Open(file, FileMode.Open, FileAccess.Read);
        return Serializer.Deserialize<SLArchive>(stream);
    }

    private static string StripPrefix(string filename, string prefix)
    {
        return filename.Substring(prefix.Length).Replace("\\", "/").TrimStart('/');
    }
}

/// <summary>
/// SerializeLib Archive struct
/// </summary>
[SerializeClass]
public struct SLArchive : IDisposable
{
    /// <summary>
    /// The MetaTags containing the metadata.
    /// </summary>
    [SerializeField(0)] public List<MetaTag> Metadata;
    
    /// <summary>
    /// The SLArFiles of this archive.
    /// </summary>
    [SerializeField(1)] public List<SLArFile> SLArFiles;

    /// <summary>
    /// A property which gives all the filenames of the file in this archive.
    /// </summary>
    public String[] FileNames => SLArFiles.Select(file => file.Name!).ToArray();
    
    /// <summary>
    /// Get indexer to get a file by its index.
    /// </summary>
    /// <param name="index">The index of the SLArFile to get.</param>
    public SLArFile this[int index] => SLArFiles[index];
    
    /// <summary>
    /// Get indexer to get a file by its path.
    /// </summary>
    /// <param name="name">The path of the SLArFile to get. (case sensitive).</param>
    public SLArFile this[string name] => SLArFiles.Find(file => file.Name == name);
    
    /// <summary>
    /// Get a meta tag by name, and optionally file.
    /// </summary>
    /// <param name="name">The name of the MetaTag to find.</param>
    /// <param name="referencedFile">If provided, the file associted with the searched tag.</param>
    /// <returns>The MetaTag if found, else null.</returns>
    public MetaTag? GetMetaTag(string name, string? referencedFile = null) => Metadata.Select(tag => tag as MetaTag?).FirstOrDefault(meta => meta!.Value.Name == name && referencedFile == meta.Value.ReferencedFile);
    
    /// <summary>
    /// Get all MetaTags associated with a file.
    /// </summary>
    /// <param name="referencedFile">The file to search for MetaTags on.</param>
    /// <returns>An array of all found MetaTags.</returns>
    public MetaTag[] GetMetaTagsForFile(string referencedFile) => Metadata.Where(tag => tag.ReferencedFile == referencedFile).ToArray();

    /// <summary>
    /// Parameterless constructor.
    /// </summary>
    public SLArchive()
    {
        Metadata = new();
        SLArFiles = new();
    }

    /// <summary>
    /// Create a new SLArchive with a list of SLArFiles and a list of MetaTags.
    /// </summary>
    /// <param name="slArFiles">The list of SLArFiles.</param>
    /// <param name="metadata">The list of MetaTags.</param>
    public SLArchive(List<SLArFile> slArFiles, List<MetaTag> metadata)
    {
        SLArFiles = slArFiles;
        Metadata = metadata;
    }

    public void Dispose()
    {
        if (SLArFiles != null)
        {
            foreach (var slarFile in SLArFiles)
            {
                slarFile.Dispose();
            }
        }
    }
}

/// <summary>
/// A tag to indicate metadata for an archive, or a file within.
/// </summary>
[SerializeClass]
public struct MetaTag
{
    [SerializeField(0)] public string Name;
    [SerializeField(1)] public string Value;
    [SerializeField(1)] public string? ReferencedFile;

    public MetaTag(string name, string value, string? referencedFile = null)
    {
        Name = name;
        Value = value;
        ReferencedFile = referencedFile;
    }

    public MetaTag()
    {
        Name = string.Empty;
        Value = string.Empty;
        ReferencedFile = null;
    }
}