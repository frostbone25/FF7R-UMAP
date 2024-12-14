using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UMapExporter.ExtractedData
{
    public class MaterialReference
    {
        public string Name;
        public List<TextureReference> TextureReferences;

        public MaterialReference()
        {
            TextureReferences = new List<TextureReference>();
        }

        public TextureReference GetTextureReferenceByMaterialParameterName(string materialParameterName)
        {
            if (TextureReferences == null)
                return null;

            for(int i = 0; i < TextureReferences.Count; i++)
            {
                if (TextureReferences[i].MaterialParameterName == materialParameterName)
                    return TextureReferences[i];
            }

            return null;
        }
    }
}
