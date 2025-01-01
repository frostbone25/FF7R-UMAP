using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using CUE4Parse.Utils;
using CUE4Parse.MappingsProvider;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Mesh;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.Meshes;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Serilog;

using SharpGLTF.Schema2;

using SkiaSharp;

using Assimp.Unmanaged;

using UMapExporter.ExtractedData;

namespace UMapExporter
{
    public class Program
    {
        private static string gamePaksPath = "";
        //private static string gamePaksPath = "D:/SteamLibrary/steamapps/common/FINAL FANTASY VII REMAKE";
        //private static string gamePaksPath = "D:/SteamLibrary/steamapps/common/FINAL FANTASY VII REMAKE/End/Content/Paks";
        private static string encryptionKey = "0x23989837645C9D28BA58072B2076E895B853A7C9E1C5591B814C4FD2A2D7B782"; //aes encryption key for game pak archives
        private static string unrealEngineMappingFilePath = "D:/Applications/app-unreal-fmodel-4-4-4/FF7R.usmap"; //mappings file location
        //private static string outputPath = "J:/ff7r/my-umap-exporter-output";
        private static string outputPath = "";
        //private static EGame gameVersion = EGame.GAME_UE4_18;
        private static EGame gameVersion = EGame.GAME_FinalFantasy7Remake;

        //package path for given asset in pak files
        private static string gameUmapPackagePath = "";
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/010-MAKO1_All.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/010-MAKO1_CharaSpec.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/010-MAKO1_Layout.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/010-MAKO1.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_010-Station.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_010-Station"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_010-Sttion"; //wrong
        //private static string gameUmapPackagePath = "End/Content/GameContents/Environment/7thPlate/Texture/T_AtachmentBlockA_01_7thPlate_C.uasset"; //intentionally wrong
        //private static string gameUmapPackagePath = "End/Content/GameContents/Environment/7thPlate/Texture/T_AtachmentBlockA_01_7thPlate_C"; //intentionally wrong
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_010-Station_Animation.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_010-Station_Collision.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_010-Station_Lighting.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_010-Station_Navigation.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_EnvironmentSound.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_Environment.umap";
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_021-MAKO.umap";
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_011-Backcloth1.umap";
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_020-ClockTower.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_020-ClockTower_Lighting.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_060-Passage.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_060-Passage_Lighting.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_030-Tstreet.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_030-Tstreet_Lighting.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_040-Elevator.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_040-Elevator_Lighting.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_050-Stair.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_050-Stair_Lighting.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle1.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle1_Lighting.umap"; 
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_120-aerithhouse.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_120-aerithhouse_lighting.umap"; 
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/080-SLU5B/Layout_Merge/080-SLU5B_Layout_180-AerithHouseRockFar.umap";
        //private static string gameUmapPackagePath = "End/Content/GameContents/Level/Game/Field/080-SLU5B/Layout_Merge/080-SLU5B_Layout_180-AerithHouseRockFar_Lighting.umap"; 
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_270-aerithhousepathway.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_271-aerithhousepathway_sub.umap";

        //end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_220-churchchapelarea.umap
        //end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_220-churchchapelarea_lighting.umap
        //end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_210-churchinsidearea.umap
        //end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_210-churchinsidearea_lighting.umap

        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_105-5thslum_night.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_105-5thslum_night_lighting.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_295-5thslumfar_night.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_295-5thslumfar_night_lighting.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_380-6thtown.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_380-6thtown_lighting.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_373-6thtowngate.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_373-6thtowngate_lighting.umap";

        //wall market
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_490-towncentermarket.umap"; //_lighting
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_500-townrestarea.umap"; //_lighting
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_510-townmainstreeta.umap"; //_lighting
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_520-townmainstreetb.umap"; //_lighting
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_530-townmainstreetc.umap"; //_lighting
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_540-towneastmarket.umap"; //_lighting
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_550-townfoodstreet.umap"; //_lighting
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_560-townneonstreet.umap"; //_lighting
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_570-townwall.umap"; //_lighting

        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_490-towncentermarket_lighting.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_500-townrestarea_lighting.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_510-townmainstreeta_lighting.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_520-townmainstreetb_lighting.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_530-townmainstreetc_lighting.umap"; 
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_540-towneastmarket_lighting.umap"; 
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_550-townfoodstreet_lighting.umap"; 
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_560-townneonstreet_lighting.umap";
        //private static string gameUmapPackagePath = "end/content/gamecontents/level/game/field/080-slu5b/layout_merge/080-slu5b_layout_570-townwall_lighting.umap";

        /*
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_EnvironmentSound.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_010-Station_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_010-Station_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_010-Station_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_010-Station_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_010-Station.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_011-Backcloth1_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_011-Backcloth1.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_020-ClockTower_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_020-ClockTower_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_020-ClockTower_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_020-ClockTower_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_020-ClockTower.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_021-MAKO_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_021-MAKO.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_030-Tstreet_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_030-Tstreet_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_030-Tstreet_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_030-Tstreet_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_030-Tstreet.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_031-TstreetReturn_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_031-TstreetReturn_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_031-TstreetReturn_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_031-TstreetReturn_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_031-TstreetReturn.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_040-Elevator_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_040-Elevator_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_040-Elevator_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_040-Elevator_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_040-Elevator.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_041-President_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_041-President_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_041-President_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_041-President.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_042-ElevatorReturn_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_042-ElevatorReturn_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_042-ElevatorReturn_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_042-ElevatorReturn_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_042-ElevatorReturn.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_050-Stair_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_050-Stair_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_050-Stair_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_050-Stair_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_050-Stair.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_051-StairReturn_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_051-StairReturn_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_051-StairReturn_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_051-StairReturn_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_051-StairReturn.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_060-Passage_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_060-Passage_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_060-Passage_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_060-Passage_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_060-Passage.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_061-PassageReturn_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_061-PassageReturn_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_061-PassageReturn_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_061-PassageReturn_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_061-PassageReturn.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-Deep_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-Deep_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-Deep_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-Deep_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-Deep.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle1_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle1_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle1_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle1.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle2_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle2_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle2_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle2.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle3_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle3_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle3_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle3.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle4_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle4_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle4_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepBattle4.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepElude1_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepElude1_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepElude1.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepElude2_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepElude2_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepElude2.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepElude3_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepElude3_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepElude3_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_070-DeepElude3.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_071-DeepReturn_Animation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_071-DeepReturn_Collision.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_071-DeepReturn_Lighting.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_071-DeepReturn_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_071-DeepReturn.umap

        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_CollisionAttribute.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_Environment_Navigation.umap
        End/Content/GameContents/Level/Game/Field/010-MAKO1/Layout_Merge/010-MAKO1_Layout_Environment.umap
        */

        //extraction options
        private static bool extractLights = true;
        private static bool extractMeshes = true;
        private static bool extractTextures = true;
        private static bool overwriteExtractedTextures = false;
        private static bool convertTexture = true;
        private static bool convertTextureToDifferentExportTypeIfFailed = true;
        //private static string textureExportType = "dds";
        //private static string textureExportType = "tga";
        private static string textureExportType = "png";
        //private static string textureExportType = "jpg";
        //private static string textureExportType = "bmp";

        //texture/material processing
        private static bool combineAlbedoAlpha = true;
        private static bool seperatePBRMaps = false;
        private static bool convertPBRMapToUnityHDRP = true;
        private static bool convertPBRMapToUnityURP = false;
        private static bool convertPBRMapToUnityBIRP = false;

        private static int userAppModeIndex = -1;
        private static bool mode_listUmaps;

        //level options
        //public float levelScale = 0.01f;
        //public float lightIntensityScale = 0.0001f;

        //|||||||||||||||||||||||||||||||||||| MAIN ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| MAIN ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| MAIN ||||||||||||||||||||||||||||||||||||

        public static string[] GetUMAPsInGame(CollectedData collectedData)
        {
            List<string> umapFilesInGame = new List<string>();

            foreach(KeyValuePair<string, GameFile> keyValuePair in collectedData.dataProvider.Files)
            {
                if(keyValuePair.Key.Contains(".umap"))
                    umapFilesInGame.Add(keyValuePair.Key);
            }

            return umapFilesInGame.ToArray();
        }

        public static bool IsGamePakPathValid(string path)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
            {
                ConsoleWriter.WriteErrorLine("pasted path string is null/empty/whitespace, please paste in the path correctly.");
                return false;
            }

            if (Directory.Exists(path) == false)
            {
                ConsoleWriter.WriteErrorLine("Game Pak Path does not exist!");
                return false;
            }

            string[] filesInDirectory = Directory.GetFiles(path);

            for (int i = 0; i < filesInDirectory.Length; i++)
            {
                if (Path.GetExtension(filesInDirectory[i]) == ".pak")
                    return true;
            }

            ConsoleWriter.WriteErrorLine("Game Pak Path does have pak files!");
            return false;
        }

        public static bool IsOutputPathValid(string path)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
            {
                ConsoleWriter.WriteErrorLine("pasted path string is null/empty/whitespace, please paste in the path correctly.");
                return false;
            }

            if (Directory.Exists(path) == false)
            {
                ConsoleWriter.WriteErrorLine("Output Path does not exist!");
                return false;
            }

