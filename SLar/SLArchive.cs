using SerializeLib;
using SerializeLib.Attributes;

namespace SLar;

/// <summary>
/// SerializeLib Archive tools
/// </summary>
public static class SLAr
{
    public static SLArchive CreateArchive((string, Stream)[] files, List<MetaTag>? metaData = null)
    {
        return new SLArchive(files.Select(tuple => new SLArFile(tuple.Item1, tuple.Item2)).ToList(), metaData ?? new List<MetaTag>());
    }

    public static SLArchive CreateArchive(string[] files, string removePrefix, List<MetaTag>? metaData = null)
    {
        return CreateArchive(files.Select(s => (StripPrefix(s, removePrefix),
                File.Open(s, FileMode.Open, FileAccess.Read) as Stream)).ToArray(), metaData);
    }

    public static SLArchive CreateArchive(string directory, List<MetaTag>? metaData = null)
    {
        var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
            .Select(s => 
                (StripPrefix(s, directory), // Strip directory path
                    File.Open(s, FileMode.Open, FileAccess.Read) as Stream)).ToArray();
        return CreateArchive(files, metaData);
    }

    public static void Extract(string archive, string outDirectory)
    {
        using var slar = OpenArchive(archive);
        slar.Extract(outDirectory);
    }

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
    [SerializeField(0)] public List<MetaTag> Metadata;
    [SerializeField(1)] public List<SLArFile> SLArFiles;

    public String[] FileNames => SLArFiles.Select(file => file.Name!).ToArray();
    
    public SLArFile this[int index] => SLArFiles[index];
    public SLArFile this[string name] => SLArFiles.Find(file => file.Name == name);
    
    public MetaTag? GetMetaTag(string name, string? referencedFile = null) => Metadata.Select(tag => tag as MetaTag?).FirstOrDefault(meta => meta!.Value.Name == name && (referencedFile == null || referencedFile == meta.Value.ReferencedFile));
    public MetaTag[] GetMetaTagsForFile(string referencedFile) => Metadata.Where(tag => tag.ReferencedFile == referencedFile).ToArray();

    public SLArchive()
    {
        Metadata = new();
        SLArFiles = new();
    }

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