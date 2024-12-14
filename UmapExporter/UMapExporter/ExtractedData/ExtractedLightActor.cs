using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UMapExporter.ExtractedData
{
    public class ExtractedLightActor
    {
        public string Name;

        public ExtractedLightType LightType;

        //transform
        public FVector Position;
        public FQuat Rotation;
        //public FVector Scale; //NOTE: scale might be needed for area lights? not sure

        //spot light
        public float OuterConeAngle;
        public float InnerConeAngle;

        public float AttenuationRadius; //light radius
        public float SourceRadius; //shadow radius/blur

        //common light properties
        public string IntensityUnits; //this isn't required per say, but it is helpful because it tells us the unit that the light intensity is at
        public float Intensity;
        public FColor LightColor;

        //specalized parameters to determine light color
        public bool bUseTemperature;
        public float Temperature;
        public string ColorTemperatureWhitePoint;

        public bool CastShadows;

        public ExtractedLightActor()
        {
            LightType = ExtractedLightType.Unknown;

            Position = FVector.ZeroVector;
            Rotation = new FQuat();

            //NOTE TO SELF: initalize most values to -1, so we can discern later if a value has been assigned or not.
            //For most of these properties, a value of -1 is unrealistic so hopefully when parsing light component values they get overriden.
            OuterConeAngle = -1;
            InnerConeAngle = -1;

            AttenuationRadius = -1;
            SourceRadius = -1;

            Intensity = -1;

            bUseTemperature = false;
            Temperature = -1;

            CastShadows = false;
        }
    }
}
