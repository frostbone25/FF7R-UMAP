using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UMapExporter.ExtractedData;

namespace UMapExporter
{
    public class ExportedLevel
    {
        //public List<ExtractedSoundActor> SoundActors; //list of actors that are sound emitters
        //public List<ExtractedGenericActor> GenericActors; //list of generic actors with no important data
        public List<ExtractedLightActor> LightActors;
        public List<ExtractedMeshActor> MeshActors;

        //public List<string> ExportedSoundReferences;
        public List<ExportedFile> ExportedMeshReferences;
        public List<ExportedFile> ExportedTextureReferences;

        public ExportedLevel()
        {
            LightActors = new List<ExtractedLightActor>();
            MeshActors = new List<ExtractedMeshActor>();

            ExportedMeshReferences = new List<ExportedFile>();
            ExportedTextureReferences = new List<ExportedFile>();
        }
    }
}