            return true;
        }

        static void Main(string[] args)
        {
            Console.Clear();

            unrealEngineMappingFilePath = string.Format("{0}/ExternalDependencies/Mappings.usmap", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 1 MODE SELECTION ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 1 MODE SELECTION ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 1 MODE SELECTION ||||||||||||||||||||||||||||||||||||

            ConsoleWriter.WriteLine("FF7R UMAP Extractor");

            while(userAppModeIndex != 0 || userAppModeIndex != 1)
            {
                ConsoleWriter.WriteLine("(1/15) Type in 0 for Map Extraction Mode, or Type in 1 for Listing all .umap file paths.");

                if(int.TryParse(Console.ReadLine(), out int parsedValue))
                {
                    if(parsedValue == 0 || parsedValue == 1)
                    {
                        userAppModeIndex = parsedValue;

                        if (userAppModeIndex == 0)
                        {
                            mode_listUmaps = false;
                            ConsoleWriter.WriteInfoLine("Map Extraction Mode Selected!");
                        }
                        else
                        {
                            mode_listUmaps = true;
                            ConsoleWriter.WriteInfoLine("Game UMAP Listing Mode Selected!");
                        }

                        break;
                    }
                    else
                    {
                        ConsoleWriter.WriteErrorLine("WRITE EITHER 0 OR 1 TO SELECT THE CORESPONDING MODE!");
                    }
                }
                else
                {
                    ConsoleWriter.WriteErrorLine("WRITE EITHER 0 OR 1 TO SELECT THE CORESPONDING MODE!");
                }
            }

            //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 2 GAME PAK FILE PATH ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 2 GAME PAK FILE PATH ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 2 GAME PAK FILE PATH ||||||||||||||||||||||||||||||||||||

            while (Directory.Exists(gamePaksPath) == false)
            {
                ConsoleWriter.WriteLine("(2/15) Enter/paste the path to the game pak files...");
                string userGamePaksPath = Console.ReadLine();

                if (IsGamePakPathValid(userGamePaksPath))
                {
                    gamePaksPath = userGamePaksPath;
                    ConsoleWriter.WriteInfoLine(string.Format("Pak Path Valid! {0}", gamePaksPath));
                    break;
                }
            }

            //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 3 OUTPUT FILE PATH ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 3 OUTPUT FILE PATH ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 3 OUTPUT FILE PATH ||||||||||||||||||||||||||||||||||||

            while (Directory.Exists(outputPath) == false)
            {
                ConsoleWriter.WriteLine("(3/15) Enter/paste the path to the output folder/directory where files will be exported to...");
                string userOutputPath = Console.ReadLine();

                if (IsOutputPathValid(userOutputPath))
                {
                    outputPath = userOutputPath;
                    ConsoleWriter.WriteInfoLine(string.Format("Output Path Valid! {0}", outputPath));
                    break;
                }
            }

            if(!mode_listUmaps)
            {

                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 4 GAME UMAP PACKAGE PATH ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 4 GAME UMAP PACKAGE PATH ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 4 GAME UMAP PACKAGE PATH ||||||||||||||||||||||||||||||||||||

                while (string.IsNullOrEmpty(gameUmapPackagePath) || string.IsNullOrWhiteSpace(gameUmapPackagePath))
                {
                    ConsoleWriter.WriteLine("(4/15) Enter/paste the path of the umap level to extract...");
                    string userGameUmapPackagePath = Console.ReadLine();

                    if (string.IsNullOrEmpty(userGameUmapPackagePath) == false || string.IsNullOrWhiteSpace(userGameUmapPackagePath) == false)
                    {
                        gameUmapPackagePath = userGameUmapPackagePath;
                        ConsoleWriter.WriteInfoLine(string.Format("Game Umap Package Path Set! {0}", gameUmapPackagePath));
                        break;
                    }
                    else
                    {
                        ConsoleWriter.WriteErrorLine("The path you entered is empty/null! {0}");
                    }
                }

                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 5 EXTRACT LIGHTS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 5 EXTRACT LIGHTS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 5 EXTRACT LIGHTS ||||||||||||||||||||||||||||||||||||

                ConsoleWriter.WriteLine("(5/15) Extract Lights?");
                ConsoleWriter.WriteLine("If YES hit enter now (leave the field empty)");
                ConsoleWriter.WriteLine("If NO then type in 0");
                string userExtractLights = Console.ReadLine();

                if (string.IsNullOrEmpty(userExtractLights) || string.IsNullOrWhiteSpace(userExtractLights))
                    extractLights = true;
                else
                    extractLights = false;

                ConsoleWriter.WriteInfoLine(string.Format("Extract Lights: {0}", extractLights));

                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 6 EXTRACT MESHES ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 6 EXTRACT MESHES ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 6 EXTRACT MESHES ||||||||||||||||||||||||||||||||||||

                ConsoleWriter.WriteLine("(6/15) Extract Meshes?");
                ConsoleWriter.WriteLine("If YES hit enter now (leave the field empty)");
                ConsoleWriter.WriteLine("If NO then type in 0");
                string userExtractMeshes = Console.ReadLine();

                if (string.IsNullOrEmpty(userExtractMeshes) || string.IsNullOrWhiteSpace(userExtractMeshes))
                    extractMeshes = true;
                else
                    extractMeshes = false;

                ConsoleWriter.WriteInfoLine(string.Format("Extract Meshes: {0}", extractMeshes));

                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 7 EXTRACT TEXTURES ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 7 EXTRACT TEXTURES ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 7 EXTRACT TEXTURES ||||||||||||||||||||||||||||||||||||

                ConsoleWriter.WriteLine("(7/15) Extract Textures?");
                ConsoleWriter.WriteLine("If YES hit enter now (leave the field empty)");
                ConsoleWriter.WriteLine("If NO then type in 0");
                string userExtractTextures = Console.ReadLine();

                if (string.IsNullOrEmpty(userExtractTextures) || string.IsNullOrWhiteSpace(userExtractTextures))
                    extractTextures = true;
                else
                    extractTextures = false;

                ConsoleWriter.WriteInfoLine(string.Format("Extract Textures: {0}", extractTextures));

                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 8 OVERWRITE EXTRACT TEXTURES ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 8 OVERWRITE EXTRACT TEXTURES ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 8 OVERWRITE EXTRACT TEXTURES ||||||||||||||||||||||||||||||||||||

                ConsoleWriter.WriteLine("(8/15) Overwite Existing Textures during Extraction? (If there is the same texture that has been extracted prior, should we overwrite it?)");
                ConsoleWriter.WriteLine("If YES hit enter now (leave the field empty)");
                ConsoleWriter.WriteLine("If NO then type in 0");
                string userOverwriteExtractTextures = Console.ReadLine();

                if (string.IsNullOrEmpty(userOverwriteExtractTextures) || string.IsNullOrWhiteSpace(userOverwriteExtractTextures))
                    overwriteExtractedTextures = true;
                else
                    overwriteExtractedTextures = false;

                ConsoleWriter.WriteInfoLine(string.Format("Overwrite Extract Textures: {0}", overwriteExtractedTextures));

                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 9 CONVERT TEXTURES ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 9 CONVERT TEXTURES ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 9 CONVERT TEXTURES ||||||||||||||||||||||||||||||||||||

                ConsoleWriter.WriteLine("(9/15) Convert Texture After Extraction?");
                ConsoleWriter.WriteLine("If YES hit enter now (leave the field empty)");
                ConsoleWriter.WriteLine("If NO then type in 0");
                string userConvertTextures = Console.ReadLine();

                if (string.IsNullOrEmpty(userConvertTextures) || string.IsNullOrWhiteSpace(userConvertTextures))
                    convertTexture = true;
                else
                    convertTexture = false;

                ConsoleWriter.WriteInfoLine(string.Format("Convert Textures: {0}", convertTexture));

                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 10 TEXTURE EXPORT TYPE ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 10 TEXTURE EXPORT TYPE ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 10 TEXTURE EXPORT TYPE ||||||||||||||||||||||||||||||||||||

                ConsoleWriter.WriteLine("(10/15) Converted Texture Format Type.");
                ConsoleWriter.WriteLine("0 = .dds | 1 = .tga | 2 = .png | 3 = .jpg | 4 = .bmp");
                ConsoleWriter.WriteLine("If you leave this empty it will revert to .png");
                string userConvertedTextureFormatType = Console.ReadLine();

                if (string.IsNullOrEmpty(userConvertedTextureFormatType) || string.IsNullOrWhiteSpace(userConvertedTextureFormatType))
                    textureExportType = "png";
                else
                {
                    if (int.TryParse(userConvertedTextureFormatType, out int parsedValue))
                    {
                        switch (parsedValue)
                        {
                            case 0:
                                textureExportType = "dds";
                                break;
                            case 1:
                                textureExportType = "tga";
                                break;
                            case 2:
                                textureExportType = "png";
                                break;
                            case 3:
                                textureExportType = "jpg";
                                break;
                            case 4:
                                textureExportType = "bmp";
                                break;
                            default:
                                textureExportType = "png";
                                break;
                        }
                    }
                    else
                    {
                        textureExportType = "png";
                    }
                }

                ConsoleWriter.WriteInfoLine(string.Format("Converted Texture Format Type: {0}", textureExportType));

                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 11 COMBINE ALBEDO AND ALPHA ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 11 COMBINE ALBEDO AND ALPHA ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 11 COMBINE ALBEDO AND ALPHA ||||||||||||||||||||||||||||||||||||

                ConsoleWriter.WriteLine("(11/15) Combine Seperate Alpha and Albedo/Color textures together?");
                ConsoleWriter.WriteLine("If YES hit enter now (leave the field empty)");
                ConsoleWriter.WriteLine("If NO then type in 0");
                string userCombineAlbedoAlpha = Console.ReadLine();

                if (string.IsNullOrEmpty(userCombineAlbedoAlpha) || string.IsNullOrWhiteSpace(userCombineAlbedoAlpha))
                    combineAlbedoAlpha = true;
                else
                    combineAlbedoAlpha = false;

                ConsoleWriter.WriteInfoLine(string.Format("Combine Albedo Alpha: {0}", combineAlbedoAlpha));

                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 12 SEPERATE PBR MAPS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 12 SEPERATE PBR MAPS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 12 SEPERATE PBR MAPS ||||||||||||||||||||||||||||||||||||

                ConsoleWriter.WriteLine("(12/15) Seperate the packed _MRV maps into seperate textures?");
                ConsoleWriter.WriteLine("If YES hit enter now (leave the field empty)");
                ConsoleWriter.WriteLine("If NO then type in 0");
                string userSeperatePBRMaps = Console.ReadLine();

                if (string.IsNullOrEmpty(userSeperatePBRMaps) || string.IsNullOrWhiteSpace(userSeperatePBRMaps))
                    seperatePBRMaps = true;
                else
                    seperatePBRMaps = false;

                ConsoleWriter.WriteInfoLine(string.Format("Seperate PBR Maps: {0}", seperatePBRMaps));

                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 13 UNITY HDRP MAPS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 13 UNITY HDRP MAPS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 13 UNITY HDRP MAPS ||||||||||||||||||||||||||||||||||||

                ConsoleWriter.WriteLine("(13/15) Convert _MRV maps into Unity HDRP mask maps?");
                ConsoleWriter.WriteLine("If YES hit enter now (leave the field empty)");
                ConsoleWriter.WriteLine("If NO then type in 0");
                string userConvertHDRP = Console.ReadLine();

                if (string.IsNullOrEmpty(userConvertHDRP) || string.IsNullOrWhiteSpace(userConvertHDRP))
                    convertPBRMapToUnityHDRP = true;
                else
                    convertPBRMapToUnityHDRP = false;

                ConsoleWriter.WriteInfoLine(string.Format("Convert _MRV to Unity HDRP: {0}", convertPBRMapToUnityHDRP));

                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 14 UNITY URP MAPS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 14 UNITY URP MAPS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 14 UNITY URP MAPS ||||||||||||||||||||||||||||||||||||

                ConsoleWriter.WriteLine("(14/15) Convert _MRV maps into Unity URP mask maps?");
                ConsoleWriter.WriteLine("If YES hit enter now (leave the field empty)");
                ConsoleWriter.WriteLine("If NO then type in 0");
                string userConvertURP = Console.ReadLine();

                if (string.IsNullOrEmpty(userConvertURP) || string.IsNullOrWhiteSpace(userConvertURP))
                    convertPBRMapToUnityURP = true;
                else
                    convertPBRMapToUnityURP = false;

                ConsoleWriter.WriteInfoLine(string.Format("Convert _MRV to Unity URP: {0}", convertPBRMapToUnityURP));

                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 15 UNITY BIRP MAPS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 15 UNITY BIRP MAPS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI - 15 UNITY BIRP MAPS ||||||||||||||||||||||||||||||||||||

                ConsoleWriter.WriteLine("(15/15) Convert _MRV maps into Unity BIRP mask maps?");
                ConsoleWriter.WriteLine("If YES hit enter now (leave the field empty)");
                ConsoleWriter.WriteLine("If NO then type in 0");
                string userConvertBIRP = Console.ReadLine();

                if (string.IsNullOrEmpty(userConvertBIRP) || string.IsNullOrWhiteSpace(userConvertBIRP))
                    convertPBRMapToUnityBIRP = true;
                else
                    convertPBRMapToUnityBIRP = false;

                ConsoleWriter.WriteInfoLine(string.Format("Convert _MRV to Unity BIRP: {0}", convertPBRMapToUnityBIRP));
            }
            else
            {
                ConsoleWriter.WriteInfoLine("UMAP listing mode enabled, skipping rest of the options...");
            }

            //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI END ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI END ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| CONSOLE APP UI END ||||||||||||||||||||||||||||||||||||

            ConsoleWriter.WriteInfoLine("STARTING MAP EXTRACTION...");

            //|||||||||||||||||||||||||||||||||||| CUE4 API SETUP ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| CUE4 API SETUP ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| CUE4 API SETUP ||||||||||||||||||||||||||||||||||||

            //using CUE4Parse API, generate a version container to specify what game version/platform we are parsing for
            VersionContainer versionContainer = new VersionContainer(gameVersion);

            //create a list of encryption keys for the game pak archives
            List<PakArchiveEncryptionKey> pakArchiveEncryptionKeys = new List<PakArchiveEncryptionKey>();

            //add a global key for all pak archives since they are the same, no GUID needs to be specified
            pakArchiveEncryptionKeys.Add(new PakArchiveEncryptionKey(new FGuid(), encryptionKey));

            //create our own data provider class, which we feed with our own data
            //NOTE: This will bloat the memory
            DataProvider dataProvider = new DataProvider(gamePaksPath, versionContainer, pakArchiveEncryptionKeys);

            ConsoleWriter.WriteInfoLine("Loading Virtual Paths...");

            //this creates a dictionary internally holding a list of file paths within the pak archives
            dataProvider.LoadVirtualPaths();

            ConsoleWriter.WriteInfoLine("Loading Mappings File...");

            //(IF PROVIDED) load a mappings file to assist with parsing
            FileUsmapTypeMappingsProvider unrealEngineMapping = new FileUsmapTypeMappingsProvider(unrealEngineMappingFilePath);
            unrealEngineMapping.Reload();
            dataProvider.MappingsContainer = unrealEngineMapping;

            ConsoleWriter.WriteSuccessLine(string.Format("Mappings File Loaded... {0}", unrealEngineMappingFilePath));

            CollectedData collectedData = new CollectedData(dataProvider, outputPath);

            //|||||||||||||||||||||||||||||||||||| COLLECT UMAP GAME FILE PATHS ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| COLLECT UMAP GAME FILE PATHS ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| COLLECT UMAP GAME FILE PATHS ||||||||||||||||||||||||||||||||||||

            if(mode_listUmaps)
            {
                string[] gameUmapFiles = GetUMAPsInGame(collectedData);

                ConsoleWriter.WriteInfoLine(string.Format("{0} .umap's in game", gameUmapFiles.Length));

                //write a json file with all of the collected data and references
                using (StreamWriter file = File.CreateText(string.Format("{0}/GameUMAPs.json", outputPath)))
                {
                    //get our json serializer
                    JsonSerializer serializer = new JsonSerializer();

                    //serialize the data and write it to the configuration file
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(file, gameUmapFiles);
                }

                ConsoleWriter.WriteSuccessLine("Wrote JSON file with game umap files to output folder... {0}");

                return;
            }

            //|||||||||||||||||||||||||||||||||||| FINDING GAME FILE ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| FINDING GAME FILE ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| FINDING GAME FILE ||||||||||||||||||||||||||||||||||||

            //given the desired map file, use that to find the actual game file with the CUE4 api
            string startingGameFilePath = FindGameFilePath(collectedData, gameUmapPackagePath);

            //if it fails for whatever reaason, it will return empty...
            if(string.IsNullOrEmpty(startingGameFilePath))
                return; //don't continue with the rest of the function

            //|||||||||||||||||||||||||||||||||||| PROCESS PACKAGE TO COLLECT REFERENCES/DATA ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| PROCESS PACKAGE TO COLLECT REFERENCES/DATA ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| PROCESS PACKAGE TO COLLECT REFERENCES/DATA ||||||||||||||||||||||||||||||||||||

            //NOTE TO SELF: If you get any loading errors here, but you are able to get the paths...
            //Then its super likely that the CUE4 API is unable to parse it because it's missing a dependency.
            //In my case/testing I found that I was missing an Oodle DLL that was required to decompress the archive in order to load the data

            //process the given umap package, and iterate through the data structures within it
            //we go through everything to first collect all of the data we want for level extraction, textures, meshes, etc.
            ProcessPackage(collectedData, startingGameFilePath);

            //after collecting all of the references after processing the umap package, print it to console
            collectedData.PrintInfo();

            //|||||||||||||||||||||||||||||||||||| JSON EXPORT COLLECTED DATA ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| JSON EXPORT COLLECTED DATA ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| JSON EXPORT COLLECTED DATA ||||||||||||||||||||||||||||||||||||

            ConsoleWriter.WriteInfoLine("Writing Json Collected Data...");

            //write a json file with all of the collected data and references
            using (StreamWriter file = File.CreateText(string.Format("{0}/{1}.json", outputPath, gameUmapPackagePath.SubstringAfterLast('/'))))
            {
                //get our json serializer
                JsonSerializer serializer = new JsonSerializer();

                //serialize the data and write it to the configuration file
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, collectedData.exportedLevel);
            }

            //|||||||||||||||||||||||||||||||||||| EXPORT COLLECTED DATA ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| EXPORT COLLECTED DATA ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| EXPORT COLLECTED DATA ||||||||||||||||||||||||||||||||||||

            ConsoleWriter.WriteInfoLine("Exporting/Extracting Collected Data...");



            ConsoleWriter.WriteLine("FINISHED!");
        }

        //|||||||||||||||||||||||||||||||||||| FIND GAME FILE PATH ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| FIND GAME FILE PATH ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| FIND GAME FILE PATH ||||||||||||||||||||||||||||||||||||

        public static string FindGameFilePath(CollectedData collectedData, string inputGameFilePath)
        {
            //the game file to return, default is null if we didn't find anything
            GameFile? gameFile = null;

            //try to find the game file, and if we fail then we will try to reformat the path to something valid and try again...
            if (collectedData.dataProvider.TryFindGameFile(inputGameFilePath, out gameFile) == false)
            {
                ConsoleWriter.WriteWarningLine("Game File not found, reformatting game file path...");

                //NOTE: according to CUE4 API for some reason we need to format the path by taking the current game file path, and appending the name of the object at the end as the "extension".
                string reformattedGameFilePath = inputGameFilePath;
                reformattedGameFilePath = string.Format("{0}.{1}", inputGameFilePath, inputGameFilePath.SubstringAfterLast("/"));

                //we will try to find the game file again, but if we fail again...
                if (collectedData.dataProvider.TryFindGameFile(reformattedGameFilePath, out gameFile) == false)
                    ConsoleWriter.WriteErrorLine(string.Format("Game file not found! {0}", reformattedGameFilePath));
            }
            else
            {
                ConsoleWriter.WriteSuccessLine(string.Format("Game file found! {0}", gameFile.Path));

                //we have a winner! return the native game file path
                return gameFile.Path;
            }

            //if all else fails... we just have to return an empty path
            return string.Empty;
        }

        //|||||||||||||||||||||||||||||||||||| PROCESS PACKAGE ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| PROCESS PACKAGE ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| PROCESS PACKAGE ||||||||||||||||||||||||||||||||||||

        public static void ProcessPackage(CollectedData collectedData, string packagePath)
        {
            //|||||||||||||||||||||||||||||||||||| LOADING PACKAGE ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| LOADING PACKAGE ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| LOADING PACKAGE ||||||||||||||||||||||||||||||||||||

            //the generic UObject holding the unreal world
            UObject? unrealWorldObject = null;

            //world package object that is loaded
            IPackage? unrealWorldPackageInterface = null;

            ConsoleWriter.WriteInfoLine("Loading package...");

            //attempt to load the map file package (load and parse)
            if (collectedData.dataProvider.TryLoadPackage(packagePath, out unrealWorldPackageInterface))
            {
                ConsoleWriter.WriteSuccessLine(string.Format("Package Loaded! {0}", unrealWorldPackageInterface.Name));

                //if the package we loaded is a 'Package' object...
                Package? unrealPackage = unrealWorldPackageInterface as Package;

                //make sure it exists
                if (unrealPackage != null)
                {
                    //iterate through each export map item to get what we need
                    foreach (FObjectExport export in unrealPackage.ExportMap)
                    {
                        //we want to pull
                        if (export.ClassName == "World")
                        {
                            unrealWorldObject = export.ExportObject.Value;
                            ConsoleWriter.WriteInfoLine("Found UObject with 'World' class name...");
                        }
                    }
                }
            }
            else
            {
                ConsoleWriter.WriteErrorLine(string.Format("Failed to load/parse the package! {0}", packagePath));
                return; //don't continue with the rest of the function
            }

            //after (hopefully) loading the unreal level package, make sure we have the captured world object.
            if(unrealWorldObject == null)
            {
                ConsoleWriter.WriteErrorLine("UObject is null! No object with class name 'World' was found.");
                return; //don't continue with the rest of the function
            }

            //|||||||||||||||||||||||||||||||||||| GETTING UWORLD ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| GETTING UWORLD ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| GETTING UWORLD ||||||||||||||||||||||||||||||||||||

            //cast the generic parsed world object as an actual world type
            UWorld? unrealWorld = unrealWorldObject as UWorld;

            //make sure that we have the actual world
            if (unrealWorld == null)
            {
                ConsoleWriter.WriteErrorLine(string.Format("Package is not a world! ExportType: {0}", unrealWorldObject.ExportType));
                return; //don't continue with the rest of the function
            }

            //|||||||||||||||||||||||||||||||||||| LOADING ULEVEL ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| LOADING ULEVEL ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| LOADING ULEVEL ||||||||||||||||||||||||||||||||||||

            ConsoleWriter.WriteInfoLine(string.Format("Loading ULevel... {0}", unrealWorld.GetPathName()));

            //the level object that will be given to us
            ULevel unrealWorldLoadedPersistentLevel = null;

            //try loading the actual level
            if (unrealWorld.PersistentLevel.TryLoad(out unrealWorldLoadedPersistentLevel))
            {
                //collect the loaded unreal level path
                collectedData.loadedUnrealLevels.Add(unrealWorld.GetPathName());

                ConsoleWriter.WriteSuccessLine(string.Format("Loaded ULevel! {0}", unrealWorld.GetPathName()));
            }
            else
            {
                ConsoleWriter.WriteErrorLine(string.Format("Unable to load level! {0}", unrealWorld.GetPathName()));
                return; //don't continue with the rest of the function
            }

            //|||||||||||||||||||||||||||||||||||| ITERATING THROUGH ULEVEL ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| ITERATING THROUGH ULEVEL ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| ITERATING THROUGH ULEVEL ||||||||||||||||||||||||||||||||||||

            ConsoleWriter.WriteLine();
            ConsoleWriter.WriteLine(string.Format("========== ITERATING THROUGH {0} ACTORS... | {1} ==========", unrealWorldLoadedPersistentLevel.Actors.Length, unrealWorldLoadedPersistentLevel.GetPathName()));

            //iterate through all of the actors that are in the level
            for (int i = 0; i < unrealWorldLoadedPersistentLevel.Actors.Length; i++)
            {
                string consoleIndexWrite = string.Format("({0, -4} / {1, -4})", i + 1, unrealWorldLoadedPersistentLevel.Actors.Length);

                //|||||||||||||||||||||||||||||||||||| CURRENT FPACKAGEINDEX (ACTOR) ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CURRENT FPACKAGEINDEX (ACTOR) ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CURRENT FPACKAGEINDEX (ACTOR) ||||||||||||||||||||||||||||||||||||

                //get the current level actor package
                FPackageIndex currentLevelPackageIndex = unrealWorldLoadedPersistentLevel.Actors[i];

                //make sure that the current level actor package is not null
                if (currentLevelPackageIndex == null || currentLevelPackageIndex.IsNull)
                {
                    ConsoleWriter.WriteErrorLine(string.Format("{0} FPackageIndex is null, skipping!", consoleIndexWrite));
                    continue;
                }

                //|||||||||||||||||||||||||||||||||||| CURRENT UOBJECT (ACTOR) ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CURRENT UOBJECT (ACTOR) ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| CURRENT UOBJECT (ACTOR) ||||||||||||||||||||||||||||||||||||

                //holds the loaded/parsed genric object of the current actor
                UObject? currentLevelUObject = null;

                //load the current actor package to get the generic object
                if (currentLevelPackageIndex.TryLoad(out currentLevelUObject) == false)
                {
                    ConsoleWriter.WriteErrorLine(string.Format("{0} Failed to load UObject in level, skipping!", consoleIndexWrite));
                    continue;
                }

                //make sure we have the loaded actor object
                if (currentLevelUObject == null)
                {
                    ConsoleWriter.WriteErrorLine("UObject is null! No object with class name 'World' was found.");
                    return; //don't continue with the rest of the function
                }

                //|||||||||||||||||||||||||||||||||||| PROCESS CURRENT UOBJECT (ACTOR) ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| PROCESS CURRENT UOBJECT (ACTOR) ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| PROCESS CURRENT UOBJECT (ACTOR) ||||||||||||||||||||||||||||||||||||

                ConsoleWriter.WriteInfoLine(string.Format("{0} Processing Actor... {1} | ExportType: {2}", consoleIndexWrite, currentLevelUObject.Name, currentLevelUObject.ExportType));

                //process the actor and it's data structures to collect any references we find
                ProcessActor(collectedData, currentLevelUObject);
            }

            ConsoleWriter.WriteLine(string.Format("========== END | {0} ==========", unrealWorldLoadedPersistentLevel.GetPathName()));
            ConsoleWriter.WriteLine();
        }

        //|||||||||||||||||||||||||||||||||||| PROCESS ACTOR ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| PROCESS ACTOR ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| PROCESS ACTOR ||||||||||||||||||||||||||||||||||||

        public static void ProcessActor(CollectedData collectedData, UObject? actor)
        {
            //make sure the actor we are about to mess with exists
            if (actor == null)
            {
                ConsoleWriter.WriteErrorLine("[ProcessActor] The given actor is null!");
                return; //don't continue with the rest of the function
            }

            //|||||||||||||||||||||||||||||||||||| HIDDEN ACTOR ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| HIDDEN ACTOR ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| HIDDEN ACTOR ||||||||||||||||||||||||||||||||||||

            bool isActorHidden = false;

            //attempt to get the visibility/hidden state of the current actor
            //if it's hidden... lets skip it (we could also just ignore this)
            if (actor.TryGetValue(out isActorHidden, "bHidden", "bHiddenInGame") && isActorHidden)
            {
                ConsoleWriter.WriteWarningLine(string.Format("{0} is hidden, skipping!", actor.Name));
                return; //don't continue with the rest of the function
            }

            //|||||||||||||||||||||||||||||||||||| ACTOR TRANSFORM ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| ACTOR TRANSFORM ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| ACTOR TRANSFORM ||||||||||||||||||||||||||||||||||||

            FTransform? transform = null;
            FPackageIndex[] components;

            //attempt to get actor location values in the scene
            if (actor.TryGetValue(out components, "InstanceComponents", "MergedMeshComponents", "BlueprintCreatedComponents"))
            {
                UObject? currentActorRootComponent = null;

                if (actor.TryGetValue(out currentActorRootComponent, "RootComponent"))
                    transform = TryGetTransform(currentActorRootComponent);
            }

            //|||||||||||||||||||||||||||||||||||| LIGHT ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| LIGHT ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| LIGHT ||||||||||||||||||||||||||||||||||||

            if(extractLights)
                ProcessLight(collectedData, actor, transform);

            //|||||||||||||||||||||||||||||||||||| MODEL ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| MODEL ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| MODEL ||||||||||||||||||||||||||||||||||||

            if(extractMeshes)
                ProcessModel(collectedData, actor, transform);

            //|||||||||||||||||||||||||||||||||||| SOUND ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| SOUND ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| SOUND ||||||||||||||||||||||||||||||||||||
            //NOT IMPLEMENTED 

            ProcessSound(collectedData, actor, transform);

            //|||||||||||||||||||||||||||||||||||| LEVEL STREAMING / WORLD PARTITIONS ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| LEVEL STREAMING / WORLD PARTITIONS ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| LEVEL STREAMING / WORLD PARTITIONS ||||||||||||||||||||||||||||||||||||
            //NOT IMPLEMENTED 

            /*
            UObject? partition = null;

            if (actor.TryGetValue(out partition, "WorldPartition"))
            {
                ConsoleWriter.WriteInfoLine("WorldPartition Found!");

                if(partition != null)
                {
                    UObject? runtimeHash = null;

                    if (partition.TryGetValue(out runtimeHash, "RuntimeHash"))
                    {
                        ConsoleWriter.WriteInfoLine("RuntimeHash Found!");

                        if(runtimeHash != null)
                        {
                            FStructFallback[]? streamingGrids = null;

                            if (runtimeHash.TryGetValue(out streamingGrids, "StreamingGrids"))
                            {
                                ConsoleWriter.WriteInfoLine("StreamingGrids Found!");
                            }
                        }
                    }
                }    
            }
            */

            //|||||||||||||||||||||||||||||||||||| ADDITIONAL WORLDS ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| ADDITIONAL WORLDS ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| ADDITIONAL WORLDS ||||||||||||||||||||||||||||||||||||
            //NOT IMPLEMENTED 

            /*
            List<FSoftObjectPath> additionalSubWorlds = new List<FSoftObjectPath>();

            // /Script/FortniteGame.BuildingFoundation:AdditionalWorlds
            if (actor.TryGetValue(out additionalSubWorlds, "AdditionalWorlds"))
            {
                ConsoleWriter.WriteInfoLine("AdditionalWorlds Found!");
            }
            */
        }

        //|||||||||||||||||||||||||||||||||||| PROCESS MODEL ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| PROCESS MODEL ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| PROCESS MODEL ||||||||||||||||||||||||||||||||||||

        public static void ProcessModel(CollectedData collectedData, UObject? unrealObject, FTransform? transform)
        {
            //make sure the generic unreal object we are about to mess with exists...
            if (unrealObject == null)
            {
                ConsoleWriter.WriteErrorLine("[ProcessModel] The given 'unrealObject' is null!");
                return; //don't continue with the rest of the function
            }

            //|||||||||||||||||||||||||||||||||||| MESH ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| MESH ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| MESH ||||||||||||||||||||||||||||||||||||

            //generic unreal object that will hold (hopefully) a mesh component
            UObject? meshComponentObject = null;

            //try getting a mesh component from the current unreal object
            unrealObject.TryGetValue(out meshComponentObject, "StaticMeshComponent", "SkeletalMeshComponent", "Component", "EndEnvironmentStaticMeshActor", "StaticMeshActor");

            //if we were not able to get a static mesh component, it will be null
            if (meshComponentObject == null)
                return; //don't continue with the rest of the function

            //attempt to get the mesh package
            FPackageIndex meshPackage = meshComponentObject!.GetOrDefault("StaticMesh", meshComponentObject!.GetOrDefault<FPackageIndex>("SkeletalMesh"));

            //if we are not able to get the mesh package, it will be null
            if (meshPackage == null || meshPackage.IsNull)
                return; //don't continue with the rest of the function

            //generic unreal object that holds (hopefully) the loaded mesh package
            UObject? meshUnrealObject = null;
            IMesh? meshUnrealInterface = null;

            //try loading the generic unreal mesh object
            meshPackage.TryLoad(out meshUnrealObject);

            //if we were not able to get the generic unreal mesh object, it will be null
            if (meshUnrealObject == null)
                return; //don't continue with the rest of the function

            //case the generic unreal mesh object as an exported mesh interface
            meshUnrealInterface = meshUnrealObject as IMesh;

            //if we were not able to get the generic unreal mesh object, it will be null
            if (meshUnrealInterface == null)
                return; //don't continue with the rest of the function

            //create a custom extracted mesh actor object, this will hold any collected data/references for this mesh
            ExtractedMeshActor extractedMeshActor = new ExtractedMeshActor();
            extractedMeshActor.Name = unrealObject.Name;
            extractedMeshActor.MeshPackagePath = unrealObject.GetPathName();

            //feed our transform values from the actor root
            if(transform != null)
            {
                //assign root transform values
                extractedMeshActor.Position = transform.Translation;
                extractedMeshActor.Rotation = transform.Rotation;
                extractedMeshActor.Scale = transform.Scale3D;

                //NOTE: even though we are assigning transform values here
                //there is a very good chance also that there is transform values on the mesh itself
                //so in order to properly utilize... we need to do some additional math

                //try getting transform values on the component we get if there wasn't any on the root
                FTransform newTransform = TryGetTransform(meshComponentObject);

                //if we have transform values also in the component
                if (newTransform != null)
                {
                    //NOTE: when getting transform values... the fields referenced are 'RelativeLocation'
                    //meaning that the transform values are relative to something...
                    //in our case we will assume that the transform values from the root are the parent
                    //and the values from the mesh itself are relative to the parent

                    FTransform adjustedTransform = transform.GetRelativeTransform(newTransform);

                    //assign adjusted values
                    extractedMeshActor.Position = adjustedTransform.Translation;
                    extractedMeshActor.Rotation = adjustedTransform.Rotation;
                    extractedMeshActor.Scale = adjustedTransform.Scale3D;
                }
            }
            //if there are no transform values from the actor root... we need to search deeper
            else
            {
                //try searching the mesh component
                FTransform newTransform = TryGetTransform(meshComponentObject);

                if(newTransform != null)
                {
                    extractedMeshActor.Position = newTransform.Translation;
                    extractedMeshActor.Rotation = newTransform.Rotation;
                    extractedMeshActor.Scale = newTransform.Scale3D;
                }
                else
                {
                    ConsoleWriter.WriteWarningLine(string.Format("No transform data found for mesh! {0}", unrealObject.GetPathName()));
                }
            }

            ExporterOptions exporterOptions = new ExporterOptions()
            {
                SocketFormat = ESocketFormat.None,
                LodFormat = ELodFormat.FirstLod,
                //LodFormat = ELodFormat.AllLods,
                //MeshFormat = EMeshFormat.ActorX,
                MeshFormat = EMeshFormat.Gltf2,
                Platform = ETexturePlatform.DesktopMobile,
                TextureFormat = CUE4Parse_Conversion.Textures.ETextureFormat.Png,
                MaterialFormat = EMaterialFormat.AllLayers,
            };

            ConsoleWriter.WriteInfoLine(string.Format("Mesh reference found... {0}", unrealObject.GetPathName()));

            if (collectedData.HasExportedMesh(extractedMeshActor.MeshPackagePath) == false)
            {
                ExportMesh(meshUnrealInterface, exporterOptions, collectedData, out string exportedMeshFilePath);

                ExportedFile exportedMeshFile = new ExportedFile();
                exportedMeshFile.PackagePathReference = extractedMeshActor.MeshPackagePath;
                exportedMeshFile.ExportedFilePath = exportedMeshFilePath;

                collectedData.exportedLevel.ExportedMeshReferences.Add(exportedMeshFile);
            }
            else
            {
                ConsoleWriter.WriteInfoLineAlternate(string.Format("Skipping mesh export for mesh because it's already been exported... {0}", unrealObject.GetPathName()));
            }    

            //|||||||||||||||||||||||||||||||||||| MATERIALS ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| MATERIALS ||||||||||||||||||||||||||||||||||||
            //|||||||||||||||||||||||||||||||||||| MATERIALS ||||||||||||||||||||||||||||||||||||

            //generic unreal object that holds (hopefully) the mesh
            UObject? meshObject = null;

            //attempt to get the generic unreal mesh object value
            meshComponentObject.TryGetValue(out meshObject, "StaticMesh", "SkeletalMesh");

            //if we were not able to get the generic unreal mesh object value, it will be null
            if (meshObject == null)
                return; //don't continue with the rest of the function

            //cast the parsed/loaded generic unreal mesh object value as a static unreal mesh
            UStaticMesh? unrealStaticMesh = meshObject as UStaticMesh;

            //if the cast failed, it will be null
            if (unrealStaticMesh == null)
                return; //don't continue with the rest of the function

            //try getting the materials property within the unreal static mesh
            FPropertyTag? StaticMaterials = GetFPropertyTagByName("StaticMaterials", unrealStaticMesh.Properties);

            //if we were unable to get the static materials property from the unreal static mesh, it will be null
            if (StaticMaterials == null)
                return; //don't continue with the rest of the function

            //static materials is an array property that contains static material objects
            ArrayProperty StaticMaterialsArrayProperty = StaticMaterials.Tag as ArrayProperty;

            ConsoleWriter.WriteInfoLine(string.Format("StaticMaterials Count: {0}", StaticMaterialsArrayProperty.Value.Properties.Count));

            //iterate through the static materials
            for (int i = 0; i < StaticMaterialsArrayProperty.Value.Properties.Count; i++)
            {
                //get the current property tag element
                FPropertyTagType propertyTagType = StaticMaterialsArrayProperty.Value.Properties[i];

                //get a static material object from the current property element
                FStaticMaterial staticMaterial = (FStaticMaterial)propertyTagType.GetValue(typeof(FStaticMaterial));

                if (staticMaterial.MaterialInterface == null)
                {
                    ConsoleWriter.WriteErrorLine("Static Material Interface Null!");
                    continue;
                }

                //generic unreal object that will hold the current static material
                UObject? materialInterfaceUnrealObject = null;

                //try loading the static material object
                staticMaterial.MaterialInterface.TryLoad(out materialInterfaceUnrealObject);

                //if we were unable to get static material, it will be null
                if (materialInterfaceUnrealObject == null)
                {
                    ConsoleWriter.WriteErrorLine(string.Format("Static Material Item ({0} / {1}) failed to load! Skipping...", i + 1, StaticMaterialsArrayProperty.Value.Properties.Count));
                    continue;
                }

                //cast the generic unreal object as an unreal material
                UUnrealMaterial unrealMaterial = materialInterfaceUnrealObject as UUnrealMaterial;

                //create a custom material reference object to collect referenced data
                //we will also fill this material reference with (hopefully) texture references in the material
                MaterialReference newMaterialReference = new MaterialReference();
                newMaterialReference.Name = unrealMaterial.Name;

                //iterate through material properties
                for (int j = 0; j < unrealMaterial.Properties.Count; j++)
                {
                    //we specifically only care about material properties that hold texture references
                    if (unrealMaterial.Properties[j].Name == "TextureParameterValues")
                    {
                        //get the current material property tag element
                        FPropertyTag unrealMaterialProperty = unrealMaterial.Properties[j];

                        //get an array property from the current material property tag element
                        ArrayProperty unrealMaterialTexturesArray = unrealMaterialProperty.Tag as ArrayProperty;

                        //iterate through the material texture property elements
                        for (int x = 0; x < unrealMaterialTexturesArray.Value.Properties.Count; x++)
                        {
                            //get the current material texture property tag element
                            FPropertyTagType unrealMaterialTexturePropertyTagType = unrealMaterialTexturesArray.Value.Properties[x];

                            //get a struct fallback object from the current material texture property tag element
                            FStructFallback unrealMaterialStructFallback = (FStructFallback)unrealMaterialTexturePropertyTagType.GetValue(typeof(FStructFallback));

                            //NOTE TO SELF: I do not like this... would like to make this scalable and less brittle
                            //go through the 3 main parameters of a material texture property
                            FPropertyTag ParameterName = unrealMaterialStructFallback.Properties[0];
                            FPropertyTag ObjectProperty = unrealMaterialStructFallback.Properties[1]; //<--- this is where the texture reference is stored
                            FPropertyTag ExpressionGUID = unrealMaterialStructFallback.Properties[2];
                            NameProperty ParameterNameProperty = ParameterName.Tag as NameProperty;

                            //attempt to get the texture object package
                            FPackageIndex packageIndex = (FPackageIndex)ObjectProperty.Tag.GetValue(typeof(FPackageIndex));

                            //generic unreal object that will hold the texture reference
                            UObject? unrealMaterialPropretyObjectExport = null;

                            //try loading the texture package...
                            packageIndex.TryLoad(out unrealMaterialPropretyObjectExport);

                            //if the loading failed, it will return null and we will just have to skip to the next element
                            if (unrealMaterialPropretyObjectExport == null)
                            {
                                ConsoleWriter.WriteErrorLine("Failed to load texture object in material! Skipping...");
                                continue;
                            }

                            //NOTE: there is a posibility here for multi-dimensional textures (Tex2D, Tex3D, TexCUBE, TexArray)
                            UTexture2D texture2D = unrealMaterialPropretyObjectExport as UTexture2D;
                            //UTexture texture = unrealMaterialPropretyObjectExport as UTexture;

                            //if the generic object fails to be casted as an unreal texture, it will return null and we will just have to skip to the next element
                            if (texture2D == null)
                            {
                                ConsoleWriter.WriteErrorLine("Failed to load texture in material! Skipping...");
                                continue;
                            }

                            //create a custom object that holds the texture reference
                            TextureReference newTextureReference = new TextureReference();
                            newTextureReference.Name = texture2D.Name;
                            newTextureReference.PackagePath = texture2D.GetPathName();
                            newTextureReference.MaterialParameterName = ParameterNameProperty.Value.Text;

                            if(extractTextures)
                            {
                                if (collectedData.HasExportedTexture(newTextureReference.PackagePath) == false)
                                {
                                    ExtractTexture(texture2D, exporterOptions, collectedData, out string exportedTextureFilePath);

                                    ExportedFile exportedTextureFile = new ExportedFile();
                                    exportedTextureFile.PackagePathReference = newTextureReference.PackagePath;
                                    exportedTextureFile.ExportedFilePath = exportedTextureFilePath;

                                    collectedData.exportedLevel.ExportedTextureReferences.Add(exportedTextureFile);
                                }
                                else
                                {
                                    ConsoleWriter.WriteInfoLineAlternate(string.Format("Skipping texture extraction/conversion because it's already been exported... {0}", texture2D.GetPathName()));
                                }
                            }    

                            //append our newly found texture reference to the current material reference we are iterating through
                            newMaterialReference.TextureReferences.Add(newTextureReference);

                            ConsoleWriter.WriteInfoLine(string.Format("Found texture reference... {0}", newTextureReference.PackagePath));
                        }
                    }
                }

                //after extracting all textures from material, do some additional processing...
                if(combineAlbedoAlpha)
                {
                    TextureReference albedo = newMaterialReference.GetTextureReferenceByMaterialParameterName("0:0:Color");
                    TextureReference alpha = newMaterialReference.GetTextureReferenceByMaterialParameterName("0:1:Coverage");

                    if (albedo != null && alpha != null)
                    {
                        ExportedFile exportedAlbedoMap = GetExportedTextureFileByPackagePath(collectedData.exportedLevel, albedo.PackagePath);
                        ExportedFile exportedAlphaMap = GetExportedTextureFileByPackagePath(collectedData.exportedLevel, alpha.PackagePath);

                        if (exportedAlbedoMap != null && exportedAlphaMap != null)
                            ImageProcessing.CombineAlbedoWithAlpha(exportedAlbedoMap.ExportedFilePath, exportedAlphaMap.ExportedFilePath);
                    }
                }

                if(seperatePBRMaps)
                {
                    TextureReference pbrMap = newMaterialReference.GetTextureReferenceByMaterialParameterName("0:2:Metallic/Roughness/Variant");

                    if(pbrMap != null)
                    {
                        ExportedFile exportedPBRMap = GetExportedTextureFileByPackagePath(collectedData.exportedLevel, pbrMap.PackagePath);

                        if(exportedPBRMap != null)
                            ImageProcessing.ExtractPBRMaps(exportedPBRMap.ExportedFilePath);
                    }
                }

                if (convertPBRMapToUnityHDRP)
                {
                    TextureReference pbrMap = newMaterialReference.GetTextureReferenceByMaterialParameterName("0:2:Metallic/Roughness/Variant");
                    TextureReference occlusionMap = newMaterialReference.GetTextureReferenceByMaterialParameterName("1:0:Occlusion");
                    ExportedFile exportedPBRMap = null;
                    ExportedFile exportedOcclusionMap = null;

                    if (pbrMap != null)
                        exportedPBRMap = GetExportedTextureFileByPackagePath(collectedData.exportedLevel, pbrMap.PackagePath);

                    if (occlusionMap != null)
                        exportedOcclusionMap = GetExportedTextureFileByPackagePath(collectedData.exportedLevel, occlusionMap.PackagePath);

                    if (exportedPBRMap != null && exportedOcclusionMap != null)
                        ImageProcessing.RemixPBR_ForUnityHDRP(exportedPBRMap.ExportedFilePath, exportedOcclusionMap.ExportedFilePath);
                    else if(exportedPBRMap != null && exportedOcclusionMap == null)
                        ImageProcessing.RemixPBR_ForUnityHDRP(exportedPBRMap.ExportedFilePath);
                }

                if (convertPBRMapToUnityURP)
                {
                    TextureReference pbrMap = newMaterialReference.GetTextureReferenceByMaterialParameterName("0:2:Metallic/Roughness/Variant");
                    TextureReference occlusionMap = newMaterialReference.GetTextureReferenceByMaterialParameterName("1:0:Occlusion");
                    ExportedFile exportedPBRMap = null;
                    ExportedFile exportedOcclusionMap = null;

                    if (pbrMap != null)
                        exportedPBRMap = GetExportedTextureFileByPackagePath(collectedData.exportedLevel, pbrMap.PackagePath);

                    if (occlusionMap != null)
                        exportedOcclusionMap = GetExportedTextureFileByPackagePath(collectedData.exportedLevel, occlusionMap.PackagePath);

                    if (exportedPBRMap != null && exportedOcclusionMap != null)
                        ImageProcessing.RemixPBR_ForUnityURP(exportedPBRMap.ExportedFilePath, exportedOcclusionMap.ExportedFilePath);
                    else if (exportedPBRMap != null && exportedOcclusionMap == null)
                        ImageProcessing.RemixPBR_ForUnityURP(exportedPBRMap.ExportedFilePath);
                }

                if(convertPBRMapToUnityBIRP)
                {
                    TextureReference pbrMap = newMaterialReference.GetTextureReferenceByMaterialParameterName("0:2:Metallic/Roughness/Variant");
                    ExportedFile exportedPBRMap = null;

                    if (pbrMap != null)
                        exportedPBRMap = GetExportedTextureFileByPackagePath(collectedData.exportedLevel, pbrMap.PackagePath);

                    if (exportedPBRMap != null)
                        ImageProcessing.RemixPBR_ForUnityBIRP(exportedPBRMap.ExportedFilePath);
                }

                //after (hopefully) capturing all texture references within the material, append it to the main mesh actor before moving on to the next material...
                extractedMeshActor.MaterialReferences.Add(newMaterialReference);
            }

            //after finishing iterating the data structure, getting the mesh, material, texture references...
            //lets append it to our data collection!
            collectedData.exportedLevel.MeshActors.Add(extractedMeshActor);
        }

        //|||||||||||||||||||||||||||||||||||| PROCESS LIGHT ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| PROCESS LIGHT ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| PROCESS LIGHT ||||||||||||||||||||||||||||||||||||

        public static void ProcessLight(CollectedData collectedData, UObject? unrealObject, FTransform? transform)
        {
            //make sure the generic unreal object we are about to mess with exists...
            if (unrealObject == null)
            {
                ConsoleWriter.WriteErrorLine("[ProcessLight] The given 'unrealObject' is null!");
                return; //don't continue with the rest of the function
            }

            UObject? unrealLightComponent = null;

            if (unrealObject.TryGetValue(out unrealLightComponent, "LightComponent", "PointLightComponent", "SpotLightComponent"))
            {
                ExtractedLightActor extractedLightActor = new ExtractedLightActor();
                extractedLightActor.Name = unrealObject.Name;

                switch(unrealLightComponent.ExportType)
                {
                    case "SpotLightComponent":
                        extractedLightActor.LightType = ExtractedLightType.Spot;
                        break;
                    case "PointLightComponent":
                        extractedLightActor.LightType = ExtractedLightType.Point;
                        break;
                    default:
                        extractedLightActor.LightType = ExtractedLightType.Unknown;
                        ConsoleWriter.WriteWarningLine(string.Format("Unknown Light Type: {0}", unrealLightComponent.ExportType));
                        break;
                }

                //|||||||||||||||||||||||||||||||||||| SPOT LIGHT CONE ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| SPOT LIGHT CONE ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| SPOT LIGHT CONE ||||||||||||||||||||||||||||||||||||

                FPropertyTag? OuterConeAngle = GetFPropertyTagByName("OuterConeAngle", unrealLightComponent.Properties); //FloatProperty
                FPropertyTag? InnerConeAngle = GetFPropertyTagByName("InnerConeAngle", unrealLightComponent.Properties); //FloatProperty

                if (OuterConeAngle == null)
                    ConsoleWriter.WriteWarningLine("Property 'OuterConeAngle' not found...");
                else
                    extractedLightActor.OuterConeAngle = (float)OuterConeAngle.Tag.GetValue(typeof(float));

                if (InnerConeAngle == null)
                    ConsoleWriter.WriteWarningLine("Property 'InnerConeAngle' not found...");
                else
                    extractedLightActor.InnerConeAngle = (float)InnerConeAngle.Tag.GetValue(typeof(float));

                //|||||||||||||||||||||||||||||||||||| LIGHT INTENSITY ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT INTENSITY ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT INTENSITY ||||||||||||||||||||||||||||||||||||

                FPropertyTag? IntensityUnits = GetFPropertyTagByName("IntensityUnits", unrealLightComponent.Properties); //EnumProperty
                FPropertyTag? Intensity = GetFPropertyTagByName("Intensity", unrealLightComponent.Properties); //FloatProperty

                if (IntensityUnits == null)
                    ConsoleWriter.WriteWarningLine("Property 'IntensityUnits' not found...");
                else
                    extractedLightActor.IntensityUnits = IntensityUnits.Tag.ToString();

                if (Intensity == null)
                    ConsoleWriter.WriteWarningLine("Property 'Intensity' not found...");
                else
                    extractedLightActor.Intensity = (float)Intensity.Tag.GetValue(typeof(float));

                //|||||||||||||||||||||||||||||||||||| LIGHT COLOR ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT COLOR ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT COLOR ||||||||||||||||||||||||||||||||||||

                FPropertyTag? LightColor = GetFPropertyTagByName("LightColor", unrealLightComponent.Properties); //LightColor

                if (LightColor == null)
                    ConsoleWriter.WriteWarningLine("Property 'LightColor' not found...");
                else
                    extractedLightActor.LightColor = (FColor)LightColor.Tag.GetValue(typeof(FColor));

                //|||||||||||||||||||||||||||||||||||| LIGHT TEMPERATURE ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT TEMPERATURE ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT TEMPERATURE ||||||||||||||||||||||||||||||||||||

                FPropertyTag? Temperature = GetFPropertyTagByName("Temperature", unrealLightComponent.Properties); //FloatProperty
                FPropertyTag? ColorTemperatureWhitePoint = GetFPropertyTagByName("ColorTemperatureWhitePoint", unrealLightComponent.Properties); //EnumProperty
                FPropertyTag? bUseTemperature = GetFPropertyTagByName("bUseTemperature", unrealLightComponent.Properties); //BoolProperty

                if (Temperature == null)
                    ConsoleWriter.WriteWarningLine("Property 'Temperature' not found...");
                else
                    extractedLightActor.Temperature = (float)Temperature.Tag.GetValue(typeof(float));

                if (ColorTemperatureWhitePoint == null)
                    ConsoleWriter.WriteWarningLine("Property 'ColorTemperatureWhitePoint' not found...");
                else
                    extractedLightActor.ColorTemperatureWhitePoint = ColorTemperatureWhitePoint.Tag.ToString();

                if (bUseTemperature == null)
                    ConsoleWriter.WriteWarningLine("Property 'bUseTemperature' not found...");
                else
                    extractedLightActor.bUseTemperature = (bool)bUseTemperature.Tag.GetValue(typeof(bool));

                //|||||||||||||||||||||||||||||||||||| LIGHT RADIUS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT RADIUS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT RADIUS ||||||||||||||||||||||||||||||||||||

                FPropertyTag? AttenuationRadius = GetFPropertyTagByName("AttenuationRadius", unrealLightComponent.Properties); //FloatProperty
                FPropertyTag? SourceRadius = GetFPropertyTagByName("SourceRadius", unrealLightComponent.Properties); //FloatProperty

                if (AttenuationRadius == null)
                    ConsoleWriter.WriteWarningLine("Property 'AttenuationRadius' not found...");
                else
                    extractedLightActor.AttenuationRadius = (float)AttenuationRadius.Tag.GetValue(typeof(float));

                if (SourceRadius == null)
                    ConsoleWriter.WriteWarningLine("Property 'SourceRadius' not found...");
                else
                    extractedLightActor.SourceRadius = (float)SourceRadius.Tag.GetValue(typeof(float));

                //|||||||||||||||||||||||||||||||||||| LIGHT SHADOWS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT SHADOWS ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT SHADOWS ||||||||||||||||||||||||||||||||||||

                FPropertyTag? CastShadows = GetFPropertyTagByName("CastShadows", unrealLightComponent.Properties); //BoolProperty
                FPropertyTag? ShadowBias = GetFPropertyTagByName("ShadowBias", unrealLightComponent.Properties); //FloatProperty

                if (CastShadows == null)
                    ConsoleWriter.WriteWarningLine("Property 'CastShadows' not found...");
                else
                    extractedLightActor.CastShadows = (bool)CastShadows.Tag.GetValue(typeof(bool));

                //|||||||||||||||||||||||||||||||||||| LIGHT MISC ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT MISC ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT MISC ||||||||||||||||||||||||||||||||||||

                FPropertyTag? LightGuid = GetFPropertyTagByName("LightGuid", unrealLightComponent.Properties); //FGuid, StructProperty
                FPropertyTag? VolumetricScatteringIntensity = GetFPropertyTagByName("VolumetricScatteringIntensity", unrealLightComponent.Properties); //FloatProperty
                FPropertyTag? VisibilityId = GetFPropertyTagByName("VisibilityId", unrealLightComponent.Properties); //IntProperty

                //|||||||||||||||||||||||||||||||||||| LIGHT TRANSFORM ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT TRANSFORM ||||||||||||||||||||||||||||||||||||
                //|||||||||||||||||||||||||||||||||||| LIGHT TRANSFORM ||||||||||||||||||||||||||||||||||||

                if (transform != null)
                {
                    extractedLightActor.Position = transform.Translation;
                    extractedLightActor.Rotation = transform.Rotation;

                    FTransform? subTransformValues = TryGetTransform(unrealLightComponent);

                    if (subTransformValues != null)
                    {
                        FTransform adjustedTransform = transform.GetRelativeTransform(subTransformValues);
                        extractedLightActor.Position = adjustedTransform.Translation;
                        extractedLightActor.Rotation = adjustedTransform.Rotation;
                    }
                }
                else
                {
                    FTransform? newTransformValues = TryGetTransform(unrealLightComponent);

                    if(newTransformValues != null)
                    {
                        extractedLightActor.Position = newTransformValues.Translation;
                        extractedLightActor.Rotation = newTransformValues.Rotation;
                    }
                }

                collectedData.exportedLevel.LightActors.Add(extractedLightActor);
            }
        }

        public static void ProcessSound(CollectedData collectedData, UObject? unrealObject, FTransform? transform)
        {
            //make sure the generic unreal object we are about to mess with exists...
            if (unrealObject == null)
            {
                ConsoleWriter.WriteErrorLine("[ProcessLight] The given 'unrealObject' is null!");
                return; //don't continue with the rest of the function
            }

            //if (unrealObject.ExportType == "SQEXSEADLayoutSound")
            //{
                //ConsoleWriter.WriteLine();
            //}

            //SQEXSEADLayoutSound
        }

        //|||||||||||||||||||||||||||||||||||| TRY GET TRANSFORM ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| TRY GET TRANSFORM ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| TRY GET TRANSFORM ||||||||||||||||||||||||||||||||||||
        //this gets some common values, that being RelativeLocation, RelativeRotation, RelativeScale3D to construct a new FTransform object.

        public static FTransform? TryGetTransform(UObject? unrealObject)
        {
            if (unrealObject == null)
            {
                ConsoleWriter.WriteErrorLine("[TryGetTransform] The given 'unrealObject' is null!");
                return null; //don't continue with the rest of the function
            }

            FVector relativeLocation = FVector.ZeroVector;
            FRotator relativeRotation = FRotator.ZeroRotator;
            FVector relativeScale = FVector.OneVector;
            FTransform? transform = new FTransform();

            if (unrealObject.TryGetValue(out relativeLocation, "RelativeLocation") == false)
                ConsoleWriter.WriteWarningLine(string.Format("[WARNING] Failed to get 'RelativeLocation' from {0}, defaulting to FVector.ZeroVector...", unrealObject.Name));

            //NOTE: This has an else block because for whatever reason in the current CUE4 API version being used here, an exception occurs when trying to create a quaternion rotation...
            if (unrealObject.TryGetValue(out relativeRotation, "RelativeRotation") == false)
                ConsoleWriter.WriteWarningLine(string.Format("[WARNING] Failed to get 'RelativeRotation' from {0}, defaulting to FRotator.ZeroRotator...", unrealObject.Name));
            else
                transform.Rotation = relativeRotation.Quaternion();

            if (unrealObject.TryGetValue(out relativeScale, "RelativeScale3D") == false)
                ConsoleWriter.WriteWarningLine(string.Format("[WARNING] Failed to get 'RelativeScale3D' from {0}, defaulting to FVector.OneVector...", unrealObject.Name));

            transform.Translation = relativeLocation;
            transform.Scale3D = relativeScale;

            return transform;
        }

        public static FPropertyTag? GetFPropertyTagByName(string name, List<FPropertyTag> propertyTags)
        {
            for (int i = 0; i < propertyTags.Count; i++)
            {
                if(propertyTags[i].Name.Text == name)
                    return propertyTags[i];
            }

            return null;
        }

        //|||||||||||||||||||||||||||||||||||| EXPORT MESH ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| EXPORT MESH ||||||||||||||||||||||||||||||||||||
        //|||||||||||||||||||||||||||||||||||| EXPORT MESH ||||||||||||||||||||||||||||||||||||

        public static void ExportMesh(IMesh? unrealMeshExportInterface, ExporterOptions exporterOptions, CollectedData collectedData, out string savedFilePath)
        {
            savedFilePath = string.Empty;
            MeshExporter exporter;

            if (unrealMeshExportInterface is UStaticMesh staticMesh)
            {
                if (File.Exists(string.Format("{0}/{1}/{2}_LOD0.glb", collectedData.outputDirectory, staticMesh.GetPathName().SubstringBeforeLast('/'), staticMesh.Name)))
                    return;

                exporter = new MeshExporter(staticMesh, exporterOptions, true);
                exporter.TryWriteToDir(new DirectoryInfo(collectedData.outputDirectory), out string label, out savedFilePath);

                if(File.Exists(savedFilePath))
                    ConsoleWriter.WriteSuccessLine(string.Format("Exported mesh! {0}", savedFilePath));
                else
                    ConsoleWriter.WriteErrorLine(string.Format("Mesh export failed!"));

                //NOTE TO SELF: keep here since this is a quick fix for when we during debugging disable file extraction/conversion to speed things up
                //savedFilePath = string.Format("{0}/{1}/{2}_LOD0.glb", collectedData.outputDirectory , staticMesh.GetPathName().SubstringBeforeLast('/'), staticMesh.Name);
            }
            else if (unrealMeshExportInterface is USkeletalMesh skeletalMesh)
            {
                if (File.Exists(string.Format("{0}/{1}/{2}_LOD0.glb", collectedData.outputDirectory, skeletalMesh.GetPathName().SubstringBeforeLast('/'), skeletalMesh.Name)))
                    return;

                exporter = new MeshExporter(skeletalMesh, exporterOptions, true);
                exporter.TryWriteToDir(new DirectoryInfo(collectedData.outputDirectory), out string label, out savedFilePath);

                if (File.Exists(savedFilePath))
                    ConsoleWriter.WriteSuccessLine(string.Format("Exported mesh! {0}", savedFilePath));
                else
                    ConsoleWriter.WriteErrorLine(string.Format("Mesh export failed!"));

                //NOTE TO SELF: keep here since this is a quick fix for when we during debugging disable file extraction/conversion to speed things up
                //savedFilePath = string.Format("{0}/{1}/{2}_LOD0.glb", collectedData.outputDirectory, skeletalMesh.GetPathName().SubstringBeforeLast('/'), skeletalMesh.Name);
            }
        }

        public static void ExtractTexture(UTexture2D unrealTexture2D, ExporterOptions exporterOptions, CollectedData collectedData, out string savedFilePath)
        {
            savedFilePath = string.Empty;

            if (unrealTexture2D == null)
                return;

            //NOTE: Unfortunately with the current mappings file we have (FF7R), and the current CUE4Parse Library...
            //getting a UTexture2D will have no mip or loaded texture data, so we can't decode or convert it.
            //to get around this, we use the library instead just to extract the raw game files, and then later 
            //we run an external process/tool to handle the texture extraction.

            string unrealTextureGameFilePath = string.Format("{0}/{1}", unrealTexture2D.GetPathName().SubstringBeforeLast('/'), unrealTexture2D.Name);
            string unrealTextureUAssetPath = unrealTextureGameFilePath + ".uasset";
            string unrealTextureUExpPath = unrealTextureGameFilePath + ".uexp";
            string unrealTextureUBulkPath = unrealTextureGameFilePath + ".ubulk";

            string unrealTextureUAssetExtractedPath = string.Format("{0}/{1}", collectedData.outputDirectory, unrealTextureUAssetPath);
            string unrealTextureUExpExtractedPath = string.Format("{0}/{1}", collectedData.outputDirectory, unrealTextureUExpPath);
            string unrealTextureUBulkExtractedPath = string.Format("{0}/{1}", collectedData.outputDirectory, unrealTextureUBulkPath);

            //NOTE TO SELF: keep here since this is a quick fix for when we during debugging disable file extraction/conversion to speed things up
            savedFilePath = Path.ChangeExtension(unrealTextureUAssetExtractedPath, "." + textureExportType);

            ///*
            string unrealTextureUAssetExtractedPathBaseDirectory = Path.GetDirectoryName(unrealTextureUAssetExtractedPath);

            if (Directory.Exists(unrealTextureUAssetExtractedPathBaseDirectory) == false)
                Directory.CreateDirectory(unrealTextureUAssetExtractedPathBaseDirectory);

            if (collectedData.dataProvider.TryFindGameFile(unrealTextureUAssetPath, out GameFile unrealTextureUAsset))
            {
                if(File.Exists(unrealTextureUAssetExtractedPath) == false || overwriteExtractedTextures)
                    File.WriteAllBytes(unrealTextureUAssetExtractedPath, unrealTextureUAsset.Read());
            }
            else
                ConsoleWriter.WriteErrorLine(string.Format("Trying to extract raw unreal texture .uasset but it doesn't exist! {0}", unrealTextureUAssetPath));

            if(collectedData.dataProvider.TryFindGameFile(unrealTextureUExpPath, out GameFile unrealTextureUExp))
            {
                if (File.Exists(unrealTextureUExpExtractedPath) == false || overwriteExtractedTextures)
                    File.WriteAllBytes(unrealTextureUExpExtractedPath, unrealTextureUExp.Read());
            }
            else
                ConsoleWriter.WriteErrorLine(string.Format("Trying to extract raw unreal texture .uexp but it doesn't exist! {0}", unrealTextureUExpPath));

            if (collectedData.dataProvider.TryFindGameFile(unrealTextureUBulkPath, out GameFile unrealTextureUBulk))
            {
                if (File.Exists(unrealTextureUBulkExtractedPath) == false || overwriteExtractedTextures)
                    File.WriteAllBytes(unrealTextureUBulkExtractedPath, unrealTextureUBulk.Read());
            }
            else
                ConsoleWriter.WriteErrorLine(string.Format("Trying to extract raw unreal texture .ubulk but it doesn't exist! {0}", unrealTextureUBulkPath));

            if(convertTexture)
            {
                if (File.Exists(unrealTextureUAssetExtractedPath))
                    ConvertTexture(unrealTextureUAssetExtractedPath); //runs external process to extract the texture
                else
                    ConsoleWriter.WriteErrorLine(string.Format("Unable to convert raw extracted texture .uasset because it doesn't exist! {0}", unrealTextureUAssetExtractedPath));
            }
            //*/
        }

        public static void ConvertTexture(string textureUAssetFilePath)
        {
            string expectedConvertedTexturePath = Path.ChangeExtension(textureUAssetFilePath, "." + textureExportType);

            if (overwriteExtractedTextures == false && File.Exists(expectedConvertedTexturePath))
                return;

            string baseOutputDirectory = Path.GetDirectoryName(textureUAssetFilePath);

            ///*
            string pythonExecutablePath = string.Format("{0}/ExternalDependencies/ue4-dds-tools-v0-6-1/python/python.exe", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            string pythonScriptPath = string.Format("{0}/ExternalDependencies/ue4-dds-tools-v0-6-1/src/main.py", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            if (File.Exists(pythonExecutablePath) == false)
                ConsoleWriter.WriteErrorLine(string.Format("Python executable missing for texture conversion! {0}", pythonExecutablePath));

            if (File.Exists(pythonScriptPath) == false)
                ConsoleWriter.WriteErrorLine(string.Format("Python script missing for texture conversion! {0}", pythonScriptPath));

            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "cmd.exe";
            processStartInfo.CreateNoWindow = false;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.WorkingDirectory = baseOutputDirectory;

            processStartInfo.ArgumentList.Add("/c");
            processStartInfo.ArgumentList.Add(pythonExecutablePath);
            processStartInfo.ArgumentList.Add(pythonScriptPath);
            processStartInfo.ArgumentList.Add(textureUAssetFilePath);
            processStartInfo.ArgumentList.Add(string.Format("--save_folder={0}", baseOutputDirectory));
            //processStartInfo.ArgumentList.Add("--skip_non_texture");
            processStartInfo.ArgumentList.Add("--version=ff7r");
            processStartInfo.ArgumentList.Add("--export_as=" + textureExportType);
            processStartInfo.ArgumentList.Add("--mode=export");

            Process process = Process.Start(processStartInfo);

            //NOTE: this stalls main thread
            while(process.HasExited == false)
                Thread.Sleep(1);

            if (File.Exists(expectedConvertedTexturePath))
                ConsoleWriter.WriteSuccessLine(string.Format("Converted texture! {0}", expectedConvertedTexturePath));
            else
            {
                if(convertTextureToDifferentExportTypeIfFailed)
                {
                    string[] textureExportTypes = new string[]
                    {
                        "png",
                        "jpg",
                        "bmp",
                        "tga",
                        "dds",
                    };

                    for(int i = 0; i < textureExportTypes.Length; i++)
                    {
                        if(textureExportType == textureExportTypes[i])
                            continue;

                        processStartInfo.ArgumentList.Clear();
                        processStartInfo.ArgumentList.Add("/c");
                        processStartInfo.ArgumentList.Add(pythonExecutablePath);
                        processStartInfo.ArgumentList.Add(pythonScriptPath);
                        processStartInfo.ArgumentList.Add(textureUAssetFilePath);
                        processStartInfo.ArgumentList.Add(string.Format("--save_folder={0}", baseOutputDirectory));
                        //processStartInfo.ArgumentList.Add("--skip_non_texture");
                        processStartInfo.ArgumentList.Add("--version=ff7r");
                        processStartInfo.ArgumentList.Add("--export_as=" + textureExportTypes[i]);
                        processStartInfo.ArgumentList.Add("--mode=export");

                        expectedConvertedTexturePath = Path.ChangeExtension(textureUAssetFilePath, "." + textureExportTypes[i]);

                        process.Close();
                        process = Process.Start(processStartInfo);

                        //NOTE: this stalls main thread
                        while (process.HasExited == false)
                            Thread.Sleep(1);

                        if (File.Exists(expectedConvertedTexturePath))
                        {
                            ConsoleWriter.WriteSuccessLine(string.Format("Converted texture! {0}", expectedConvertedTexturePath));
                            break;
                        }
                        else
                            ConsoleWriter.WriteErrorLine(string.Format("Texture conversion failed! {0}", expectedConvertedTexturePath));
                    }
                }
                else
                {
                    ConsoleWriter.WriteErrorLine(string.Format("Texture conversion failed! {0}", expectedConvertedTexturePath));
                }
            }
            //*/
        }

        private static string ReformatPathStringWithForwardSlashes(string initalString)
        {
            string reformattedString = initalString.Replace("\\", "/").Replace("//", "/");
            return Regex.Replace(reformattedString, "/{2,}", "/");
        }

        private static ExportedFile GetExportedFileByPackagePath(List<ExportedFile> exportedFiles, string packagePath)
        {
            for(int i = 0; i < exportedFiles.Count; i++)
            {
                if (exportedFiles[i].PackagePathReference == packagePath)
                    return exportedFiles[i];
            }

            return null;
        }

        private static ExportedFile GetExportedTextureFileByPackagePath(ExportedLevel exportedLevel, string packagePath)
        {
            return GetExportedFileByPackagePath(exportedLevel.ExportedTextureReferences, packagePath);
        }

        private static ExportedFile GetExportedMeshFileByPackagePath(ExportedLevel exportedLevel, string packagePath)
        {
            return GetExportedFileByPackagePath(exportedLevel.ExportedMeshReferences, packagePath);
        }
    }
}