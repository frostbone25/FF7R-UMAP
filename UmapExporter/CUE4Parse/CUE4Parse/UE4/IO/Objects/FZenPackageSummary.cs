using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.IO.Objects
{
    public enum EZenPackageVersion : uint
    {
        Initial,
        DataResourceTable,
        ImportedPackageNames,

        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }

    public struct FZenPackageVersioningInfo
    {
        public EZenPackageVersion ZenVersion;
        public FPackageFileVersion PackageVersion;
        public int LicenseeVersion;
        public FCustomVersion[] CustomVersions;

        public FZenPackageVersioningInfo(FArchive Ar)
        {
            ZenVersion = Ar.Read<EZenPackageVersion>();
            PackageVersion = Ar.Read<FPackageFileVersion>();
            LicenseeVersion = Ar.Read<int>();
            CustomVersions = Ar.ReadArray<FCustomVersion>();
        }
    }

    public readonly struct FZenPackageSummary
    {
        public readonly uint bHasVersioningInfo;
        public readonly uint HeaderSize;
        public readonly FMappedName Name;
        public readonly EPackageFlags PackageFlags;
        public readonly uint CookedHeaderSize;
        public readonly int ImportedPublicExportHashesOffset;
        public readonly int ImportMapOffset;
        public readonly int ExportMapOffset;
        public readonly int ExportBundleEntriesOffset;
        public readonly int GraphDataOffset;
        public readonly int ImportedPackageNamesOffset;

        public FZenPackageSummary(FArchive Ar)
        {
            bHasVersioningInfo = Ar.Read<uint>();
            HeaderSize = Ar.Read<uint>();
            Name = Ar.Read<FMappedName>();
            PackageFlags = Ar.Read<EPackageFlags>();
            CookedHeaderSize = Ar.Read<uint>();
            ImportedPublicExportHashesOffset = Ar.Read<int>();
            ImportMapOffset = Ar.Read<int>();
            ExportMapOffset = Ar.Read<int>();
            ExportBundleEntriesOffset = Ar.Read<int>();
            GraphDataOffset = Ar.Read<int>();

            if (Ar.Game >= EGame.GAME_UE5_3)
            {
                ImportedPackageNamesOffset = Ar.Read<int>();
            }

            ImportedPackageNamesOffset = -1;
        }
    }
}
