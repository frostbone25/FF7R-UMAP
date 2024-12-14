using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CUE4Parse.FileProvider;
using UMapExporter.ExtractedData;

namespace UMapExporter
{
    public class CollectedData
    {
        public DataProvider dataProvider;
        public ExportedLevel exportedLevel;
        public List<string> loadedUnrealLevels;

        public string outputDirectory;

        public CollectedData(DataProvider dataProvider, string outputDirectory)
        {
            exportedLevel = new ExportedLevel();
            loadedUnrealLevels = new List<string>();

            this.dataProvider = dataProvider;
            this.outputDirectory = outputDirectory;
        }

        private static bool HasExportedFile(string packagePathReference, List<ExportedFile> exportedFiles)
        {
            for (int i = 0; i < exportedFiles.Count; i++)
            {
                if (exportedFiles[i].PackagePathReference.Equals(packagePathReference))
                    return true;
            }

            return false;
        }

        public bool HasExportedMesh(string meshPackagePathReference) => HasExportedFile(meshPackagePathReference, exportedLevel.ExportedMeshReferences);

        public bool HasExportedTexture(string texturePackagePathReference) => HasExportedFile(texturePackagePathReference, exportedLevel.ExportedTextureReferences);

        public void PrintInfo()
        {
            ConsoleWriter.WriteInfoLine(string.Format("{0} Level Light Actors Collected...", exportedLevel.LightActors.Count));
            ConsoleWriter.WriteInfoLine(string.Format("{0} Level Mesh Actors Collected...", exportedLevel.MeshActors.Count));
            ConsoleWriter.WriteInfoLine(string.Format("{0} Mesh Asset References Collected...", exportedLevel.ExportedMeshReferences.Count));
            ConsoleWriter.WriteInfoLine(string.Format("{0} Texture Asset References Collected...", exportedLevel.ExportedTextureReferences.Count));
        }
    }
}
