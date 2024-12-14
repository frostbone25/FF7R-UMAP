using CUE4Parse.UE4.Objects.Core.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UMapExporter.ExtractedData
{
    public class ExtractedMeshActor
    {
        public string Name;

        public FVector Position;
        public FQuat Rotation;
        public FVector Scale;

        public string MeshPackagePath;
        public List<MaterialReference> MaterialReferences;

        public ExtractedMeshActor()
        {
            Scale = FVector.OneVector;
            MaterialReferences = new List<MaterialReference>();
        }
    }
}
