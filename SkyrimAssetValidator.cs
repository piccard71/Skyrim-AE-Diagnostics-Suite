using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace SkyrimAutoDiag
{
    public class AssetCheckReport
    {
        public FormKey CheckedFormKey { get; set; }
        public string WinningMod { get; set; } = string.Empty;
        public string ExpectedMeshPath { get; set; } = string.Empty;
        public bool MeshExists { get; set; }
        public string? MeshFoundInLocation { get; set; }
        public List<string> MissingTextures { get; set; } = new();
        public List<string> CorruptedOrInvalidDimensionTextures { get; set; } = new();
    }

    public class SkyrimAssetValidator
    {
        private static readonly Regex DdsPathRegex = new(
            @"(?:textures[\\/])?[a-zA-Z0-9_\-\s\.\\/]+\.dds",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public AssetCheckReport ValidateObjectAssets(string dataFolderPath, FormKey targetFormKey)
        {
            var report = new AssetCheckReport { CheckedFormKey = targetFormKey };
            using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);

            if (!env.LinkCache.TryResolveContext<ISkyrimMajorRecord, ISkyrimMajorRecordGetter>(
                    targetFormKey, out var context))
                return report;

            if (context.Record is not IModeledGetter modeled ||
                modeled.Model == null ||
                string.IsNullOrWhiteSpace(modeled.Model.File.GivenPath))
                return report;

            report.WinningMod = context.ModKey.FileName;
            string relMeshPath = NormalizeSkyrimPath(modeled.Model.File.GivenPath, "meshes");
            report.ExpectedMeshPath = relMeshPath;

            var bsaReaders = LoadAllBsaReaders(dataFolderPath);
            byte[]? nifBytes = TryReadAssetBytes(dataFolderPath, relMeshPath, bsaReaders, out string? foundLoc);
            report.MeshExists = nifBytes != null;
            report.MeshFoundInLocation = foundLoc;

            if (nifBytes != null)
            {
                foreach (var texPath in ExtractDdsPathsFromNif(nifBytes))
                {
                    string normTex = NormalizeSkyrimPath(texPath, "textures");
                    byte[]? ddsBytes = TryReadAssetBytes(dataFolderPath, normTex, bsaReaders, out _);
                    if (ddsBytes == null)
                    {
                        report.MissingTextures.Add(normTex);
                    }
                    else if (!ValidateDdsHeaderAndDimensions(ddsBytes, out string reason))
                    {
                        report.CorruptedOrInvalidDimensionTextures.Add($"{normTex} ({reason})");
                    }
                }
            }
            return report;
        }

        // Prüft die ersten 20 Bytes des .dds Headers auf "DDS "-Magic und ob Höhe/Breite durch 4 teilbar sind
        public bool ValidateDdsHeaderAndDimensions(byte[] ddsBytes, out string errorReason)
        {
            errorReason = string.Empty;
            if (ddsBytes.Length < 128)
            {
                errorReason = "File truncated (< 128 bytes)";
                return false;
            }

            // Magic "DDS " = 0x20534444
            uint magic = BitConverter.ToUInt32(ddsBytes, 0);
            if (magic != 0x20534444)
            {
                errorReason = "Invalid DDS Magic Header";
                return false;
            }

            uint height = BitConverter.ToUInt32(ddsBytes, 12);
            uint width = BitConverter.ToUInt32(ddsBytes, 16);

            if (height == 0 || width == 0 || (height % 4 != 0) || (width % 4 != 0))
            {
                errorReason = $"Invalid BC/DXT Dimensions: {width}x{height} (not divisible by 4 -> CTD risk!)";
                return false;
            }
            return true;
        }

        private List<(string BsaName, IArchiveReader Reader)> LoadAllBsaReaders(string dataFolderPath)
        {
            var list = new List<(string, IArchiveReader)>();
            if (!Directory.Exists(dataFolderPath)) return list;

            foreach (var bsaFile in Directory.GetFiles(dataFolderPath, "*.bsa"))
            {
                try
                {
                    var reader = Archive.CreateReader(GameRelease.SkyrimSE, bsaFile);
                    list.Add((Path.GetFileName(bsaFile), reader));
                }
                catch { }
            }
            return list;
        }

        private byte[]? TryReadAssetBytes(
            string dataFolder,
            string relativeAssetPath,
            List<(string BsaName, IArchiveReader Reader)> bsaReaders,
            out string? location)
        {
            string loosePath = Path.Combine(dataFolder, relativeAssetPath);
            if (File.Exists(loosePath))
            {
                location = "Loose File";
                return File.ReadAllBytes(loosePath);
            }

            string folderDir = Path.GetDirectoryName(relativeAssetPath)?.Replace('/', '\\') ?? string.Empty;
            string fileName = Path.GetFileName(relativeAssetPath);

            foreach (var (bsaName, reader) in bsaReaders)
            {
                if (reader.TryGetFolder(folderDir, out var bsaFolder))
                {
                    var file = bsaFolder.Files.FirstOrDefault(f =>
                        string.Equals(Path.GetFileName(f.Path), fileName, StringComparison.OrdinalIgnoreCase));
                    if (file != null)
                    {
                        location = $"BSA ({bsaName})";
                        return file.GetBytes();
                    }
                }
            }
            location = null;
            return null;
        }

        private HashSet<string> ExtractDdsPathsFromNif(byte[] nifBytes)
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string ascii = Encoding.ASCII.GetString(nifBytes);
            foreach (Match m in DdsPathRegex.Matches(ascii))
            {
                string raw = m.Value.Trim();
                if (!string.IsNullOrWhiteSpace(raw)) results.Add(raw);
            }
            return results;
        }

        private string NormalizeSkyrimPath(string rawPath, string prefix)
        {
            string c = rawPath.Replace('/', '\\').TrimStart('\\');
            if (!c.StartsWith(prefix + "\\", StringComparison.OrdinalIgnoreCase))
                c = Path.Combine(prefix, c);
            return c;
        }
    }
}